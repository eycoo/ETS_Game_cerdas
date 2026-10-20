using UnityEngine;

public enum Team
{
    Player,
    Enemy
}

public enum NumberStyle
{
    Normal,
    Crit,
    Burn,
    PlayerHurt,
    Heal
}

public struct DamageInfo
{
    public float amount;
    public Vector3 direction;
    public float knockback;
    public bool crit;
    public int burnLevel;
    public bool burnTick;
}
