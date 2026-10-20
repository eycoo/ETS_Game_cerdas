using UnityEngine;

// Animasi prosedural Capy (tanpa Animator): menghadap arah, langkah memantul, ayunan pedang,
// kilatan saat terkena, berkedip saat kebal, jatuh saat kalah, dan lompat gembira saat menang.
public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform body;
    [SerializeField] private Transform swordPivot;
    [SerializeField] private Transform footL;
    [SerializeField] private Transform footR;
    [SerializeField] private Transform scarfTail;
    [SerializeField] private Transform yuzu;

    private PlayerController player;
    private MeshRenderer[] renderers;
    private Material[][] originals;
    private Material[][] flashes;
    private bool flashing;
    private bool hidden;
    private float flashUntil;
    private float blinkUntil;
    private float walkPhase;
    private float attackT = 1f;
    private float squash;
    private float celebrateT = -1f;
    private float deathT = -1f;
    private Vector3 footLBase;
    private Vector3 footRBase;
    private Quaternion swordBase;
    private Vector3 yuzuBase;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        if (visualRoot == null) visualRoot = transform.Find("Visual");
        if (body == null) body = visualRoot.Find("Body");
        if (swordPivot == null) swordPivot = body.Find("SwordPivot");
        if (footL == null) footL = body.Find("FootL");
        if (footR == null) footR = body.Find("FootR");
        if (scarfTail == null) scarfTail = body.Find("ScarfTail");
        if (yuzu == null) yuzu = body.Find("Yuzu");

        footLBase = footL != null ? footL.localPosition : Vector3.zero;
        footRBase = footR != null ? footR.localPosition : Vector3.zero;
        swordBase = swordPivot != null ? swordPivot.localRotation : Quaternion.identity;
        yuzuBase = yuzu != null ? yuzu.localPosition : Vector3.zero;

        renderers = visualRoot.GetComponentsInChildren<MeshRenderer>(true);
        originals = new Material[renderers.Length][];
        flashes = new Material[renderers.Length][];
        Material flash = GameDatabase.I != null ? GameDatabase.I.flashMaterial : null;
        for (int i = 0; i < renderers.Length; i++)
        {
            originals[i] = renderers[i].sharedMaterials;
            flashes[i] = new Material[originals[i].Length];
            for (int k = 0; k < flashes[i].Length; k++)
                flashes[i][k] = flash;
        }
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        float time = Time.time;

        if (deathT >= 0f)
        {
            deathT += dt;
            float k = Mathf.Clamp01(deathT / 0.5f);
            body.localRotation = Quaternion.Euler(0f, 0f, 90f * CapyMath.EaseOutCubic(k));
            body.localPosition = new Vector3(0f, -0.25f * k, 0f);
            SetFlash(false);
            SetHidden(false);
            return;
        }

        // Menghadap arah gerak atau target.
        Quaternion targetRot = Quaternion.LookRotation(player.Facing);
        visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, targetRot, 1f - Mathf.Exp(-18f * dt));

        float speed01 = Mathf.Clamp01(player.Velocity.Flat().magnitude / Mathf.Max(0.1f, player.Stats.MoveSpeed));
        walkPhase += dt * (6f + 9f * speed01);
        float bob = Mathf.Abs(Mathf.Sin(walkPhase)) * 0.13f * speed01;
        float breathe = Mathf.Sin(time * 2.4f) * 0.025f * (1f - speed01);
        squash = Mathf.MoveTowards(squash, 0f, dt * 4f);

        float jump = 0f;
        float spin = 0f;
        if (celebrateT >= 0f)
        {
            celebrateT += Time.unscaledDeltaTime;
            float phase = (celebrateT % 0.7f) / 0.7f;
            jump = Mathf.Sin(phase * Mathf.PI) * 0.7f;
            spin = celebrateT * 360f;
        }

        body.localPosition = new Vector3(0f, bob + jump, 0f);
        body.localScale = new Vector3(1f + squash * 0.22f - breathe, 1f - squash * 0.28f + breathe, 1f + squash * 0.22f - breathe);
        body.localRotation = Quaternion.Euler(speed01 * 7f, spin, Mathf.Sin(walkPhase) * 5f * speed01);

        if (footL != null)
        {
            footL.localPosition = footLBase + new Vector3(0f, Mathf.Max(0f, Mathf.Sin(walkPhase)) * 0.12f, Mathf.Cos(walkPhase) * 0.14f) * speed01;
            footR.localPosition = footRBase + new Vector3(0f, Mathf.Max(0f, -Mathf.Sin(walkPhase)) * 0.12f, -Mathf.Cos(walkPhase) * 0.14f) * speed01;
        }

        // Ayunan pedang: cepat ke depan lalu kembali.
        attackT += dt / 0.2f;
        if (swordPivot != null)
        {
            float swing = attackT < 1f ? Mathf.Sin(attackT * Mathf.PI) : 0f;
            float idleSway = Mathf.Sin(time * 2f) * 4f;
            swordPivot.localRotation = swordBase * Quaternion.Euler(swing * 95f + idleSway, -swing * 50f, 0f);
        }

        if (scarfTail != null)
            scarfTail.localRotation = Quaternion.Euler(20f * speed01 + Mathf.Sin(time * 5f) * 6f, 0f, Mathf.Sin(time * 3.3f) * 8f);

        if (yuzu != null)
            yuzu.localPosition = yuzuBase + new Vector3(0f, Mathf.Abs(Mathf.Sin(walkPhase)) * 0.05f * speed01, 0f);

        SetFlash(time < flashUntil);
        bool blink = time < blinkUntil && Mathf.FloorToInt(time * 16f) % 2 == 0;
        SetHidden(blink && !flashing);
    }

    private void SetFlash(bool on)
    {
        if (on == flashing)
            return;
        flashing = on;
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sharedMaterials = on ? flashes[i] : originals[i];
    }

    private void SetHidden(bool on)
    {
        if (on == hidden)
            return;
        hidden = on;
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = !on;
    }

    public void OnAttack()
    {
        attackT = 0f;
        squash = Mathf.Max(squash, 0.25f);
    }

    public void OnDash()
    {
        squash = -0.7f;
    }

    public void OnHurt(float invulnerability)
    {
        flashUntil = Time.time + 0.1f;
        blinkUntil = Time.time + invulnerability;
        squash = 0.6f;
    }

    public void OnDeath()
    {
        deathT = 0f;
    }

    public void Celebrate()
    {
        celebrateT = 0f;
    }
}
