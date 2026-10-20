using UnityEngine;

// Indeks layer harus sama dengan ProjectSettings/TagManager.asset.
public static class Layers
{
    public const int Player = 8;
    public const int Obstacle = 9;
    public const int Enemy = 10;
    public const int Flying = 11;

    public const int ObstacleMask = 1 << Obstacle;
}

public static class CapyMath
{
    public static Vector3 Flat(this Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    public static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    public static float FlatDistanceSq(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    public static Vector3 DirectionTo(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        d.y = 0f;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
    }

    // Garis pandang: true jika tidak ada tembok atau obstacle di antara dua titik.
    public static bool HasLineOfSight(Vector3 from, Vector3 to, float radius = 0.2f)
    {
        Vector3 a = new Vector3(from.x, 0.8f, from.z);
        Vector3 b = new Vector3(to.x, 0.8f, to.z);
        Vector3 d = b - a;
        float dist = d.magnitude;
        if (dist < 0.05f)
            return true;

        return !Physics.SphereCast(a, radius, d / dist, out _, dist, Layers.ObstacleMask, QueryTriggerInteraction.Ignore);
    }

    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        t -= 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }

    public static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }

    public static float EaseInOutSine(float t)
    {
        return -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(t)) - 1f) * 0.5f;
    }

    public static string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    public static Color WithAlpha(this Color c, float a)
    {
        c.a = a;
        return c;
    }
}
