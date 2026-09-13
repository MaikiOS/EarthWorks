using System.Collections.Generic;
using UnityEngine;

namespace OstrixMods.EarthWorks
{
    internal static class RoadProjectFactory
    {
        public static bool TryCreate(RoadBuildPlan plan, out string reason)
        {
            reason = string.Empty;
            if (plan == null || !plan.IsValid || plan.Record == null)
            {
                reason = plan?.InvalidReason ?? EarthWorksLocalization.Text("board_plan_missing");
                return false;
            }
            if (!TryFindBoardPosition(plan.Record, out Vector3 position, out Quaternion rotation))
            {
                reason = EarthWorksLocalization.Text("board_safe_place_missing");
                return false;
            }

            GameObject prefab = Jotunn.Managers.PrefabManager.Instance.GetPrefab(
                EarthWorksPlugin.BoardPrefabName);
            if (!prefab)
            {
                reason = EarthWorksLocalization.Text("board_prefab_missing");
                return false;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
            RaiseAboveGround(instance, position.y);
            RoadProjectBoard board = instance ? instance.GetComponent<RoadProjectBoard>() : null;
            if (!board || !board.Initialize(plan.Record))
            {
                if (instance)
                {
                    UnityEngine.Object.Destroy(instance);
                }
                reason = EarthWorksLocalization.Text("board_creation_failed");
                return false;
            }

            return true;
        }

        private static void RaiseAboveGround(GameObject instance, float groundY)
        {
            if (!instance)
            {
                return;
            }
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                instance.transform.position += Vector3.up * 0.8f;
                return;
            }
            float minimumY = float.MaxValue;
            foreach (Renderer renderer in renderers)
            {
                minimumY = Mathf.Min(minimumY, renderer.bounds.min.y);
            }
            instance.transform.position += Vector3.up * (groundY + 0.05f - minimumY);
        }

        private static bool TryFindBoardPosition(
            RoadProjectRecord record,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = default(Vector3);
            rotation = Quaternion.identity;
            Vector3 start = record.CenterLine[0];
            Vector3 direction = record.CenterLine[1] - start;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return false;
            }
            direction.Normalize();
            Vector3 normal = new Vector3(-direction.z, 0f, direction.x);
            RoadProjectPoint first = record.Points[0];
            float startLeft = first.LeftWidth > 0f ? first.LeftWidth : record.DefaultLeftWidth;
            float startRight = first.RightWidth > 0f ? first.RightWidth : record.DefaultRightWidth;
            float sideDistance = Mathf.Max(startLeft, startRight) +
                EarthWorksPlugin.EffectiveShoulderWidth + 2.2f;
            float[] alongOffsets = { 0f, -2f, 2f, -4f, 4f };
            float[] sides = { 1f, -1f };
            List<Piece> pieces = new List<Piece>();

            foreach (float along in alongOffsets)
            {
                foreach (float side in sides)
                {
                    Vector3 candidate = start + direction * along + normal * sideDistance * side;
                    if (!RoadTerrain.TrySnapToVertex(candidate, out candidate) ||
                        (ZoneSystem.instance && candidate.y < ZoneSystem.instance.m_waterLevel + 0.15f) ||
                        !PrivateArea.CheckAccess(candidate, 0f, false, false) ||
                        Location.IsInsideNoBuildLocation(candidate))
                    {
                        continue;
                    }
                    pieces.Clear();
                    Piece.GetAllPiecesInRadius(candidate, 1.75f, pieces);
                    if (pieces.Count > 0)
                    {
                        continue;
                    }

                    position = candidate;
                    rotation = Quaternion.LookRotation(-normal * side, Vector3.up);
                    return true;
                }
            }
            return false;
        }
    }
}
