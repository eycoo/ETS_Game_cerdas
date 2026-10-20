using System.Collections;
using UnityEngine;

// Serangan otomatis ala Archero: saat Capy DIAM, ia mengayunkan pedang ke musuh terdekat
// dan melepaskan gelombang tebasan. Saat bergerak, Capy tidak menyerang (fokus menghindar).
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float targetRange = 20f;
    [SerializeField] private float firstAttackDelay = 0.12f;

    public EnemyBase Target { get; private set; }
    public bool IsAttacking { get; private set; }

    private PlayerController player;
    private float nextAttackTime;
    private bool wasMoving = true;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        Target = null;
        IsAttacking = false;

        if (!player.CanAct || player.Health.IsDead)
            return;

        if (player.IsMoving || player.IsDashing)
        {
            wasMoving = true;
            return;
        }

        Target = EnemyBase.FindTarget(transform.position, targetRange);
        if (Target == null)
            return;

        IsAttacking = true;
        player.SetFacing(Target.transform.position - transform.position);

        // Jeda kecil setelah berhenti, tapi tetap menghormati kecepatan serang (anti spam berhenti-jalan).
        if (wasMoving)
        {
            wasMoving = false;
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + firstAttackDelay);
        }

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + player.Stats.AttackInterval;
            StartCoroutine(Volley(Target));
        }
    }

    private IEnumerator Volley(EnemyBase target)
    {
        int volleys = 1 + player.Stats.Multishot;
        for (int v = 0; v < volleys; v++)
        {
            if (!player.CanAct || player.Health.IsDead)
                yield break;

            Vector3 dir;
            if (target != null && target.IsTargetable)
            {
                dir = CapyMath.DirectionTo(transform.position, target.transform.position);
            }
            else
            {
                EnemyBase other = EnemyBase.FindTarget(transform.position, targetRange);
                dir = other != null ? CapyMath.DirectionTo(transform.position, other.transform.position) : player.Facing;
            }

            FirePattern(dir);
            player.Visual.OnAttack();
            AudioManager.Sfx("swing", 0.55f, 1f + v * 0.1f);

            if (v < volleys - 1)
                yield return new WaitForSeconds(0.1f);
        }
    }

    private void FirePattern(Vector3 dir)
    {
        PlayerStats s = player.Stats;
        Vector3 origin = transform.position + Vector3.up * 0.8f + dir * 0.55f;
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

        for (int i = 0; i < s.Front; i++)
        {
            float offset = (i - (s.Front - 1) * 0.5f) * 0.5f;
            Spawn(origin + right * offset, dir);
        }

        for (int p = 1; p <= s.DiagonalPairs; p++)
        {
            float angle = 8f + 22f * p;
            Spawn(origin, Quaternion.Euler(0f, angle, 0f) * dir);
            Spawn(origin, Quaternion.Euler(0f, -angle, 0f) * dir);
        }

        for (int r = 0; r < s.Rear; r++)
            Spawn(transform.position + Vector3.up * 0.8f - dir * 0.55f, -dir);
    }

    private void Spawn(Vector3 pos, Vector3 dir)
    {
        // Jangan sampai gelombang lahir di dalam tembok saat Capy menempel dinding.
        Vector3 center = transform.position + Vector3.up * 0.8f;
        Vector3 to = pos - center;
        RaycastHit hit;
        if (to.sqrMagnitude > 0.0001f && Physics.Raycast(center, to.normalized, out hit, to.magnitude, Layers.ObstacleMask, QueryTriggerInteraction.Ignore))
            pos = center + to.normalized * Mathf.Max(0f, hit.distance - 0.1f);

        PlayerStats s = player.Stats;
        GameObject go = Instantiate(GameDatabase.I.swordWave, pos, Quaternion.LookRotation(dir));
        Projectile p = go.GetComponent<Projectile>();
        bool crit = Random.value < s.CritChance;
        float dmg = s.Damage * (crit ? s.CritMultiplier : 1f);
        p.Launch(Team.Player, dir, s.ProjectileSpeed, dmg, crit, s.Pierce, s.Ricochet, s.BurnLevel);
    }
}
