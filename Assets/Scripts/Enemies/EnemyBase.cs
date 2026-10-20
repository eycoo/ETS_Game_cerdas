using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Dasar semua musuh: darah, kilatan saat terkena, knockback, efek terbakar, mati + jatuhkan XP,
// dan animasi muncul. Setiap jenis musuh hanya perlu mengisi Think() dengan teknik AI-nya.
public abstract class EnemyBase : MonoBehaviour
{
    public static readonly List<EnemyBase> Active = new List<EnemyBase>();

    [Header("Identity")]
    [SerializeField] protected string displayName = "Enemy";

    [Header("Stats")]
    [SerializeField] protected float maxHealth = 30f;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float contactDamage = 10f;
    [SerializeField] protected int xpValue = 1;
    [SerializeField] protected float radius = 0.5f;
    [SerializeField] protected float knockbackResist;
    [SerializeField] protected float healthBarHeight = 1.8f;
    [SerializeField] protected Color bloodColor = Color.white;
    [SerializeField] protected float yuzuDropChance = 0.05f;

    [Header("Visual")]
    [SerializeField] protected Transform visualRoot;

    public string DisplayName => displayName;
    public float MaxHealth => maxHealth;
    public float Health { get; protected set; }
    public bool IsAlive => !isDead;
    public bool IsSpawning { get; private set; }
    public float Radius => radius;
    public float HealthBarHeight => healthBarHeight;
    public float LastHitTime { get; private set; } = -10f;
    public RoomController Room { get; set; }
    public virtual bool IsBoss => false;
    public virtual bool IsFlying => false;
    public virtual bool IsTargetable => !isDead && !IsSpawning;
    public abstract string AITechnique { get; }
    public abstract string AIState { get; }
    public virtual Color AIColor => new Color(0.6f, 0.9f, 1f);

    protected Rigidbody rb;
    protected Vector3 desiredVelocity;
    protected Vector3 knockbackVelocity;
    protected bool isDead;
    protected float damageTakenMultiplier = 1f;
    protected float damageScale = 1f;
    protected bool aiFrozen;
    private bool scaled;
    protected PlayerController Player => PlayerController.I;

