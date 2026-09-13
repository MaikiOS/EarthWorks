using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

    internal sealed class RoadProjectBoard : MonoBehaviour, Hoverable, Interactable
    {
        private const string DataKey = "ew_road_data";
        private const string StageKey = "ew_road_stage";
        private const string AdvanceRpc = "EW_Advance";
        [SerializeField] private float hoverOffset;
        private static readonly RoadProjectStage[] ConstructionStages =
        {
            RoadProjectStage.Setup,
            RoadProjectStage.Marking,
            RoadProjectStage.Clearing,
            RoadProjectStage.Earthworks,
            RoadProjectStage.Surfacing,
            RoadProjectStage.Completion
        };
        private ZNetView netView;
        private RoadProjectRecord record;
        private uint lastDataRevision;
        private LineRenderer routeLine;
        private LineRenderer leftBoundary;
        private LineRenderer rightBoundary;
        private readonly List<GameObject> stakes = new List<GameObject>();
        private Material routeMaterial;
        private bool completionHidden;
        private bool stakePrefabUnavailable;

        private void Awake()
        {
            netView = GetComponent<ZNetView>();
            if (netView)
            {
                netView.Register(AdvanceRpc, RPC_Advance);
            }
        }

        private void Update()
        {
            RefreshRecord();
            if (record == null)
            {
                return;
            }
            RoadProjectStage stage = GetStage();
            if (stage == RoadProjectStage.Completed)
            {
                HideCompletedVisuals();
                return;
            }
            if (!routeLine)
            {
                return;
            }
            Player player = Player.m_localPlayer;
            bool visible = player && Vector3.Distance(player.transform.position, transform.position) < 100f;
            bool showMarking = visible && stage >= RoadProjectStage.Marking &&
                stage < RoadProjectStage.Completed;
            routeLine.enabled = visible;
            if (leftBoundary)
            {
                leftBoundary.enabled = showMarking;
                rightBoundary.enabled = showMarking;
            }
            foreach (GameObject stake in stakes)
            {
                if (stake)
                {
                    stake.SetActive(showMarking);
                }
            }
            if (visible)
            {
                Color color = new Color(1f, 0.72f, 0.18f, 0.9f);
                routeLine.startColor = color;
                routeLine.endColor = color;
            }
        }

        private void HideCompletedVisuals()
        {
            if (completionHidden)
            {
                return;
            }
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
            completionHidden = true;
        }

        private void OnDestroy()
        {
            if (routeMaterial)
            {
                Destroy(routeMaterial);
            }
        }

        public bool Initialize(RoadProjectRecord project)
        {
            if (!netView || !netView.IsValid() || project == null)
            {
                return false;
            }
            netView.m_persistent = true;
            netView.ClaimOwnership();
            if (!netView.IsOwner())
            {
                return false;
            }
            byte[] data = project.Serialize();
            ZDO zdo = netView.GetZDO();
            zdo.Set(DataKey, data);
            zdo.Set(StageKey, (int)RoadProjectStage.Setup);
            record = project;
            lastDataRevision = zdo.DataRevision;
            BuildRouteLine();
            return true;
        }

        public string GetHoverName()
        {
            return EarthWorksLocalization.Text("board_name");
        }

        public float GetHoverOffset()
        {
            return hoverOffset;
        }

        internal void SetHoverOffset(float value)
        {
            hoverOffset = value;
        }

        public string GetHoverText()
        {
            RefreshRecord();
            if (record == null)
            {
                return EarthWorksLocalization.Text("board_corrupt");
            }
            RoadProjectStage stage = GetStage();
            int stageNumber = stage == RoadProjectStage.Completed
                ? ConstructionStages.Length
                : (int)stage + 1;
            string result = EarthWorksLocalization.Text(
                "board_hover",
                stageNumber,
                EarthWorksLocalization.StageName(stage),
                record.Length);
            result += "\n" + BuildStageProgress(stage);
            if (stage == RoadProjectStage.Completed)
            {
                return result;
            }

            string useKey = ZInput.instance != null
                ? ZInput.instance.GetBoundKeyString("Use", false)
                : "E";
            if (string.IsNullOrEmpty(useKey))
            {
                useKey = "E";
            }
            return result +
                "\n" + EarthWorksLocalization.Text(
                    "board_result",
                    EarthWorksLocalization.StageResult(stage)) +
                "\n" + EarthWorksLocalization.Text(
                    "board_action",
                    useKey,
                    EarthWorksLocalization.StageAction(stage));
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || !netView || !netView.IsValid())
            {
                return false;
            }
            Player player = user as Player;
            if (!EarthWorksPlugin.IsDeveloperLab(player))
            {
                player?.Message(
                    MessageHud.MessageType.Center,
                    EarthWorksLocalization.Text("lab_only"));
                return true;
            }
            netView.InvokeRPC(AdvanceRpc);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        private void RPC_Advance(long sender)
        {
            if (!netView || !netView.IsOwner())
            {
                return;
            }
            RefreshRecord();
            Player worker = RoadProjectAuthority.ResolvePlayer(sender);
            if (record == null || !EarthWorksPlugin.IsDeveloperLab(worker) ||
                Vector3.Distance(worker.transform.position, transform.position) > 5f)
            {
                return;
            }
            if (!RoadProjectAuthority.HasPrivateAreaAccess(worker, transform.position))
            {
                Notify(EarthWorksLocalization.Text("board_access_lost"));
                return;
            }
            if (HasThreat(worker))
            {
                Notify(EarthWorksLocalization.Text("board_threat"));
                return;
            }

            RoadProjectStage stage = GetStage();
            switch (stage)
            {
                case RoadProjectStage.Setup:
                    AdvanceTo(RoadProjectStage.Marking, "stage_setup_done");
                    break;
                case RoadProjectStage.Marking:
                    AdvanceTo(RoadProjectStage.Clearing, "stage_marking_done");
                    break;
                case RoadProjectStage.Clearing:
                    RoadTerrainApplier.ApplyClearing(record, (success, message) =>
                    {
                        Notify(message);
                        if (success)
                        {
                            SetStage(RoadProjectStage.Earthworks);
                        }
                    });
                    break;
                case RoadProjectStage.Earthworks:
                    RoadTerrainApplier.ApplyHeights(record, (success, message) =>
                    {
                        Notify(message);
                        if (success)
                        {
                            SetStage(RoadProjectStage.Surfacing);
                        }
                    });
                    break;
                case RoadProjectStage.Surfacing:
                    RoadTerrainApplier.ApplySurface(record, (success, message) =>
                    {
                        Notify(message);
                        if (success)
                        {
                            SetStage(RoadProjectStage.Completion);
                        }
                    });
                    break;
                case RoadProjectStage.Completion:
                    AdvanceTo(RoadProjectStage.Completed, "stage_completed");
                    break;
                case RoadProjectStage.Completed:
                    Notify(EarthWorksLocalization.Text("stage_already_completed"));
                    break;
            }
        }

        internal static bool HasThreat(Player player)
        {
            if (!player)
            {
                return false;
            }
            foreach (Character character in Character.GetAllCharacters())
            {
                if (!character || character == player || !BaseAI.IsEnemy(player, character))
                {
                    continue;
                }
                float distance = Vector3.Distance(player.transform.position, character.transform.position);
                if (distance <= 25f)
                {
                    return true;
                }
                BaseAI ai = character.GetComponent<BaseAI>();
                if (distance <= 50f && ai && (ai.IsAlerted() || ai.IsAggravated()))
                {
                    return true;
                }
            }
            return false;
        }

        private void RefreshRecord()
        {
            if (!netView || !netView.IsValid())
            {
                return;
            }
            ZDO zdo = netView.GetZDO();
            if (record != null && zdo.DataRevision == lastDataRevision)
            {
                return;
            }
            byte[] data = zdo.GetByteArray(DataKey, null);
            if (data == null)
            {
                return;
            }
            if (RoadProjectRecord.TryDeserialize(data, out RoadProjectRecord parsed))
            {
                record = parsed;
                lastDataRevision = zdo.DataRevision;
                BuildRouteLine();
            }
        }

        private void BuildRouteLine()
        {
            if (record == null || record.CenterLine.Count < 2)
            {
                return;
            }
            if (!routeLine)
            {
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                if (!shader)
                {
                    return;
                }
                routeMaterial = new Material(shader)
                {
                    name = "EarthWorks_ProjectRouteMaterial"
                };
                routeLine = CreateLine("EarthWorks_ProjectRoute", 0.08f);
                leftBoundary = CreateLine("EarthWorks_MarkingLeft", 0.055f);
                rightBoundary = CreateLine("EarthWorks_MarkingRight", 0.055f);
            }
            routeLine.positionCount = record.CenterLine.Count;
            routeLine.SetPositions(record.CenterLine.ToArray());
            BuildMarking();
        }

        private void BuildMarking()
        {
            int count = record.PreviewSamples.Count > 1
                ? record.PreviewSamples.Count
                : record.CenterLine.Count;
            Vector3[] left = new Vector3[count];
            Vector3[] right = new Vector3[count];
            for (int i = 0; i < count; ++i)
            {
                Vector3 center = record.PreviewSamples.Count > 1
                    ? record.PreviewSamples[i].Position
                    : record.CenterLine[i];
                Vector3 before = i > 0
                    ? SamplePosition(i - 1)
                    : center;
                Vector3 after = i + 1 < count
                    ? SamplePosition(i + 1)
                    : center;
                float leftWidth = record.PreviewSamples.Count > 1
                    ? record.PreviewSamples[i].LeftWidth
                    : record.DefaultLeftWidth;
                float rightWidth = record.PreviewSamples.Count > 1
                    ? record.PreviewSamples[i].RightWidth
                    : record.DefaultRightWidth;
                RoadRibbonMath.GetOffsets(
                    before,
                    center,
                    after,
                    leftWidth,
                    rightWidth,
                    out Vector3 leftOffset,
                    out Vector3 rightOffset);
                left[i] = Grounded(center + leftOffset) + Vector3.up * 0.8f;
                right[i] = Grounded(center + rightOffset) + Vector3.up * 0.8f;
            }
            leftBoundary.positionCount = count;
            rightBoundary.positionCount = count;
            leftBoundary.SetPositions(left);
            rightBoundary.SetPositions(right);
            Color rope = new Color(1f, 0.62f, 0.12f, 0.95f);
            leftBoundary.startColor = leftBoundary.endColor = rope;
            rightBoundary.startColor = rightBoundary.endColor = rope;

            int requiredStakes = ((count - 1) / 8 + 1) * 2;
            while (stakes.Count < requiredStakes)
            {
                GameObject stake = CreateVanillaStake("EarthWorks_MarkingStake_" + stakes.Count);
                if (!stake)
                {
                    break;
                }
                stakes.Add(stake);
            }
            int stakeIndex = 0;
            for (int i = 0; i < count && stakeIndex + 1 < stakes.Count; i += 8)
            {
                SetStake(stakes[stakeIndex++], left[i]);
                SetStake(stakes[stakeIndex++], right[i]);
            }
            for (int i = stakeIndex; i < stakes.Count; ++i)
            {
                stakes[i].SetActive(false);
            }
        }

        private Vector3 SamplePosition(int index)
        {
            return record.PreviewSamples.Count > 1
                ? record.PreviewSamples[index].Position
                : record.CenterLine[index];
        }

        private static Vector3 Grounded(Vector3 position)
        {
            return RoadTerrain.TryGetHeight(position, out float ground)
                ? new Vector3(position.x, ground, position.z)
                : position;
        }

        private LineRenderer CreateLine(string name, float width)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = routeMaterial;
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private GameObject CreateVanillaStake(string name)
        {
            if (stakePrefabUnavailable)
            {
                return null;
            }
            GameObject prefab = Jotunn.Managers.PrefabManager.Instance.GetPrefab("wood_pole");
            if (!prefab)
            {
                stakePrefabUnavailable = true;
                EarthWorksPlugin.Log.LogWarning("EarthWorks could not find vanilla prefab wood_pole for site marking.");
                return null;
            }

            GameObject stake = new GameObject(name);
            stake.transform.SetParent(transform, false);
            CopyVisualHierarchy(prefab.transform, stake.transform, false);
            if (stake.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                Destroy(stake);
                stakePrefabUnavailable = true;
                EarthWorksPlugin.Log.LogWarning("Vanilla prefab wood_pole has no compatible renderers.");
                return null;
            }
            stake.SetActive(false);
            return stake;
        }

        private static void CopyVisualHierarchy(Transform source, Transform target, bool copyTransform)
        {
            if (copyTransform)
            {
                target.localPosition = source.localPosition;
                target.localRotation = source.localRotation;
                target.localScale = source.localScale;
            }

            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
            if (sourceFilter && sourceRenderer)
            {
                target.gameObject.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                MeshRenderer renderer = target.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = sourceRenderer.sharedMaterials;
                renderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                renderer.receiveShadows = sourceRenderer.receiveShadows;
            }

            foreach (Transform child in source)
            {
                GameObject copy = new GameObject(child.name);
                copy.transform.SetParent(target, false);
                CopyVisualHierarchy(child, copy.transform, true);
            }
        }

        private static void SetStake(GameObject stake, Vector3 ropePoint)
        {
            stake.transform.position = ropePoint - Vector3.up * 0.8f;
            stake.transform.rotation = Quaternion.identity;
        }

        private static string BuildStageProgress(RoadProjectStage stage)
        {
            List<string> completed = new List<string>();
            List<string> remaining = new List<string>();
            for (int i = 0; i < ConstructionStages.Length; ++i)
            {
                if (stage == RoadProjectStage.Completed || i < (int)stage)
                {
                    completed.Add(EarthWorksLocalization.StageName(ConstructionStages[i]));
                }
                else
                {
                    remaining.Add(EarthWorksLocalization.StageName(ConstructionStages[i]));
                }
            }
            return EarthWorksLocalization.Text(
                "board_stage_lists",
                completed.Count > 0
                    ? string.Join(", ", completed.ToArray())
                    : EarthWorksLocalization.Text("board_none"),
                remaining.Count > 0
                    ? string.Join(", ", remaining.ToArray())
                    : EarthWorksLocalization.Text("board_none"));
        }

        private RoadProjectStage GetStage()
        {
            if (!netView || !netView.IsValid())
            {
                return RoadProjectStage.Setup;
            }
            int value = netView.GetZDO().GetInt(StageKey, 0);
            return Enum.IsDefined(typeof(RoadProjectStage), value)
                ? (RoadProjectStage)value
                : RoadProjectStage.Setup;
        }

        private void AdvanceTo(RoadProjectStage stage, string messageKey)
        {
            SetStage(stage);
            Notify(EarthWorksLocalization.Text(messageKey));
        }

        private void SetStage(RoadProjectStage stage)
        {
            netView.GetZDO().Set(StageKey, (int)stage);
        }

        private static void Notify(string message)
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, message);
        }
    }
}
