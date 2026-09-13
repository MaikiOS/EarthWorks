using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace OstrixMods.EarthWorks
{
    internal enum RoadCameraMode
    {
        Player,
        Plan,
        Isometric
    }

    internal sealed class RoadEditorCamera
    {
        private static readonly FieldInfo MainCameraField =
            AccessTools.Field(typeof(GameCamera), "m_camera");
        private static readonly FieldInfo SkyCameraField =
            AccessTools.Field(typeof(GameCamera), "m_skyCamera");

        private RoadCameraMode mode;
        private Vector3 focus;
        private float orthographicSize = 28f;
        private float isometricYaw = 45f;
        private float isometricPitch = 55f;
        private Camera activeCamera;
        private bool applied;
        private bool pointerDragging;
        private Vector3 previousMouse;
        private Vector3 previousPointerMouse;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private float nextThreatCheck;
        private int originalCullingMask = -1;
        private bool showCharacters = true;
        private bool showPieces = true;
        private bool showWorldObjects = true;
        private bool showGrid = true;
        private bool hasPointerGround;
        private Vector3 pointerGround;
        private RoadEditorGrid editorGrid;

        public bool Active => mode != RoadCameraMode.Player;
        public RoadCameraMode Mode => mode;
        public bool ShowGrid => showGrid;
        public string PointerText => hasPointerGround
            ? $"Точка под курсором: X {pointerGround.x:0.0} · Y {pointerGround.y:0.0} · Z {pointerGround.z:0.0}"
            : "Точка под курсором: вне загруженного рельефа";
        public string HudText => Active
            ? "РЕДАКТОР ПРОЕКТА · " + EarthWorksLocalization.CameraModeName(mode)
            : "F7 · открыть редактор проекта";
        public string ControlsText => Active
            ? "MMB: двигать · Shift+MMB: вращать · WASD/стрелки: двигать · колесо: масштаб · Numpad 7/5: вид"
            : "F7: редактор Plan/Isometric";

        public void Update(RoadDraftSession session, bool toolActive)
        {
            if (!toolActive)
            {
                Deactivate();
                return;
            }
            bool inputAvailable = EarthWorksPlugin.IsGameplayInputAvailable();
            if (inputAvailable && Input.GetKeyDown(EarthWorksPlugin.EditorCameraKey.Value))
            {
                if (Active)
                {
                    Deactivate();
                }
                else
                {
                    Activate(session);
                }
            }
            if (!Active)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Player.m_localPlayer?.SetMoveDir(Vector3.zero);
            if (EarthWorksPlugin.IsDeveloperLab(Player.m_localPlayer) &&
                EarthWorksPlugin.PlayerZdo(Player.m_localPlayer)?.GetInt("ew_test_panel", 0) == 1)
            {
                return;
            }
            if (!inputAvailable)
            {
                return;
            }
            if (Time.unscaledTime >= nextThreatCheck)
            {
                nextThreatCheck = Time.unscaledTime + 0.5f;
                if (RoadProjectBoard.HasThreat(Player.m_localPlayer))
                {
                    Player.m_localPlayer?.Message(
                        MessageHud.MessageType.Center,
                        "Редактор закрыт: рядом враждебное существо.");
                    Deactivate();
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Keypad7))
            {
                SetMode(RoadCameraMode.Plan);
            }
            if (Input.GetKeyDown(KeyCode.Keypad5))
            {
                SetMode(mode == RoadCameraMode.Plan ? RoadCameraMode.Isometric : RoadCameraMode.Plan);
            }
            if (Input.GetKeyDown(KeyCode.Home) &&
                session.TryGetCameraBounds(out Vector3 routeCenter, out float radius))
            {
                focus = routeCenter;
                orthographicSize = Mathf.Clamp(radius * 1.15f, 5f, 140f);
            }
            if (Input.GetKeyDown(KeyCode.KeypadPeriod) &&
                session.TryGetSelectedPosition(out Vector3 selected))
            {
                focus = selected;
                orthographicSize = Mathf.Clamp(orthographicSize, 5f, 22f);
            }

            bool overUi = EarthWorksPlugin.IsPointerOverEditorUi();
            HandleNavigation(overUi);
            session.HandleEditorShortcuts();
            if (Input.GetMouseButtonDown(1))
            {
                session.CancelEditorOperation();
            }
            if (!overUi)
            {
                HandlePointer(session);
            }
            else if (pointerDragging && Input.GetMouseButtonUp(0))
            {
                session.EndEditorPointer();
                pointerDragging = false;
            }
        }

        public void SetMode(RoadCameraMode value)
        {
            if (!Active || value == RoadCameraMode.Player)
            {
                return;
            }
            mode = value;
        }

        public void SetSceneLayers(bool characters, bool pieces, bool worldObjects)
        {
            showCharacters = characters;
            showPieces = pieces;
            showWorldObjects = worldObjects;
            ApplyCullingMask();
        }

        public void SetGridVisible(bool visible)
        {
            showGrid = visible;
        }

        public void Apply(GameCamera gameCamera)
        {
            if (!Active)
            {
                if (applied)
                {
                    RestorePerspective();
                }
                return;
            }
            Camera camera = MainCameraField?.GetValue(gameCamera) as Camera;
            if (!camera)
            {
                return;
            }

            Quaternion rotation = ViewRotation();
            float distance = Mathf.Max(40f, orthographicSize * 2.5f);
            camera.transform.SetPositionAndRotation(
                focus - rotation * Vector3.forward * distance,
                rotation);
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            activeCamera = camera;
            if (originalCullingMask < 0)
            {
                originalCullingMask = camera.cullingMask;
            }
            ApplyCullingMask();

            Camera sky = SkyCameraField?.GetValue(gameCamera) as Camera;
            if (sky)
            {
                sky.transform.rotation = rotation;
                sky.orthographic = true;
                sky.orthographicSize = orthographicSize;
            }
            bool overUi = EarthWorksPlugin.IsPointerOverEditorUi();
            hasPointerGround = !overUi && TryGetGroundPoint(out pointerGround);
            EnsureGrid();
            editorGrid.Render(focus, orthographicSize, showGrid, hasPointerGround, pointerGround);
            ReleaseCursor();
            applied = true;
        }

        public bool TryGetGroundPoint(out Vector3 point)
        {
            point = default(Vector3);
            if (!Active || !activeCamera)
            {
                return false;
            }
            Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                1000f,
                LayerMask.GetMask("terrain"),
                QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            point = RoadTerrain.TrySnapToVertex(hit.point, out Vector3 snapped)
                ? snapped
                : hit.point;
            return true;
        }

        public bool TryWorldToGuiPoint(Vector3 world, out Vector2 point)
        {
            point = default(Vector2);
            if (!Active || !activeCamera)
            {
                return false;
            }
            Vector3 screen = activeCamera.WorldToScreenPoint(world);
            if (screen.z <= 0f)
            {
                return false;
            }
            point = new Vector2(screen.x, Screen.height - screen.y);
            return true;
        }

        public void ExitForDamage()
        {
            if (!Active)
            {
                return;
            }
            Player.m_localPlayer?.Message(
                MessageHud.MessageType.Center,
                "Редактор закрыт: персонаж получил урон.");
            Deactivate();
        }

        public void DeactivateForLaboratoryReset()
        {
            Deactivate();
        }

        public void Dispose()
        {
            Deactivate();
            editorGrid?.Dispose();
            editorGrid = null;
        }

        private void Activate(RoadDraftSession session)
        {
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            mode = RoadCameraMode.Plan;
            if (session != null && session.TryGetCameraBounds(out Vector3 center, out float radius))
            {
                focus = center;
                orthographicSize = Mathf.Clamp(radius * 1.15f, 5f, 140f);
            }
            else if (Player.m_localPlayer)
            {
                focus = Player.m_localPlayer.transform.position;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HandleNavigation(bool overUi)
        {
            KeyboardPan();
            Vector3 mouse = Input.mousePosition;
            if (Input.GetMouseButtonDown(2))
            {
                previousMouse = mouse;
            }
            if (Input.GetMouseButton(2))
            {
                Vector2 delta = mouse - previousMouse;
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                if (control)
                {
                    orthographicSize = Mathf.Clamp(
                        orthographicSize * Mathf.Exp(delta.y * 0.008f),
                        3f,
                        180f);
                }
                else if (shift)
                {
                    if (mode == RoadCameraMode.Plan)
                    {
                        mode = RoadCameraMode.Isometric;
                    }
                    isometricYaw += delta.x * 0.28f;
                    isometricPitch = Mathf.Clamp(isometricPitch - delta.y * 0.2f, 25f, 82f);
                }
                else
                {
                    Pan(delta);
                }
                previousMouse = mouse;
            }
            if (!overUi && Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
            {
                orthographicSize = Mathf.Clamp(
                    orthographicSize * Mathf.Exp(-Input.mouseScrollDelta.y * 0.12f),
                    3f,
                    180f);
            }
        }

        private void HandlePointer(RoadDraftSession session)
        {
            if (Input.GetMouseButtonDown(0) && TryGetGroundPoint(out Vector3 down))
            {
                session.BeginEditorPointer(
                    down,
                    Mathf.Clamp(orthographicSize * 0.05f, 1.4f, 6f),
                    activeCamera,
                    Input.mousePosition);
                pointerDragging = true;
                previousPointerMouse = Input.mousePosition;
            }
            if (pointerDragging && Input.GetMouseButton(0) && TryGetGroundPoint(out Vector3 drag))
            {
                Vector3 mouse = Input.mousePosition;
                session.DragEditorPointer(
                    drag,
                    mouse - previousPointerMouse,
                    orthographicSize * 2f / Mathf.Max(1f, Screen.height));
                previousPointerMouse = mouse;
            }
            if (pointerDragging && Input.GetMouseButtonUp(0))
            {
                session.EndEditorPointer();
                pointerDragging = false;
            }
        }

        private void Pan(Vector2 delta)
        {
            ViewPlaneAxes(out Vector3 right, out Vector3 up);
            float unitsPerPixel = orthographicSize * 2f / Mathf.Max(1f, Screen.height);
            focus -= right * delta.x * unitsPerPixel;
            focus -= up * delta.y * unitsPerPixel;
        }

        private void KeyboardPan()
        {
            float horizontal = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float vertical = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) -
                (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f)
            {
                return;
            }
            ViewPlaneAxes(out Vector3 right, out Vector3 up);
            Vector3 direction = right * horizontal + up * vertical;
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }
            focus += direction * Mathf.Max(8f, orthographicSize) * Time.unscaledDeltaTime;
        }

        private void ViewPlaneAxes(out Vector3 right, out Vector3 up)
        {
            Quaternion rotation = ViewRotation();
            right = rotation * Vector3.right;
            up = rotation * Vector3.up;
            right.y = 0f;
            up.y = 0f;
            right = right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;
            up = up.sqrMagnitude > 0.001f ? up.normalized : Vector3.forward;
        }

        private Quaternion ViewRotation()
        {
            if (mode == RoadCameraMode.Plan)
            {
                return Quaternion.LookRotation(Vector3.down, Vector3.forward);
            }
            return Quaternion.Euler(isometricPitch, isometricYaw, 0f);
        }

        private void Deactivate()
        {
            if (mode == RoadCameraMode.Player && !applied)
            {
                return;
            }
            mode = RoadCameraMode.Player;
            pointerDragging = false;
            hasPointerGround = false;
            editorGrid?.Hide();
            RestorePerspective();
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }

        private void RestorePerspective()
        {
            if (activeCamera)
            {
                if (originalCullingMask >= 0)
                {
                    activeCamera.cullingMask = originalCullingMask;
                }
                activeCamera.orthographic = false;
            }
            if (GameCamera.instance && SkyCameraField?.GetValue(GameCamera.instance) is Camera sky && sky)
            {
                sky.orthographic = false;
            }
            activeCamera = null;
            originalCullingMask = -1;
            applied = false;
        }

        internal static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ApplyCullingMask()
        {
            if (!activeCamera || originalCullingMask < 0)
            {
                return;
            }
            int mask = originalCullingMask;
            SetLayer(ref mask, "character", showCharacters);
            SetLayer(ref mask, "piece", showPieces);
            SetLayer(ref mask, "static_solid", showWorldObjects);
            activeCamera.cullingMask = mask;
        }

        private void EnsureGrid()
        {
            if (editorGrid == null)
            {
                editorGrid = new RoadEditorGrid();
            }
        }

        private static void SetLayer(ref int mask, string layerName, bool visible)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                return;
            }
            int bit = 1 << layer;
            mask = visible ? mask | bit : mask & ~bit;
        }

    }

    internal sealed class RoadEditorGrid
    {
        private const float Spacing = 2f;
        private readonly GameObject root;
        private readonly GameObject gridObject;
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly LineRenderer cursorX;
        private readonly LineRenderer cursorZ;
        private readonly LineRenderer cursorPin;
        private int centerX = int.MinValue;
        private int centerZ = int.MinValue;
        private int halfSteps = -1;

        public RoadEditorGrid()
        {
            root = new GameObject("EarthWorks_EditorGrid") { hideFlags = HideFlags.HideAndDontSave };
            gridObject = new GameObject("EarthWorks_EditorTerrainGrid") { hideFlags = HideFlags.HideAndDontSave };
            gridObject.transform.SetParent(root.transform, false);
            MeshFilter filter = gridObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gridObject.AddComponent<MeshRenderer>();
            mesh = new Mesh { name = "EarthWorks_EditorTerrainGridMesh" };
            mesh.MarkDynamic();
            mesh.indexFormat = IndexFormat.UInt32;
            filter.sharedMesh = mesh;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (!shader)
            {
                throw new System.InvalidOperationException("No compatible editor-grid shader is available.");
            }
            material = new Material(shader)
            {
                name = "EarthWorks_EditorGridMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            cursorX = CreateLine("EarthWorks_EditorCursorX");
            cursorZ = CreateLine("EarthWorks_EditorCursorZ");
            cursorPin = CreateLine("EarthWorks_EditorCursorPin");
            Hide();
        }

        public void Render(Vector3 focus, float viewSize, bool gridVisible, bool hasPointer, Vector3 pointer)
        {
            const int rebuildStep = 10;
            int nextX = Mathf.RoundToInt(focus.x / (Spacing * rebuildStep)) * rebuildStep;
            int nextZ = Mathf.RoundToInt(focus.z / (Spacing * rebuildStep)) * rebuildStep;
            int nextHalfSteps = Mathf.Clamp(Mathf.CeilToInt(viewSize * 1.6f / Spacing), 10, 45);
            if (nextX != centerX || nextZ != centerZ || nextHalfSteps != halfSteps)
            {
                centerX = nextX;
                centerZ = nextZ;
                halfSteps = nextHalfSteps;
                RebuildGrid();
            }
            gridObject.SetActive(gridVisible);
            RenderCursor(hasPointer, pointer, viewSize);
            root.SetActive(true);
        }

        public void Hide()
        {
            if (root)
            {
                root.SetActive(false);
            }
        }

        public void Dispose()
        {
            if (mesh) UnityEngine.Object.Destroy(mesh);
            if (material) UnityEngine.Object.Destroy(material);
            if (root) UnityEngine.Object.Destroy(root);
        }

        private void RebuildGrid()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Color> colors = new List<Color>();
            int minX = centerX - halfSteps;
            int maxX = centerX + halfSteps;
            int minZ = centerZ - halfSteps;
            int maxZ = centerZ + halfSteps;
            int size = halfSteps * 2 + 1;
            Vector3[,] points = new Vector3[size, size];
            bool[,] valid = new bool[size, size];
            for (int ix = 0; ix < size; ++ix)
            {
                for (int iz = 0; iz < size; ++iz)
                {
                    Vector3 point = new Vector3((minX + ix) * Spacing, 0f, (minZ + iz) * Spacing);
                    if (RoadTerrain.TryGetHeight(point, out float height))
                    {
                        point.y = height + 0.12f;
                        points[ix, iz] = point;
                        valid[ix, iz] = true;
                    }
                }
            }
            for (int ix = 0; ix < size; ++ix)
            {
                Color color = (minX + ix) % 5 == 0
                    ? new Color(0.55f, 0.9f, 1f, 0.52f)
                    : new Color(0.45f, 0.7f, 0.78f, 0.2f);
                for (int iz = 0; iz < size - 1; ++iz)
                {
                    AddGridSegment(vertices, colors, points, valid, ix, iz, ix, iz + 1, color);
                }
            }
            for (int iz = 0; iz < size; ++iz)
            {
                Color color = (minZ + iz) % 5 == 0
                    ? new Color(0.55f, 0.9f, 1f, 0.52f)
                    : new Color(0.45f, 0.7f, 0.78f, 0.2f);
                for (int ix = 0; ix < size - 1; ++ix)
                {
                    AddGridSegment(vertices, colors, points, valid, ix, iz, ix + 1, iz, color);
                }
            }
            int[] indices = new int[vertices.Count];
            for (int i = 0; i < indices.Length; ++i) indices[i] = i;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
        }

        private static void AddGridSegment(
            List<Vector3> vertices,
            List<Color> colors,
            Vector3[,] points,
            bool[,] valid,
            int ax,
            int az,
            int bx,
            int bz,
            Color color)
        {
            if (!valid[ax, az] || !valid[bx, bz])
            {
                return;
            }
            vertices.Add(points[ax, az]);
            vertices.Add(points[bx, bz]);
            colors.Add(color);
            colors.Add(color);
        }

        private void RenderCursor(bool visible, Vector3 point, float viewSize)
        {
            cursorX.gameObject.SetActive(visible);
            cursorZ.gameObject.SetActive(visible);
            cursorPin.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }
            float arm = Mathf.Clamp(viewSize * 0.035f, 0.8f, 3.5f);
            float width = Mathf.Clamp(viewSize * 0.009f, 0.1f, 0.75f);
            SetTerrainLine(cursorX, point + Vector3.left * arm, point + Vector3.right * arm, width);
            SetTerrainLine(cursorZ, point + Vector3.back * arm, point + Vector3.forward * arm, width);
            cursorPin.startWidth = cursorPin.endWidth = width;
            cursorPin.SetPosition(0, point + Vector3.up * 0.18f);
            cursorPin.SetPosition(1, point + Vector3.up * Mathf.Clamp(viewSize * 0.08f, 1.5f, 7f));
        }

        private static void SetTerrainLine(LineRenderer line, Vector3 a, Vector3 b, float width)
        {
            if (RoadTerrain.TryGetHeight(a, out float ay)) a.y = ay + 0.18f;
            if (RoadTerrain.TryGetHeight(b, out float by)) b.y = by + 0.18f;
            line.startWidth = line.endWidth = width;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
        }

        private LineRenderer CreateLine(string name)
        {
            GameObject lineObject = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            lineObject.transform.SetParent(root.transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.numCapVertices = 2;
            Color color = new Color(0.15f, 1f, 0.95f, 1f);
            line.startColor = line.endColor = color;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }
    }

    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    internal static class RoadEditorCameraPatch
    {
        private static void Postfix(GameCamera __instance)
        {
            EarthWorksPlugin.Instance?.EditorCamera?.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
    internal static class RoadEditorMouseCapturePatch
    {
        private static bool Prefix()
        {
            if (EarthWorksPlugin.Instance?.EditorCamera?.Active != true)
            {
                return true;
            }
            RoadEditorCamera.ReleaseCursor();
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), "TakeInput")]
    internal static class RoadEditorPlayerInputPatch
    {
        private static void Postfix(Player __instance, ref bool __result)
        {
            if (__instance == Player.m_localPlayer &&
                EarthWorksPlugin.Instance?.EditorCamera?.Active == true)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "OnDamaged")]
    internal static class RoadEditorDamagePatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                EarthWorksPlugin.Instance?.EditorCamera?.ExitForDamage();
            }
        }
    }
}
