using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// WIZARD — teknik AI: BEHAVIOR TREE (memakai framework BT dari Praktikum 06).
//
// Selector
//  |- Sequence: [sedang sibuk?] -> tunggu (Running)
//  |- Sequence: [Capy terlalu dekat?] -> CooldownGate(Teleport)
//  |- Sequence: [pengikut kurang?]    -> CooldownGate(Summon slime)
//  |- Sequence: [Capy terlihat?]      -> CooldownGate(Cast orb sihir)
//  |- Reposition (steering: jaga jarak + strafe)
public class WizardAI : EnemyBase
{
    [Header("Wizard BT")]
    [SerializeField] private float preferredDistance = 7f;
    [SerializeField] private float tooCloseDistance = 3.8f;
    [SerializeField] private float castCooldown = 2.6f;
    [SerializeField] private float teleportCooldown = 4.5f;
    [SerializeField] private float summonCooldown = 13f;
    [SerializeField] private int maxSummons = 2;
    [SerializeField] private float orbSpeed = 7.5f;
    [SerializeField] private float orbDamage = 12f;
    [SerializeField] private int orbCount = 3;
    [SerializeField] private Transform body;
    [SerializeField] private Transform staffOrb;

    private BTNode tree;
    private string currentAction = "IDLE";
    private bool busy;
    private bool hidden;
    private float strafeSign = 1f;
    private float strafeFlipTimer;
    private float floatPhase;
    private float orbGlow;
    private Vector3 teleportTarget;
    private Vector3 baseOrbScale = Vector3.one;
    private readonly List<EnemyBase> summons = new List<EnemyBase>();

    public override bool IsTargetable => base.IsTargetable && !hidden;
    public override string AITechnique => "BEHAVIOR TREE";
    public override string AIState => currentAction;
    public override Color AIColor => new Color(0.85f, 0.6f, 1f);

    protected override void Awake()
    {
        base.Awake();
        if (body == null) body = visualRoot.Find("Body");
        if (staffOrb == null && body != null) staffOrb = body.Find("Staff/Orb");
        if (staffOrb != null) baseOrbScale = staffOrb.localScale;
        floatPhase = Random.Range(0f, 6f);
    }

    protected override void OnSpawned()
    {
        CooldownGate castGate = new CooldownGate(new ActionNode(StartCast), castCooldown, 1.0f);
        CooldownGate teleportGate = new CooldownGate(new ActionNode(StartTeleport), teleportCooldown, 1.5f);
        CooldownGate summonGate = new CooldownGate(new ActionNode(StartSummon), summonCooldown, 3.5f);

        tree = new SelectorNode(new List<BTNode>
        {
            new SequenceNode(new List<BTNode> { new ConditionNode(() => busy), new ActionNode(() => NodeState.Running) }),
            new SequenceNode(new List<BTNode> { new ConditionNode(PlayerTooClose), teleportGate }),
            new SequenceNode(new List<BTNode> { new ConditionNode(NeedsSummons), summonGate }),
            new SequenceNode(new List<BTNode> { new ConditionNode(CanSeePlayer), castGate }),
            new ActionNode(Reposition)
        });
    }

    protected override void Think(float dt)
    {
        if (tree != null)
            tree.Tick();
        Animate(dt);
    }

    protected override void IdleAnimation(float dt)
    {
        Animate(dt);
    }

    // ------------------------------------------------------------ kondisi

    private bool PlayerTooClose()
    {
        return DistanceToPlayer() < tooCloseDistance;
    }

    private bool NeedsSummons()
    {
        summons.RemoveAll(s => s == null || !s.IsAlive);
        return summons.Count < maxSummons && GameDatabase.I.slimeSmall != null;
    }

    // ------------------------------------------------------------ aksi

    private NodeState StartCast()
    {
        StartCoroutine(CastRoutine());
        return NodeState.Success;
    }

