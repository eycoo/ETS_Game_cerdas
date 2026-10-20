using System.Collections.Generic;
using UnityEngine;

// Tanda bahaya di lantai sebelum serangan musuh (lingkaran atau persegi panjang yang terisi).
// Pemain yang adil selalu diberi peringatan ini, seperti di game komersial.
public class Telegraph : MonoBehaviour
{
    public static readonly List<Telegraph> Active = new List<Telegraph>();

    private Transform outline;
    private Transform fill;
    private MeshRenderer outlineRenderer;
    private MeshRenderer fillRenderer;
    private MaterialPropertyBlock mpbOutline;
    private MaterialPropertyBlock mpbFill;
    private Color color;
    private float duration;
    private float t;
    private bool isRect;
    private float size;
    private float length;
    private float spin;
    private bool autoDestroy;

    public float Progress => duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
    public bool IsDanger => spin == 0f;
    public bool IsRect => isRect;

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

    // Apakah titik p berada di area bahaya telegraph ini (dipakai AutoPilot).
    public bool Contains(Vector3 p, float margin)
    {
        if (!isRect)
            return CapyMath.FlatDistance(p, transform.position) < size * 0.5f + margin;

        Vector3 local = transform.InverseTransformPoint(new Vector3(p.x, transform.position.y, p.z));
        return local.z > -margin && local.z < length + margin && Mathf.Abs(local.x) < size * 0.5f + margin;
    }

    public static Telegraph Circle(Vector3 center, float radius, float duration, Color color, bool autoDestroy = false)
    {
        Telegraph tg = Create(GameDatabase.I.telegraphCircle, color, duration, autoDestroy);
        tg.isRect = false;
        tg.size = radius * 2f;
        tg.transform.position = new Vector3(center.x, center.y + 0.04f + Random.Range(0f, 0.01f), center.z);
        tg.Apply();
        return tg;
    }

    public static Telegraph Rect(Vector3 start, Vector3 dir, float length, float width, float duration, Color color, bool autoDestroy = false)
    {
        Telegraph tg = Create(GameDatabase.I.telegraphRect, color, duration, autoDestroy);
        tg.isRect = true;
        tg.size = width;
        tg.length = length;
        tg.SetLine(start, dir);
        tg.Apply();
        return tg;
    }

    public static Telegraph Spawn(Vector3 center, float radius, float duration, Color color)
    {
        Telegraph tg = Create(GameDatabase.I.spawnRune, color, duration, true);
        tg.isRect = false;
        tg.size = radius * 2f;
        tg.spin = 120f;
        tg.transform.position = new Vector3(center.x, 0.05f, center.z);
        tg.Apply();
        return tg;
    }

    private static Telegraph Create(Texture tex, Color color, float duration, bool autoDestroy)
    {
        GameObject go = new GameObject("Telegraph");
        Telegraph tg = go.AddComponent<Telegraph>();
        tg.color = color;
        tg.duration = Mathf.Max(0.01f, duration);
        tg.autoDestroy = autoDestroy;
        tg.mpbOutline = new MaterialPropertyBlock();
        tg.mpbFill = new MaterialPropertyBlock();
        tg.outlineRenderer = MakeQuad(go.transform, "Outline");
        tg.fillRenderer = MakeQuad(go.transform, "Fill");
        tg.outline = tg.outlineRenderer.transform;
        tg.fill = tg.fillRenderer.transform;
        tg.mpbOutline.SetTexture("_MainTex", tex);
        tg.mpbFill.SetTexture("_MainTex", tex);
        return tg;
    }

    private static MeshRenderer MakeQuad(Transform parent, string quadName)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = quadName;
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(parent, false);
        q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        MeshRenderer r = q.GetComponent<MeshRenderer>();
        r.sharedMaterial = GameDatabase.I.decalMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return r;
    }

    public void SetLine(Vector3 start, Vector3 dir)
    {
        dir = dir.Flat();
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.forward;
        transform.position = new Vector3(start.x, 0.045f, start.z);
        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    public void SetColor(Color c)
    {
        color = c;
    }

    private void Update()
    {
        t += Time.deltaTime;
        if (spin != 0f)
            transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
        Apply();

        if ((autoDestroy && t >= duration + 0.08f) || t > duration + 6f)
            Destroy(gameObject);
    }

    private void Apply()
    {
        float p = Progress;
        float pulse = p > 0.75f ? 0.5f + 0.5f * Mathf.Sin(t * 40f) : 0f;

        if (isRect)
        {
            outline.localScale = new Vector3(size, length, 1f);
            outline.localPosition = new Vector3(0f, 0f, length * 0.5f);
            fill.localScale = new Vector3(size, length * p, 1f);
            fill.localPosition = new Vector3(0f, 0.005f, length * p * 0.5f);
        }
        else
        {
            outline.localScale = new Vector3(size, size, 1f);
            float s = size * (spin != 0f ? CapyMath.EaseOutBack(Mathf.Min(1f, p * 3f)) : p);
            fill.localScale = new Vector3(s, s, 1f);
            fill.localPosition = new Vector3(0f, 0.005f, 0f);
        }

        mpbOutline.SetColor("_Color", color.WithAlpha(0.28f + pulse * 0.2f));
        mpbFill.SetColor("_Color", color.WithAlpha(spin != 0f ? 0.9f : 0.42f + pulse * 0.25f));
        outlineRenderer.SetPropertyBlock(mpbOutline);
        fillRenderer.SetPropertyBlock(mpbFill);
    }
}
