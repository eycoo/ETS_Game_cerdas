using UnityEngine;

// Efek partikel prosedural. Semua efek memakai beberapa ParticleSystem global (World space)
// dan memancarkan partikel lewat Emit(), jadi tidak ada prefab efek yang perlu diatur satu per satu.
public class VFX : MonoBehaviour
{
    private static VFX I;

    private ParticleSystem sparks;
    private ParticleSystem poof;
    private ParticleSystem sparkle;
    private ParticleSystem debris;
    private ParticleSystem glow;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        I = null;
    }

    public static void Ensure()
    {
        if (I != null)
            return;
        GameObject go = new GameObject("VFX");
        I = go.AddComponent<VFX>();
        I.Build();
    }

    private void Build()
    {
        GameDatabase db = GameDatabase.I;
        sparks = CreateSystem("Sparks", Mat(db.additiveMaterial, db.sparkTexture), ParticleSystemRenderMode.Stretch, 1.4f, 2f, true, false, null);
        poof = CreateSystem("Poof", Mat(db.decalMaterial, db.softCircle), ParticleSystemRenderMode.Billboard, -0.08f, 3f, false, true, null);
        sparkle = CreateSystem("Sparkle", Mat(db.additiveMaterial, db.starTexture), ParticleSystemRenderMode.Billboard, -0.25f, 1.2f, true, false, null);
        debris = CreateSystem("Debris", db.debrisMaterial, ParticleSystemRenderMode.Mesh, 2.6f, 0.4f, true, false, db.cubeMesh);
        glow = CreateSystem("Glow", Mat(db.additiveMaterial, db.softCircle), ParticleSystemRenderMode.Billboard, -0.3f, 2f, true, false, null);
    }

    private static Material Mat(Material baseMaterial, Texture texture)
    {
        Material m = new Material(baseMaterial);
        m.mainTexture = texture;
        return m;
    }

    private ParticleSystem CreateSystem(string systemName, Material mat, ParticleSystemRenderMode mode, float gravity, float drag, bool shrink, bool grow, Mesh mesh)
    {
        GameObject go = new GameObject(systemName);
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 4000;
        main.gravityModifier = gravity;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.startLifetime = 1f;
        main.startRotation3D = mesh != null;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        if (shrink || grow)
        {
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve curve = grow ? AnimationCurve.EaseInOut(0f, 0.55f, 1f, 1.5f) : AnimationCurve.Linear(0f, 1f, 1f, 0f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        if (drag > 0f)
        {
            ParticleSystem.LimitVelocityOverLifetimeModule lim = ps.limitVelocityOverLifetime;
            lim.enabled = true;
            lim.limit = 1000f;
            lim.drag = drag;
            lim.multiplyDragByParticleSize = false;
            lim.multiplyDragByParticleVelocity = false;
        }

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = mode;
        if (mode == ParticleSystemRenderMode.Stretch)
        {
            r.velocityScale = 0.05f;
            r.lengthScale = 2f;
        }
        if (mesh != null)
        {
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = mesh;
        }
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        ps.Play();
        return ps;
    }

    private static void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, Color color, float size, float life, bool mesh)
    {
        ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();
        ep.position = pos;
        ep.velocity = vel;
        ep.startColor = color;
        ep.startSize = size;
        ep.startLifetime = life;
        ep.applyShapeToPosition = false;
        if (mesh)
        {
            ep.rotation3D = Random.insideUnitSphere * 180f;
            ep.angularVelocity3D = Random.insideUnitSphere * 500f;
        }
        else
        {
            ep.rotation = Random.Range(0f, 360f);
        }
        ps.Emit(ep, 1);
    }

    public static void Sparks(Vector3 pos, Color color, int count = 10, float speed = 7f)
    {
        if (I == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 0.8f + 0.15f;
            Emit(I.sparks, pos, dir.normalized * speed * Random.Range(0.4f, 1f), color, Random.Range(0.07f, 0.15f), Random.Range(0.18f, 0.38f), false);
        }
    }

    public static void Poof(Vector3 pos, Color color, int count = 12, float size = 0.9f)
    {
        if (I == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Random.insideUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 0.6f;
            Emit(I.poof, pos + dir * 0.3f, dir * Random.Range(1f, 3.2f), color, size * Random.Range(0.6f, 1.2f), Random.Range(0.45f, 0.8f), false);
        }
    }

    public static void Debris(Vector3 pos, Color color, int count = 8, float speed = 5f)
    {
        if (I == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Random.insideUnitSphere;
            dir.y = Mathf.Abs(dir.y) + 0.7f;
            Emit(I.debris, pos, dir.normalized * speed * Random.Range(0.5f, 1f), color, Random.Range(0.1f, 0.26f), Random.Range(0.6f, 1.0f), true);
        }
    }

    public static void Sparkle(Vector3 pos, Color color, int count = 12)
    {
        if (I == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 vel = (Random.insideUnitSphere + Vector3.up * 1.4f) * 1.6f;
            Emit(I.sparkle, pos + Random.insideUnitSphere * 0.6f, vel, color, Random.Range(0.18f, 0.4f), Random.Range(0.5f, 0.95f), false);
        }
    }

    public static void Glow(Vector3 pos, Color color, float size, float life, Vector3 velocity)
    {
        if (I == null) return;
        Emit(I.glow, pos, velocity, color, size, life, false);
    }

    public static void Fire(Vector3 pos)
    {
        if (I == null) return;
        Color c = Color.Lerp(new Color(1f, 0.35f, 0.05f), new Color(1f, 0.8f, 0.2f), Random.value);
        Emit(I.glow, pos + Random.insideUnitSphere * 0.3f, Vector3.up * Random.Range(1f, 2f), c, Random.Range(0.25f, 0.45f), Random.Range(0.25f, 0.45f), false);
    }

    public static void Ring(Vector3 pos, Color color, float radius, float duration = 0.35f)
    {
        RingPulse.Spawn(pos, color, radius, duration);
    }
}
