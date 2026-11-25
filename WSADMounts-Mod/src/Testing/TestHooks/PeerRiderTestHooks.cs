using KydlCore.Common;
using UnityEngine;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TestLab hooks for T-007c (D-033): the lab's second player (Bjornita) is made the saddle's rider of the test mount while THIS client keeps
/// owning it, so her "Controls" RPCs (sent by the lab with peerRpc) steer the mount on this client the way the game does for a rider. The game
/// itself would hand ownership to her (Sadle.RPC_RequestControl); the virtual player cannot simulate a creature, so the host keeps it. TEST ONLY.
/// </summary>
internal static class PeerRiderTestHooks
{
    /// <summary>
    /// Writes the other player's character id into the saddle's "user" ZDO value (what Sadle.RPC_RequestControl writes, without the ownership
    /// change). Returns "user set <id>, rider 1.2 m away" or "no ...".
    /// </summary>
    [TestHook("WSADMounts.peerRider")]
    private static string PeerRider()
    {
        GameObject mount = RidingTestHooks.TestMount;
        ZNetView view = mount != null ? mount.GetComponent<ZNetView>() : null;
        Player me = Player.m_localPlayer;
        if (view == null || !view.IsValid() || me == null || ZNet.instance == null)
        {
            ModLog.Warn($"PeerRiderTestHooks.PeerRider: mount {view != null && view.IsValid()}, player {me != null}, ZNet {ZNet.instance != null}");
            return "no mount or no player";
        }

        long mine = me.GetZDOID().UserID;
        ZDO rider = null;
        int characters = 0;
        foreach (ZDO zdo in ZNet.instance.GetAllCharacterZDOS())
        {
            characters++;
            if (zdo.m_uid.UserID != mine)
            {
                rider = zdo;
                break;
            }
        }

        if (rider == null)
        {
            ModLog.Warn($"PeerRiderTestHooks.PeerRider: no other player's character among {characters} (mine {mine})");
            return "no other player";
        }

        view.GetZDO().Set(ZDOVars.s_user, rider.m_uid.UserID);
        float distance = Vector3.Distance(rider.GetPosition(), mount.transform.position);
        string answer = $"user set {rider.m_uid.UserID}, rider {distance:0.0} m away (saddle range 10 m), owner me {view.IsOwner()}";
        ModLog.Info($"PeerRiderTestHooks.PeerRider: {answer}");
        return answer;
    }
}