    private IEnumerator CastRoutine()
    {
        busy = true;
        currentAction = "CAST SPELL";
        desiredVelocity = Vector3.zero;
        AudioManager.Sfx("magic_charge", 0.45f);

        float t = 0f;
        while (t < 0.65f)
        {
            if (isDead) yield break;
            t += Time.deltaTime;
            orbGlow = t / 0.65f;
            FaceTowards(Player.transform.position - transform.position, 15f);
            if (staffOrb != null && Random.value < 30f * Time.deltaTime)
                VFX.Glow(staffOrb.position, new Color(0.8f, 0.45f, 1f, 0.9f), 0.35f, 0.25f, Random.insideUnitSphere);
            yield return null;
        }

        Vector3 dir = CapyMath.DirectionTo(transform.position, Player.transform.position);
        Vector3 origin = (staffOrb != null ? staffOrb.position : transform.position + Vector3.up * 1.2f);
        origin.y = 0.9f;
        GameObject prefab = GameDatabase.I.magicOrb;
        for (int i = 0; i < orbCount && prefab != null; i++)
        {
            float angle = (i - (orbCount - 1) * 0.5f) * 16f;
            Vector3 d = Quaternion.Euler(0f, angle, 0f) * dir;
            GameObject go = Instantiate(prefab, origin + d * 0.4f, Quaternion.LookRotation(d));
            go.GetComponent<Projectile>().Launch(Team.Enemy, d, orbSpeed, orbDamage * damageScale);
        }
        AudioManager.Sfx("magic_cast", 0.6f);
        orbGlow = 0f;

        yield return new WaitForSeconds(0.3f);
        busy = false;
    }

    private NodeState StartTeleport()
    {
        if (Room == null || Room.Grid == null)
            return NodeState.Failure;

        Vector3 player = Player.transform.position;
        Vector3 point;
        bool found = Room.Grid.BestPoint(p =>
        {
            float score = -Mathf.Abs(CapyMath.FlatDistance(p, player) - (preferredDistance + 1f));
            if (CapyMath.HasLineOfSight(p, player, 0.2f))
                score += 2f;
            return score + Random.value;
        }, 30, out point);

        if (!found)
            return NodeState.Failure;

        teleportTarget = point;
        StartCoroutine(TeleportRoutine());
        return NodeState.Success;
    }

    private IEnumerator TeleportRoutine()
    {
        busy = true;
        currentAction = "TELEPORT";
        desiredVelocity = Vector3.zero;
        AudioManager.Sfx("teleport", 0.6f);
        Color purple = new Color(0.75f, 0.45f, 1f);
        VFX.Sparkle(transform.position + Vector3.up, purple, 18);
        VFX.Poof(transform.position + Vector3.up * 0.8f, purple.WithAlpha(0.6f), 8, 0.9f);

        yield return ScaleBody(1f, 0f, 0.18f);
        hidden = true;
        if (rb != null)
            rb.position = teleportTarget;
        transform.position = teleportTarget;
        yield return new WaitForSeconds(0.15f);
        hidden = false;
        VFX.Sparkle(teleportTarget + Vector3.up, purple, 18);
        VFX.Ring(teleportTarget, purple, 1.6f, 0.3f);
        yield return ScaleBody(0f, 1f, 0.18f);

        busy = false;
    }

