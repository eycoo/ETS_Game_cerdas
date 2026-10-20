using System.Collections.Generic;
using UnityEngine;

// Gelombang kejut berbentuk cincin dari hantaman Golem (fase marah).
// Ketebalan cincin tetap walau jari-jarinya membesar, jadi mesh dibangun ulang tiap frame.
// Pemain bisa menghindar dengan dash menembus cincin (i-frame).
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Shockwave : MonoBehaviour
{
    private const int Segments = 72;

    public static readonly List<Shockwave> Active = new List<Shockwave>();

    public float CurrentRadius => radius;
    public float Speed => speed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Active.Clear();
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private float maxRadius;
    private float speed;
    private float damage;
    private float thickness = 0.7f;
    private float radius = 0.5f;
    private bool hitPlayer;
    private Color color;
    private Mesh mesh;
    private Vector3[] vertices;
    private MaterialPropertyBlock mpb;
    private MeshRenderer meshRenderer;

    public static void Spawn(Vector3 center, float maxRadius, float speed, float damage, Color color)
    {
        GameObject go = new GameObject("Shockwave");
        go.transform.position = new Vector3(center.x, 0.12f, center.z);
        Shockwave sw = go.AddComponent<Shockwave>();
        sw.maxRadius = maxRadius;
        sw.speed = speed;
        sw.damage = damage;
        sw.color = color;
        sw.Init();
    }

    private void Init()
    {
        mesh = new Mesh();
        mesh.name = "ShockwaveRing";
        vertices = new Vector3[(Segments + 1) * 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] tris = new int[Segments * 6];
        // Garis tengah tekstur lingkaran lembut dipakai sebagai pita: terang di tengah, pudar di tepi.
        for (int i = 0; i <= Segments; i++)
        {
            uv[i * 2] = new Vector2(0.5f, 0f);
            uv[i * 2 + 1] = new Vector2(0.5f, 1f);
        }
        for (int i = 0; i < Segments; i++)
        {
            int a = i * 2;
            tris[i * 6 + 0] = a;
            tris[i * 6 + 1] = a + 1;
            tris[i * 6 + 2] = a + 2;
            tris[i * 6 + 3] = a + 1;
            tris[i * 6 + 4] = a + 3;
            tris[i * 6 + 5] = a + 2;
        }
        Rebuild();
        mesh.uv = uv;
        mesh.triangles = tris;
        GetComponent<MeshFilter>().sharedMesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = GameDatabase.I.additiveMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        mpb = new MaterialPropertyBlock();
        mpb.SetTexture("_MainTex", GameDatabase.I.softCircle);
    }

    private void Rebuild()
    {
        float inner = Mathf.Max(0f, radius - thickness * 0.5f);
        float outer = radius + thickness * 0.5f;
        for (int i = 0; i <= Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            vertices[i * 2] = d * inner;
            vertices[i * 2 + 1] = d * outer + Vector3.up * 0.25f;
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    private void Update()
    {
        radius += speed * Time.deltaTime;
        Rebuild();

        float fade = 1f - Mathf.Clamp01((radius - maxRadius * 0.7f) / (maxRadius * 0.3f));
        mpb.SetColor("_Color", color.WithAlpha(color.a * fade));
        meshRenderer.SetPropertyBlock(mpb);

        PlayerController player = PlayerController.I;
        if (!hitPlayer && player != null)
        {
            float d = CapyMath.FlatDistance(player.transform.position, transform.position);
            if (Mathf.Abs(d - radius) < thickness * 0.5f + player.Radius * 0.5f && player.Health.CanBeHit)
            {
                player.Health.TakeDamage(damage, transform.position);
                hitPlayer = true;
            }
        }

        if (radius >= maxRadius)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }
}
