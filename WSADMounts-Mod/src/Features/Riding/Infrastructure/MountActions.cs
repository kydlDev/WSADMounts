using System;
using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// Makes a ridden mount jump or attack (D-037). The rider owns the mount while riding (Sadle.RPC_RequestControl hands the ZDO to the rider),
/// so the action normally runs right here; otherwise the mod's RPC <see cref="Rpc"/> asks the owner. Jump = the game's own Character.Jump
/// (m_jumpForce 10 on Lox / Asksvin / Moose, animator trigger "jump", synced by ZSyncAnimation). Attack = equip one of the mount's own attack
/// items and start it with Humanoid.StartAttack, as MonsterAI.DoAttack does; the AI only swaps the weapon while it has a target and is not
/// attacking (MonsterAI.SelectBestAttack), and a ridden mount that moves never reaches that code (Sadle.UpdateRiding returns true first).
/// </summary>
internal static class MountActions
{
    internal const string Rpc = "WSADMounts_MountAction_v1";

    // The heavy attack each mount fired last (secondary attack cycles through them).
    private static readonly Dictionary<Character, int> LastHeavy = new Dictionary<Character, int>();

    /// <summary>Registers the owner-side RPC on this mount's network view (Sadle.Awake). Expected outcome: true when registered.</summary>
    internal static bool Register(ZNetView view, Character mount)
    {
        bool ok = view != null && view.IsValid() && mount != null;
        if (ok)
        {
            view.Register<int>(Rpc, (sender, kind) => OnRpc(mount, sender, kind));
        }

        if (ModLog.IsDue("MountActions.Register", 10d)) ModLog.Verbose($"MountActions.Register: {(mount != null ? mount.m_name : "<none>")} view valid {view != null && view.IsValid()} -> {ok}");
        return ok;
    }

    /// <summary>
    /// The rider asked for <paramref name="kind"/>. Expected outcome: true when it ran here (owner) or the RPC to the owner was sent.
    /// </summary>
    internal static bool Request(Character mount, ZNetView view, MountActionKind kind)
    {
        if (kind == MountActionKind.None || mount == null || view == null || !view.IsValid())
        {
            ModLog.Verbose($"MountActions.Request: nothing to do (kind {kind}, mount {mount != null}, view valid {view != null && view.IsValid()})");
            return kind == MountActionKind.None;
        }

        if (view.IsOwner())
        {
            ModLog.Verbose($"MountActions.Request: {mount.m_name} {kind} runs here (this client owns the mount)");
            return Perform(mount, kind, "rider owns the mount");
        }

        view.InvokeRPC(Rpc, (int)kind);
        ModLog.Decision($"mount action of {mount.m_name}", $"sent {kind} to the owner", $"owner {view.GetZDO().GetOwner()}, me {ZDOMan.GetSessionID()}");
        return view.IsValid();
    }

    private static void OnRpc(Character mount, long sender, int kind)
    {
        ZNetView view = mount != null ? mount.GetComponent<ZNetView>() : null;
        bool owner = view != null && view.IsValid() && view.IsOwner();
        bool known = Enum.IsDefined(typeof(MountActionKind), kind);
        if (owner && known)
        {
            Perform(mount, (MountActionKind)kind, $"RPC from {sender}");
        }
        else
        {
            ModLog.Warn($"MountActions.OnRpc: ignored kind {kind} from {sender} (known {known}, owner here {owner}, mount {(mount != null ? mount.m_name : "<none>")})");
        }
    }

    private static bool Perform(Character mount, MountActionKind kind, string why)
    {
        bool done = kind == MountActionKind.Jump ? DoJump(mount) : DoAttack(mount, kind == MountActionKind.HeavyAttack);
        ModLog.Decision($"mount action of {mount.m_name}", $"{kind} {(done ? "started" : "not started")}", why);
        return done;
    }

