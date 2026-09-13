using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OstrixMods.EarthWorks
{
    internal sealed class RoadPlanPreview
    {
        private readonly GameObject root;
        private readonly Mesh mesh;
        private readonly Material material;

        public RoadPlanPreview()
        {
            root = new GameObject("EarthWorks_RoadPlanPreview")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            MeshFilter filter = root.AddComponent<MeshFilter>();
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            mesh = new Mesh { name = "EarthWorks_RoadPlanGrid" };
            mesh.MarkDynamic();
            mesh.indexFormat = IndexFormat.UInt32;
            filter.sharedMesh = mesh;
            Shader shader = Shader.Find("Sprites/Default") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Standard");
            if (!shader)
            {
                throw new InvalidOperationException("No compatible road-plan preview shader is available.");
            }
            material = new Material(shader)
            {
                name = "EarthWorks_RoadPlanMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Hide();
        }

        public void Render(RoadBuildPlan plan, bool showDifference)
        {
            mesh.Clear();
            if (plan == null || plan.Edits.Count == 0)
            {
                Hide();
                return;
            }

            Dictionary<GridVertexKey, RoadVertexEdit> targets =
                new Dictionary<GridVertexKey, RoadVertexEdit>();
            HashSet<GridCellKey> cells = new HashSet<GridCellKey>();
            foreach (RoadVertexEdit edit in plan.Edits)
            {
                targets[new GridVertexKey(edit.Heightmap, edit.GridX, edit.GridZ)] = edit;
                AddAdjacentCells(cells, edit.Heightmap, edit.GridX, edit.GridZ);
            }

            List<Vector3> vertices = new List<Vector3>(cells.Count * 10);
            List<Color> colors = new List<Color>(cells.Count * 10);
            List<int> indices = new List<int>(cells.Count * 10);
            foreach (GridCellKey cell in cells)
            {
                Vector3 a = FinalVertex(cell.Heightmap, targets, cell.X, cell.Z, out float deltaA);
                Vector3 b = FinalVertex(cell.Heightmap, targets, cell.X + 1, cell.Z, out float deltaB);
                Vector3 c = FinalVertex(cell.Heightmap, targets, cell.X, cell.Z + 1, out float deltaC);
                Vector3 d = FinalVertex(cell.Heightmap, targets, cell.X + 1, cell.Z + 1, out float deltaD);
                float delta = (deltaA + deltaB + deltaC + deltaD) * 0.25f;
                Color color = !plan.IsValid
                    ? new Color(1f, 0.12f, 0.08f, 0.95f)
                    : !showDifference
                        ? new Color(0.28f, 1f, 0.3f, 0.92f)
                    : delta > 0.03f
                        ? new Color(0.2f, 0.65f, 1f, 0.9f)
                        : delta < -0.03f
                            ? new Color(1f, 0.52f, 0.12f, 0.9f)
                            : new Color(0.92f, 0.92f, 0.86f, 0.75f);
                AddLine(vertices, colors, indices, a, b, color);
                AddLine(vertices, colors, indices, a, c, color);
                AddLine(vertices, colors, indices, b, c, color);
                AddLine(vertices, colors, indices, b, d, color);
                AddLine(vertices, colors, indices, c, d, color);
            }

            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
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
            if (mesh)
            {
                UnityEngine.Object.Destroy(mesh);
            }
            if (material)
            {
                UnityEngine.Object.Destroy(material);
            }
            if (root)
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        private static void AddAdjacentCells(
            HashSet<GridCellKey> cells,
            Heightmap heightmap,
            int x,
            int z)
        {
            for (int cellZ = z - 1; cellZ <= z; ++cellZ)
            {
                for (int cellX = x - 1; cellX <= x; ++cellX)
                {
                    if (cellX >= 0 && cellZ >= 0 &&
                        cellX < heightmap.m_width && cellZ < heightmap.m_width)
                    {
                        cells.Add(new GridCellKey(heightmap, cellX, cellZ));
                    }
                }
            }
        }

        private static Vector3 FinalVertex(
            Heightmap heightmap,
            Dictionary<GridVertexKey, RoadVertexEdit> targets,
            int x,
            int z,
            out float delta)
        {
            Vector3 vertex = RoadTerrain.GetWorldVertex(heightmap, x, z);
            if (targets.TryGetValue(new GridVertexKey(heightmap, x, z), out RoadVertexEdit edit))
            {
                delta = edit.TargetHeight - vertex.y;
                vertex.y = edit.TargetHeight;
            }
            else
            {
                delta = 0f;
            }
            vertex.y += 0.07f;
            return vertex;
        }

        private static void AddLine(
            List<Vector3> vertices,
            List<Color> colors,
            List<int> indices,
            Vector3 a,
            Vector3 b,
            Color color)
        {
            int first = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            colors.Add(color);
            colors.Add(color);
            indices.Add(first);
            indices.Add(first + 1);
        }

        private readonly struct GridVertexKey : IEquatable<GridVertexKey>
        {
            private readonly Heightmap heightmap;
            private readonly int x;
            private readonly int z;

            public GridVertexKey(Heightmap heightmap, int x, int z)
            {
                this.heightmap = heightmap;
                this.x = x;
                this.z = z;
            }

            public bool Equals(GridVertexKey other)
            {
                return heightmap == other.heightmap && x == other.x && z == other.z;
            }

            public override bool Equals(object obj)
            {
                return obj is GridVertexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((heightmap ? heightmap.GetInstanceID() : 0) * 397 ^ x) * 397 ^ z;
                }
            }
        }

        private readonly struct GridCellKey : IEquatable<GridCellKey>
        {
            public readonly Heightmap Heightmap;
            public readonly int X;
            public readonly int Z;

            public GridCellKey(Heightmap heightmap, int x, int z)
            {
                Heightmap = heightmap;
                X = x;
                Z = z;
            }

            public bool Equals(GridCellKey other)
            {
                return Heightmap == other.Heightmap && X == other.X && Z == other.Z;
            }

            public override bool Equals(object obj)
            {
                return obj is GridCellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Heightmap ? Heightmap.GetInstanceID() : 0) * 397 ^ X) * 397 ^ Z;
                }
            }
        }
    }
}
