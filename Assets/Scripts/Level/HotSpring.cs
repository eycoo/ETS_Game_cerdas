using UnityEngine;

// Pemandian air panas (onsen) — kesukaan capybara. Memulihkan darah sekali pakai.
public class HotSpring : MonoBehaviour
{
    [SerializeField] private float healPercent = 1f;
    [SerializeField] private float radius = 1.7f;
    [SerializeField] private bool decorative;

    private bool used;
    private float riseT;

    public bool IsAvailable => !decorative && !used;

    private void Start()
    {
        if (!decorative)
            transform.localScale = Vector3.zero;
        else
            riseT = 1f;
    }

    private void Update()
    {
        riseT += Time.deltaTime * 2f;
        if (!decorative)
            transform.localScale = Vector3.one * CapyMath.EaseOutBack(Mathf.Clamp01(riseT));

        // Uap: laju tetap per detik, berhenti saat game di-pause (deltaTime = 0).
        if (Random.value < 8f * Time.deltaTime)
        {
            Vector3 p = transform.position + new Vector3(Random.Range(-1f, 1f), 0.3f, Random.Range(-1f, 1f));
            VFX.Glow(p, new Color(1f, 1f, 1f, used ? 0.08f : 0.14f), Random.Range(0.6f, 1.1f), 1.2f, Vector3.up * 0.8f);
        }

        if (decorative || used || riseT < 1f)
            return;

        PlayerController pl = PlayerController.I;
        if (pl != null && CapyMath.FlatDistance(pl.transform.position, transform.position) < radius && GameManager.I.IsPlaying)
        {
            used = true;
            pl.Health.HealPercent(healPercent);
            VFX.Poof(transform.position + Vector3.up * 0.5f, new Color(1f, 1f, 1f, 0.7f), 20, 1.5f);
            AudioManager.Sfx("splash", 0.8f);
            GameUI.I.ShowToast("Ahh... hot spring! HP fully restored", new Color(0.6f, 0.9f, 1f), true);
        }
    }
}