    private IEnumerator ScaleBody(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (isDead) yield break;
            t += Time.deltaTime;
            float k = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            if (body != null)
                body.localScale = new Vector3(k, Mathf.Lerp(from, to, Mathf.Clamp01(t / duration * 0.8f)) * 1.2f, k);
            yield return null;
        }
        if (body != null)
            body.localScale = Vector3.one * to;
    }

    private NodeState StartSummon()
    {
        StartCoroutine(SummonRoutine());
        return NodeState.Success;
    }

    private IEnumerator SummonRoutine()
    {
        busy = true;
        currentAction = "SUMMON";
        desiredVelocity = Vector3.zero;
        AudioManager.Sfx("summon", 0.5f);

        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < 2; i++)
        {
            Vector3 offset = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(1.5f, 2.5f);
            Vector3 p = transform.position + offset;
            if (Room != null)
            {
                p = Room.ClampInside(p, 1.2f);
                if (Room.Grid != null && !Room.Grid.IsWalkable(p))
                    p = transform.position;
            }
            points.Add(p);
            Telegraph.Spawn(p, 0.8f, 0.6f, new Color(0.7f, 0.4f, 1f));
        }

        float t = 0f;
        while (t < 0.6f)
        {
            if (isDead) yield break;
            t += Time.deltaTime;
            orbGlow = t / 0.6f;
            yield return null;
        }

        foreach (Vector3 p in points)
        {
            GameObject go = Instantiate(GameDatabase.I.slimeSmall, p, Quaternion.identity);
            EnemyBase e = go.GetComponent<EnemyBase>();
            summons.Add(e);
            if (Room != null)
                Room.RegisterEnemy(e);
            VFX.Poof(p + Vector3.up * 0.3f, new Color(0.7f, 0.45f, 1f, 0.6f), 6, 0.7f);
        }
        orbGlow = 0f;
        yield return new WaitForSeconds(0.3f);
        busy = false;
    }

    private NodeState Reposition()
    {
        currentAction = "REPOSITION";
        Vector3 pos = transform.position;
        Vector3 toPlayer = CapyMath.DirectionTo(pos, Player.transform.position);
        float d = DistanceToPlayer();

        Vector3 radial = Vector3.zero;
        if (d < preferredDistance - 1f) radial = -toPlayer;
        else if (d > preferredDistance + 1f) radial = toPlayer;

        strafeFlipTimer -= Time.deltaTime;
        if (strafeFlipTimer <= 0f)
        {
            strafeFlipTimer = Random.Range(1.5f, 3f);
            strafeSign = Random.value < 0.5f ? -1f : 1f;
        }
        Vector3 strafe = Vector3.Cross(Vector3.up, toPlayer) * strafeSign;
        Vector3 wish = radial + strafe * 0.7f;
        Vector3 avoid = Steering.AvoidWalls(pos, wish, 1.6f);
        if (Vector3.Dot(avoid, strafe) < -0.3f)
            strafeSign = -strafeSign;
        Vector3 separation = Steering.Separation(this, 1.2f, Active);

        Vector3 v = wish + avoid * 1.5f + separation;
        desiredVelocity = v.sqrMagnitude > 0.01f ? v.normalized * moveSpeed : Vector3.zero;
        FaceTowards(toPlayer, 8f);
        return NodeState.Running;
    }

    // ------------------------------------------------------------ visual

    private void Animate(float dt)
    {
        floatPhase += dt * 2.2f;
        if (body != null && !busy)
            body.localPosition = new Vector3(0f, 0.15f + Mathf.Sin(floatPhase) * 0.12f, 0f);
        else if (body != null && currentAction != "TELEPORT")
            body.localPosition = new Vector3(0f, 0.15f + Mathf.Sin(floatPhase) * 0.12f, 0f);

        if (staffOrb != null)
            staffOrb.localScale = baseOrbScale * (1f + orbGlow * 0.8f + Mathf.Sin(Time.time * 6f) * 0.05f);
    }

    protected override void OnDeath()
    {
        StopAllCoroutines();
    }

    public override void DrawAIDebug()
    {
        Vector3 p = transform.position + Vector3.up * 0.1f;
        AIDebugDraw.Circle(p, tooCloseDistance, new Color(1f, 0.4f, 0.8f, 0.35f), 0.05f, 32);
        AIDebugDraw.Circle(Player.transform.position + Vector3.up * 0.1f, preferredDistance, new Color(0.8f, 0.6f, 1f, 0.12f), 0.04f, 48);
        bool sees = CanSeePlayer();
        AIDebugDraw.Line(p, Player.transform.position + Vector3.up * 0.1f, sees ? new Color(0.8f, 0.6f, 1f, 0.5f) : new Color(1f, 0.3f, 0.3f, 0.35f), 0.04f);
        if (currentAction == "TELEPORT")
            AIDebugDraw.Line(p, teleportTarget + Vector3.up * 0.1f, new Color(1f, 0.6f, 1f, 0.9f), 0.08f);
    }
}
