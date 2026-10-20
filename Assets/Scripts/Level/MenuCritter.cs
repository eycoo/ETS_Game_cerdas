using UnityEngine;

// Hiasan menu utama: makhluk kecil yang melompat-lompat di tempat sambil menoleh.
public class MenuCritter : MonoBehaviour
{
    [SerializeField] private Transform body;
    [SerializeField] private float hopInterval = 1.4f;
    [SerializeField] private float hopHeight = 0.5f;

    private float timer;
    private float hopT = -1f;
    private Quaternion targetRot;

    private void Start()
    {
        if (body == null)
            body = transform;
        timer = Random.Range(0f, hopInterval);
        targetRot = transform.rotation;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = hopInterval * Random.Range(0.7f, 1.3f);
            hopT = 0f;
            targetRot = Quaternion.Euler(0f, Random.Range(-60f, 60f) + 180f, 0f);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 3f);

        if (hopT >= 0f)
        {
            hopT += Time.deltaTime / 0.45f;
            float k = Mathf.Clamp01(hopT);
            body.localPosition = new Vector3(0f, Mathf.Sin(k * Mathf.PI) * hopHeight, 0f);
            body.localScale = k < 1f ? new Vector3(0.9f, 1.15f, 0.9f) : Vector3.one;
            if (hopT >= 1f)
                hopT = -1f;
        }
        else
        {
            float b = Mathf.Sin(Time.time * 3f) * 0.04f;
            body.localScale = new Vector3(1f + b, 1f - b, 1f + b);
        }
    }
}
