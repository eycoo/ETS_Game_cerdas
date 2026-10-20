using System.Collections.Generic;
using UnityEngine;

// EYE BAT — teknik AI: FLOCKING / BOIDS (Reynolds) + steering.
// Setiap kelelawar hanya melihat tetangga dalam radius tertentu, lalu menggabungkan:
// Separation (jangan bertabrakan), Alignment (searah kawanan), Cohesion (tetap berkumpul),
// Target (mengitari Capy), dan Bounds (tetap di dalam ruangan).
// Sesekali satu kelelawar menukik (swoop) ke arah Capy setelah memberi tanda.
public class BatAI : EnemyBase
{
    private enum State
    {
        Flock,
        Telegraph,
        Swoop,
        Recover
    }

    private static readonly List<BatAI> AllBats = new List<BatAI>();

    [Header("Flocking")]
    [SerializeField] private float maxSpeed = 5.5f;
    [SerializeField] private float maxForce = 14f;
    [SerializeField] private float neighborRadius = 3.6f;
    [SerializeField] private float separationRadius = 1.4f;
    [SerializeField] private float wSeparation = 2.2f;
    [SerializeField] private float wAlignment = 1.0f;
    [SerializeField] private float wCohesion = 0.9f;
    [SerializeField] private float wTarget = 1.4f;
    [SerializeField] private float wBounds = 3f;
    [SerializeField] private float orbitRadius = 3.8f;
    [SerializeField] private float flyHeight = 1.5f;

    [Header("Swoop")]
    [SerializeField] private float swoopSpeed = 13f;
    [SerializeField] private float swoopTime = 0.42f;
    [SerializeField] private float telegraphTime = 0.5f;
    [SerializeField] private float swoopCooldownMin = 2.5f;
    [SerializeField] private float swoopCooldownMax = 5.5f;
    [SerializeField] private Transform body;
    [SerializeField] private Transform wingL;
    [SerializeField] private Transform wingR;

    private State state = State.Flock;
    private float stateTime;
    private Vector3 velocity;
    private Vector3 swoopDir;
    private float swoopCooldown;
    private float orbitAngle;
    private float flap;
    private readonly List<BatAI> neighbors = new List<BatAI>();
    private Vector3 fSeparation;
    private Vector3 fAlignment;
    private Vector3 fCohesion;
    private Vector3 fTarget;

    public Vector3 Velocity => velocity;
    public override bool IsFlying => true;
    public override string AITechnique => "FLOCKING";
    public override string AIState => state == State.Flock ? "BOIDS  " + neighbors.Count + " neighbors" : state.ToString().ToUpper();
    public override Color AIColor => new Color(0.75f, 0.55f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetBats()
    {
        AllBats.Clear();
    }

    protected override void Awake()
    {
        base.Awake();
        if (body == null) body = visualRoot.Find("Body");
        if (wingL == null && body != null) wingL = body.Find("WingL");
        if (wingR == null && body != null) wingR = body.Find("WingR");
        orbitAngle = Random.Range(0f, Mathf.PI * 2f);
        swoopCooldown = Random.Range(swoopCooldownMin, swoopCooldownMax);
        flap = Random.Range(0f, 6f);
        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, flyHeight, p.z);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        AllBats.Add(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        AllBats.Remove(this);
    }

    private void ChangeState(State next)
    {
        state = next;
        stateTime = 0f;
    }

    protected override void Think(float dt)
    {
        stateTime += dt;
        FindNeighbors();

        switch (state)
        {
            case State.Flock:
                Vector3 acc = Flocking(dt);
                velocity = Vector3.ClampMagnitude(velocity + acc * dt, maxSpeed);
                swoopCooldown -= dt;
                if (swoopCooldown <= 0f && DistanceToPlayer() < 7.5f && CountSwooping() < 2)
                    ChangeState(State.Telegraph);
                break;

            case State.Telegraph:
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, 20f * dt);
                if (stateTime >= telegraphTime)
                {
                    Vector3 predicted = Player.transform.position + Player.Velocity.Flat() * 0.25f;
                    swoopDir = CapyMath.DirectionTo(transform.position, predicted);
                    ChangeState(State.Swoop);
                    AudioManager.Sfx("bat_screech", 0.35f, Random.Range(0.9f, 1.2f));
                }
                break;

            case State.Swoop:
                velocity = swoopDir * swoopSpeed;
                if (stateTime >= swoopTime)
                    ChangeState(State.Recover);
                break;

            case State.Recover:
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, 12f * dt);
                if (stateTime >= 0.35f)
                {
                    ChangeState(State.Flock);
                    swoopCooldown = Random.Range(swoopCooldownMin, swoopCooldownMax);
                }
                break;
        }

