using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// GOLEM KING (boss) — teknik AI: BEHAVIOR TREE dengan dua fase.
//
// Selector
//  |- Sequence: [sedang menyerang?]          -> tunggu (Running)
//  |- Sequence: [HP <= 50% dan belum marah?] -> Enrage (fase 2)
//  |- Sequence: [Capy dekat?]                -> CooldownGate(Ground Slam)
//  |- Sequence: [fase 2?]                    -> CooldownGate(Rock Rain)
//  |- Sequence: [Capy jauh & terlihat?]      -> CooldownGate(Charge)
//  |- Sequence: [Capy agak jauh?]            -> CooldownGate(Rock Throw)
//  |- Chase (steering Seek + hindari tembok)
//
// Charge yang menabrak tembok membuat Golem pusing (STUNNED) dan menerima damage 1.5x.
public class GolemBossAI : EnemyBase
{
    [Header("Golem Boss")]
    [SerializeField] private float slamRange = 4.2f;
    [SerializeField] private float slamRadius = 3.6f;
    [SerializeField] private float slamDamage = 22f;
    [SerializeField] private float slamWindup = 0.95f;
    [SerializeField] private float chargeSpeed = 15f;
    [SerializeField] private float chargeWindup = 0.85f;
    [SerializeField] private float chargeDamage = 25f;
    [SerializeField] private float chargeMaxDistance = 18f;
    [SerializeField] private float stunTime = 1.8f;
    [SerializeField] private float rockDamage = 15f;
    [SerializeField] private float slamCooldown = 3.2f;
    [SerializeField] private float chargeCooldown = 7f;
    [SerializeField] private float throwCooldown = 4.2f;
    [SerializeField] private float rainCooldown = 9f;

    [Header("Parts")]
    [SerializeField] private Transform body;
    [SerializeField] private Transform armL;
    [SerializeField] private Transform armR;
    [SerializeField] private Transform legL;
    [SerializeField] private Transform legR;
    [SerializeField] private Transform head;
    [SerializeField] private Renderer[] glowRenderers;
    [SerializeField] private Material enragedMaterial;
    [SerializeField] private Material dormantMaterial;
    [SerializeField] private Material awakeMaterial;
    [SerializeField] private Light coreLight;

    private BTNode tree;
    private string currentAction = "DORMANT";
    private bool busy;
    private bool awake;
    private bool enraged;
    private bool stunned;
    private float walkPhase;
    private float armRaiseL;
    private float armRaiseR;
    private float lean;
    private float shake;
    private Vector3 chargeDir;
    private float chargeLength;
    private readonly List<Telegraph> telegraphs = new List<Telegraph>();

    public override bool IsBoss => true;
    public override bool IsTargetable => base.IsTargetable && awake;
    public override string AITechnique => "BEHAVIOR TREE";
    public override string AIState => currentAction;
    public override Color AIColor => enraged ? new Color(1f, 0.4f, 0.3f) : new Color(0.5f, 0.9f, 1f);
    public bool Enraged => enraged;
    public bool IsAwake => awake;
    private float SpeedMul => enraged ? 1.3f : 1f;

    protected override float SpawnDuration => 0.01f;

    protected override void Awake()
    {
        base.Awake();
        if (body == null) body = visualRoot.Find("Body");
        if (body != null)
        {
            if (armL == null) armL = body.Find("ArmL");
            if (armR == null) armR = body.Find("ArmR");
            if (legL == null) legL = body.Find("LegL");
            if (legR == null) legR = body.Find("LegR");
            if (head == null) head = body.Find("Head");
        }
        aiFrozen = true;
        SetGlow(dormantMaterial, new Color(0.3f, 0.5f, 0.6f), 0.5f);
    }

    private void SetGlow(Material mat, Color lightColor, float intensity)
    {
        if (mat != null && glowRenderers != null)
        {
            foreach (Renderer r in glowRenderers)
            {
                if (r != null)
                    SetMaterial(r, mat);
            }
        }
        if (coreLight != null)
        {
            coreLight.color = lightColor;
            coreLight.intensity = intensity;
        }
    }

