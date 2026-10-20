using System.Collections.Generic;
using UnityEngine;

// Buah yuzu: memulihkan 15% darah. Kadang jatuh dari musuh.
public class YuzuPickup : MonoBehaviour
{
    [SerializeField] private float healPercent = 0.15f;
    [SerializeField] private Transform visual;

    private float age;
    private Vector3 velocity;
    private float verticalSpeed;
    private float height = 0.5f;

    public static void Spawn(Vector3 pos)
    {
        GameObject prefab = GameDatabase.I != null ? GameDatabase.I.yuzuHeal : null;
        if (prefab == null)
            return;
        GameObject go = Instantiate(prefab, new Vector3(pos.x, 0.5f, pos.z), Quaternion.identity);
        YuzuPickup y = go.GetComponent<YuzuPickup>();
        Vector2 r = Random.insideUnitCircle.normalized * 1.5f;
        y.velocity = new Vector3(r.x, 0f, r.y);
        y.verticalSpeed = 5f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        velocity = Vector3.MoveTowards(velocity, Vector3.zero, 4f * dt);
        verticalSpeed -= 18f * dt;
        height = Mathf.Max(0.45f, height + verticalSpeed * dt);
        if (height <= 0.45f)
            verticalSpeed = 0f;

        Vector3 pos = transform.position + velocity * dt;
        pos.y = height + (verticalSpeed == 0f ? Mathf.Sin(Time.time * 3f) * 0.1f : 0f);
        transform.position = pos;
        if (visual != null)
            visual.Rotate(0f, 90f * dt, 0f, Space.World);

        PlayerController p = PlayerController.I;
        if (p == null || age < 0.4f || p.Health.IsDead)
            return;

        float d = CapyMath.FlatDistance(p.transform.position, transform.position);
        if (d < 1.8f)
            transform.position = Vector3.MoveTowards(transform.position, p.transform.position + Vector3.up * 0.6f, 10f * dt);
        if (d < 0.7f)
        {
            p.Health.HealPercent(healPercent);
            Destroy(gameObject);
        }
    }
}