        Move(dt);
    }

    private void FindNeighbors()
    {
        neighbors.Clear();
        float r2 = neighborRadius * neighborRadius;
        for (int i = 0; i < AllBats.Count; i++)
        {
            BatAI b = AllBats[i];
            if (b == this || !b.IsAlive || b.Room != Room)
                continue;
            if (CapyMath.FlatDistanceSq(b.transform.position, transform.position) < r2)
                neighbors.Add(b);
        }
    }

    private int CountSwooping()
    {
        int n = 0;
        for (int i = 0; i < AllBats.Count; i++)
        {
            if (AllBats[i].state == State.Telegraph || AllBats[i].state == State.Swoop)
                n++;
        }
        return n;
    }

    private Vector3 Flocking(float dt)
    {
        Vector3 pos = transform.position.Flat();
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 center = Vector3.zero;

        for (int i = 0; i < neighbors.Count; i++)
        {
            BatAI n = neighbors[i];
            Vector3 offset = pos - n.transform.position.Flat();
            float dist = offset.magnitude;
            if (dist < separationRadius && dist > 0.001f)
                separation += offset / dist * (1f - dist / separationRadius);
            alignment += n.velocity;
            center += n.transform.position.Flat();
        }

        fSeparation = Vector3.zero;
        fAlignment = Vector3.zero;
        fCohesion = Vector3.zero;
        if (neighbors.Count > 0)
        {
            if (separation.sqrMagnitude > 0.0001f)
                fSeparation = separation.normalized * maxSpeed - velocity;
            if (alignment.sqrMagnitude > 0.0001f)
                fAlignment = alignment.normalized * maxSpeed - velocity;
            center /= neighbors.Count;
            fCohesion = Steering.Seek(pos, center, maxSpeed) - velocity;
        }

        // Target: titik yang berputar di sekeliling Capy, sehingga kawanan mengitari mangsanya.
        orbitAngle += dt * 0.7f;
        Vector3 orbitPoint = Player.transform.position + new Vector3(Mathf.Cos(orbitAngle), 0f, Mathf.Sin(orbitAngle)) * orbitRadius;
        fTarget = Steering.Arrive(pos, orbitPoint, maxSpeed, 2f) - velocity;

        Vector3 bounds = Vector3.zero;
        if (Room != null)
        {
            Vector3 c = Room.transform.position;
            float hx = Room.size.x * 0.5f - 1.5f;
            float hz = Room.size.y * 0.5f - 1.5f;
            if (pos.x > c.x + hx) bounds.x = -(pos.x - (c.x + hx));
            if (pos.x < c.x - hx) bounds.x = (c.x - hx) - pos.x;
            if (pos.z > c.z + hz) bounds.z = -(pos.z - (c.z + hz));
            if (pos.z < c.z - hz) bounds.z = (c.z - hz) - pos.z;
            bounds *= maxSpeed;
        }

        Vector3 acc = fSeparation * wSeparation + fAlignment * wAlignment + fCohesion * wCohesion + fTarget * wTarget + bounds * wBounds;
        return Vector3.ClampMagnitude(acc, maxForce);
    }

    private void Move(float dt)
    {
        Vector3 v = velocity + knockbackVelocity;
        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, 30f * dt);
        Vector3 pos = transform.position + v.Flat() * dt;

        if (Room != null)
            pos = Room.ClampInside(pos, 0.7f);

        float targetHeight = flyHeight + Mathf.Sin(Time.time * 3f + orbitAngle) * 0.15f;
        if (state == State.Swoop) targetHeight = 0.9f;
        pos.y = Mathf.Lerp(transform.position.y, targetHeight, 1f - Mathf.Exp(-8f * dt));
        transform.position = pos;

        Vector3 look = state == State.Flock || state == State.Telegraph ? Player.transform.position - pos : v;
        FaceTowards(look, 10f);
        Animate(dt);
    }

    protected override void IdleAnimation(float dt)
    {
        Animate(dt);
    }

    private void Animate(float dt)
    {
        float speed = state == State.Swoop ? 3.5f : 1f;
        flap += dt * 18f * speed;
        float wing = Mathf.Sin(flap) * 40f;
        if (wingL != null) wingL.localRotation = Quaternion.Euler(0f, 0f, 15f + wing);
        if (wingR != null) wingR.localRotation = Quaternion.Euler(0f, 0f, -15f - wing);
        if (body != null)
        {
            float shake = state == State.Telegraph ? Mathf.Sin(Time.time * 70f) * 0.06f : 0f;
            body.localPosition = new Vector3(shake, Mathf.Sin(flap * 0.5f) * 0.05f, 0f);
            float tilt = state == State.Telegraph ? -25f : (state == State.Swoop ? 25f : 0f);
            body.localRotation = Quaternion.Euler(tilt, 0f, 0f);
        }
    }

    public override void TakeDamage(DamageInfo info)
    {
        base.TakeDamage(info);
        if (state == State.Telegraph)
            ChangeState(State.Recover);
    }

    public override void DrawAIDebug()
    {
        Vector3 p = transform.position.Flat() + Vector3.up * 0.1f;
        for (int i = 0; i < neighbors.Count; i++)
            AIDebugDraw.Line(p, neighbors[i].transform.position.Flat() + Vector3.up * 0.1f, new Color(0.75f, 0.55f, 1f, 0.45f), 0.04f);
        AIDebugDraw.Circle(p, separationRadius, new Color(1f, 0.5f, 0.3f, 0.25f), 0.03f, 20);
        AIDebugDraw.Line(p, p + velocity.Flat() * 0.4f, new Color(1f, 1f, 1f, 0.9f), 0.07f);
        if (state == State.Telegraph || state == State.Swoop)
            AIDebugDraw.Line(p, p + swoopDir * 5f, new Color(1f, 0.25f, 0.2f, 0.9f), 0.1f);
    }
}
