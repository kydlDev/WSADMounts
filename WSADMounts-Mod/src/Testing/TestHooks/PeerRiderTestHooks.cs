using HarmonyLib;
using KydlCore.Common;
using KydlCore.Features.Diagnostics.Contracts;
using UnityEngine;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TestLab hooks for T-007c (D-033): the lab's second player (Bjornita) is made the saddle's rider of the test mount and seated on it
/// (PeerSeat), while THIS client keeps owning it, so her "Controls" RPCs (sent by the lab with peerRpc) steer the mount on this client the way the game does for a rider. The game
/// itself would hand ownership to her (Sadle.RPC_RequestControl); the virtual player cannot simulate a creature, so the host keeps it. TEST ONLY.
/// </summary>
internal static class PeerRiderTestHooks
{
    /// <summary>
    /// Writes the other player's character id into the saddle's "user" ZDO value (what Sadle.RPC_RequestControl writes, without the ownership
    /// change), and seats her on the saddle (PeerSeat). Returns "user set <id>, seated True, ..." or "no ...".
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
        PeerSeat seat = mount.GetComponent<PeerSeat>() ?? mount.AddComponent<PeerSeat>();
        bool seated = seat.Begin(rider);
        bool tracked = TrackMount(mount);
        float distance = Vector3.Distance(rider.GetPosition(), mount.transform.position);
        string answer = $"user set {rider.m_uid.UserID}, seated {seated}, rider {distance:0.0} m from the mount (saddle range 10 m), owner me {view.IsOwner()}, tracked {tracked}";
        ModLog.Info($"PeerRiderTestHooks.PeerRider: {answer}");
        return answer;
    }

    /// <summary>
    /// Tracks the peer-ridden test mount in KydlCore's flight recorder (a3g16c-3: after her stop command the Lox walked 6.3 m on its own and the
    /// dump had no line about it). Expected outcome: true when the recorder took the mount.
    /// </summary>
    private static bool TrackMount(GameObject mount)
    {
        Sadle saddle = mount.GetComponentInChildren<Sadle>();
        MonsterAI ai = mount.GetComponent<MonsterAI>();
        Character character = mount.GetComponent<Character>();
        if (saddle == null || ai == null || character == null)
        {
            ModLog.Warn($"PeerRiderTestHooks.TrackMount: saddle {saddle != null}, MonsterAI {ai != null}, character {character != null} - not tracked");
            return saddle != null && ai != null && character != null;
        }

        bool tracked = FlightRecorder.Track(character, "WSADMounts peer-ridden test mount", () => MountFacts(saddle, ai, character));
        ModLog.Verbose($"PeerRiderTestHooks.TrackMount: {mount.name} tracked {tracked} - {MountFacts(saddle, ai, character)}");
        return tracked;
    }

    /// <summary>What drives the mount: the saddle's rider state and command, and the AI's own branches (follow, target, move dir).</summary>
    private static string MountFacts(Sadle saddle, MonsterAI ai, Character character)
    {
        Traverse sad = Traverse.Create(saddle);
        Traverse brain = Traverse.Create(ai);
        Vector3 control = sad.Field("m_controlDir").GetValue<Vector3>();
        Vector3 move = character.GetMoveDir();
        GameObject follow = ai.GetFollowTarget();
        Character target = brain.Field("m_targetCreature").GetValue<Character>();
        return $"validUser={saddle.HaveValidUser()} speed={sad.Field("m_speed").GetValue()} control=({control.x:0.##},{control.z:0.##}) "
            + $"move=({move.x:0.##},{move.z:0.##}) run={character.IsRunning()} follow={(follow != null ? follow.name : "none")} "
            + $"target={(target != null ? target.m_name : "none")} alerted={ai.IsAlerted()} randomTarget={brain.Field("m_randomMoveTarget").GetValue<Vector3>()} "
            + $"reachedRandom={brain.Field("m_reachedRandomMoveTarget").GetValue<bool>()}";
    }
}
