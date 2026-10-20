using UnityEngine;

// Peti harta setelah Ruangan 2: membuka satu pilihan upgrade gratis.
public class TreasureChest : MonoBehaviour
{
    [SerializeField] private Transform lid;
    [SerializeField] private Light glow;

    private bool opened;
    private float openT;
    private float riseT;

    public bool IsOpened => opened;

    private void Start()
    {
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        riseT += Time.deltaTime * 2.5f;
        transform.localScale = Vector3.one * CapyMath.EaseOutBack(Mathf.Clamp01(riseT));

        if (!opened)
        {
            if (glow != null)
                glow.intensity = 2.5f + Mathf.Sin(Time.time * 4f) * 0.8f;

            PlayerController p = PlayerController.I;
            if (p != null && riseT > 1f && CapyMath.FlatDistance(p.transform.position, transform.position) < 1.7f && GameManager.I.IsPlaying)
                Open();
            return;
        }

        openT += Time.deltaTime * 3f;
        if (lid != null)
            lid.localRotation = Quaternion.Euler(-110f * CapyMath.EaseOutBack(Mathf.Clamp01(openT)), 0f, 0f);
        if (glow != null)
            glow.intensity = Mathf.Lerp(glow.intensity, 0f, Time.deltaTime * 2f);
    }

    private void Open()
    {
        opened = true;
        VFX.Sparkle(transform.position + Vector3.up, new Color(1f, 0.85f, 0.3f), 36);
        VFX.Ring(transform.position, new Color(1f, 0.85f, 0.35f), 2.5f, 0.45f);
        GameUI.I.ShowToast("TREASURE!", new Color(1f, 0.85f, 0.35f), true);
        GameManager.I.QueueUpgrade(true);
    }
}
