using System.Collections.Generic;
using UnityEngine;

// SKELETON ARCHER — teknik AI: FINITE STATE MACHINE + PATHFINDING A*.
// State: Reposition -> Aim -> Shoot -> Cooldown -> Reposition ..., dan Retreat bila Capy terlalu dekat.
// Posisi menembak dipilih dengan memberi skor pada sel grid (jarak ideal + garis pandang),
// lalu didatangi dengan jalur A*. Sebelum menembak, garis bidikan merah tampil sebagai peringatan.
public class SkeletonArcherAI : EnemyBase
{
    public enum State
    {
        Reposition,
        Aim,
        Shoot,
        Cooldown,
        Retreat
    }

    [Header("Archer FSM")]
    [SerializeField] private float idealDistance = 7.5f;
    [SerializeField] private float retreatDistance = 3.5f;
    [SerializeField] private float aimTime = 0.85f;
    [SerializeField] private float cooldownTime = 0.8f;
    [SerializeField] private float arrowSpeed = 13f;
    [SerializeField] private float arrowDamage = 12f;
    [SerializeField] private Transform body;
    [SerializeField] private Transform bowPivot;

    private State state = State.Reposition;
    private float stateTime;
    private Vector3 aimDir = Vector3.forward;
    private Vector3 moveTarget;
    private bool hasMoveTarget;
    private readonly List<Vector3> path = new List<Vector3>();
    private int pathIndex;
    private float walkPhase;
    private Telegraph aimLine;

    public override string AITechnique => "FSM";
    public override string AIState => state.ToString().ToUpper();
    public override Color AIColor => new Color(1f, 0.75f, 0.35f);

    protected override void Awake()
    {
        base.Awake();
        if (body == null) body = visualRoot.Find("Body");
        if (bowPivot == null && body != null) bowPivot = body.Find("BowPivot");
    }

    protected override void OnSpawned()
    {
        ChangeState(State.Reposition);
    }

    private void ChangeState(State next)
    {
        if (aimLine != null)
        {
            Destroy(aimLine.gameObject);
            aimLine = null;
        }

        state = next;
        stateTime = 0f;

        switch (next)
        {
            case State.Reposition:
                PickSpot(false);
                break;
            case State.Retreat:
                PickSpot(true);
                break;
            case State.Aim:
                aimDir = CapyMath.DirectionTo(transform.position, Player.transform.position);
                aimLine = Telegraph.Rect(transform.position, aimDir, 14f, 0.22f, aimTime, new Color(1f, 0.2f, 0.2f));
                AudioManager.Sfx("bow_draw", 0.45f);
                break;
        }
    }

    protected override void Think(float dt)
    {
        stateTime += dt;
        float dist = DistanceToPlayer();

        switch (state)
        {
            case State.Reposition:
                FollowPath(moveSpeed, dt);
                if (dist < retreatDistance)
                {
                    ChangeState(State.Retreat);
                }
                else if (stateTime > 0.5f && dist < idealDistance + 3f && CanSeePlayer())
                {
                    ChangeState(State.Aim);
                }
                else if (!hasMoveTarget || stateTime > 3.5f)
                {
                    PickSpot(false);
                    stateTime = 0.3f;
                }
                break;

            case State.Retreat:
                FollowPath(moveSpeed * 1.25f, dt);
                if (stateTime > 1.4f || !hasMoveTarget)
                    ChangeState(CanSeePlayer() ? State.Aim : State.Reposition);
                break;

            case State.Aim:
                desiredVelocity = Vector3.zero;
                if (stateTime < aimTime * 0.7f)
                    aimDir = CapyMath.DirectionTo(transform.position, Player.transform.position);
                if (aimLine != null)
                    aimLine.SetLine(transform.position + aimDir * 0.5f, aimDir);
                FaceTowards(aimDir, 20f);
                if (stateTime >= aimTime)
                    ChangeState(State.Shoot);
                break;

            case State.Shoot:
                Shoot();
                ChangeState(State.Cooldown);
                break;

            case State.Cooldown:
                desiredVelocity = Vector3.zero;
                if (stateTime >= cooldownTime)
                {
                    if (dist < retreatDistance)
                        ChangeState(State.Retreat);
                    else if (Random.value < 0.4f && CanSeePlayer())
                        ChangeState(State.Aim);
                    else
                        ChangeState(State.Reposition);
                }
                break;
        }

        Animate(dt);
    }

