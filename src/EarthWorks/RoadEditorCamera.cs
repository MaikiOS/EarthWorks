using System.Reflection;
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
            ? EarthWorksLocalization.Text("camera_pointer", pointerGround.x, pointerGround.y, pointerGround.z)
            : EarthWorksLocalization.Text("camera_pointer_unloaded");
        public string HudText => Active
            ? EarthWorksLocalization.Text("camera_editor_title", EarthWorksLocalization.CameraModeName(mode))
            : EarthWorksLocalization.Text("camera_open");
        public string ControlsText => Active
            ? EarthWorksLocalization.Text("camera_active_help")
            : EarthWorksLocalization.Text("camera_inactive_help");

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
                        EarthWorksLocalization.Text("camera_closed_threat"));
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
                EarthWorksLocalization.Text("camera_closed_damage"));
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

}
