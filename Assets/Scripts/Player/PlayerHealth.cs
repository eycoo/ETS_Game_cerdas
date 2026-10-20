using UnityEngine;

// Darah Capy: kebal sebentar setelah terkena (i-frame), knockback, regen, dan heal.
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float hurtInvulnerability = 0.8f;

    public float Current { get; private set; }
    public float Max => player.Stats.MaxHP;
    public bool IsDead { get; private set; }
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    public bool CanBeHit
    {
        get
        {
            if (IsDead || IsInvulnerable || player.IsDashing)
                return false;
            return GameManager.I == null || !GameManager.I.GodMode;
        }
    }

    private PlayerController player;
    private float invulnerableUntil;
    private float regenBuffer;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        Current = player.Stats.MaxHP;
    }

    public void TakeDamage(float amount, Vector3 sourcePosition)
    {
        if (!CanBeHit)
            return;
        if (GameManager.I != null && !GameManager.I.IsPlaying)
            return;

        int dmg = Mathf.Max(1, Mathf.RoundToInt(amount));
        Current = Mathf.Max(0f, Current - dmg);
        if (GameManager.I != null)
            GameManager.I.Stats.damageTaken += dmg;

        invulnerableUntil = Time.time + hurtInvulnerability;
        player.AddKnockback(CapyMath.DirectionTo(sourcePosition, transform.position) * 8f);
        player.Visual.OnHurt(hurtInvulnerability);

        WorldUI.Number(transform.position + Vector3.up * 1.7f, dmg, NumberStyle.PlayerHurt);
        VFX.Sparks(transform.position + Vector3.up * 0.8f, new Color(1f, 0.45f, 0.35f), 12, 6f);
        CameraRig.Shake(0.45f);
        AudioManager.Sfx("player_hurt");
        if (GameUI.I != null)
            GameUI.I.DamageFlash();
        if (GameManager.I != null)
            GameManager.I.HitStop(0.06f);

        if (Current <= 0f)
            Die();
    }

    public void Heal(float amount, bool showEffects = true)
    {
        if (IsDead)
            return;

        float before = Current;
        Current = Mathf.Min(Max, Current + amount);
        int healed = Mathf.RoundToInt(Current - before);
        if (healed <= 0)
            return;

        WorldUI.Number(transform.position + Vector3.up * 1.7f, healed, NumberStyle.Heal);
        if (showEffects)
        {
            VFX.Sparkle(transform.position + Vector3.up, new Color(0.45f, 1f, 0.5f), 14);
            AudioManager.Sfx("heal");
        }
    }

    public void HealPercent(float percent, bool showEffects = true)
    {
        Heal(Max * percent, showEffects);
    }

    public void IncreaseMax(float amount)
    {
        player.Stats.MaxHP += amount;
        Current += amount;
    }

    public void SetInvulnerable(float duration)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);
    }

    private void Update()
    {
        if (IsDead || player.Stats.Regen <= 0f || GameManager.I == null || !GameManager.I.IsPlaying)
            return;

        regenBuffer += player.Stats.Regen * Time.deltaTime;
        if (regenBuffer >= 1f && Current < Max)
        {
            int whole = Mathf.FloorToInt(regenBuffer);
            regenBuffer -= whole;
            Current = Mathf.Min(Max, Current + whole);
        }
        else if (Current >= Max)
        {
            regenBuffer = 0f;
        }
    }

    private void Die()
    {
        IsDead = true;
        player.Visual.OnDeath();
        VFX.Poof(transform.position + Vector3.up * 0.6f, new Color(0.85f, 0.65f, 0.45f), 16, 1f);
        if (GameManager.I != null)
            GameManager.I.OnPlayerDied();
    }
}
