using System.Collections.Generic;
using UnityEngine;

// Kristal XP: terpental keluar dari musuh, lalu tertarik ke Capy (magnet).
public class XPGem : MonoBehaviour
{
    public static readonly List<XPGem> All = new List<XPGem>();

    [SerializeField] private Transform visual;

    private int value = 1;
    private Vector3 velocity;
    private float verticalSpeed;
    private float height = 0.5f;
    private float age;
    private bool magnet;
    private float magnetSpeed;
    private float bobPhase;

    private static float lastCollectTime;
    private static int combo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        All.Clear();
        combo = 0;
        lastCollectTime = 0f;
    }

    public static void Spawn(Vector3 pos, int value)
    {
        GameObject prefab = GameDatabase.I != null ? GameDatabase.I.xpGem : null;
        if (prefab == null)
            return;

        GameObject go = Instantiate(prefab, new Vector3(pos.x, 0.5f, pos.z), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        XPGem gem = go.GetComponent<XPGem>();
        gem.value = value;
        if (value >= 5)
            go.transform.localScale *= 1.6f;
        Vector2 r = Random.insideUnitCircle.normalized * Random.Range(1.2f, 3.2f);
        gem.velocity = new Vector3(r.x, 0f, r.y);
        gem.verticalSpeed = Random.Range(4f, 6f);
        gem.bobPhase = Random.Range(0f, 6f);
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        PlayerController p = PlayerController.I;

        if (!magnet)
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, 5f * dt);
            verticalSpeed -= 18f * dt;
            height += verticalSpeed * dt;
            if (height < 0.4f)
            {
                height = 0.4f;
                verticalSpeed = Mathf.Abs(verticalSpeed) * 0.35f;
                if (verticalSpeed < 1f)
                    verticalSpeed = 0f;
            }

            Vector3 pos = transform.position + velocity * dt;
            if (Physics.CheckSphere(new Vector3(pos.x, 0.6f, pos.z), 0.2f, Layers.ObstacleMask, QueryTriggerInteraction.Ignore))
            {
                pos = transform.position;
                velocity = -velocity * 0.5f;
            }
            float bob = verticalSpeed == 0f ? Mathf.Sin(Time.time * 3f + bobPhase) * 0.08f : 0f;
            pos.y = height + bob;
            transform.position = pos;

            if (p != null && age > 0.45f && CapyMath.FlatDistance(p.transform.position, pos) < p.Stats.MagnetRadius)
                magnet = true;
        }
        else if (p != null)
        {
            magnetSpeed = Mathf.Min(magnetSpeed + 45f * dt, 24f);
            Vector3 target = p.transform.position + Vector3.up * 0.7f;
            transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * dt);
            if (Vector3.Distance(transform.position, target) < 0.45f)
                Collect(p);
        }

        if (visual != null)
            visual.Rotate(0f, 160f * dt, 0f, Space.World);
    }

    private void Collect(PlayerController p)
    {
        if (Time.time - lastCollectTime < 0.5f)
            combo = Mathf.Min(combo + 1, 14);
        else
            combo = 0;
        lastCollectTime = Time.time;

        p.Level.AddXP(value);
        AudioManager.Sfx("xp", 0.35f, 1f + combo * 0.045f, 0.01f);
        VFX.Sparkle(transform.position, new Color(0.45f, 0.9f, 1f), 3);
        Destroy(gameObject);
    }

    public static void CollectAll()
    {
        foreach (XPGem g in All)
        {
            g.magnet = true;
            g.age = 1f;
        }
    }
}