    private static bool DoJump(Character mount)
    {
        bool onGround = mount.IsOnGround();
        float before = mount.GetVelocity().y;
        mount.Jump();
        float after = mount.GetVelocity().y;
        bool jumped = after > before + 0.5f;
        ModLog.Verbose($"MountActions.DoJump: {mount.m_name} on ground {onGround}, upward speed {before:0.##} -> {after:0.##} m/s, jump force {mount.m_jumpForce:0.#} -> jumped {jumped}");
        return jumped;
    }

    private static bool DoAttack(Character mount, bool heavy)
    {
        var humanoid = mount as Humanoid;
        List<ItemDrop.ItemData> attacks = AttacksOf(humanoid);
        int index;
        if (heavy)
        {
            LastHeavy.TryGetValue(mount, out int last);
            index = MountActionRules.NextHeavy(attacks.Count, last);
        }
        else
        {
            index = attacks.Count > 0 ? MountActionRules.LightIndex : -1;
        }

        if (humanoid == null || index < 0)
        {
            ModLog.Warn($"MountActions.DoAttack: {mount.m_name} has no attack (humanoid {humanoid != null}, attacks {attacks.Count})");
            return index >= 0 && humanoid != null;
        }

        ItemDrop.ItemData item = attacks[index];
        bool equipped = humanoid.IsItemEquiped(item) || humanoid.EquipItem(item, triggerEquipEffects: false);
        bool started = equipped && humanoid.StartAttack(null, false);
        if (started && heavy)
        {
            Prune();
            LastHeavy[mount] = index;
        }

        string animation = item.m_shared.m_attack?.m_attackAnimation ?? "<none>";
        ModLog.Verbose($"MountActions.DoAttack: {mount.m_name} {(heavy ? "heavy" : "light")} attack {index + 1} of {attacks.Count} '{Name(item)}' (animation {animation}), equipped {equipped}, in attack {humanoid.InAttack()} -> started {started}");
        return started;
    }

    /// <summary>The mount's attack items in the order of its prefab's m_defaultItems (the first one is the light attack).</summary>
    private static List<ItemDrop.ItemData> AttacksOf(Humanoid humanoid)
    {
        var result = new List<ItemDrop.ItemData>();
        if (humanoid == null || humanoid.m_defaultItems == null)
        {
            ModLog.Verbose($"MountActions.AttacksOf: no attacks (humanoid {humanoid != null}, default items {humanoid?.m_defaultItems != null})");
            return result;
        }

        List<ItemDrop.ItemData> carried = humanoid.GetInventory().GetAllItems();
        foreach (GameObject prefab in humanoid.m_defaultItems)
        {
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            string name = drop != null ? drop.m_itemData.m_shared.m_name : null;
            ItemDrop.ItemData item = name == null ? null : carried.Find(i => i.m_shared.m_name == name && i.HavePrimaryAttack());
            if (item != null)
            {
                result.Add(item);
            }
        }

        ModLog.Verbose($"MountActions.AttacksOf: {humanoid.m_name} {result.Count} attack(s) of {humanoid.m_defaultItems.Length} default item(s), {carried.Count} carried: {string.Join(", ", result.ConvertAll(Name).ToArray())}");
        return result;
    }

    private static string Name(ItemDrop.ItemData item) => item.m_dropPrefab != null ? item.m_dropPrefab.name : item.m_shared.m_name;

    private static void Prune()
    {
        if (LastHeavy.Count < 16)
        {
            ModLog.Verbose($"MountActions.Prune: {LastHeavy.Count} mount(s) known, nothing to forget yet");
            return;
        }

        var gone = new List<Character>();
        foreach (Character known in LastHeavy.Keys)
        {
            if (known == null) gone.Add(known);
        }

        gone.ForEach(g => LastHeavy.Remove(g));
        ModLog.Verbose($"MountActions.Prune: {gone.Count} unloaded mount(s) forgotten, {LastHeavy.Count} left");
    }
}
