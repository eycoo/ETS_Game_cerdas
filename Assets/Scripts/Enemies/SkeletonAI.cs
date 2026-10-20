using System.Collections.Generic;
using UnityEngine;

// SKELETON WARRIOR — teknik AI: FINITE STATE MACHINE + PATHFINDING A*.
// State: Patrol -> Chase -> WindUp -> Attack -> Recover -> Chase ...
// Saat Chase, jika Capy terlihat langsung ia berjalan lurus; jika terhalang obstacle
// ia meminta jalur A* dari grid ruangan dan mengikutinya titik demi titik.
public class SkeletonAI : EnemyBase
{
    public enum State
    {
        Patrol,
        Chase,
        WindUp,
        Attack,
        Recover
    }

    [Header("Skeleton FSM")]
    [SerializeField] private float detectRange = 9f;
    [SerializeField] private float attackRange = 1.9f;
    [SerializeField] private float windUpTime = 0.55f;
    [SerializeField] private float lungeSpeed = 9f;
    [SerializeField] private float lungeTime = 0.2f;
    [SerializeField] private float recoverTime = 0.6f;
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float patrolSpeedFactor = 0.45f;
    [SerializeField] private Transform body;
    [SerializeField] private Transform swordPivot;
    [SerializeField] private Transform legL;
    [SerializeField] private Transform legR;

    private State state = State.Patrol;
    private float stateTime;
    private Vector3 lungeDir = Vector3.forward;
    private bool attackHit;
    private bool alerted;
    private readonly List<Vector3> path = new List<Vector3>();
    private int pathIndex;
    private float repathTimer;
    private bool directChase;
    private Vector3 patrolTarget;
    private float walkPhase;
    private Telegraph telegraph;
    private Quaternion swordBase;

    public State CurrentState => state;
    public override string AITechnique => "FSM";
    public override string AIState
    {
        get
        {
            if (state == State.Chase)
                return directChase ? "CHASE  (direct)" : "CHASE  (A* path)";
            return state.ToString().ToUpper();
        }
    }
    public override Color AIColor => new Color(1f, 0.85f, 0.4f);

    protected override void Awake()
    {
        base.Awake();
        if (body == null) body = visualRoot.Find("Body");
        if (swordPivot == null && body != null) swordPivot = body.Find("SwordPivot");
        if (legL == null && body != null) legL = body.Find("LegL");
        if (legR == null && body != null) legR = body.Find("LegR");
        if (swordPivot != null) swordBase = swordPivot.localRotation;
    }

    protected override void OnSpawned()
    {
        PickPatrolTarget();
        ChangeState(State.Patrol);
    }

    private void ChangeState(State next)
    {
        if (telegraph != null)
        {
            Destroy(telegraph.gameObject);
            telegraph = null;
        }

        state = next;
        stateTime = 0f;

        switch (next)
        {
            case State.WindUp:
                lungeDir = CapyMath.DirectionTo(transform.position, Player.transform.position);
                telegraph = Telegraph.Rect(transform.position, lungeDir, attackRange + 1.1f, 1.5f, windUpTime, new Color(1f, 0.25f, 0.2f));
                AudioManager.Sfx("rattle", 0.5f);
                break;
            case State.Attack:
                attackHit = false;
                AudioManager.Sfx("swing", 0.6f, 0.7f);
                break;
            case State.Chase:
                repathTimer = 0f;
                break;
        }
    }

    protected override void Think(float dt)
    {
        stateTime += dt;
        float dist = DistanceToPlayer();

        switch (state)
        {
            case State.Patrol:
                MoveTowards(patrolTarget, moveSpeed * patrolSpeedFactor, dt);
                if (CapyMath.FlatDistance(transform.position, patrolTarget) < 0.6f)
                    PickPatrolTarget();
                if (alerted || dist < detectRange || stateTime > 2.5f)
                {
                    alerted = true;
                    ChangeState(State.Chase);
                }
                break;

            case State.Chase:
                MoveTowards(Player.transform.position, moveSpeed, dt);
                if (dist < attackRange && CanSeePlayer())
                    ChangeState(State.WindUp);
                break;

            case State.WindUp:
                desiredVelocity = Vector3.zero;
                if (stateTime < windUpTime * 0.6f)
                {
                    lungeDir = CapyMath.DirectionTo(transform.position, Player.transform.position);
                    if (telegraph != null)
                        telegraph.SetLine(transform.position, lungeDir);
                }
                FaceTowards(lungeDir, 20f);
                if (stateTime >= windUpTime)
                    ChangeState(State.Attack);
                break;

            case State.Attack:
                desiredVelocity = lungeDir * lungeSpeed;
                if (!attackHit)
                {
                    Vector3 toPlayer = (Player.transform.position - transform.position).Flat();
                    float reach = radius + Player.Radius + 0.9f;
                    if (toPlayer.magnitude < reach && Vector3.Dot(toPlayer.normalized, lungeDir) > 0.1f)
                    {
                        Player.Health.TakeDamage(attackDamage * damageScale, transform.position);
                        attackHit = true;
                    }
                }
                if (stateTime >= lungeTime)
                    ChangeState(State.Recover);
                break;

            case State.Recover:
                desiredVelocity = Vector3.zero;
                if (stateTime >= recoverTime)
                    ChangeState(State.Chase);
                break;
        }

        Animate(dt);
    }

