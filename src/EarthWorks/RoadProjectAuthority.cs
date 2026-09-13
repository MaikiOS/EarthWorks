using System.Collections;
using System.Reflection;
using UnityEngine;

namespace OstrixMods.EarthWorks
{
    internal static class RoadProjectAuthority
    {
        private static readonly FieldInfo AllAreasField = typeof(PrivateArea).GetField(
            "m_allAreas",
            BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo IsEnabledMethod = typeof(PrivateArea).GetMethod(
            "IsEnabled",
            BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo IsInsideMethod = typeof(PrivateArea).GetMethod(
            "IsInside",
            BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo IsPermittedMethod = typeof(PrivateArea).GetMethod(
            "IsPermitted",
            BindingFlags.NonPublic | BindingFlags.Instance);

        internal static Player ResolvePlayer(long sender)
        {
            if (!ZNet.instance)
            {
                return null;
            }
            if (sender == ZNet.GetUID())
            {
                return Player.m_localPlayer;
            }
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            return peer != null ? Player.GetPlayer(peer.m_playerID) : null;
        }

        internal static bool HasPrivateAreaAccess(Player player, Vector3 position)
        {
            if (!player || AllAreasField == null || IsEnabledMethod == null ||
                IsInsideMethod == null || IsPermittedMethod == null)
            {
                return false;
            }
            IEnumerable areas = AllAreasField.GetValue(null) as IEnumerable;
            if (areas == null)
            {
                return false;
            }
            long playerId = player.GetPlayerID();
            foreach (object value in areas)
            {
                PrivateArea area = value as PrivateArea;
                if (!area || !(bool)IsEnabledMethod.Invoke(area, null) ||
                    !(bool)IsInsideMethod.Invoke(area, new object[] { position, 0f }))
                {
                    continue;
                }
                Piece piece = area.GetComponent<Piece>();
                if ((piece && piece.GetCreator() == playerId) ||
                    (bool)IsPermittedMethod.Invoke(area, new object[] { playerId }))
                {
                    continue;
                }
                return false;
            }
            return true;
        }
    }
}
