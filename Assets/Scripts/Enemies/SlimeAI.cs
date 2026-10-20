using UnityEngine;

// SLIME — teknik AI: STEERING BEHAVIOR (Seek + Separation + Wander).
// Slime melompat-lompat. Setiap kali akan melompat, arah lompatan dihitung dari gabungan
// tiga gaya steering: Seek ke Capy, Separation dari slime lain, dan sedikit Wander.
// Slime besar membelah jadi dua slime kecil saat mati.
public class SlimeAI : EnemyBase
{
    private enum State
    {
        Idle,
        Squash,
        Hop,
        Land
    }

    [Header("Slime")]
    [SerializeField] private bool isBig;
    [SerializeField] private float hopInterval = 0.9f;
    [SerializeField] private float hopDuration = 0.5f;
    [SerializeField] private float hopHeight = 0.9f;
    [SerializeField] private float seekWeight = 1f;
    [SerializeField] private float separationWeight = 1.6f;
    [SerializeField] private float wanderWeight = 0.35f;
    [SerializeField] private Transform body;

    private State state = State.Idle;
    private float timer;
    private Vector3 hopVelocity;
    private float wanderAngle;
    private Vector3 lastSeek;
    private Vector3 lastSeparation;
    private Vector3 lastWander;

    public override string AITechnique => "STEERING";
    public override string AIState => state.ToString().ToUpper() + "  seek+separate";
    public override Color AIColor => new Color(0.55f, 1f, 0.45f);

    protected override void Awake()
    {
        base.Awake();
        if (body == null)
            body = visualRoot.Find("Body");
        wanderAngle = Random.Range(0f, Mathf.PI * 2f);
    }

    protected override void OnSpawned()
    {
        if (state == State.Idle)
            timer = Random.Range(0.1f, hopInterval);
    }

    // Dipanggil untuk slime kecil hasil pembelahan: langsung melompat menjauh dari induknya.
    public void LaunchFromSplit(Vector3 dir)
    {
        hopVelocity = dir.Flat().normalized * moveSpeed * 1.2f;
        state = State.Hop;
        timer = hopDuration;
    }

    protected override float SpawnDuration => hopVelocity.sqrMagnitude > 0f ? 0.15f : 0.4f;

    protected override void Think(float dt)
    {
        timer -= dt;
        switch (state)
        {
            case State.Idle:
                desiredVelocity = Vector3.zero;
                if (timer <= 0f)
                {
                    state = State.Squash;
                    timer = 0.22f;
                }
                break;

            case State.Squash:
                desiredVelocity = Vector3.zero;
                if (timer <= 0f)
                {
                    hopVelocity = ComputeSteering() * moveSpeed;
                    state = State.Hop;
                    timer = hopDuration;
                    if (isBig)
                        AudioManager.Sfx("slime_hop", 0.25f, 0.8f);
                }
                break;

            case State.Hop:
                desiredVelocity = hopVelocity;
                FaceTowards(hopVelocity, 10f);
                if (timer <= 0f)
                {
                    state = State.Land;
                    timer = 0.16f;
                    if (isBig)
                        VFX.Poof(transform.position, new Color(0.45f, 0.85f, 0.4f, 0.5f), 3, 0.5f);
                }
                break;

            case State.Land:
                desiredVelocity = Vector3.zero;
                if (timer <= 0f)
                {
                    state = State.Idle;
                    timer = hopInterval * Random.Range(0.75f, 1.25f);
                }
                break;
        }
        Animate();
    }

    private Vector3 ComputeSteering()
    {
        Vector3 pos = transform.position;
        lastSeek = Steering.Seek(pos, Player.transform.position, 1f);
        lastSeparation = Steering.Separation(this, 1.3f, Active);
        lastWander = Steering.Wander(ref wanderAngle, Forward, 6f, 0.5f);
        Vector3 sum = lastSeek * seekWeight + lastSeparation * separationWeight + lastWander * wanderWeight;
        if (sum.sqrMagnitude < 0.001f)
            sum = lastSeek;
        return sum.normalized;
    }

    protected override void IdleAnimation(float dt)
    {
        if (body == null)
            return;
        float b = Mathf.Sin(Time.time * 3f + wanderAngle) * 0.04f;
        body.localScale = new Vector3(1f + b, 1f - b, 1f + b);
        body.localPosition = Vector3.zero;
    }

    private void Animate()
    {
        if (body == null)
            return;

        float y = 0f;
        Vector3 scale = Vector3.one;
        switch (state)
        {
            case State.Idle:
                float b = Mathf.Sin(Time.time * 3f + wanderAngle) * 0.04f;
                scale = new Vector3(1f + b, 1f - b, 1f + b);
                break;
            case State.Squash:
                float k = 1f - Mathf.Clamp01(timer / 0.22f);
                scale = new Vector3(1f + 0.3f * k, 1f - 0.35f * k, 1f + 0.3f * k);
                break;
            case State.Hop:
                float t = 1f - Mathf.Clamp01(timer / hopDuration);
                y = Mathf.Sin(t * Mathf.PI) * hopHeight;
                scale = new Vector3(0.85f, 1.22f, 0.85f);
                break;
            case State.Land:
                float l = Mathf.Clamp01(timer / 0.16f);
                scale = new Vector3(1f + 0.35f * l, 1f - 0.35f * l, 1f + 0.35f * l);
                break;
        }
        body.localPosition = new Vector3(0f, y, 0f);
        body.localScale = Vector3.Lerp(body.localScale, scale, 1f - Mathf.Exp(-25f * Time.deltaTime));
    }

    protected override void TouchDamage()
    {
        // Hanya melukai saat tidak sedang di udara tinggi, supaya terasa adil.
        if (state == State.Hop && timer < hopDuration * 0.75f && timer > hopDuration * 0.25f)
            return;
        base.TouchDamage();
    }

    protected override void OnDeath()
    {
        if (!isBig || GameDatabase.I.slimeSmall == null)
            return;

        Vector3 side = Vector3.Cross(Vector3.up, Forward);
        for (int i = 0; i < 2; i++)
        {
            Vector3 dir = (i == 0 ? side : -side) + Random.insideUnitSphere.Flat() * 0.3f;
            Vector3 pos = transform.position + dir.normalized * 0.4f;
            GameObject go = Instantiate(GameDatabase.I.slimeSmall, pos, Quaternion.identity);
            SlimeAI child = go.GetComponent<SlimeAI>();
            child.LaunchFromSplit(dir);
            if (Room != null)
                Room.RegisterEnemy(child);
        }
    }

    public override void DrawAIDebug()
    {
        Vector3 p = transform.position + Vector3.up * 0.1f;
        AIDebugDraw.Line(p, p + lastSeek * 1.6f, new Color(0.4f, 1f, 0.4f, 0.9f), 0.08f);
        AIDebugDraw.Line(p, p + lastSeparation * 1.6f, new Color(1f, 0.5f, 0.3f, 0.9f), 0.08f);
        AIDebugDraw.Line(p, p + lastWander * 1.0f, new Color(0.6f, 0.6f, 1f, 0.9f), 0.06f);
        AIDebugDraw.Circle(p, 1.3f + radius, new Color(1f, 0.5f, 0.3f, 0.25f), 0.03f, 24);
    }
}
