using UnityEngine;

// Batu yang dilempar melengkung oleh Golem. Titik jatuhnya ditandai lingkaran merah.
public class LobProjectile : MonoBehaviour
{
    [SerializeField] private float damageRadius = 1.4f;
    [SerializeField] private Transform spinner;
    [SerializeField] private Color dustColor = new Color(0.62f, 0.6f, 0.58f);

    private Vector3 start;
    private Vector3 target;
    private float duration;
    private float height;
    private float damage;
    private float t;
    private bool launched;
    private Telegraph marker;
    private Vector3 spinAxis;

    public void Launch(Vector3 from, Vector3 to, float flightTime, float arcHeight, float hitDamage)
    {
        start = from;
        target = new Vector3(to.x, 0f, to.z);
        duration = Mathf.Max(0.2f, flightTime);
        height = arcHeight;
        damage = hitDamage;
        launched = true;
        spinAxis = Random.onUnitSphere;
        marker = Telegraph.Circle(target, damageRadius, duration, new Color(1f, 0.3f, 0.2f));
        transform.position = start;
    }

    private void Update()
    {
        if (!launched)
            return;

        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        Vector3 p = Vector3.Lerp(start, target, k);
        p.y = Mathf.Lerp(start.y, 0.3f, k) + Mathf.Sin(k * Mathf.PI) * height;
        transform.position = p;
        if (spinner != null)
            spinner.Rotate(spinAxis, 360f * Time.deltaTime, Space.World);

        if (k >= 1f)
            Land();
    }

    private void Land()
    {
        PlayerController player = PlayerController.I;
        if (player != null && CapyMath.FlatDistance(player.transform.position, target) < damageRadius + player.Radius * 0.5f)
            player.Health.TakeDamage(damage, target);

        VFX.Debris(target + Vector3.up * 0.3f, dustColor, 10, 6f);
        VFX.Poof(target + Vector3.up * 0.2f, dustColor.WithAlpha(0.7f), 10, 1.2f);
        VFX.Ring(target, dustColor, damageRadius * 1.4f, 0.35f);
        CameraRig.Shake(0.15f);
        AudioManager.Sfx("rock_land", 0.6f);

        if (marker != null)
            Destroy(marker.gameObject);
        Destroy(gameObject);
    }
}
