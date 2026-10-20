using System.Collections.Generic;
using UnityEngine;

// Kumpulan steering behavior (materi Praktikum 03). Semua fungsi mengembalikan
// kecepatan/gaya yang diinginkan pada bidang XZ.
public static class Steering
{
    public static Vector3 Seek(Vector3 position, Vector3 target, float maxSpeed)
    {
        return CapyMath.DirectionTo(position, target) * maxSpeed;
    }

    public static Vector3 Flee(Vector3 position, Vector3 threat, float maxSpeed)
    {
        return -CapyMath.DirectionTo(position, threat) * maxSpeed;
    }

    // Arrive: melambat saat mendekati target, berhenti tepat di target.
    public static Vector3 Arrive(Vector3 position, Vector3 target, float maxSpeed, float slowRadius)
    {
        Vector3 d = (target - position).Flat();
        float dist = d.magnitude;
        if (dist < 0.05f)
            return Vector3.zero;
        float speed = maxSpeed * Mathf.Min(1f, dist / Mathf.Max(0.01f, slowRadius));
        return d / dist * speed;
    }

    // Pursue: mengejar posisi target di masa depan berdasarkan kecepatannya.
    public static Vector3 Pursue(Vector3 position, Vector3 target, Vector3 targetVelocity, float maxSpeed, float maxPrediction)
    {
        float dist = CapyMath.FlatDistance(position, target);
        float prediction = Mathf.Min(maxPrediction, dist / Mathf.Max(0.1f, maxSpeed));
        return Seek(position, target + targetVelocity.Flat() * prediction, maxSpeed);
    }

    // Wander: titik di lingkaran depan agen yang bergeser acak sedikit demi sedikit.
    public static Vector3 Wander(ref float wanderAngle, Vector3 forward, float jitter, float dt)
    {
        wanderAngle += Random.Range(-jitter, jitter) * dt;
        forward = forward.Flat();
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        Vector3 circleCenter = forward.normalized * 1.5f;
        Vector3 offset = new Vector3(Mathf.Cos(wanderAngle), 0f, Mathf.Sin(wanderAngle));
        return (circleCenter + offset).normalized;
    }

    // Separation: menjauh dari tetangga yang terlalu dekat (makin dekat, makin kuat).
    public static Vector3 Separation(EnemyBase self, float range, List<EnemyBase> others)
    {
        Vector3 force = Vector3.zero;
        Vector3 pos = self.transform.position;
        for (int i = 0; i < others.Count; i++)
        {
            EnemyBase o = others[i];
            if (o == null || o == self || !o.IsAlive || o.IsFlying != self.IsFlying)
                continue;

            Vector3 d = (pos - o.transform.position).Flat();
            float min = range + o.Radius;
            float sq = d.sqrMagnitude;
            if (sq > min * min)
                continue;

            float dist = Mathf.Sqrt(sq);
            if (dist < 0.001f)
            {
                force += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                continue;
            }
            force += d / dist * (1f - dist / min);
        }
        return force;
    }

    // Menghindari tembok dengan tiga sensor (depan, kiri, kanan).
    public static Vector3 AvoidWalls(Vector3 position, Vector3 velocity, float lookAhead)
    {
        Vector3 dir = velocity.Flat();
        if (dir.sqrMagnitude < 0.01f)
            return Vector3.zero;
        dir.Normalize();

        Vector3 origin = new Vector3(position.x, 0.6f, position.z);
        Vector3 force = Vector3.zero;
        Vector3[] probes =
        {
            dir,
            Quaternion.Euler(0f, 35f, 0f) * dir,
            Quaternion.Euler(0f, -35f, 0f) * dir
        };

        for (int i = 0; i < probes.Length; i++)
        {
            float len = i == 0 ? lookAhead : lookAhead * 0.7f;
            RaycastHit hit;
            if (Physics.Raycast(origin, probes[i], out hit, len, Layers.ObstacleMask, QueryTriggerInteraction.Ignore))
                force += hit.normal.Flat() * (1f - hit.distance / len);
        }
        return force;
    }
}
