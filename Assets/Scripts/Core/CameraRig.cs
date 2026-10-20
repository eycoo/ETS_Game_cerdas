using UnityEngine;

// Kamera top-down miring ala Archero: mengikuti Player dengan halus, sedikit ditarik ke
// tengah ruangan, punya look-ahead, guncangan (trauma), dan fokus sinematik untuk intro boss.
public class CameraRig : MonoBehaviour
{
    public static CameraRig I { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private float pitch = 56f;
    [SerializeField] private float distance = 25f;
    [SerializeField] private float smoothTime = 0.14f;
    [SerializeField] private float lookAhead = 1.2f;
    [SerializeField] private float roomPull = 0.3f;

    private Vector3 focus;
    private Vector3 focusVelocity;
    private float trauma;
    private bool overrideActive;
    private Vector3 overridePoint;
    private float overrideWeight;
    private float overrideZoom;
    private PlayerController player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        I = null;
    }

    public static void Shake(float amount)
    {
        if (I != null)
            I.trauma = Mathf.Clamp01(I.trauma + amount);
    }

    private void Awake()
    {
        I = this;
    }

    private void Start()
    {
        if (target == null && PlayerController.I != null)
            target = PlayerController.I.transform;
        SnapToTarget();
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    public void Focus(Vector3 point, float zoom)
    {
        overrideActive = true;
        overridePoint = point;
        overrideZoom = zoom;
    }

    public void ReleaseFocus()
    {
        overrideActive = false;
    }

    public void SnapToTarget()
    {
        if (target != null)
            focus = DesiredFocus();
        focusVelocity = Vector3.zero;
        Apply(0f);
    }

    private Vector3 DesiredFocus()
    {
        Vector3 desired = target.position;

        if (player == null)
            player = target.GetComponent<PlayerController>();

        if (player != null)
        {
            desired += player.Velocity.Flat() * (lookAhead / Mathf.Max(1f, player.Stats.MoveSpeed));

            GameManager gm = GameManager.I;
            if (gm != null && gm.CurrentRoom != null && gm.CurrentRoom.Contains(target.position, 0f))
                desired = Vector3.Lerp(desired, gm.CurrentRoom.transform.position, roomPull);
        }

        desired.y = 0f;
        return desired;
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        Vector3 desired = target != null ? DesiredFocus() : focus;

        overrideWeight = Mathf.MoveTowards(overrideWeight, overrideActive ? 1f : 0f, dt * 1.6f);
        float w = CapyMath.EaseInOutSine(overrideWeight);
        desired = Vector3.Lerp(desired, overridePoint, w);

        focus = Vector3.SmoothDamp(focus, desired, ref focusVelocity, smoothTime, Mathf.Infinity, dt);
        Apply(dt);
    }

    private void Apply(float dt)
    {
        float dist = distance + overrideZoom * CapyMath.EaseInOutSine(overrideWeight);
        Quaternion rot = Quaternion.Euler(pitch, 0f, 0f);
        Vector3 pos = focus + Vector3.up * 0.6f - rot * Vector3.forward * dist;

        float shake = trauma * trauma;
        float t = Time.unscaledTime * 24f;
        Vector3 offset = new Vector3(Mathf.PerlinNoise(t, 0.37f) - 0.5f, Mathf.PerlinNoise(0.71f, t) - 0.5f, 0f) * 1.3f * shake;
        float roll = (Mathf.PerlinNoise(t * 0.8f, t * 0.6f) - 0.5f) * 5f * shake;

        transform.SetPositionAndRotation(pos + rot * offset, rot * Quaternion.Euler(0f, 0f, roll));
        trauma = Mathf.Max(0f, trauma - dt * 1.9f);
    }
}
