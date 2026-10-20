using UnityEngine;

// Gerbang jeruji: turun ke lantai saat terbuka, naik saat ruangan dikunci.
public class Door : MonoBehaviour
{
    [SerializeField] private Transform bars;
    [SerializeField] private Collider blocker;
    [SerializeField] private float openDepth = 3.4f;
    [SerializeField] private bool startOpen = true;
    [SerializeField] private Light statusLight;

    private bool open;
    private Vector3 closedLocal;

    public bool IsOpen => open;

    private void Awake()
    {
        if (bars != null)
            closedLocal = bars.localPosition;
        open = startOpen;
        if (bars != null)
            bars.localPosition = closedLocal + (open ? Vector3.down * openDepth : Vector3.zero);
        if (blocker != null)
            blocker.enabled = !open;
        UpdateLight();
    }

    public void Open()
    {
        if (open)
            return;
        open = true;
        if (blocker != null)
            blocker.enabled = false;
        AudioManager.Sfx("door", 0.7f);
        VFX.Poof(transform.position + Vector3.up * 0.3f, new Color(0.7f, 0.68f, 0.65f, 0.6f), 8, 1f);
        UpdateLight();
    }

    public void Close()
    {
        if (!open)
            return;
        open = false;
        if (blocker != null)
            blocker.enabled = true;
        AudioManager.Sfx("door", 0.7f, 0.85f);
        VFX.Poof(transform.position + Vector3.up * 0.3f, new Color(0.7f, 0.68f, 0.65f, 0.6f), 8, 1f);
        CameraRig.Shake(0.12f);
        UpdateLight();
    }

    private void UpdateLight()
    {
        if (statusLight != null)
            statusLight.color = open ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.35f, 0.25f);
    }

    private void Update()
    {
        if (bars == null)
            return;
        Vector3 target = closedLocal + (open ? Vector3.down * openDepth : Vector3.zero);
        bars.localPosition = Vector3.MoveTowards(bars.localPosition, target, Time.deltaTime * 9f);
    }
}
