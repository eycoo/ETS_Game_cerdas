using System.Collections.Generic;
using UnityEngine;

// Statistik Capy yang berubah karena upgrade (lihat UpgradeLibrary).
public class PlayerStats
{
    public float MaxHP = 100f;
    public float MoveSpeed = 6.2f;
    public float AttackInterval = 0.6f;
    public float Damage = 12f;
    public float CritChance = 0.05f;
    public float CritMultiplier = 2f;
    public float ProjectileSpeed = 16f;
    public int Front = 1;
    public int Multishot;
    public int DiagonalPairs;
    public int Rear;
    public int Pierce;
    public int Ricochet;
    public int BurnLevel;
    public int OrbitCount;
    public float Regen;
    public float DashCooldown = 1.1f;
    public bool DashDamage;
    public int HealOnKill;
    public float MagnetRadius = 2.8f;

    private readonly Dictionary<UpgradeId, int> levels = new Dictionary<UpgradeId, int>();

    public int GetLevel(UpgradeId id)
    {
        int v;
        return levels.TryGetValue(id, out v) ? v : 0;
    }

    public void AddLevel(UpgradeId id)
    {
        levels[id] = GetLevel(id) + 1;
    }

    public IEnumerable<KeyValuePair<UpgradeId, int>> AllLevels()
    {
        return levels;
    }
}

// Level dan XP. Kurva XP: 5, 10, 15, 20, ... (naik 5 tiap level).
public class PlayerLevel
{
    public int Level { get; private set; } = 1;
    public int XP { get; private set; }
    public int XPToNext => 5 + (Level - 1) * 5;
    public float Progress01 => Mathf.Clamp01((float)XP / XPToNext);

    public void AddXP(int amount)
    {
        XP += amount;
        while (XP >= XPToNext)
        {
            XP -= XPToNext;
            Level++;
            if (GameManager.I != null)
                GameManager.I.QueueUpgrade(false);

            PlayerController p = PlayerController.I;
            if (p != null)
            {
                VFX.Ring(p.transform.position, new Color(0.45f, 0.85f, 1f), 3f, 0.5f);
                VFX.Sparkle(p.transform.position + Vector3.up, new Color(0.5f, 0.9f, 1f), 20);
            }
        }
    }
}
