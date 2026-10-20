using System.Collections;
using UnityEngine;

// Serangan otomatis ala HoloCure: selama ada musuh dalam jangkauan, Capy terus mengayunkan pedang
// dan melepaskan gelombang tebasan, juga sambil berlari. Arahnya mengikuti kursor mouse / stik kanan;
// tanpa bidikan manual (autopilot) arahnya ke musuh terdekat.
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float targetRange = 20f;
    // Penyeimbang serang-sambil-lari: kecepatan serang -30% saat Capy bergerak.
    [SerializeField] private float movingAttackSpeed = 0.7f;
    // Gelombang tambahan dari skill (Twin Slash, Wide Swing, Diagonal, Rear) hanya 50% damage.
    [SerializeField] private float extraWaveDamage = 0.5f;

    public EnemyBase Target { get; private set; }
    public bool IsAttacking { get; private set; }

    private PlayerController player;
    private float nextAttackTime;

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

        if (player.IsDashing)
            return;

        Target = EnemyBase.FindTarget(transform.position, targetRange);
        if (Target == null)
            return;

        IsAttacking = true;
        player.SetFacing(AimDirection(Target));

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + player.Stats.AttackInterval / (player.IsMoving ? movingAttackSpeed : 1f);
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

            FirePattern(AimDirection(target), v == 0);
            player.Visual.OnAttack();
            AudioManager.Sfx("swing", 0.55f, 1f + v * 0.1f);

            if (v < volleys - 1)
                yield return new WaitForSeconds(0.1f);
        }
    }

    private Vector3 AimDirection(EnemyBase target)
    {
        Vector3 dir;
        if (CapyInput.Aim(transform.position, out dir))
            return dir;
        if (target != null && target.IsTargetable)
            return CapyMath.DirectionTo(transform.position, target.transform.position);
        EnemyBase other = EnemyBase.FindTarget(transform.position, targetRange);
        return other != null ? CapyMath.DirectionTo(transform.position, other.transform.position) : player.Facing;
    }

    // Hanya gelombang tengah pada ayunan pertama yang damage penuh; sisanya gelombang tambahan dari skill.
    private void FirePattern(Vector3 dir, bool firstVolley)
    {
        PlayerStats s = player.Stats;
        Vector3 origin = transform.position + Vector3.up * 0.8f + dir * 0.55f;
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        int main = (s.Front - 1) / 2;

        for (int i = 0; i < s.Front; i++)
        {
            float offset = (i - (s.Front - 1) * 0.5f) * 0.5f;
            Spawn(origin + right * offset, dir, firstVolley && i == main ? 1f : extraWaveDamage);
        }

        for (int p = 1; p <= s.DiagonalPairs; p++)
        {
            float angle = 8f + 22f * p;
            Spawn(origin, Quaternion.Euler(0f, angle, 0f) * dir, extraWaveDamage);
            Spawn(origin, Quaternion.Euler(0f, -angle, 0f) * dir, extraWaveDamage);
        }

        for (int r = 0; r < s.Rear; r++)
            Spawn(transform.position + Vector3.up * 0.8f - dir * 0.55f, -dir, extraWaveDamage);
    }

    private void Spawn(Vector3 pos, Vector3 dir, float damageScale)
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
        float dmg = s.Damage * damageScale * (crit ? s.CritMultiplier : 1f);
        p.Launch(Team.Player, dir, s.ProjectileSpeed, dmg, crit, s.Pierce, s.Ricochet, s.BurnLevel);
    }
}
