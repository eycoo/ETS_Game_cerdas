using System.Collections.Generic;
using UnityEngine;

// Gerak Capy (WASD/stik), dash dengan i-frame, dan knockback.
// Kamera menghadap utara tanpa yaw, jadi input langsung dipetakan ke sumbu X/Z dunia.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController I { get; private set; }

    [SerializeField] private float acceleration = 80f;
    [SerializeField] private float dashSpeed = 21f;
    [SerializeField] private float dashDuration = 0.17f;
    [SerializeField] private float radius = 0.45f;

    public PlayerStats Stats { get; private set; } = new PlayerStats();
    public PlayerLevel Level { get; private set; } = new PlayerLevel();
    public PlayerHealth Health { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public PlayerVisual Visual { get; private set; }

    public float Radius => radius;
    public Vector3 Velocity { get; private set; }
    public Vector3 Facing { get; private set; } = Vector3.forward;
    public Vector3 MoveInput => moveInput;
    public bool IsMoving { get; private set; }
    public bool IsDashing => dashTimer > 0f;
    public float DashCooldown01 => Stats.DashCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(dashCooldownTimer / Stats.DashCooldown);
    public bool CanAct => GameManager.I == null || GameManager.I.IsPlaying;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 currentVelocity;
    private Vector3 knockback;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;
    private float dashTrailTimer;
    private readonly HashSet<EnemyBase> dashHits = new HashSet<EnemyBase>();
    private static readonly List<EnemyBase> buffer = new List<EnemyBase>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        I = null;
    }

    private void Awake()
    {
        I = this;
        rb = GetComponent<Rigidbody>();
        Health = GetComponent<PlayerHealth>();
        Combat = GetComponent<PlayerCombat>();
        Visual = GetComponent<PlayerVisual>();

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void Update()
    {
        if (!CanAct || Health.IsDead)
        {
            moveInput = Vector3.zero;
            IsMoving = false;
            return;
        }

        Vector2 input = CapyInput.Move();
        moveInput = new Vector3(input.x, 0f, input.y);
        IsMoving = moveInput.sqrMagnitude > 0.01f;
        // Saat menyerang, Capy menghadap arah bidikan (diatur PlayerCombat), bukan arah lari.
        if (IsMoving && !IsDashing && !Combat.IsAttacking)
            Facing = moveInput.normalized;

        dashCooldownTimer -= Time.deltaTime;
        if (CapyInput.DashPressed() && dashCooldownTimer <= 0f && !IsDashing)
            StartDash();

        if (IsDashing)
        {
            dashTimer -= Time.deltaTime;
            dashTrailTimer -= Time.deltaTime;
            if (dashTrailTimer <= 0f)
            {
                dashTrailTimer = 0.025f;
                VFX.Poof(transform.position + Vector3.up * 0.4f, new Color(1f, 0.93f, 0.8f, 0.55f), 2, 0.55f);
            }
            if (Stats.DashDamage)
                DashDamage();
            if (dashTimer <= 0f)
                currentVelocity = dashDirection * Stats.MoveSpeed;
        }
    }

    private void FixedUpdate()
    {
        Velocity = rb.linearVelocity;

        if (Health.IsDead)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (IsDashing)
            currentVelocity = dashDirection * dashSpeed;
        else
            currentVelocity = Vector3.MoveTowards(currentVelocity, moveInput * Stats.MoveSpeed, acceleration * Time.fixedDeltaTime);

        Vector3 v = currentVelocity + knockback;
        rb.linearVelocity = new Vector3(v.x, 0f, v.z);
        knockback = Vector3.MoveTowards(knockback, Vector3.zero, 40f * Time.fixedDeltaTime);
    }

    private void StartDash()
    {
        dashDirection = IsMoving ? moveInput.normalized : Facing;
        dashTimer = dashDuration;
        dashCooldownTimer = Stats.DashCooldown;
        Health.SetInvulnerable(dashDuration + 0.1f);
        dashHits.Clear();
        Visual.OnDash();
        AudioManager.Sfx("dash", 0.8f);
    }

    private void DashDamage()
    {
        buffer.Clear();
        buffer.AddRange(EnemyBase.Active);
        foreach (EnemyBase e in buffer)
        {
            if (e == null || !e.IsTargetable || dashHits.Contains(e))
                continue;
            float r = radius + e.Radius + 0.3f;
            if (CapyMath.FlatDistanceSq(transform.position, e.transform.position) > r * r)
                continue;

            DamageInfo info = new DamageInfo();
            info.amount = Stats.Damage * 1.5f;
            info.direction = dashDirection;
            info.knockback = 7f;
            e.TakeDamage(info);
            dashHits.Add(e);
        }
    }

    public void AddKnockback(Vector3 force)
    {
        knockback += force.Flat();
    }

    public void SetFacing(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            Facing = dir.normalized;
    }

    public void TeleportTo(Vector3 pos)
    {
        pos.y = 0f;
        rb.position = pos;
        transform.position = pos;
        rb.linearVelocity = Vector3.zero;
        currentVelocity = Vector3.zero;
        knockback = Vector3.zero;
    }
}
