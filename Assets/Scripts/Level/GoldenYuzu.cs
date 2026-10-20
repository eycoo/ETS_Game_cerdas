using UnityEngine;

// Golden Yuzu: muncul setelah Golem King kalah. Ambil untuk menang.
public class GoldenYuzu : MonoBehaviour
{
    private float t;
    private bool collected;
    private Vector3 basePos;

    public static void Spawn(Vector3 pos)
    {
        GameObject prefab = GameDatabase.I.goldenYuzu;
        if (prefab == null)
            return;
        GameObject go = Instantiate(prefab, new Vector3(pos.x, 0f, pos.z), Quaternion.identity);
        go.GetComponent<GoldenYuzu>().basePos = new Vector3(pos.x, 0f, pos.z);
    }

    private void Update()
    {
        t += Time.deltaTime;
        float rise = CapyMath.EaseOutBack(Mathf.Clamp01(t / 1.2f));
        transform.position = basePos + Vector3.up * (0.2f + rise * 1.3f + Mathf.Sin(t * 2.5f) * 0.15f);
        transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        transform.localScale = Vector3.one * Mathf.Max(0.01f, rise);

        if (Random.value < 18f * Time.deltaTime)
            VFX.Sparkle(transform.position, new Color(1f, 0.85f, 0.3f), 1);

        PlayerController p = PlayerController.I;
        if (collected || p == null || t < 1.2f)
            return;
        if (CapyMath.FlatDistance(p.transform.position, basePos) < 1.5f)
        {
            collected = true;
            VFX.Sparkle(transform.position, new Color(1f, 0.85f, 0.3f), 50);
            VFX.Ring(basePos, new Color(1f, 0.85f, 0.35f), 5f, 0.6f);
            AudioManager.Sfx("levelup", 1f, 1.2f);
            GameManager.I.OnGoldenYuzuCollected();
            gameObject.SetActive(false);
        }
    }
}