    private void Shoot()
    {
        GameObject prefab = GameDatabase.I.arrow;
        if (prefab == null)
            return;
        Vector3 spawn = transform.position + Vector3.up * 0.9f + aimDir * 0.6f;
        GameObject go = Instantiate(prefab, spawn, Quaternion.LookRotation(aimDir));
        go.GetComponent<Projectile>().Launch(Team.Enemy, aimDir, arrowSpeed, arrowDamage * damageScale);
        AudioManager.Sfx("arrow", 0.6f);
    }

    // Memberi skor pada sel-sel grid: jarak ideal ke Capy, ada garis pandang, tidak terlalu jauh berjalan.
    private void PickSpot(bool retreat)
    {
        hasMoveTarget = false;
        if (Room == null || Room.Grid == null)
            return;

        Vector3 me = transform.position;
        Vector3 player = Player.transform.position;
        Vector3 point;
        bool found = Room.Grid.BestPoint(p =>
        {
            float d = CapyMath.FlatDistance(p, player);
            float score = -Mathf.Abs(d - (retreat ? idealDistance + 1f : idealDistance));
            if (CapyMath.HasLineOfSight(p, player, 0.2f))
                score += 3f;
            score -= CapyMath.FlatDistance(p, me) * (retreat ? 0.05f : 0.15f);
            return score;
        }, 28, out point);

        if (!found)
            return;

        moveTarget = point;
        hasMoveTarget = Room.Grid.FindPath(me, moveTarget, path);
        pathIndex = path.Count > 1 ? 1 : 0;
    }

    private void FollowPath(float speed, float dt)
    {
        if (!hasMoveTarget || pathIndex >= path.Count)
        {
            hasMoveTarget = false;
            desiredVelocity = Vector3.zero;
            return;
        }

        Vector3 pos = transform.position;
        if (CapyMath.FlatDistance(pos, path[pathIndex]) < 0.45f)
            pathIndex++;
        if (pathIndex >= path.Count)
        {
            hasMoveTarget = false;
            desiredVelocity = Vector3.zero;
            return;
        }

        Vector3 steer = Steering.Seek(pos, path[pathIndex], speed);
        Vector3 separation = Steering.Separation(this, 0.9f, Active) * speed;
        desiredVelocity = Vector3.ClampMagnitude(steer + separation * 0.6f, speed);
        if (desiredVelocity.sqrMagnitude > 0.1f)
            FaceTowards(desiredVelocity, 10f);
    }

    protected override void OnDeath()
    {
        if (aimLine != null)
            Destroy(aimLine.gameObject);
    }

    private void Animate(float dt)
    {
        float speed01 = Mathf.Clamp01((rb != null ? rb.linearVelocity.Flat().magnitude : 0f) / Mathf.Max(0.1f, moveSpeed));
        walkPhase += dt * 11f * speed01;
        if (body != null)
        {
            body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(walkPhase)) * 0.08f * speed01, 0f);
            body.localRotation = Quaternion.Euler(state == State.Aim ? -6f : speed01 * 6f, 0f, Mathf.Sin(walkPhase) * 4f * speed01);
        }
        if (bowPivot != null)
        {
            float draw = state == State.Aim ? Mathf.Clamp01(stateTime / aimTime) : 0f;
            bowPivot.localScale = new Vector3(1f, 1f, 1f - draw * 0.25f);
        }
    }

    public override void DrawAIDebug()
    {
        Vector3 p = transform.position + Vector3.up * 0.1f;
        AIDebugDraw.Circle(p, retreatDistance, new Color(1f, 0.4f, 0.3f, 0.35f), 0.04f, 32);
        AIDebugDraw.Circle(Player.transform.position + Vector3.up * 0.1f, idealDistance, new Color(1f, 0.8f, 0.4f, 0.12f), 0.04f, 48);
        if ((state == State.Reposition || state == State.Retreat) && hasMoveTarget && pathIndex < path.Count)
        {
            AIDebugDraw.Path(path.GetRange(pathIndex, path.Count - pathIndex), p, new Color(0.3f, 0.95f, 1f, 0.95f));
            AIDebugDraw.Circle(moveTarget + Vector3.up * 0.1f, 0.35f, new Color(0.3f, 0.95f, 1f, 0.95f), 0.06f, 16);
        }
    }
}
