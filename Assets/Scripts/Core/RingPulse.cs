using UnityEngine;

// Cincin yang membesar lalu memudar di lantai (dipakai untuk benturan, level up, dsb).
public class RingPulse : MonoBehaviour
{
    private MeshRenderer quad;
    private MaterialPropertyBlock mpb;
    private Color color;
    private float radius;
    private float duration;
    private float t;

    public static void Spawn(Vector3 pos, Color color, float radius, float duration)
    {
        GameDatabase db = GameDatabase.I;
        if (db == null) return;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "RingPulse";
        Destroy(go.GetComponent<Collider>());
        go.transform.position = new Vector3(pos.x, pos.y + 0.06f, pos.z);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        RingPulse rp = go.AddComponent<RingPulse>();
        rp.quad = go.GetComponent<MeshRenderer>();
        rp.quad.sharedMaterial = db.additiveMaterial;
        rp.quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rp.quad.receiveShadows = false;
        rp.mpb = new MaterialPropertyBlock();
        rp.mpb.SetTexture("_MainTex", db.ringTexture);
        rp.color = color;
        rp.radius = radius;
        rp.duration = duration;
        rp.Apply(0f);
    }

    private void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        Apply(k);
        if (k >= 1f)
            Destroy(gameObject);
    }

    private void Apply(float k)
    {
        float s = Mathf.Lerp(0.2f, 1f, CapyMath.EaseOutCubic(k)) * radius * 2f;
        transform.localScale = new Vector3(s, s, 1f);
        mpb.SetColor("_Color", color.WithAlpha(color.a * (1f - k)));
        quad.SetPropertyBlock(mpb);
    }
}
