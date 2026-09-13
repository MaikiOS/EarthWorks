using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OstrixMods.EarthWorks
{
    internal static class RoadTerrainApplier
    {
        private static readonly FieldInfo WidthField = AccessTools.Field(typeof(TerrainComp), "m_width");
        private static readonly FieldInfo ModifiedHeightField = AccessTools.Field(typeof(TerrainComp), "m_modifiedHeight");
        private static readonly FieldInfo LevelDeltaField = AccessTools.Field(typeof(TerrainComp), "m_levelDelta");
        private static readonly FieldInfo SmoothDeltaField = AccessTools.Field(typeof(TerrainComp), "m_smoothDelta");
        private static readonly FieldInfo ModifiedPaintField = AccessTools.Field(typeof(TerrainComp), "m_modifiedPaint");
        private static readonly FieldInfo PaintMaskField = AccessTools.Field(typeof(TerrainComp), "m_paintMask");
        private static readonly FieldInfo OperationsField = AccessTools.Field(typeof(TerrainComp), "m_operations");
        private static readonly FieldInfo LastOpPointField = AccessTools.Field(typeof(TerrainComp), "m_lastOpPoint");
        private static readonly FieldInfo LastOpRadiusField = AccessTools.Field(typeof(TerrainComp), "m_lastOpRadius");
        private static readonly FieldInfo NetViewField = AccessTools.Field(typeof(TerrainComp), "m_nview");
        private static readonly MethodInfo SaveMethod =
            AccessTools.Method(typeof(TerrainComp), "Save", new[] { typeof(bool) });

        public static void ApplyHeights(RoadProjectRecord record, Action<bool, string> completed)
        {
            if (!TryPrepare(record, false, out List<CompilerBatch> batches, out string error))
            {
                completed(false, error);
                return;
            }

            try
            {
                ClaimAndSnapshot(batches, false);
                foreach (CompilerBatch batch in batches)
                {
                    ApplyBatchHeights(batch);
                }
                SaveAll(batches, record);
            }
            catch (Exception exception)
            {
                EarthWorksPlugin.Log.LogError(exception);
                bool rolledBack = TryRollback(batches, false);
                completed(false, rolledBack
                    ? EarthWorksLocalization.Text("terrain_apply_rolled_back")
                    : EarthWorksLocalization.Text("terrain_apply_rollback_failed"));
                return;
            }

            ResetClutter(record);
            completed(true, EarthWorksLocalization.Text("terrain_applied"));
        }

        public static void ApplySurface(RoadProjectRecord record, Action<bool, string> completed)
        {
            ApplyPaint(record, false, completed);
        }

        public static void ApplyClearing(RoadProjectRecord record, Action<bool, string> completed)
        {
            ApplyPaint(record, true, completed);
        }

        private static void ApplyPaint(
            RoadProjectRecord record,
            bool clearing,
            Action<bool, string> completed)
        {
            if (!TryPrepare(record, true, out List<CompilerBatch> batches, out string error))
            {
                completed(false, error);
                return;
            }
            if (batches.Count == 0)
            {
                ResetClutter(record);
                completed(true, EarthWorksLocalization.Text("surface_bare_no_paint"));
                return;
            }

            try
            {
                ClaimAndSnapshot(batches, true);
                foreach (CompilerBatch batch in batches)
                {
                    foreach (RuntimeEdit edit in batch.Edits)
                    {
                        Vector3 maskPosition = edit.WorldPosition - new Vector3(0.5f, 0f, 0.5f);
                        batch.Heightmap.WorldToVertexMask(maskPosition, out int x, out int z);
                        if (x < 0 || z < 0 || x > batch.Width || z > batch.Width)
                        {
                            continue;
                        }
                        int index = z * (batch.Width + 1) + x;
                        Color paint = clearing || edit.Surface == RoadSurface.Bare
                            ? Heightmap.m_paintMaskDirt
                            : Heightmap.m_paintMaskPaved;
                        batch.ModifiedPaint[index] = true;
                        batch.PaintMask[index] = paint;
                    }
                }
                SaveAll(batches, record);
            }
            catch (Exception exception)
            {
                EarthWorksPlugin.Log.LogError(exception);
                bool rolledBack = TryRollback(batches, true);
                completed(false, rolledBack
                    ? EarthWorksLocalization.Text("surface_apply_rolled_back")
                    : EarthWorksLocalization.Text("surface_apply_rollback_failed"));
                return;
            }

            ResetClutter(record);
            completed(true, clearing
                ? EarthWorksLocalization.Text("clearing_applied")
                : EarthWorksLocalization.Text("surface_applied"));
        }

        private static bool TryPrepare(
            RoadProjectRecord record,
            bool paintOnly,
            out List<CompilerBatch> result,
            out string error)
        {
            result = null;
            if (record == null || record.Edits.Count == 0)
            {
                error = EarthWorksLocalization.Text("terrain_plan_corrupt");
                return false;
            }
            Dictionary<TerrainComp, CompilerBatch> batches = new Dictionary<TerrainComp, CompilerBatch>();
            HashSet<RuntimeVertexKey> addedVertices = new HashSet<RuntimeVertexKey>();
            try
            {
                foreach (RoadProjectEdit stored in record.Edits)
                {
                    if (paintOnly && !stored.PaintRoadbed)
                    {
                        continue;
                    }
                    if (!PrivateArea.CheckAccess(stored.Position, 0f, false, false) ||
                        Location.IsInsideNoBuildLocation(stored.Position))
                    {
                        error = EarthWorksLocalization.Text("terrain_access_lost");
                        return false;
                    }

                    bool foundCopy = false;
                    // Boundary terrain vertices exist in both neighboring Heightmaps.
                    foreach (Heightmap heightmap in Heightmap.GetAllHeightmaps())
                    {
                        if (!heightmap)
                        {
                            continue;
                        }
                        heightmap.WorldToVertex(stored.Position, out int x, out int z);
                        if (x < 0 || z < 0 || x > heightmap.m_width || z > heightmap.m_width)
                        {
                            continue;
                        }
                        Vector3 vertex = RoadTerrain.GetWorldVertex(heightmap, x, z);
                        if (Mathf.Abs(vertex.x - stored.Position.x) > 0.01f ||
                            Mathf.Abs(vertex.z - stored.Position.z) > 0.01f)
                        {
                            continue;
                        }
                        foundCopy = true;
                        RuntimeVertexKey key = new RuntimeVertexKey(heightmap, x, z);
                        if (!addedVertices.Add(key))
                        {
                            continue;
                        }

                        TerrainComp compiler = heightmap.GetAndCreateTerrainCompiler();
                        if (!compiler)
                        {
                            error = EarthWorksLocalization.Text("terrain_comp_create_failed");
                            return false;
                        }
                        if (!batches.TryGetValue(compiler, out CompilerBatch batch))
                        {
                            batch = CreateBatch(compiler, heightmap);
                            if (batch == null)
                            {
                                error = EarthWorksLocalization.Text("terrain_structure_incompatible");
                                return false;
                            }
                            batches.Add(compiler, batch);
                        }
                        batch.Edits.Add(new RuntimeEdit
                        {
                            GridX = x,
                            GridZ = z,
                            WorldPosition = stored.Position,
                            TargetHeight = stored.TargetHeight,
                            Surface = stored.Surface
                        });
                    }

                    if (!foundCopy)
                    {
                        error = EarthWorksLocalization.Text("terrain_unloaded");
                        return false;
                    }
                }
            }
            catch (Exception exception)
            {
                EarthWorksPlugin.Log.LogError(exception);
                error = EarthWorksLocalization.Text("terrain_prepare_failed");
                return false;
            }

            result = new List<CompilerBatch>(batches.Values);
            error = string.Empty;
            return true;
        }

        private static CompilerBatch CreateBatch(TerrainComp compiler, Heightmap heightmap)
        {
            int width = WidthField == null ? -1 : (int)WidthField.GetValue(compiler);
            bool[] modifiedHeight = ModifiedHeightField?.GetValue(compiler) as bool[];
            float[] levelDelta = LevelDeltaField?.GetValue(compiler) as float[];
            float[] smoothDelta = SmoothDeltaField?.GetValue(compiler) as float[];
            bool[] modifiedPaint = ModifiedPaintField?.GetValue(compiler) as bool[];
            Color[] paintMask = PaintMaskField?.GetValue(compiler) as Color[];
            ZNetView netView = NetViewField?.GetValue(compiler) as ZNetView;
            int expected = (width + 1) * (width + 1);
            if (width != heightmap.m_width || modifiedHeight == null || levelDelta == null ||
                smoothDelta == null || modifiedPaint == null || paintMask == null || netView == null ||
                modifiedHeight.Length != expected || levelDelta.Length != expected ||
                smoothDelta.Length != expected || modifiedPaint.Length != expected ||
                paintMask.Length != expected || SaveMethod == null || OperationsField == null ||
                LastOpPointField == null || LastOpRadiusField == null)
            {
                return null;
            }

            return new CompilerBatch
            {
                Compiler = compiler,
                Heightmap = heightmap,
                Width = width,
                ModifiedHeight = modifiedHeight,
                LevelDelta = levelDelta,
                SmoothDelta = smoothDelta,
                ModifiedPaint = modifiedPaint,
                PaintMask = paintMask,
                NetView = netView
            };
        }

        private static void ClaimAndSnapshot(List<CompilerBatch> batches, bool paint)
        {
            foreach (CompilerBatch batch in batches)
            {
                batch.NetView.ClaimOwnership();
            }
            foreach (CompilerBatch batch in batches)
            {
                if (!batch.NetView.IsValid() || !batch.NetView.IsOwner())
                {
                    throw new InvalidOperationException("Terrain ownership was not granted.");
                }
                batch.OriginalOperations = (int)OperationsField.GetValue(batch.Compiler);
                batch.OriginalLastOpPoint = (Vector3)LastOpPointField.GetValue(batch.Compiler);
                batch.OriginalLastOpRadius = (float)LastOpRadiusField.GetValue(batch.Compiler);
                if (paint)
                {
                    batch.OriginalModifiedPaint = (bool[])batch.ModifiedPaint.Clone();
                    batch.OriginalPaintMask = (Color[])batch.PaintMask.Clone();
                }
                else
                {
                    int rowWidth = batch.Width + 1;
                    HashSet<int> captured = new HashSet<int>();
                    foreach (RuntimeEdit edit in batch.Edits)
                    {
                        int index = edit.GridZ * rowWidth + edit.GridX;
                        if (captured.Add(index))
                        {
                            batch.HeightSnapshots.Add(new HeightSnapshot(
                                index,
                                batch.ModifiedHeight[index],
                                batch.LevelDelta[index],
                                batch.SmoothDelta[index]));
                        }
                    }
                }
                batch.SnapshotCaptured = true;
            }
        }

        private static void ApplyBatchHeights(CompilerBatch batch)
        {
            int rowWidth = batch.Width + 1;
            foreach (RuntimeEdit edit in batch.Edits)
            {
                int index = edit.GridZ * rowWidth + edit.GridX;
                float currentLocal = batch.Heightmap.GetHeight(edit.GridX, edit.GridZ);
                float targetLocal = edit.TargetHeight - batch.Compiler.transform.position.y;
                float requested = batch.LevelDelta[index] + batch.SmoothDelta[index] +
                    targetLocal - currentLocal;
                batch.LevelDelta[index] = Mathf.Clamp(
                    requested,
                    -EarthWorksPlugin.EffectiveTerrainDelta,
                    EarthWorksPlugin.EffectiveTerrainDelta);
                batch.SmoothDelta[index] = 0f;
                batch.ModifiedHeight[index] = true;
            }
        }

        private static void SaveAll(List<CompilerBatch> batches, RoadProjectRecord record)
        {
            foreach (CompilerBatch batch in batches)
            {
                if (!batch.NetView.IsValid() || !batch.NetView.IsOwner())
                {
                    throw new InvalidOperationException("Terrain ownership was lost before save.");
                }
                int operations = (int)OperationsField.GetValue(batch.Compiler);
                OperationsField.SetValue(batch.Compiler, operations + 1);
                LastOpPointField.SetValue(batch.Compiler, record.Center);
                LastOpRadiusField.SetValue(batch.Compiler, record.Radius);
                SaveMethod.Invoke(batch.Compiler, new object[] { false });
                batch.Heightmap.Poke(0, false);
            }
        }

        private static bool TryRollback(List<CompilerBatch> batches, bool paint)
        {
            bool success = true;
            foreach (CompilerBatch batch in batches)
            {
                if (!batch.SnapshotCaptured)
                {
                    continue;
                }
                try
                {
                    if (paint)
                    {
                        Array.Copy(batch.OriginalModifiedPaint, batch.ModifiedPaint, batch.ModifiedPaint.Length);
                        Array.Copy(batch.OriginalPaintMask, batch.PaintMask, batch.PaintMask.Length);
                    }
                    else
                    {
                        foreach (HeightSnapshot snapshot in batch.HeightSnapshots)
                        {
                            batch.ModifiedHeight[snapshot.Index] = snapshot.Modified;
                            batch.LevelDelta[snapshot.Index] = snapshot.Level;
                            batch.SmoothDelta[snapshot.Index] = snapshot.Smooth;
                        }
                    }
                    OperationsField.SetValue(batch.Compiler, batch.OriginalOperations);
                    LastOpPointField.SetValue(batch.Compiler, batch.OriginalLastOpPoint);
                    LastOpRadiusField.SetValue(batch.Compiler, batch.OriginalLastOpRadius);
                    SaveMethod.Invoke(batch.Compiler, new object[] { false });
                    batch.Heightmap.Poke(0, false);
                }
                catch (Exception exception)
                {
                    success = false;
                    EarthWorksPlugin.Log.LogError(exception);
                }
            }
            return success;
        }

        private static void ResetClutter(RoadProjectRecord record)
        {
            if (ClutterSystem.instance)
            {
                ClutterSystem.instance.ResetGrass(record.Center, record.Radius);
            }
        }

        private sealed class CompilerBatch
        {
            public TerrainComp Compiler;
            public Heightmap Heightmap;
            public int Width;
            public bool[] ModifiedHeight;
            public float[] LevelDelta;
            public float[] SmoothDelta;
            public bool[] ModifiedPaint;
            public Color[] PaintMask;
            public ZNetView NetView;
            public readonly List<RuntimeEdit> Edits = new List<RuntimeEdit>();
            public readonly List<HeightSnapshot> HeightSnapshots = new List<HeightSnapshot>();
            public bool[] OriginalModifiedPaint;
            public Color[] OriginalPaintMask;
            public int OriginalOperations;
            public Vector3 OriginalLastOpPoint;
            public float OriginalLastOpRadius;
            public bool SnapshotCaptured;
        }

        private sealed class RuntimeEdit
        {
            public int GridX;
            public int GridZ;
            public Vector3 WorldPosition;
            public float TargetHeight;
            public RoadSurface Surface;
        }

        private readonly struct HeightSnapshot
        {
            public readonly int Index;
            public readonly bool Modified;
            public readonly float Level;
            public readonly float Smooth;

            public HeightSnapshot(int index, bool modified, float level, float smooth)
            {
                Index = index;
                Modified = modified;
                Level = level;
                Smooth = smooth;
            }
        }

        private readonly struct RuntimeVertexKey : IEquatable<RuntimeVertexKey>
        {
            private readonly int heightmapId;
            private readonly int x;
            private readonly int z;

            public RuntimeVertexKey(Heightmap heightmap, int x, int z)
            {
                heightmapId = heightmap.GetInstanceID();
                this.x = x;
                this.z = z;
            }

            public bool Equals(RuntimeVertexKey other)
            {
                return heightmapId == other.heightmapId && x == other.x && z == other.z;
            }

            public override bool Equals(object obj)
            {
                return obj is RuntimeVertexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (heightmapId * 397 ^ x) * 397 ^ z;
                }
            }
        }
    }
}
