using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI yang menempel pada objek dunia: bar darah musuh, bar darah Capy, angka damage,
// dan label AI (saat AI Vision aktif) yang menuliskan teknik + state AI tiap musuh.
public class WorldUI : MonoBehaviour
{
    public static WorldUI I { get; private set; }

    private class Bar
    {
        public RectTransform root;
        public Image fill;
        public Image bg;
        public Text label;
        public bool used;
    }

    private class FloatingNumber
    {
        public RectTransform rt;
        public Text text;
        public Vector3 world;
        public float t;
        public float life;
        public float scale;
        public Vector3 drift;
    }

    private Canvas canvas;
    private RectTransform canvasRect;
    private Camera cam;
    private readonly Dictionary<EnemyBase, Bar> bars = new Dictionary<EnemyBase, Bar>();
    private readonly Stack<Bar> barPool = new Stack<Bar>();
    private readonly List<EnemyBase> removeBuffer = new List<EnemyBase>();
    private readonly List<FloatingNumber> numbers = new List<FloatingNumber>();
    private readonly Stack<FloatingNumber> numberPool = new Stack<FloatingNumber>();
    private Bar playerBar;
    private RectTransform barLayer;
    private RectTransform numberLayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        I = null;
    }

    public static void Ensure()
    {
        if (I != null)
            return;
        GameObject go = new GameObject("WorldUI");
        I = go.AddComponent<WorldUI>();
        I.Build();
    }

    private void Build()
    {
        canvas = UIFactory.Canvas("WorldCanvas", 5);
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        canvasRect = canvas.GetComponent<RectTransform>();
        barLayer = UIFactory.Rect("Bars", canvas.transform);
        barLayer.Stretch();
        numberLayer = UIFactory.Rect("Numbers", canvas.transform);
        numberLayer.Stretch();
        playerBar = CreateBar(new Color(0.4f, 0.9f, 0.45f), 96f);
    }

    private Bar CreateBar(Color fillColor, float width)
    {
        GameDatabase db = GameDatabase.I;
        Bar b = new Bar();
        b.root = UIFactory.Rect("Bar", barLayer);
        b.root.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 14f));
        b.bg = UIFactory.Image(b.root, "Bg", db.roundedSmall, new Color(0.06f, 0.04f, 0.1f, 0.85f));
        b.bg.rectTransform.Stretch();
        b.fill = UIFactory.Image(b.root, "Fill", db.roundedSmall, fillColor);
        RectTransform rt = b.fill.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(2f, 2f);
        rt.offsetMax = new Vector2(-2f, -2f);
        b.label = UIFactory.Text(b.root, "AI", "", 20, Color.white, TextAnchor.LowerCenter, true, 2f);
        b.label.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(420f, 30f));
        return b;
    }

    public static void Number(Vector3 world, int amount, NumberStyle style)
    {
        if (I == null)
            return;
        I.SpawnNumber(world, amount, style);
    }

    private void SpawnNumber(Vector3 world, int amount, NumberStyle style)
    {
        FloatingNumber n = numberPool.Count > 0 ? numberPool.Pop() : CreateNumber();
        n.rt.gameObject.SetActive(true);
        n.world = world + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.2f, 0.2f));
        n.t = 0f;
        n.drift = new Vector3(Random.Range(-0.6f, 0.6f), 1.6f, 0f);

        switch (style)
        {
            case NumberStyle.Crit:
                n.text.text = amount + "!";
                n.text.color = new Color(1f, 0.85f, 0.25f);
                n.text.fontSize = 50;
                n.scale = 1.4f;
                n.life = 0.9f;
                break;
            case NumberStyle.Burn:
                n.text.text = amount.ToString();
                n.text.color = new Color(1f, 0.55f, 0.15f);
                n.text.fontSize = 28;
                n.scale = 1f;
                n.life = 0.6f;
                break;
            case NumberStyle.PlayerHurt:
                n.text.text = "-" + amount;
                n.text.color = new Color(1f, 0.3f, 0.25f);
                n.text.fontSize = 46;
                n.scale = 1.3f;
                n.life = 0.9f;
                break;
            case NumberStyle.Heal:
                n.text.text = "+" + amount;
                n.text.color = new Color(0.45f, 1f, 0.5f);
                n.text.fontSize = 42;
                n.scale = 1.2f;
                n.life = 1f;
                break;
            default:
                n.text.text = amount.ToString();
                n.text.color = Color.white;
                n.text.fontSize = 36;
                n.scale = 1f;
                n.life = 0.7f;
                break;
        }
        numbers.Add(n);
    }

    private FloatingNumber CreateNumber()
    {
        FloatingNumber n = new FloatingNumber();
        n.text = UIFactory.Text(numberLayer, "Num", "", 36, Color.white, TextAnchor.MiddleCenter, true, 2.5f);
        n.rt = n.text.rectTransform;
        n.rt.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 60f));
        return n;
    }

    private bool ToCanvas(Vector3 world, out Vector2 local)
    {
        Vector3 sp = cam.WorldToScreenPoint(world);
        if (sp.z < 0f)
        {
            local = Vector2.zero;
            return false;
        }
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out local);
    }

    private void LateUpdate()
    {
        cam = Camera.main;
        if (cam == null)
            return;

        bool vision = GameManager.I != null && GameManager.I.AIVision;
        UpdateEnemyBars(vision);
        UpdatePlayerBar(vision);
        UpdateNumbers();
    }

    private void UpdateEnemyBars(bool vision)
    {
        foreach (KeyValuePair<EnemyBase, Bar> kv in bars)
            kv.Value.used = false;

        for (int i = 0; i < EnemyBase.Active.Count; i++)
        {
            EnemyBase e = EnemyBase.Active[i];
            if (e == null || !e.IsAlive)
                continue;
            if (e.IsBoss && !vision)
                continue;
            if (e.IsBoss && !e.IsTargetable)
                continue;

            Bar b;
            if (!bars.TryGetValue(e, out b))
            {
                b = barPool.Count > 0 ? barPool.Pop() : CreateBar(new Color(0.95f, 0.3f, 0.32f), 80f);
                b.root.gameObject.SetActive(true);
                bars[e] = b;
                float w = Mathf.Clamp(50f + e.MaxHealth * 0.5f, 56f, 120f);
                b.root.sizeDelta = new Vector2(w, 13f);
            }
            b.used = true;

            Vector2 local;
            if (!ToCanvas(e.transform.position + Vector3.up * e.HealthBarHeight, out local))
            {
                b.root.gameObject.SetActive(false);
                continue;
            }
            b.root.gameObject.SetActive(true);
            b.root.anchoredPosition = local + new Vector2(0f, 10f);

            bool showBar = !e.IsBoss && !e.IsSpawning;
            b.bg.enabled = showBar;
            float v = Mathf.Clamp01(e.Health / e.MaxHealth);
            b.fill.rectTransform.anchorMax = new Vector2(v, 1f);
            b.fill.enabled = showBar && v > 0.02f;

            if (vision)
            {
                b.label.enabled = true;
                b.label.text = e.AITechnique + "  |  " + e.AIState;
                b.label.color = e.AIColor;
                b.label.fontSize = e.IsBoss ? 30 : 20;
            }
            else
            {
                b.label.enabled = false;
            }
        }

        removeBuffer.Clear();
        foreach (KeyValuePair<EnemyBase, Bar> kv in bars)
        {
            if (!kv.Value.used)
                removeBuffer.Add(kv.Key);
        }
        foreach (EnemyBase e in removeBuffer)
        {
            Bar b = bars[e];
            b.root.gameObject.SetActive(false);
            barPool.Push(b);
            bars.Remove(e);
        }
    }

    private void UpdatePlayerBar(bool vision)
    {
        PlayerController p = PlayerController.I;
        if (p == null || p.Health.IsDead)
        {
            playerBar.root.gameObject.SetActive(false);
            return;
        }

        Vector2 local;
        if (!ToCanvas(p.transform.position + Vector3.up * 2.1f, out local))
        {
            playerBar.root.gameObject.SetActive(false);
            return;
        }
        playerBar.root.gameObject.SetActive(true);
        playerBar.root.anchoredPosition = local + new Vector2(0f, 8f);
        float v = Mathf.Clamp01(p.Health.Current / p.Health.Max);
        playerBar.fill.rectTransform.anchorMax = new Vector2(v, 1f);
        playerBar.fill.enabled = v > 0.02f;
        playerBar.fill.color = v < 0.3f ? new Color(1f, 0.35f, 0.3f) : new Color(0.4f, 0.9f, 0.45f);

        AutoPilot pilot = p.GetComponent<AutoPilot>();
        if (pilot != null && pilot.enabled)
        {
            playerBar.label.enabled = true;
            playerBar.label.text = "UTILITY AI  |  " + pilot.Mode;
            playerBar.label.color = new Color(0.4f, 0.9f, 1f);
        }
        else
        {
            playerBar.label.enabled = false;
        }
    }

    private void UpdateNumbers()
    {
        float dt = Time.unscaledDeltaTime;
        for (int i = numbers.Count - 1; i >= 0; i--)
        {
            FloatingNumber n = numbers[i];
            n.t += dt;
            float k = n.t / n.life;
            if (k >= 1f)
            {
                n.rt.gameObject.SetActive(false);
                numberPool.Push(n);
                numbers.RemoveAt(i);
                continue;
            }

            Vector3 world = n.world + n.drift * CapyMath.EaseOutCubic(k) * 0.8f;
            Vector2 local;
            if (ToCanvas(world, out local))
                n.rt.anchoredPosition = local;

            float pop = k < 0.15f ? Mathf.Lerp(0.4f, 1.25f, k / 0.15f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((k - 0.15f) / 0.2f));
            n.rt.localScale = Vector3.one * pop * n.scale;
            Color c = n.text.color;
            c.a = k > 0.65f ? 1f - (k - 0.65f) / 0.35f : 1f;
            n.text.color = c;
        }
    }
}