    private MeshRenderer[] renderers;
    private Material[][] originalMaterials;
    private Material[][] flashMaterials;
    private float flashUntil;
    private float lastFlashStart = -10f;
    private bool flashing;
    private float burnUntil;
    private float burnTickTimer;
    private int burnLevel;
    private float squash;
    private Vector3 baseVisualScale = Vector3.one;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Active.Clear();
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (visualRoot == null)
            visualRoot = transform;
        baseVisualScale = visualRoot.localScale;
        Health = maxHealth;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }

        CacheRenderers();
    }

    public void CacheRenderers()
    {
        renderers = visualRoot.GetComponentsInChildren<MeshRenderer>(true);
        originalMaterials = new Material[renderers.Length][];
        flashMaterials = new Material[renderers.Length][];
        Material flash = GameDatabase.I != null ? GameDatabase.I.flashMaterial : null;
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
            flashMaterials[i] = new Material[originalMaterials[i].Length];
            for (int k = 0; k < flashMaterials[i].Length; k++)
                flashMaterials[i][k] = flash;
        }
    }

    // Untuk AI yang mengganti material (misal kristal Golem jadi merah saat marah).
    // Hanya slot yang berisi salah satu material `from` yang diganti, karena model impor
    // menggabungkan bagian menyala dan tidak menyala dalam satu renderer.
    protected void ReplaceMaterial(Renderer target, Material mat, Material[] from)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != target)
                continue;
            Material[] arr = (Material[])originalMaterials[i].Clone();
            for (int k = 0; k < arr.Length; k++)
            {
                if (System.Array.IndexOf(from, arr[k]) >= 0)
                    arr[k] = mat;
            }
            originalMaterials[i] = arr;
            if (!flashing)
                renderers[i].sharedMaterials = arr;
        }
    }

    protected virtual void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);
    }

    protected virtual void OnDisable()
    {
        Active.Remove(this);
    }

    protected virtual void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    protected virtual float SpawnDuration => 0.4f;

    private IEnumerator SpawnRoutine()
    {
        IsSpawning = true;
        float t = 0f;
        while (t < SpawnDuration)
        {
            t += Time.deltaTime;
            float k = CapyMath.EaseOutBack(Mathf.Clamp01(t / SpawnDuration));
            visualRoot.localScale = baseVisualScale * Mathf.Max(0.01f, k);
            yield return null;
        }
        visualRoot.localScale = baseVisualScale;
        IsSpawning = false;
        OnSpawned();
    }

    protected virtual void OnSpawned()
    {
    }

    protected virtual void Update()
    {
        if (isDead)
            return;

        UpdateFlash();
        UpdateBurn();
        UpdateSquash();

        GameManager gm = GameManager.I;
        bool playing = gm == null || gm.IsPlaying;
        if (IsSpawning || !playing || aiFrozen || Player == null || Player.Health.IsDead)
        {
            desiredVelocity = Vector3.zero;
            IdleAnimation(Time.deltaTime);
            return;
        }

        Think(Time.deltaTime);
        TouchDamage();
    }

    protected abstract void Think(float dt);

    protected virtual void IdleAnimation(float dt)
    {
    }

    protected virtual void FixedUpdate()
    {
        if (rb == null || rb.isKinematic)
            return;
        Vector3 v = desiredVelocity + knockbackVelocity;
        rb.linearVelocity = new Vector3(v.x, 0f, v.z);
        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, 35f * Time.fixedDeltaTime);
    }

    protected virtual void TouchDamage()
    {
        if (contactDamage <= 0f)
            return;
        float r = radius + Player.Radius;
        if (CapyMath.FlatDistanceSq(transform.position, Player.transform.position) < r * r)
            Player.Health.TakeDamage(contactDamage, transform.position);
    }

    // ------------------------------------------------------------------ damage

    public virtual void TakeDamage(DamageInfo info)
    {
        if (isDead || IsSpawning)
            return;

        float amount = info.amount * damageTakenMultiplier;
        Health -= amount;
        LastHitTime = Time.time;

        int shown = Mathf.Max(1, Mathf.RoundToInt(amount));
        NumberStyle style = info.burnTick ? NumberStyle.Burn : (info.crit ? NumberStyle.Crit : NumberStyle.Normal);
        WorldUI.Number(transform.position + Vector3.up * healthBarHeight * 0.85f, shown, style);
        if (GameManager.I != null)
            GameManager.I.Stats.damageDealt += shown;

        if (!info.burnTick)
        {
            // Kilatan putih dibatasi frekuensinya supaya musuh tidak putih terus saat dihujani serangan.
            if (Time.time - lastFlashStart > (IsBoss ? 0.4f : 0.16f))
            {
                lastFlashStart = Time.time;
                flashUntil = Time.time + 0.06f;
            }
            squash = 1f;
            if (info.knockback > 0f)
                knockbackVelocity += info.direction.Flat().normalized * info.knockback * (1f - knockbackResist);

            Vector3 hitPoint = transform.position + Vector3.up * healthBarHeight * 0.5f;
            VFX.Sparks(hitPoint, info.crit ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.95f, 0.85f), info.crit ? 14 : 8, 7f);
            AudioManager.Sfx(info.crit ? "crit" : "hit", 0.55f);
            if (info.burnLevel > 0)
            {
                burnLevel = Mathf.Max(burnLevel, info.burnLevel);
                burnUntil = Time.time + 2.2f;
            }
            OnHit(info);
        }

        if (Health <= 0f)
            Die();
    }

    protected virtual void OnHit(DamageInfo info)
    {
    }

    // Musuh di ruangan lanjut lebih tebal dan lebih sakit (dipanggil RoomController).
    public void ApplyRoomScaling(float healthMultiplier, float damageMultiplier)
    {
        if (scaled)
            return;
        scaled = true;
        maxHealth *= healthMultiplier;
        Health = maxHealth;
        contactDamage *= damageMultiplier;
        damageScale = damageMultiplier;
    }

    public void Kill()
    {
        if (isDead)
            return;
        Health = 0f;
        Die();
    }

    private void UpdateBurn()
    {
        if (Time.time >= burnUntil)
            return;

        if (Random.value < 20f * Time.deltaTime)
            VFX.Fire(transform.position + Vector3.up * healthBarHeight * 0.45f);

        burnTickTimer -= Time.deltaTime;
        if (burnTickTimer <= 0f)
        {
            burnTickTimer = 0.5f;
            DamageInfo tick = new DamageInfo();
            tick.amount = 3f * burnLevel;
            tick.burnTick = true;
            TakeDamage(tick);
        }
    }

    private void UpdateFlash()
    {
        bool want = Time.time < flashUntil;
        if (want == flashing)
            return;
        flashing = want;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].sharedMaterials = want ? flashMaterials[i] : originalMaterials[i];
        }
    }

    private void UpdateSquash()
    {
        if (IsSpawning)
            return;
        squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 7f);
        float s = squash * 0.22f;
        visualRoot.localScale = Vector3.Scale(baseVisualScale, new Vector3(1f + s, 1f - s, 1f + s));
    }

    // ------------------------------------------------------------------ death

    protected virtual void Die()
    {
        if (isDead)
            return;
        isDead = true;
        Health = 0f;
        Active.Remove(this);
        desiredVelocity = Vector3.zero;

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (flashing)
        {
            flashing = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].sharedMaterials = originalMaterials[i];
            }
        }

        DropLoot();
        Vector3 center = transform.position + Vector3.up * healthBarHeight * 0.45f;
        VFX.Poof(center, bloodColor.WithAlpha(0.85f), 14, 1f);
        VFX.Debris(center, bloodColor, 8, 5f);
        AudioManager.Sfx("enemy_die", 0.7f);
        CameraRig.Shake(IsBoss ? 0.9f : 0.12f);

        if (GameManager.I != null)
            GameManager.I.OnEnemyKilled(this);

        OnDeath();

        if (Room != null)
            Room.OnEnemyDied(this);

        StartCoroutine(DeathRoutine());
    }

    protected virtual void OnDeath()
    {
    }

    protected virtual IEnumerator DeathRoutine()
    {
        float t = 0f;
        Vector3 start = visualRoot.localScale;
        while (t < 0.14f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.14f);
            visualRoot.localScale = Vector3.Scale(start, new Vector3(1f + k * 0.4f, Mathf.Max(0.02f, 1f - k), 1f + k * 0.4f));
            yield return null;
        }
        Destroy(gameObject);
    }

    protected virtual void DropLoot()
    {
        int remaining = xpValue;
        while (remaining > 0)
        {
            int v = remaining >= 5 ? 5 : 1;
            remaining -= v;
            XPGem.Spawn(transform.position, v);
        }

        if (Random.value < yuzuDropChance)
            YuzuPickup.Spawn(transform.position);
    }

    // ------------------------------------------------------------------ helpers

    protected bool CanSeePlayer()
    {
        return CapyMath.HasLineOfSight(transform.position, Player.transform.position, 0.25f);
    }

    protected float DistanceToPlayer()
    {
        return CapyMath.FlatDistance(transform.position, Player.transform.position);
    }

    // Yang diputar adalah model (visualRoot), bukan Rigidbody, supaya interpolasi fisika tetap mulus.
    protected void FaceTowards(Vector3 dir, float sharpness = 12f)
    {
        dir = dir.Flat();
        if (dir.sqrMagnitude < 0.0001f)
            return;
        Quaternion target = Quaternion.LookRotation(dir.normalized);
        visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, target, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
    }

    protected Vector3 Forward => visualRoot.forward.Flat().normalized;

    public virtual void DrawAIDebug()
    {
    }

    public static EnemyBase FindTarget(Vector3 from, float maxRange)
    {
        EnemyBase best = null;
        float bestScore = float.MaxValue;
        float maxSq = maxRange * maxRange;
        for (int i = 0; i < Active.Count; i++)
        {
            EnemyBase e = Active[i];
            if (e == null || !e.IsTargetable)
                continue;
            float d = CapyMath.FlatDistanceSq(from, e.transform.position);
            if (d > maxSq)
                continue;
            float score = d;
            if (!CapyMath.HasLineOfSight(from, e.transform.position, 0.15f))
                score += 400f;
            if (score < bestScore)
            {
                bestScore = score;
                best = e;
            }
        }
        return best;
    }

    public static EnemyBase FindNearest(Vector3 from, float maxRange, ICollection<EnemyBase> exclude)
    {
        EnemyBase best = null;
        float bestSq = maxRange * maxRange;
        for (int i = 0; i < Active.Count; i++)
        {
            EnemyBase e = Active[i];
            if (e == null || !e.IsTargetable || (exclude != null && exclude.Contains(e)))
                continue;
            float d = CapyMath.FlatDistanceSq(from, e.transform.position);
            if (d < bestSq)
            {
                bestSq = d;
                best = e;
            }
        }
        return best;
    }
}
