using UnityEngine;

// Bayangan bulat di lantai: selalu di y=0 di bawah induknya (berguna untuk kelelawar dan batu terbang).
public class BlobShadow : MonoBehaviour
{
    [SerializeField] private float size = 1f;

    private void LateUpdate()
    {
        Transform parent = transform.parent;
        if (parent == null)
            return;
        Vector3 p = parent.position;
        transform.position = new Vector3(p.x, 0.03f, p.z);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        float h = Mathf.Clamp01(p.y / 6f);
        float s = size * (1f - h * 0.5f);
        transform.localScale = new Vector3(s / Mathf.Max(0.001f, parent.lossyScale.x), s / Mathf.Max(0.001f, parent.lossyScale.z), 1f);
    }
}
