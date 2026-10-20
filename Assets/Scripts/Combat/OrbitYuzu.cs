using System.Collections.Generic;
using UnityEngine;

// Upgrade "Yuzu Orbit": jeruk yang berputar mengelilingi Capy dan melukai musuh yang tersentuh.
public class OrbitYuzu : MonoBehaviour
{
    [SerializeField] private float orbitRadius = 1.8f;
    [SerializeField] private float orbitSpeed = 220f;
    [SerializeField] private float orbRadius = 0.45f;

    private readonly List<Transform> orbs = new List<Transform>();
    private readonly Dictionary<EnemyBase, float> nextHit = new Dictionary<EnemyBase, float>();
    private static readonly List<EnemyBase> buffer = new List<EnemyBase>();
    private PlayerController player;
    private float angle;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        int count = player.Stats.OrbitCount;
        while (orbs.Count < count && GameDatabase.I.orbitYuzu != null)
        {
            Transform orb = Instantiate(GameDatabase.I.orbitYuzu).transform;
            orbs.Add(orb);
            VFX.Sparkle(transform.position + Vector3.up, new Color(1f, 0.6f, 0.1f), 10);
        }

        if (orbs.Count == 0)
            return;

        angle += orbitSpeed * Time.deltaTime;
        for (int i = 0; i < orbs.Count; i++)
        {
            float a = angle + i * 360f / orbs.Count;
            Vector3 pos = transform.position + Quaternion.Euler(0f, a, 0f) * Vector3.forward * orbitRadius;
            pos.y = 0.9f + Mathf.Sin((Time.time + i) * 4f) * 0.1f;
            orbs[i].position = pos;
            orbs[i].Rotate(0f, 400f * Time.deltaTime, 0f, Space.World);
        }

        if (GameManager.I != null && !GameManager.I.IsPlaying)
            return;

        buffer.Clear();
        buffer.AddRange(EnemyBase.Active);
        float damage = player.Stats.Damage * 0.7f;
        foreach (EnemyBase e in buffer)
        {
            if (e == null || !e.IsTargetable)
                continue;

            float t;
            if (nextHit.TryGetValue(e, out t) && Time.time < t)
                continue;

            for (int i = 0; i < orbs.Count; i++)
            {
                float r = orbRadius + e.Radius;
                if (CapyMath.FlatDistanceSq(orbs[i].position, e.transform.position) > r * r)
                    continue;

                DamageInfo info = new DamageInfo();
                info.amount = damage;
                info.direction = CapyMath.DirectionTo(transform.position, e.transform.position);
                info.knockback = 4f;
                info.burnLevel = player.Stats.BurnLevel;
                e.TakeDamage(info);
                nextHit[e] = Time.time + 0.5f;
                VFX.Sparks(orbs[i].position, new Color(1f, 0.65f, 0.15f), 6, 4f);
                break;
            }
        }
    }

    private void OnDestroy()
    {
        foreach (Transform orb in orbs)
        {
            if (orb != null)
                Destroy(orb.gameObject);
        }
    }
}
