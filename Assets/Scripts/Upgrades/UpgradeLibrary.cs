using System.Collections.Generic;
using UnityEngine;

// Urutan enum ini juga urutan ikon di GameDatabase.upgradeIcons.
public enum UpgradeId
{
    TwinSlash,
    WideSwing,
    DiagonalSlash,
    RearGuard,
    PiercingEdge,
    BouncyBlade,
    SharpSword,
    Zoomies,
    ChillVibes,
    ThickFur,
    YuzuSnack,
    YuzuOrbit,
    SpicyYuzu,
    OnsenAura,
    LuckyLeaf,
    DashMaster,
    Munchies
}

public class UpgradeDef
{
    public UpgradeId id;
    public string name;
    public string description;
    public int maxLevel;
    public Color color;
    public System.Action<PlayerController> apply;
    public System.Func<PlayerController, bool> canOffer;
}

// Daftar kemampuan yang ditawarkan saat naik level (ala Archero: pilih 1 dari 3).
public static class UpgradeLibrary
{
    private static readonly Color Attack = new Color(1f, 0.55f, 0.25f);
    private static readonly Color Defense = new Color(0.4f, 0.85f, 0.45f);
    private static readonly Color Utility = new Color(0.45f, 0.75f, 1f);
    private static readonly Color Special = new Color(0.85f, 0.55f, 1f);

    private static List<UpgradeDef> all;

    public static List<UpgradeDef> All
    {
        get
        {
            if (all == null)
                all = Build();
            return all;
        }
    }

    private static List<UpgradeDef> Build()
    {
        List<UpgradeDef> list = new List<UpgradeDef>();

        list.Add(Def(UpgradeId.TwinSlash, "Twin Slash", "Each swing fires one more half-damage wave right after.", 3, Attack,
            p => p.Stats.Multishot++));
        list.Add(Def(UpgradeId.WideSwing, "Wide Swing", "+1 parallel half-damage wave.", 2, Attack,
            p => p.Stats.Front++));
        list.Add(Def(UpgradeId.DiagonalSlash, "Diagonal Slash", "+2 half-damage waves at diagonal angles.", 2, Attack,
            p => p.Stats.DiagonalPairs++));
        list.Add(Def(UpgradeId.RearGuard, "Rear Guard", "Also slash behind you (half damage).", 1, Attack,
            p => p.Stats.Rear++));
        list.Add(Def(UpgradeId.PiercingEdge, "Piercing Edge", "Waves pass through +1 enemy.", 3, Attack,
            p => p.Stats.Pierce++));
        list.Add(Def(UpgradeId.BouncyBlade, "Bouncy Blade", "Waves bounce to +1 nearby enemy.", 3, Attack,
            p => p.Stats.Ricochet++));
        list.Add(Def(UpgradeId.SharpSword, "Sharp Sword", "+25% damage.", 5, Attack,
            p => p.Stats.Damage += PlayerStats.BaseDamage * 0.25f));
        list.Add(Def(UpgradeId.Zoomies, "Zoomies", "+12% move speed.", 3, Utility,
            p => p.Stats.MoveSpeed *= 1.12f));
        list.Add(Def(UpgradeId.ChillVibes, "Chill Vibes", "+20% attack speed.", 5, Utility,
            p => p.Stats.AttackInterval = PlayerStats.BaseAttackInterval / (1f + 0.2f * (p.Stats.GetLevel(UpgradeId.ChillVibes) + 1))));
        list.Add(Def(UpgradeId.ThickFur, "Thick Fur", "+25 max HP and heal 25.", 5, Defense,
            p => p.Health.IncreaseMax(25f)));
        list.Add(Def(UpgradeId.YuzuSnack, "Yuzu Snack", "Heal 40% HP right now.", 99, Defense,
            p => p.Health.HealPercent(0.4f),
            p => p.Health.Current < p.Health.Max * 0.75f));
        list.Add(Def(UpgradeId.YuzuOrbit, "Yuzu Orbit", "+1 yuzu circling you, hurting enemies.", 3, Special,
            p => p.Stats.OrbitCount++));
        list.Add(Def(UpgradeId.SpicyYuzu, "Spicy Yuzu", "Hits set enemies on fire.", 2, Special,
            p => p.Stats.BurnLevel++));
        list.Add(Def(UpgradeId.OnsenAura, "Onsen Aura", "Regenerate 1.5 HP per second.", 2, Defense,
            p => p.Stats.Regen += 1.5f));
        list.Add(Def(UpgradeId.LuckyLeaf, "Lucky Leaf", "+12% critical chance.", 3, Special,
            p => p.Stats.CritChance += 0.12f));
        list.Add(Def(UpgradeId.DashMaster, "Dash Master", "Dash cooldown -30% and dashing hurts enemies.", 2, Utility,
            p => { p.Stats.DashCooldown *= 0.7f; p.Stats.DashDamage = true; }));
        list.Add(Def(UpgradeId.Munchies, "Munchies", "Heal 3 HP for every enemy defeated.", 2, Defense,
            p => p.Stats.HealOnKill += 3));

        return list;
    }

    private static UpgradeDef Def(UpgradeId id, string name, string desc, int maxLevel, Color color,
        System.Action<PlayerController> apply, System.Func<PlayerController, bool> canOffer = null)
    {
        UpgradeDef d = new UpgradeDef();
        d.id = id;
        d.name = name;
        d.description = desc;
        d.maxLevel = maxLevel;
        d.color = color;
        d.apply = apply;
        d.canOffer = canOffer;
        return d;
    }

    public static List<UpgradeDef> RollChoices(PlayerController player, int count)
    {
        List<UpgradeDef> pool = new List<UpgradeDef>();
        foreach (UpgradeDef d in All)
        {
            if (player.Stats.GetLevel(d.id) >= d.maxLevel)
                continue;
            if (d.canOffer != null && !d.canOffer(player))
                continue;
            pool.Add(d);
        }

        List<UpgradeDef> result = new List<UpgradeDef>();
        while (result.Count < count && pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            result.Add(pool[i]);
            pool.RemoveAt(i);
        }
        return result;
    }

    public static void Apply(PlayerController player, UpgradeDef def)
    {
        def.apply(player);
        player.Stats.AddLevel(def.id);
    }

    public static UpgradeDef Get(UpgradeId id)
    {
        foreach (UpgradeDef d in All)
        {
            if (d.id == id)
                return d;
        }
        return null;
    }
}