    // Dipanggil RoomController saat intro boss.
    public IEnumerator WakeUp()
    {
        currentAction = "WAKING UP";
        AudioManager.Sfx("rumble", 0.8f);
        float t = 0f;
        while (t < 1.0f)
        {
            t += Time.deltaTime;
            shake = 0.06f;
            CameraRig.Shake(0.02f);
            yield return null;
        }
        SetGlow(awakeMaterial, new Color(0.5f, 0.9f, 1f), 6f);
        VFX.Sparkle(transform.position + Vector3.up * 3f, new Color(0.5f, 0.9f, 1f), 30);
        AudioManager.Sfx("roar", 0.9f);
        CameraRig.Shake(0.8f);

        t = 0f;
        while (t < 1.0f)
        {
            t += Time.deltaTime;
            armRaiseL = armRaiseR = Mathf.Sin(Mathf.Clamp01(t / 1f) * Mathf.PI) * 1.2f;
            shake = 0.04f;
            yield return null;
        }
        armRaiseL = armRaiseR = 0f;
        shake = 0f;

        awake = true;
        aiFrozen = false;
        currentAction = "CHASE";
        BuildTree();
    }

    private void BuildTree()
    {
        CooldownGate slamGate = new CooldownGate(new ActionNode(StartSlam), slamCooldown, 0.5f);
        CooldownGate chargeGate = new CooldownGate(new ActionNode(StartCharge), chargeCooldown, 4f);
        CooldownGate throwGate = new CooldownGate(new ActionNode(StartThrow), throwCooldown, 1.2f);
        CooldownGate rainGate = new CooldownGate(new ActionNode(StartRockRain), rainCooldown, 2f);

        tree = new SelectorNode(new List<BTNode>
        {
            new SequenceNode(new List<BTNode> { new ConditionNode(() => busy), new ActionNode(() => NodeState.Running) }),
            new SequenceNode(new List<BTNode> { new ConditionNode(() => !enraged && Health <= maxHealth * 0.5f), new ActionNode(StartEnrage) }),
            new SequenceNode(new List<BTNode> { new ConditionNode(() => DistanceToPlayer() < slamRange), slamGate }),
            new SequenceNode(new List<BTNode> { new ConditionNode(() => enraged), rainGate }),
            new SequenceNode(new List<BTNode> { new ConditionNode(() => DistanceToPlayer() > 5.5f && CanSeePlayer()), chargeGate }),
            new SequenceNode(new List<BTNode> { new ConditionNode(() => DistanceToPlayer() > 4f), throwGate }),
            new ActionNode(Chase)
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

    // ------------------------------------------------------------ aksi

    private NodeState Chase()
    {
        currentAction = "CHASE";
        Vector3 pos = transform.position;
        Vector3 toPlayer = CapyMath.DirectionTo(pos, Player.transform.position);
        Vector3 steer = Steering.Seek(pos, Player.transform.position, moveSpeed * SpeedMul) + Steering.AvoidWalls(pos, toPlayer, 2.5f) * moveSpeed;
        desiredVelocity = Vector3.ClampMagnitude(steer, moveSpeed * SpeedMul);
        FaceTowards(toPlayer, 5f);
        return NodeState.Running;
    }

    private NodeState StartSlam()
    {
        StartCoroutine(SlamRoutine());
        return NodeState.Success;
    }

    private IEnumerator SlamRoutine()
    {
        busy = true;
        currentAction = "GROUND SLAM";
        desiredVelocity = Vector3.zero;
        float windup = slamWindup / SpeedMul;
        Telegraph tg = Track(Telegraph.Circle(transform.position, slamRadius, windup, new Color(1f, 0.25f, 0.15f)));

        float t = 0f;
        while (t < windup)
        {
            t += Time.deltaTime;
            float k = t / windup;
            armRaiseL = armRaiseR = k * 1.4f;
            lean = -10f * k;
            shake = k * 0.04f;
            FaceTowards(Player.transform.position - transform.position, 6f);
            yield return null;
        }
        Untrack(tg);

        armRaiseL = armRaiseR = -0.4f;
        lean = 14f;
        shake = 0f;
        Vector3 pos = transform.position;
        CameraRig.Shake(0.6f);
        AudioManager.Sfx("slam", 0.9f);
        VFX.Debris(pos + Vector3.up * 0.3f, new Color(0.55f, 0.55f, 0.6f), 18, 8f);
        VFX.Poof(pos + Vector3.up * 0.3f, new Color(0.7f, 0.68f, 0.65f, 0.8f), 18, 1.6f);
        VFX.Ring(pos, new Color(1f, 0.85f, 0.6f), slamRadius * 1.1f, 0.4f);
        if (CapyMath.FlatDistance(Player.transform.position, pos) < slamRadius + Player.Radius * 0.5f)
            Player.Health.TakeDamage(slamDamage * damageScale, pos);
        if (enraged)
            Shockwave.Spawn(pos, 13f, 7.5f, 15f * damageScale, new Color(1f, 0.55f, 0.25f));

        yield return new WaitForSeconds(0.65f / SpeedMul);
        armRaiseL = armRaiseR = 0f;
        lean = 0f;
        busy = false;
    }

    private NodeState StartCharge()
    {
        StartCoroutine(ChargeRoutine());
        return NodeState.Success;
    }

    private float MeasureCharge(Vector3 dir)
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        RaycastHit hit;
        if (Physics.SphereCast(origin, radius * 0.8f, dir, out hit, chargeMaxDistance, Layers.ObstacleMask, QueryTriggerInteraction.Ignore))
            return Mathf.Max(1f, hit.distance);
        return chargeMaxDistance;
    }

    private IEnumerator ChargeRoutine()
    {
        busy = true;
        currentAction = "CHARGE";
        desiredVelocity = Vector3.zero;
        chargeDir = CapyMath.DirectionTo(transform.position, Player.transform.position);
        chargeLength = MeasureCharge(chargeDir);
        float windup = chargeWindup / SpeedMul;
        Telegraph tg = Track(Telegraph.Rect(transform.position, chargeDir, chargeLength + radius, radius * 2f + 0.4f, windup, new Color(1f, 0.25f, 0.15f)));
        AudioManager.Sfx("rumble", 0.6f);

        float t = 0f;
        while (t < windup)
        {
            t += Time.deltaTime;
            if (t < windup * 0.55f)
            {
                chargeDir = CapyMath.DirectionTo(transform.position, Player.transform.position);
                chargeLength = MeasureCharge(chargeDir);
                tg.SetLine(transform.position, chargeDir);
            }
            FaceTowards(chargeDir, 14f);
            lean = -15f * (t / windup);
            shake = 0.05f;
            yield return null;
        }
        Untrack(tg);

        AudioManager.Sfx("charge", 0.8f);
        lean = 20f;
        shake = 0f;
        float traveled = 0f;
        bool hitPlayer = false;
        bool hitWall = false;
        float speed = chargeSpeed * SpeedMul;
        float dustTimer = 0f;

        while (traveled < chargeLength + 0.5f && t < 4f)
        {
            float dt = Time.deltaTime;
            t += dt;
            RaycastHit hit;
            if (Physics.SphereCast(transform.position + Vector3.up, radius * 0.8f, chargeDir, out hit, speed * dt + 0.25f, Layers.ObstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitWall = true;
                break;
            }

            desiredVelocity = chargeDir * speed;
            traveled += speed * dt;

            if (!hitPlayer && CapyMath.FlatDistance(Player.transform.position, transform.position) < radius + Player.Radius + 0.2f)
            {
                hitPlayer = true;
                Vector3 side = Vector3.Cross(Vector3.up, chargeDir);
                if (Vector3.Dot(Player.transform.position - transform.position, side) < 0f)
                    side = -side;
                Player.Health.TakeDamage(chargeDamage * damageScale, transform.position);
                Player.AddKnockback(side * 10f + chargeDir * 6f);
            }

            dustTimer -= dt;
            if (dustTimer <= 0f)
            {
                dustTimer = 0.05f;
                VFX.Poof(transform.position, new Color(0.7f, 0.65f, 0.6f, 0.6f), 2, 1f);
            }
            walkPhase += dt * 18f;
            yield return null;
        }

        desiredVelocity = Vector3.zero;
        lean = 0f;

        if (hitWall || traveled >= chargeLength - 0.5f)
        {
            CameraRig.Shake(0.7f);
            AudioManager.Sfx("slam", 0.8f, 0.8f);
            Vector3 front = transform.position + chargeDir * radius + Vector3.up;
            VFX.Debris(front, new Color(0.55f, 0.55f, 0.6f), 16, 7f);
            VFX.Poof(front, new Color(0.7f, 0.68f, 0.65f, 0.8f), 12, 1.3f);
            yield return StunRoutine();
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
        }
        busy = false;
    }

    private IEnumerator StunRoutine()
    {
        stunned = true;
        currentAction = "STUNNED!";
        damageTakenMultiplier = 1.5f;
        if (GameUI.I != null)
            GameUI.I.ShowToast("GOLEM IS STUNNED!  ATTACK!", new Color(1f, 0.9f, 0.4f));

        float t = 0f;
        float starTimer = 0f;
        while (t < stunTime)
        {
            t += Time.deltaTime;
            starTimer -= Time.deltaTime;
            if (starTimer <= 0f)
            {
                starTimer = 0.12f;
                float a = t * 8f;
                Vector3 p = transform.position + Vector3.up * 5.2f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.9f;
                VFX.Sparkle(p, new Color(1f, 0.9f, 0.3f), 1);
            }
            lean = Mathf.Sin(t * 6f) * 6f;
            yield return null;
        }

        damageTakenMultiplier = 1f;
        stunned = false;
        lean = 0f;
    }

    private NodeState StartThrow()
    {
        StartCoroutine(ThrowRoutine());
        return NodeState.Success;
    }

    private IEnumerator ThrowRoutine()
    {
        busy = true;
        currentAction = "ROCK THROW";
        desiredVelocity = Vector3.zero;

        float t = 0f;
        while (t < 0.5f / SpeedMul)
        {
            t += Time.deltaTime;
            armRaiseR = t / (0.5f / SpeedMul) * 1.5f;
            FaceTowards(Player.transform.position - transform.position, 10f);
            yield return null;
        }

        int count = enraged ? 5 : 3;
        for (int i = 0; i < count; i++)
        {
            Vector3 target = Player.transform.position;
            if (i == 0)
                target += Player.Velocity.Flat() * 0.6f;
            else
                target += new Vector3(Random.Range(-2.6f, 2.6f), 0f, Random.Range(-2.6f, 2.6f));
            if (Room != null)
                target = Room.ClampInside(target, 1f);

            Vector3 hand = armR != null ? armR.position + Vector3.up * 0.5f : transform.position + Vector3.up * 3f;
            ThrowRock(hand, target, 1.0f + i * 0.08f, 4.5f);
            armRaiseR = 1.5f - (i + 1) / (float)count * 1.8f;
            yield return new WaitForSeconds(0.12f);
        }
        AudioManager.Sfx("throw", 0.7f);

        yield return new WaitForSeconds(0.45f / SpeedMul);
        armRaiseR = 0f;
        busy = false;
    }

    private void ThrowRock(Vector3 from, Vector3 to, float flightTime, float height)
    {
        GameObject prefab = GameDatabase.I.rock;
        if (prefab == null)
            return;
        GameObject go = Instantiate(prefab, from, Random.rotation);
        go.GetComponent<LobProjectile>().Launch(from, to, flightTime, height, rockDamage * damageScale);
    }

    private NodeState StartRockRain()
    {
        StartCoroutine(RockRainRoutine());
        return NodeState.Success;
    }

    private IEnumerator RockRainRoutine()
    {
        busy = true;
        currentAction = "ROCK RAIN";
        desiredVelocity = Vector3.zero;
        AudioManager.Sfx("roar", 0.6f, 1.15f);

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            armRaiseL = armRaiseR = Mathf.Sin(t / 0.6f * Mathf.PI) * 1.3f;
            shake = 0.05f;
            CameraRig.Shake(0.02f);
            yield return null;
        }
        shake = 0f;
        armRaiseL = armRaiseR = 0f;

        for (int i = 0; i < 11; i++)
        {
            Vector3 target;
            if (i < 3)
                target = Player.transform.position + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            else if (Room != null)
                target = Room.transform.position + new Vector3(Random.Range(-Room.size.x * 0.45f, Room.size.x * 0.45f), 0f, Random.Range(-Room.size.y * 0.45f, Room.size.y * 0.45f));
            else
                target = transform.position + Random.insideUnitSphere.Flat() * 8f;
            if (Room != null)
                target = Room.ClampInside(target, 1f);

            ThrowRock(target + Vector3.up * 14f, target, 1.25f + Random.Range(0f, 0.4f), 0f);
            yield return new WaitForSeconds(0.06f);
        }

        yield return new WaitForSeconds(0.6f);
        busy = false;
    }

    private NodeState StartEnrage()
    {
        StartCoroutine(EnrageRoutine());
        return NodeState.Success;
    }

    private IEnumerator EnrageRoutine()
    {
        busy = true;
        enraged = true;
        currentAction = "ENRAGE!";
        desiredVelocity = Vector3.zero;
        damageTakenMultiplier = 0.2f;

        if (GameUI.I != null)
            GameUI.I.ShowBanner("THE GOLEM KING IS ENRAGED!", "Shockwaves incoming. DASH through them!", new Color(1f, 0.35f, 0.25f), 2.4f);
        AudioManager.Sfx("roar", 1f, 0.85f);
        if (GameManager.I != null)
            GameManager.I.HitStop(0.12f);
        CameraRig.Shake(1f);
        SetGlow(enragedMaterial, new Color(1f, 0.3f, 0.2f), 4.5f);

        float t = 0f;
        while (t < 1.4f)
        {
            t += Time.deltaTime;
            armRaiseL = armRaiseR = 1.4f;
            shake = 0.07f;
            if (Random.value < 18f * Time.deltaTime)
                VFX.Fire(transform.position + Vector3.up * Random.Range(1f, 4f) + Random.insideUnitSphere.Flat());
            yield return null;
        }
        VFX.Ring(transform.position, new Color(1f, 0.4f, 0.2f), 6f, 0.5f);
        Shockwave.Spawn(transform.position, 13f, 7.5f, 15f * damageScale, new Color(1f, 0.55f, 0.25f));

        shake = 0f;
        armRaiseL = armRaiseR = 0f;
        damageTakenMultiplier = 1f;
        busy = false;
    }

    // ------------------------------------------------------------ visual

    private void Animate(float dt)
    {
        float speed01 = Mathf.Clamp01((rb != null ? rb.linearVelocity.Flat().magnitude : 0f) / Mathf.Max(0.1f, moveSpeed));
        walkPhase += dt * 6f * speed01;

        if (body != null)
        {
            Vector3 jitter = shake > 0f ? Random.insideUnitSphere * shake : Vector3.zero;
            body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(walkPhase)) * 0.12f * speed01, 0f) + jitter;
            body.localRotation = Quaternion.Slerp(body.localRotation, Quaternion.Euler(lean, 0f, Mathf.Sin(walkPhase) * 3f * speed01), 1f - Mathf.Exp(-10f * dt));
        }
        if (legL != null)
        {
            legL.localRotation = Quaternion.Euler(Mathf.Sin(walkPhase) * 22f * speed01, 0f, 0f);
            legR.localRotation = Quaternion.Euler(-Mathf.Sin(walkPhase) * 22f * speed01, 0f, 0f);
        }
        if (armL != null)
        {
            float swing = Mathf.Sin(walkPhase) * 18f * speed01;
            armL.localRotation = Quaternion.Slerp(armL.localRotation, Quaternion.Euler(-armRaiseL * 100f - swing, 0f, 0f), 1f - Mathf.Exp(-14f * dt));
            armR.localRotation = Quaternion.Slerp(armR.localRotation, Quaternion.Euler(-armRaiseR * 100f + swing, 0f, 0f), 1f - Mathf.Exp(-14f * dt));
        }
        if (head != null && stunned)
            head.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 5f) * 15f, Mathf.Sin(Time.time * 7f) * 8f);
        else if (head != null)
            head.localRotation = Quaternion.identity;
    }

    private Telegraph Track(Telegraph tg)
    {
        telegraphs.Add(tg);
        return tg;
    }

    private void Untrack(Telegraph tg)
    {
        telegraphs.Remove(tg);
        if (tg != null)
            Destroy(tg.gameObject);
    }

    // ------------------------------------------------------------ mati

    protected override void OnDeath()
    {
        StopAllCoroutines();
        foreach (Telegraph tg in telegraphs)
        {
            if (tg != null)
                Destroy(tg.gameObject);
        }
        telegraphs.Clear();
        currentAction = "DEFEATED";

        if (GameManager.I != null)
        {
            GameManager.I.SlowMotion(0.25f, 2.2f);
            GameManager.I.HitStop(0.15f);
        }
        if (GameUI.I != null)
            GameUI.I.HideBossBar();
        AudioManager.Music(MusicTrack.None, 0.5f);
        AudioManager.Sfx("boss_die", 1f);
    }

    protected override IEnumerator DeathRoutine()
    {
        float t = 0f;
        Vector3 start = transform.position;
        while (t < 2.0f)
        {
            t += Time.deltaTime;
            if (body != null)
                body.localPosition = Random.insideUnitSphere * 0.12f + Vector3.down * t * 0.8f;
            if (Random.value < 20f * Time.deltaTime)
            {
                Vector3 p = start + Vector3.up * Random.Range(0.5f, 4.5f) + Random.insideUnitSphere.Flat() * 1.5f;
                VFX.Debris(p, new Color(0.55f, 0.55f, 0.6f), 3, 5f);
                VFX.Sparkle(p, new Color(0.5f, 0.9f, 1f), 2);
            }
            yield return null;
        }

        VFX.Debris(start + Vector3.up * 2f, new Color(0.55f, 0.55f, 0.6f), 40, 10f);
        VFX.Poof(start + Vector3.up * 1.5f, new Color(0.75f, 0.72f, 0.7f, 0.9f), 30, 2.2f);
        VFX.Ring(start, new Color(1f, 0.85f, 0.4f), 8f, 0.6f);
        CameraRig.Shake(1f);
        AudioManager.Sfx("slam", 1f, 0.7f);
        GoldenYuzu.Spawn(start);
        Destroy(gameObject);
    }

    public override void DrawAIDebug()
    {
        if (!awake)
            return;
        Vector3 p = transform.position + Vector3.up * 0.1f;
        AIDebugDraw.Circle(p, slamRange, new Color(1f, 0.4f, 0.3f, 0.35f), 0.06f, 48);
        AIDebugDraw.Circle(p, 5.5f, new Color(1f, 0.8f, 0.3f, 0.2f), 0.05f, 48);
        if (currentAction == "CHARGE")
            AIDebugDraw.Line(p, p + chargeDir * chargeLength, new Color(1f, 0.3f, 0.2f, 0.9f), 0.12f);
        else
            AIDebugDraw.Line(p, Player.transform.position + Vector3.up * 0.1f, new Color(0.5f, 0.9f, 1f, 0.4f), 0.05f);
    }
}
