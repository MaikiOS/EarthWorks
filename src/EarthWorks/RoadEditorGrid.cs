using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OstrixMods.EarthWorks
{
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

}

