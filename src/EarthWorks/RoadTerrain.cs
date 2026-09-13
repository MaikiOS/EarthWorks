using System.Collections.Generic;
using OstrixMods.EarthWorks.Geometry;
using UnityEngine;

namespace OstrixMods.EarthWorks
{
    internal static class RoadTerrain
    {
        public static bool TrySnapToVertex(Vector3 point, out Vector3 snapped)
        {
            snapped = default(Vector3);
            Heightmap heightmap = Heightmap.FindHeightmap(point);
            if (!heightmap)
            {
                return false;
            }

            heightmap.WorldToVertex(point, out int x, out int z);
            if (x < 0 || z < 0 || x > heightmap.m_width || z > heightmap.m_width)
            {
                return false;
            }

            snapped = GetWorldVertex(heightmap, x, z);
            return true;
        }

        public static bool TryGetHeight(Vector3 point, out float height)
        {
            height = 0f;
            return Heightmap.GetHeight(point, out height);
        }

        public static bool TryFitEndpointPlane(
            Vector3 center,
            Vector2 roadAxis,
            bool start,
            out RoadEndpointPlane plane,
            out float terrainGridScale)
        {
            plane = default(RoadEndpointPlane);
            terrainGridScale = 0f;
            if (roadAxis.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            Heightmap centerHeightmap = Heightmap.FindHeightmap(center);
            if (!centerHeightmap || centerHeightmap.m_scale <= 0f)
            {
                return false;
            }
            terrainGridScale = centerHeightmap.m_scale;
            float radius = terrainGridScale * 2f;
            roadAxis.Normalize();
            Vector2 normal = new Vector2(-roadAxis.y, roadAxis.x);
            List<Vector3> samples = new List<Vector3>(9);
            for (int alongSample = 0; alongSample < 3; ++alongSample)
            {
                int alongIndex = start ? alongSample - 2 : alongSample;
                for (int lateralIndex = -1; lateralIndex <= 1; ++lateralIndex)
                {
                    Vector3 samplePoint = center + new Vector3(
                        (roadAxis.x * alongIndex + normal.x * lateralIndex) * radius,
                        0f,
                        (roadAxis.y * alongIndex + normal.y * lateralIndex) * radius);
                    if (!TryGetAveragedSnappedVertex(samplePoint, out Vector3 snapped))
                    {
                        return false;
                    }
                    bool duplicate = false;
                    foreach (Vector3 existing in samples)
                    {
                        float dx = existing.x - snapped.x;
                        float dz = existing.z - snapped.z;
                        if (dx * dx + dz * dz < 0.0001f)
                        {
                            duplicate = true;
                            break;
                        }
                    }
                    if (!duplicate)
                    {
                        samples.Add(snapped);
                    }
                }
            }
            if (samples.Count < 5)
            {
                return false;
            }

            List<RoadPlaneSample> fitSamples = new List<RoadPlaneSample>(samples.Count);
            foreach (Vector3 sample in samples)
            {
                double dx = sample.x - center.x;
                double dz = sample.z - center.z;
                fitSamples.Add(new RoadPlaneSample(
                    dx * roadAxis.x + dz * roadAxis.y,
                    dx * normal.x + dz * normal.y,
                    sample.y));
            }
            return RoadProfileMath.TryFitPlane(fitSamples, out plane, terrainGridScale * 0.75f);
        }

        private static bool TryGetAveragedSnappedVertex(Vector3 point, out Vector3 snapped)
        {
            if (!TrySnapToVertex(point, out snapped))
            {
                return false;
            }
            float height = 0f;
            int copies = 0;
            foreach (Heightmap heightmap in Heightmap.GetAllHeightmaps())
            {
                if (!heightmap)
                {
                    continue;
                }
                heightmap.WorldToVertex(snapped, out int x, out int z);
                if (x < 0 || z < 0 || x > heightmap.m_width || z > heightmap.m_width)
                {
                    continue;
                }
                Vector3 copy = GetWorldVertex(heightmap, x, z);
                if (Mathf.Abs(copy.x - snapped.x) > 0.001f ||
                    Mathf.Abs(copy.z - snapped.z) > 0.001f)
                {
                    continue;
                }
                height += copy.y;
                ++copies;
            }
            if (copies == 0)
            {
                return false;
            }
            snapped.y = height / copies;
            return true;
        }

        public static Vector3 GetWorldVertex(Heightmap heightmap, int x, int z)
        {
            float halfSize = heightmap.m_width * heightmap.m_scale * 0.5f;
            Vector3 origin = heightmap.transform.position;
            return new Vector3(
                origin.x - halfSize + x * heightmap.m_scale,
                origin.y + heightmap.GetHeight(x, z),
                origin.z - halfSize + z * heightmap.m_scale);
        }
    }
}
