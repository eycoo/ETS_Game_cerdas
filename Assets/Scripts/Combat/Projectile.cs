using System.Collections.Generic;
using UnityEngine;

// Proyektil lurus untuk kedua tim: gelombang pedang Capy, orb sihir Wizard, panah Skeleton.
// Tabrakan dicek manual (jarak XZ ke musuh/Player dan raycast ke tembok), jadi tidak butuh Rigidbody.
public class Projectile : MonoBehaviour
{
    [SerializeField] private float radius = 0.35f;
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private float knockback = 3.5f;
    [SerializeField] private Color impactColor = Color.white;
    [SerializeField] private Transform spinner;
    [SerializeField] private Vector3 spinAxis = Vector3.forward;
    [SerializeField] private float spinSpeed;
    [SerializeField] private float homingTime;
    [SerializeField] private float homingTurnRate;

    public static readonly List<Projectile> EnemyActive = new List<Projectile>();
    private static readonly List<EnemyBase> buffer = new List<EnemyBase>();
    private readonly List<EnemyBase> hits = new List<EnemyBase>();

    private Team team;
    private Vector3 direction;
    private float speed;
    private float damage;
    private bool crit;
    private int pierce;
    private int ricochet;
    private int burn;
    private float age;
    private bool launched;
    private Vector3 baseScale;

    public Vector3 Direction => direction;
    public float Speed => speed;
    public float Radius => radius;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        EnemyActive.Clear();
    }

    private void OnDestroy()
    {
        EnemyActive.Remove(this);
    }

    public void Launch(Team owner, Vector3 dir, float projectileSpeed, float projectileDamage, bool isCrit = false, int pierceCount = 0, int ricochetCount = 0, int burnLevel = 0)
    {
        team = owner;
        direction = dir.Flat().normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        crit = isCrit;
        pierce = pierceCount;
        ricochet = ricochetCount;
        burn = burnLevel;
        launched = true;
        if (owner == Team.Enemy && !EnemyActive.Contains(this))
            EnemyActive.Add(this);
        baseScale = transform.localScale;
        transform.rotation = Quaternion.LookRotation(direction);
        if (crit)
            baseScale *= 1.25f;
        transform.localScale = baseScale * 0.4f;
    }

    private void Update()
    {
        if (!launched)
            return;

        float dt = Time.deltaTime;
        age += dt;
        if (age >= lifetime)
        {
            Despawn(false);
            return;
        }

        transform.localScale = baseScale * Mathf.Min(1f, 0.4f + age * 9f);

        if (team == Team.Enemy && homingTime > 0f && age < homingTime && PlayerController.I != null)
        {
            Vector3 to = CapyMath.DirectionTo(transform.position, PlayerController.I.transform.position);
            direction = Vector3.RotateTowards(direction, to, homingTurnRate * Mathf.Deg2Rad * dt, 0f);
        }

        Vector3 step = direction * speed * dt;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, step.magnitude + 0.05f, Layers.ObstacleMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point - direction * 0.05f;
            Despawn(true);
            return;
        }

        transform.position += step;
        transform.rotation = Quaternion.LookRotation(direction);
        if (spinner != null && spinSpeed != 0f)
            spinner.Rotate(spinAxis, spinSpeed * dt, Space.Self);

        if (team == Team.Player)
            HitEnemies();
        else
            HitPlayer();
    }

    private void HitEnemies()
    {
        buffer.Clear();
        buffer.AddRange(EnemyBase.Active);

        for (int i = 0; i < buffer.Count; i++)
        {
            EnemyBase e = buffer[i];
            if (e == null || !e.IsTargetable || hits.Contains(e))
                continue;

            float r = radius + e.Radius;
            if (CapyMath.FlatDistanceSq(e.transform.position, transform.position) > r * r)
                continue;

            DamageInfo info = new DamageInfo();
            info.amount = damage;
            info.direction = direction;
            info.knockback = knockback;
            info.crit = crit;
            info.burnLevel = burn;
            e.TakeDamage(info);
            hits.Add(e);

            if (pierce > 0)
            {
                pierce--;
                continue;
            }

            if (ricochet > 0)
            {
                EnemyBase next = EnemyBase.FindNearest(transform.position, 9f, hits);
                if (next != null)
                {
                    ricochet--;
                    direction = CapyMath.DirectionTo(transform.position, next.transform.position);
                    age = Mathf.Min(age, lifetime * 0.4f);
                    VFX.Sparks(transform.position, impactColor, 6, 4f);
                    return;
                }
            }

            Despawn(true);
            return;
        }
    }

    private void HitPlayer()
    {
        PlayerController p = PlayerController.I;
        if (p == null || p.Health.IsDead)
            return;

        float r = radius + p.Radius * 0.8f;
        if (CapyMath.FlatDistanceSq(p.transform.position, transform.position) > r * r)
            return;

        // Saat dash (i-frame) proyektil menembus Player.
        if (!p.Health.CanBeHit)
            return;

        p.Health.TakeDamage(damage, transform.position - direction);
        Despawn(true);
    }

    private void Despawn(bool impact)
    {
        if (impact)
        {
            VFX.Sparks(transform.position, impactColor, 8, 5f);
            VFX.Glow(transform.position, impactColor.WithAlpha(0.8f), 0.9f, 0.18f, Vector3.zero);
        }
        else
        {
            VFX.Poof(transform.position, impactColor.WithAlpha(0.4f), 3, 0.4f);
        }
        Destroy(gameObject);
    }
}