    private void MoveTowards(Vector3 target, float speed, float dt)
    {
        Vector3 pos = transform.position;
        NavGrid grid = Room != null ? Room.Grid : null;
        directChase = grid == null || CapyMath.HasLineOfSight(pos, target, radius * 0.9f);

        Vector3 steer;
        if (directChase)
        {
            path.Clear();
            steer = Steering.Seek(pos, target, speed);
        }
        else
        {
            repathTimer -= dt;
            if (repathTimer <= 0f || pathIndex >= path.Count)
            {
                grid.FindPath(pos, target, path);
                pathIndex = path.Count > 1 ? 1 : 0;
                repathTimer = 0.4f;
            }

            if (pathIndex < path.Count && CapyMath.FlatDistance(pos, path[pathIndex]) < 0.45f)
                pathIndex++;

            Vector3 next = pathIndex < path.Count ? path[pathIndex] : target;
            steer = Steering.Seek(pos, next, speed);
        }

        Vector3 separation = Steering.Separation(this, 0.9f, Active) * speed;
        desiredVelocity = Vector3.ClampMagnitude(steer + separation * 0.8f, speed);
        if (desiredVelocity.sqrMagnitude > 0.1f)
            FaceTowards(desiredVelocity, 10f);
    }

    private void PickPatrolTarget()
    {
        Vector3 pos = transform.position;
        Vector3 point;
        if (Room != null && Room.Grid != null && Room.Grid.BestPoint(p => -Mathf.Abs(CapyMath.FlatDistance(p, pos) - 3.5f) + Random.value, 20, out point))
            patrolTarget = point;
        else
            patrolTarget = pos + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
    }

    protected override void OnHit(DamageInfo info)
    {
        alerted = true;
    }

    protected override void OnDeath()
    {
        if (telegraph != null)
            Destroy(telegraph.gameObject);
    }

    private void Animate(float dt)
    {
        float speed01 = Mathf.Clamp01((rb != null ? rb.linearVelocity.Flat().magnitude : 0f) / Mathf.Max(0.1f, moveSpeed));
        walkPhase += dt * 11f * speed01;

        if (body != null)
        {
            float shake = state == State.WindUp ? Mathf.Sin(Time.time * 60f) * 0.03f : 0f;
            body.localPosition = new Vector3(shake, Mathf.Abs(Mathf.Sin(walkPhase)) * 0.08f * speed01, 0f);
            float lean = state == State.WindUp ? -12f : (state == State.Attack ? 18f : speed01 * 6f);
            body.localRotation = Quaternion.Euler(lean, 0f, Mathf.Sin(walkPhase) * 4f * speed01);
        }

        if (legL != null)
        {
            legL.localRotation = Quaternion.Euler(Mathf.Sin(walkPhase) * 30f * speed01, 0f, 0f);
            legR.localRotation = Quaternion.Euler(-Mathf.Sin(walkPhase) * 30f * speed01, 0f, 0f);
        }

        if (swordPivot != null)
        {
            float angle;
            if (state == State.WindUp)
                angle = -110f * Mathf.Clamp01(stateTime / (windUpTime * 0.5f));
            else if (state == State.Attack)
                angle = Mathf.Lerp(-110f, 70f, Mathf.Clamp01(stateTime / lungeTime));
            else if (state == State.Recover)
                angle = Mathf.Lerp(70f, 0f, Mathf.Clamp01(stateTime / recoverTime));
            else
                angle = Mathf.Sin(walkPhase) * 12f;
            swordPivot.localRotation = swordBase * Quaternion.Euler(angle, 0f, 0f);
        }
    }

    public override void DrawAIDebug()
    {
        Vector3 p = transform.position + Vector3.up * 0.1f;
        AIDebugDraw.Circle(p, detectRange, new Color(1f, 0.85f, 0.3f, 0.18f), 0.04f, 48);
        AIDebugDraw.Circle(p, attackRange, new Color(1f, 0.3f, 0.2f, 0.45f), 0.05f, 24);

        if (state == State.Chase || state == State.Patrol)
        {
            if (directChase || path.Count == 0)
            {
                Vector3 target = state == State.Chase ? Player.transform.position : patrolTarget;
                AIDebugDraw.Line(p, target + Vector3.up * 0.1f, new Color(1f, 0.9f, 0.4f, 0.6f), 0.06f);
            }
            else
            {
                AIDebugDraw.Path(path.GetRange(pathIndex, path.Count - pathIndex), p, new Color(0.3f, 0.95f, 1f, 0.95f));
            }
        }
    }
}
