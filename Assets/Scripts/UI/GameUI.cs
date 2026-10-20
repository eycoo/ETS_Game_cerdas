using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Seluruh UI di dalam game: HUD, banner, kartu level-up, pause, kalah, menang, bar boss.
// Dibangun lewat kode saat scene dimulai.
public class GameUI : MonoBehaviour
{
    public static GameUI I { get; private set; }

    private Canvas hud;
    private Canvas overlay;
    private Canvas fadeCanvas;

    private Image hpFill;
    private Image hpGhost;
    private Text hpText;
    private Image xpFill;
    private Text levelText;
    private Text roomText;
    private Text roomSubText;
    private Text timerText;
    private Text killsText;
    private Image dashFill;
    private Image dashIcon;
    private RectTransform upgradeRow;
    private int shownUpgradeTotal = -1;
    private Text hintText;
    private CanvasGroup hintGroup;
    private Coroutine hintRoutine;

    private CanvasGroup bannerGroup;
    private Text bannerTitle;
    private Text bannerSub;
    private Coroutine bannerRoutine;

    private CanvasGroup toastGroup;
    private Text toastText;
    private Coroutine toastRoutine;

    private RectTransform cineTop;
    private RectTransform cineBottom;
    private float cineTarget;
    private float cineValue;

    private Image flashImage;
    private float flashAlpha;
    private GameObject aiLegend;

    private GameObject bossRoot;
    private Image bossFill;
    private Image bossGhost;
    private Text bossName;
    private EnemyBase boss;
    private CanvasGroup bossTitleGroup;
    private Text bossTitleText;
    private Text bossTitleSub;

    private GameObject levelUpPanel;
    private Text levelUpTitle;
    private Text levelUpSub;
    private readonly List<RectTransform> cards = new List<RectTransform>();
    private List<UpgradeDef> currentChoices;
    private System.Action<UpgradeDef> onPick;
    private float choiceOpenTime;
    private bool choosing;

    private GameObject pausePanel;
    private Text musicLabel;
    private Text sfxLabel;
    private GameObject gameOverPanel;
    private GameObject victoryPanel;

    private Image fadeImage;
    private Coroutine fadeRoutine;

    private float hpGhostValue = 1f;
    private float bossGhostValue = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        I = null;
    }

    public static void Ensure()
    {
        if (I != null)
            return;
        GameObject go = new GameObject("GameUI");
        I = go.AddComponent<GameUI>();
        I.Build();
    }

    private void Build()
    {
        UIFactory.EnsureEventSystem();
        BuildHUD();
        BuildOverlay();
        BuildFade();
    }

    // ================================================================== HUD

    private void BuildHUD()
    {
        GameDatabase db = GameDatabase.I;
        hud = UIFactory.Canvas("HUD", 10);
        hud.transform.SetParent(transform, false);
        Transform root = hud.transform;

        // Kilatan merah saat terkena.
        flashImage = UIFactory.Image(root, "DamageFlash", db.vignette, new Color(1f, 0.15f, 0.1f, 0f), false);
        flashImage.rectTransform.Stretch();

        // Kiri atas: potret, HP, XP.
        RectTransform tl = UIFactory.Rect("TopLeft", root).Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(560f, 150f));
        Image portrait = UIFactory.Image(tl, "Portrait", db.circle, UIFactory.Orange, false);
        portrait.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(122f, 122f));
        Image face = UIFactory.Image(portrait.transform, "Face", db.capyFace, Color.white, false);
        face.rectTransform.Stretch(6f, 6f, 6f, 6f);
        Image ring = UIFactory.Image(portrait.transform, "Ring", db.ring, UIFactory.Cream, false);
        ring.rectTransform.Stretch(-3f, -3f, -3f, -3f);
        Image badge = UIFactory.Image(portrait.transform, "LevelBadge", db.rounded, new Color(0.26f, 0.5f, 0.95f));
        badge.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(92f, 38f));
        levelText = UIFactory.Text(badge.transform, "Level", "Lv 1", 26, Color.white, TextAnchor.MiddleCenter, true);
        levelText.rectTransform.Stretch();

        Image hpBg = UIFactory.Image(tl, "HPBar", db.rounded, new Color(0.07f, 0.05f, 0.11f, 0.88f));
        hpBg.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -14f), new Vector2(390f, 46f));
        hpGhost = MakeFill(hpBg.transform, new Color(1f, 0.92f, 0.65f, 0.95f));
        hpFill = MakeFill(hpBg.transform, UIFactory.Green);
        Image heart = UIFactory.Image(hpBg.transform, "Heart", db.heart, new Color(1f, 0.38f, 0.45f), false);
        heart.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(2f, 0f), new Vector2(56f, 56f));
        hpText = UIFactory.Text(hpBg.transform, "HPText", "100 / 100", 28, Color.white, TextAnchor.MiddleCenter, true);
        hpText.rectTransform.Stretch(20f, 0f, 0f, 0f);

        Image xpBg = UIFactory.Image(tl, "XPBar", db.rounded, new Color(0.07f, 0.05f, 0.11f, 0.88f));
        xpBg.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -68f), new Vector2(390f, 24f));
        xpFill = MakeFill(xpBg.transform, UIFactory.Blue);
        Image gem = UIFactory.Image(xpBg.transform, "Gem", db.gemIcon, Color.white, false);
        gem.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(2f, 0f), new Vector2(34f, 34f));

        // Tengah atas: nama ruangan dan sisa musuh.
        RectTransform tc = UIFactory.Rect("TopCenter", root).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(900f, 100f));
        roomText = UIFactory.Text(tc, "Room", "", 32, UIFactory.Cream, TextAnchor.UpperCenter, true);
        roomText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 44f));
        roomSubText = UIFactory.Text(tc, "RoomSub", "", 24, new Color(1f, 0.85f, 0.6f), TextAnchor.UpperCenter, false);
        roomSubText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(900f, 36f));

        // Kanan atas: waktu dan jumlah kill.
        RectTransform tr = UIFactory.Rect("TopRight", root).Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(300f, 100f));
        Image clock = UIFactory.Image(tr, "Clock", db.clockIcon, Color.white, false);
        clock.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, 0f), new Vector2(42f, 42f));
        timerText = UIFactory.Text(tr, "Timer", "00:00", 38, UIFactory.Cream, TextAnchor.MiddleRight, true);
        timerText.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, 2f), new Vector2(140f, 44f));
        Image skull = UIFactory.Image(tr, "Skull", db.skullIcon, Color.white, false);
        skull.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -50f), new Vector2(40f, 40f));
        killsText = UIFactory.Text(tr, "Kills", "0", 32, UIFactory.Cream, TextAnchor.MiddleRight, true);
        killsText.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -50f), new Vector2(140f, 40f));

        // AI Vision legend.
        aiLegend = BuildLegend(root);
        aiLegend.SetActive(false);

        // Kiri bawah: dash.
        Image dashBg = UIFactory.Image(root, "Dash", db.circle, new Color(0.07f, 0.05f, 0.11f, 0.8f), false);
        dashBg.rectTransform.Place(Vector2.zero, Vector2.zero, new Vector2(40f, 54f), new Vector2(108f, 108f));
        dashFill = UIFactory.Image(dashBg.transform, "Fill", db.circle, new Color(1f, 0.82f, 0.45f, 0.95f), false);
        dashFill.rectTransform.Stretch(5f, 5f, 5f, 5f);
        dashFill.type = Image.Type.Filled;
        dashFill.fillMethod = Image.FillMethod.Radial360;
        dashFill.fillOrigin = (int)Image.Origin360.Top;
        dashFill.fillClockwise = true;
        Image dashInner = UIFactory.Image(dashBg.transform, "Inner", db.circle, new Color(0.16f, 0.12f, 0.24f, 1f), false);
        dashInner.rectTransform.Stretch(12f, 12f, 12f, 12f);
        dashIcon = UIFactory.Image(dashBg.transform, "Icon", db.dashIcon, Color.white, false);
        dashIcon.rectTransform.Stretch(24f, 24f, 24f, 24f);
        Text dashKey = UIFactory.Text(dashBg.transform, "Key", "SPACE", 20, UIFactory.Cream, TextAnchor.UpperCenter, true);
        dashKey.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(140f, 26f));

        // Kanan bawah: ikon upgrade yang dimiliki.
        upgradeRow = UIFactory.Rect("Upgrades", root).Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(700f, 140f));

        // Petunjuk kontrol.
        Image hintBg = UIFactory.Image(root, "Hint", db.rounded, new Color(0.07f, 0.05f, 0.11f, 0.75f));
        hintBg.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(1180f, 56f));
        hintText = UIFactory.Text(hintBg.transform, "Text", "", 26, UIFactory.Cream, TextAnchor.MiddleCenter, false);
        hintText.rectTransform.Stretch();
        hintGroup = UIFactory.Group(hintBg.gameObject);
        hintGroup.alpha = 0f;

        // Bar boss.
        bossRoot = UIFactory.Rect("BossBar", root).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(1000f, 90f)).gameObject;
        bossName = UIFactory.Text(bossRoot.transform, "Name", "GOLEM KING", 40, new Color(1f, 0.8f, 0.55f), TextAnchor.LowerCenter, true, 3f);
        bossName.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(1000f, 50f));
        Image bossBg = UIFactory.Image(bossRoot.transform, "Bg", db.rounded, new Color(0.07f, 0.05f, 0.11f, 0.9f));
        bossBg.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1000f, 40f));
        bossGhost = MakeFill(bossBg.transform, new Color(1f, 0.92f, 0.65f, 0.95f));
        bossFill = MakeFill(bossBg.transform, new Color(0.95f, 0.25f, 0.3f));
        Image half = UIFactory.Image(bossBg.transform, "Half", null, new Color(1f, 1f, 1f, 0.6f), false);
        half.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, 40f));
        bossRoot.SetActive(false);

        // Banner judul ruangan.
        RectTransform banner = UIFactory.Rect("Banner", root).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1700f, 220f));
        bannerGroup = UIFactory.Group(banner.gameObject);
        bannerGroup.alpha = 0f;
        bannerTitle = UIFactory.Text(banner, "Title", "", 88, UIFactory.Orange, TextAnchor.MiddleCenter, true, 4f);
        bannerTitle.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1700f, 110f));
        bannerSub = UIFactory.Text(banner, "Sub", "", 34, UIFactory.Cream, TextAnchor.MiddleCenter, false, 2f);
        bannerSub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1700f, 50f));

        // Toast.
        RectTransform toast = UIFactory.Rect("Toast", root).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(1400f, 110f));
        toastGroup = UIFactory.Group(toast.gameObject);
        toastGroup.alpha = 0f;
        toastText = UIFactory.Text(toast, "Text", "", 56, Color.white, TextAnchor.MiddleCenter, true, 3.5f);
        toastText.rectTransform.Stretch();

        // Kartu judul boss.
        RectTransform bt = UIFactory.Rect("BossTitle", root).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(1920f, 220f));
        Image band = UIFactory.Image(bt, "Band", db.glow, new Color(0f, 0f, 0f, 0.65f), false);
        band.rectTransform.Stretch(-200f, -200f, 0f, 0f);
        bossTitleText = UIFactory.Text(bt, "Title", "", 110, new Color(1f, 0.45f, 0.3f), TextAnchor.MiddleCenter, true, 4f);
        bossTitleText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(1600f, 130f));
        bossTitleSub = UIFactory.Text(bt, "Sub", "", 36, UIFactory.Cream, TextAnchor.MiddleCenter, false);
        bossTitleSub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -58f), new Vector2(1600f, 50f));
        bossTitleGroup = UIFactory.Group(bt.gameObject);
        bossTitleGroup.alpha = 0f;

        // Bar sinematik.
        cineTop = UIFactory.Image(root, "CineTop", null, Color.black, false).rectTransform;
        cineTop.anchorMin = new Vector2(0f, 1f);
        cineTop.anchorMax = new Vector2(1f, 1f);
        cineTop.pivot = new Vector2(0.5f, 1f);
        cineTop.sizeDelta = new Vector2(0f, 0f);
        cineBottom = UIFactory.Image(root, "CineBottom", null, Color.black, false).rectTransform;
        cineBottom.anchorMin = new Vector2(0f, 0f);
        cineBottom.anchorMax = new Vector2(1f, 0f);
        cineBottom.pivot = new Vector2(0.5f, 0f);
        cineBottom.sizeDelta = new Vector2(0f, 0f);
    }

    private Image MakeFill(Transform parent, Color color)
    {
        Image fill = UIFactory.Image(parent, "Fill", GameDatabase.I.rounded, color);
        RectTransform rt = fill.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(4f, 4f);
        rt.offsetMax = new Vector2(-4f, -4f);
        return fill;
    }

    private static void SetBar(Image fill, float value)
    {
        value = Mathf.Clamp01(value);
        RectTransform rt = fill.rectTransform;
        rt.anchorMax = new Vector2(value, 1f);
        RectTransform parent = rt.parent as RectTransform;
        float width = parent != null ? parent.rect.width * value : 100f;
        fill.enabled = width > 10f;
    }

    private GameObject BuildLegend(Transform root)
    {
        GameDatabase db = GameDatabase.I;
        Image panel = UIFactory.Image(root, "AILegend", db.rounded, new Color(0.07f, 0.05f, 0.11f, 0.85f));
        panel.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -130f), new Vector2(430f, 300f));
        Text title = UIFactory.Text(panel.transform, "Title", "AI VISION  [V]", 32, new Color(0.5f, 0.95f, 1f), TextAnchor.UpperLeft, true);
        title.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -14f), new Vector2(400f, 40f));

        string[] rows =
        {
            "STEERING  -  Slime",
            "FSM + A*  -  Skeleton",
            "BEHAVIOR TREE  -  Wizard, Golem",
            "FLOCKING  -  Eye Bat",
            "UTILITY AI  -  Autopilot [F10]"
        };
        Color[] colors =
        {
            new Color(0.55f, 1f, 0.45f),
            new Color(1f, 0.85f, 0.4f),
            new Color(0.85f, 0.6f, 1f),
            new Color(0.75f, 0.55f, 1f),
            new Color(0.4f, 0.9f, 1f)
        };
        for (int i = 0; i < rows.Length; i++)
        {
            Image dot = UIFactory.Image(panel.transform, "Dot", db.circle, colors[i], false);
            dot.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(26f, -78f - i * 40f), new Vector2(20f, 20f));
            Text t = UIFactory.Text(panel.transform, "Row", rows[i], 24, UIFactory.Cream, TextAnchor.MiddleLeft, false, 1.5f);
            t.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(58f, -78f - i * 40f), new Vector2(360f, 36f));
        }
        Text hint = UIFactory.Text(panel.transform, "Hint", "Cyan = A* path   Red X = blocked cell", 19, new Color(0.75f, 0.75f, 0.85f), TextAnchor.MiddleLeft, false, 1f);
        hint.rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 14f), new Vector2(400f, 30f));
        return panel.gameObject;
    }

    private void LateUpdate()
    {
        GameManager gm = GameManager.I;
        PlayerController p = PlayerController.I;
        float dt = Time.unscaledDeltaTime;

        if (p != null)
        {
            float hp = p.Health.Max > 0f ? p.Health.Current / p.Health.Max : 0f;
            SetBar(hpFill, hp);
            hpGhostValue = hpGhostValue > hp ? Mathf.MoveTowards(hpGhostValue, hp, dt * 0.6f) : hp;
            SetBar(hpGhost, hpGhostValue);
            hpText.text = Mathf.CeilToInt(p.Health.Current) + " / " + Mathf.RoundToInt(p.Health.Max);
            hpFill.color = hp < 0.3f ? Color.Lerp(UIFactory.Red, new Color(1f, 0.6f, 0.5f), Mathf.PingPong(Time.unscaledTime * 3f, 1f)) : UIFactory.Green;

            SetBar(xpFill, p.Level.Progress01);
            levelText.text = "Lv " + p.Level.Level;

            float cd = p.DashCooldown01;
            dashFill.fillAmount = cd;
            dashIcon.color = cd >= 1f ? Color.white : new Color(1f, 1f, 1f, 0.35f);

            RefreshUpgrades(p);
        }

        if (gm != null)
        {
            timerText.text = CapyMath.FormatTime(gm.Stats.time);
            killsText.text = gm.Stats.kills.ToString();
            UpdateRoomText(gm);
        }

        if (boss != null && bossRoot.activeSelf)
        {
            float v = boss.MaxHealth > 0f ? Mathf.Max(0f, boss.Health) / boss.MaxHealth : 0f;
            SetBar(bossFill, v);
            bossGhostValue = bossGhostValue > v ? Mathf.MoveTowards(bossGhostValue, v, dt * 0.35f) : v;
            SetBar(bossGhost, bossGhostValue);
            GolemBossAI golem = boss as GolemBossAI;
            bossName.text = golem != null && golem.Enraged ? "GOLEM KING  (ENRAGED)" : "GOLEM KING";
        }

        cineValue = Mathf.MoveTowards(cineValue, cineTarget, dt * 400f);
        cineTop.sizeDelta = new Vector2(0f, cineValue);
        cineBottom.sizeDelta = new Vector2(0f, cineValue);

        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * 2.2f);
        flashImage.color = new Color(1f, 0.15f, 0.1f, flashAlpha);
        if (p != null && !p.Health.IsDead && p.Health.Current / p.Health.Max < 0.3f)
            flashImage.color = new Color(1f, 0.15f, 0.1f, Mathf.Max(flashAlpha, 0.18f + Mathf.Sin(Time.unscaledTime * 4f) * 0.08f));

        UpdateChoiceInput();
    }

    private void UpdateRoomText(GameManager gm)
    {
        RoomController room = gm.CurrentRoom;
        if (room == null)
        {
            roomText.text = "";
            roomSubText.text = "";
            return;
        }

        if (room.isSafeRoom)
        {
            roomText.text = room.roomName.ToUpper();
            roomSubText.text = "Head north into the dungeon";
            return;
        }

        roomText.text = "ROOM " + room.roomNumber + " / 5   " + room.roomName.ToUpper();
        if (room.State == RoomController.RoomState.Active)
        {
            if (room.isBossRoom)
                roomSubText.text = "Defeat the Golem King!";
            else
                roomSubText.text = "Wave " + Mathf.Min(room.WaveIndex + 1, room.WaveCount) + "/" + room.WaveCount + "     Enemies left: " + room.AliveCount;
        }
        else if (room.State == RoomController.RoomState.Cleared)
        {
            roomSubText.text = room.isBossRoom ? "Grab the Golden Yuzu!" : "Cleared! Go through the north door";
        }
        else
        {
            roomSubText.text = "";
        }
    }

    private void RefreshUpgrades(PlayerController p)
    {
        int total = 0;
        foreach (KeyValuePair<UpgradeId, int> kv in p.Stats.AllLevels())
            total += kv.Value;
        if (total == shownUpgradeTotal)
            return;
        shownUpgradeTotal = total;

        for (int i = upgradeRow.childCount - 1; i >= 0; i--)
            Destroy(upgradeRow.GetChild(i).gameObject);

        GameDatabase db = GameDatabase.I;
        int index = 0;
        foreach (KeyValuePair<UpgradeId, int> kv in p.Stats.AllLevels())
        {
            UpgradeDef def = UpgradeLibrary.Get(kv.Key);
            if (def == null || def.id == UpgradeId.YuzuSnack)
                continue;
            int col = index % 9;
            int row = index / 9;
            Image bg = UIFactory.Image(upgradeRow, "Up", db.circle, def.color, false);
            bg.rectTransform.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-col * 70f, row * 70f), new Vector2(62f, 62f));
            Image icon = UIFactory.Image(bg.transform, "Icon", db.GetUpgradeIcon((int)def.id), Color.white, false);
            icon.rectTransform.Stretch(8f, 8f, 8f, 8f);
            if (kv.Value > 1)
            {
                Text lv = UIFactory.Text(bg.transform, "Lv", kv.Value.ToString(), 22, Color.white, TextAnchor.LowerRight, true, 2f);
                lv.rectTransform.Stretch(0f, -4f, 0f, -6f);
            }
            index++;
        }
    }

    // ================================================================== banner, toast, hint

    public void ShowBanner(string title, string subtitle, Color color, float duration)
    {
        bannerTitle.text = title;
        bannerTitle.color = color;
        bannerSub.text = subtitle;
        if (bannerRoutine != null)
            StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(duration));
    }

    private IEnumerator BannerRoutine(float duration)
    {
        Transform tr = bannerGroup.transform;
        StartCoroutine(UIFactory.PopScale(tr, 1.25f, 1f, 0.35f));
        yield return UIFactory.FadeGroup(bannerGroup, bannerGroup.alpha, 1f, 0.2f);
        yield return new WaitForSecondsRealtime(duration);
        yield return UIFactory.FadeGroup(bannerGroup, 1f, 0f, 0.45f);
        bannerRoutine = null;
    }

    public void ShowToast(string text, Color color, bool big = false)
    {
        toastText.text = text;
        toastText.color = color;
        toastText.fontSize = big ? 72 : 52;
        if (toastRoutine != null)
            StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        StartCoroutine(UIFactory.PopScale(toastGroup.transform, 0.5f, 1f, 0.3f));
        toastGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.4f);
        yield return UIFactory.FadeGroup(toastGroup, 1f, 0f, 0.4f);
        toastRoutine = null;
    }

    public void ShowHint(string text, float duration)
    {
        hintText.text = text;
        if (hintRoutine != null)
            StopCoroutine(hintRoutine);
        hintRoutine = StartCoroutine(HintRoutine(duration));
    }

    private IEnumerator HintRoutine(float duration)
    {
        yield return UIFactory.FadeGroup(hintGroup, hintGroup.alpha, 1f, 0.3f);
        yield return new WaitForSecondsRealtime(duration);
        yield return UIFactory.FadeGroup(hintGroup, 1f, 0f, 0.6f);
        hintRoutine = null;
    }

    public void DamageFlash()
    {
        flashAlpha = 0.55f;
    }

    public void SetCinematicBars(bool on)
    {
        cineTarget = on ? 120f : 0f;
    }

    public void SetAIVision(bool on)
    {
        aiLegend.SetActive(on);
        ShowToast(on ? "AI VISION ON" : "AI VISION OFF", new Color(0.5f, 0.95f, 1f));
    }

    // ================================================================== boss

    public void ShowBossBar(EnemyBase target)
    {
        boss = target;
        bossGhostValue = 1f;
        bossRoot.SetActive(true);
        StartCoroutine(UIFactory.PopScale(bossRoot.transform, 0.6f, 1f, 0.35f));
    }

    public void HideBossBar()
    {
        bossRoot.SetActive(false);
        boss = null;
    }

    public void ShowBossTitle(string title, string subtitle)
    {
        bossTitleText.text = title;
        bossTitleSub.text = subtitle;
        StartCoroutine(BossTitleRoutine());
    }

    private IEnumerator BossTitleRoutine()
    {
        StartCoroutine(UIFactory.PopScale(bossTitleText.transform, 1.6f, 1f, 0.4f));
        yield return UIFactory.FadeGroup(bossTitleGroup, 0f, 1f, 0.25f);
        yield return new WaitForSecondsRealtime(2.2f);
        yield return UIFactory.FadeGroup(bossTitleGroup, 1f, 0f, 0.5f);
    }

    // ================================================================== overlay panels

    private void BuildOverlay()
    {
        overlay = UIFactory.Canvas("Overlay", 50);
        overlay.transform.SetParent(transform, false);
        BuildLevelUp();
        BuildPause();
    }

    private GameObject PanelRoot(string name, Color dim)
    {
        RectTransform root = UIFactory.Rect(name, overlay.transform);
        root.Stretch();
        Image d = UIFactory.Image(root, "Dim", null, dim, false);
        d.rectTransform.Stretch();
        d.raycastTarget = true;
        UIFactory.Group(root.gameObject);
        root.gameObject.SetActive(false);
        return root.gameObject;
    }

    private void BuildLevelUp()
    {
        GameDatabase db = GameDatabase.I;
        levelUpPanel = PanelRoot("LevelUp", new Color(0.03f, 0.02f, 0.07f, 0.75f));
        Transform root = levelUpPanel.transform;

        Image glow = UIFactory.Image(root, "Glow", db.glow, new Color(1f, 0.7f, 0.3f, 0.25f), false);
        glow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 360f), new Vector2(1200f, 300f));
        levelUpTitle = UIFactory.Text(root, "Title", "LEVEL UP!", 100, UIFactory.Orange, TextAnchor.MiddleCenter, true, 4f);
        levelUpTitle.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 370f), new Vector2(1400f, 130f));
        levelUpSub = UIFactory.Text(root, "Sub", "Choose a new skill", 34, UIFactory.Cream, TextAnchor.MiddleCenter, false);
        levelUpSub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 285f), new Vector2(1400f, 50f));

        for (int i = 0; i < 3; i++)
            cards.Add(BuildCard(root, i));
    }

    private RectTransform BuildCard(Transform parent, int index)
    {
        GameDatabase db = GameDatabase.I;
        Image frame = UIFactory.Image(parent, "Card" + index, db.rounded, Color.white);
        frame.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((index - 1) * 430f, -50f), new Vector2(390f, 520f));
        frame.raycastTarget = true;
        Button b = frame.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        int captured = index;
        b.onClick.AddListener(() => Pick(captured));
        frame.gameObject.AddComponent<UIHover>();

        Image inner = UIFactory.Image(frame.transform, "Inner", db.rounded, new Color(0.13f, 0.1f, 0.2f, 1f));
        inner.rectTransform.Stretch(7f, 7f, 7f, 7f);
        Image header = UIFactory.Image(frame.transform, "Header", db.glow, Color.white, false);
        header.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(380f, 300f));
        Image circle = UIFactory.Image(frame.transform, "IconBg", db.circle, Color.white, false);
        circle.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(190f, 190f));
        Image icon = UIFactory.Image(circle.transform, "Icon", db.circle, Color.white, false);
        icon.rectTransform.Stretch(26f, 26f, 26f, 26f);

        Text title = UIFactory.Text(frame.transform, "Name", "", 42, Color.white, TextAnchor.MiddleCenter, true, 3f);
        title.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(370f, 60f));
        Text desc = UIFactory.Text(frame.transform, "Desc", "", 27, UIFactory.Cream, TextAnchor.UpperCenter, false, 1.5f);
        desc.horizontalOverflow = HorizontalWrapMode.Wrap;
        desc.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(320f, 120f));
        Text lv = UIFactory.Text(frame.transform, "Level", "", 26, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, true, 2f);
        lv.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(360f, 40f));
        Text key = UIFactory.Text(frame.transform, "Key", "[" + (index + 1) + "]", 26, new Color(0.7f, 0.7f, 0.8f), TextAnchor.MiddleCenter, true, 1.5f);
        key.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 36f), new Vector2(200f, 36f));
        return frame.rectTransform;
    }

    public void ShowUpgradeChoice(List<UpgradeDef> choices, bool treasure, System.Action<UpgradeDef> callback)
    {
        currentChoices = choices;
        onPick = callback;
        choosing = true;
        choiceOpenTime = Time.unscaledTime;
        levelUpTitle.text = treasure ? "TREASURE!" : "LEVEL UP!";
        levelUpTitle.color = treasure ? new Color(1f, 0.85f, 0.3f) : UIFactory.Orange;
        levelUpSub.text = treasure ? "A bonus skill from the chest. Pick one!" : "Choose a new skill";

        GameDatabase db = GameDatabase.I;
        PlayerController p = PlayerController.I;
        for (int i = 0; i < cards.Count; i++)
        {
            RectTransform card = cards[i];
            bool used = i < choices.Count;
            card.gameObject.SetActive(used);
            if (!used)
                continue;

            UpgradeDef def = choices[i];
            int current = p != null ? p.Stats.GetLevel(def.id) : 0;
            card.GetComponent<Image>().color = def.color;
            card.Find("Header").GetComponent<Image>().color = def.color.WithAlpha(0.35f);
            card.Find("IconBg").GetComponent<Image>().color = def.color;
            card.Find("IconBg/Icon").GetComponent<Image>().sprite = db.GetUpgradeIcon((int)def.id);
            card.Find("Name").GetComponent<Text>().text = def.name;
            card.Find("Desc").GetComponent<Text>().text = def.description;
            string lvText = def.maxLevel >= 99 ? "INSTANT" : (current == 0 ? "NEW!" : "Lv " + current + "  >  " + (current + 1));
            card.Find("Level").GetComponent<Text>().text = lvText;
            card.localScale = Vector3.one;
        }

        levelUpPanel.SetActive(true);
        CanvasGroup g = levelUpPanel.GetComponent<CanvasGroup>();
        StartCoroutine(UIFactory.FadeGroup(g, 0f, 1f, 0.2f));
        StartCoroutine(UIFactory.PopScale(levelUpTitle.transform, 1.5f, 1f, 0.35f));
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].gameObject.activeSelf)
                StartCoroutine(CardIntro(cards[i], i * 0.07f));
        }

        if (EventSystem.current != null && cards.Count > 0)
            EventSystem.current.SetSelectedGameObject(cards[Mathf.Min(1, choices.Count - 1)].gameObject);
    }

    private IEnumerator CardIntro(RectTransform card, float delay)
    {
        card.localScale = Vector3.zero;
        yield return new WaitForSecondsRealtime(delay);
        yield return UIFactory.PopScale(card, 0.3f, 1f, 0.35f);
    }

    private void UpdateChoiceInput()
    {
        if (!choosing || Time.unscaledTime - choiceOpenTime < 0.35f)
            return;
        int c = CapyInput.ChoicePressed();
        if (c >= 0)
            Pick(c);
    }

    private void Pick(int index)
    {
        if (!choosing || currentChoices == null || index < 0 || index >= currentChoices.Count)
            return;
        if (Time.unscaledTime - choiceOpenTime < 0.35f)
            return;

        choosing = false;
        UpgradeDef def = currentChoices[index];
        StartCoroutine(CloseLevelUp(index, def));
    }

    private IEnumerator CloseLevelUp(int index, UpgradeDef def)
    {
        StartCoroutine(UIFactory.PopScale(cards[index], 1.15f, 1.05f, 0.2f));
        yield return new WaitForSecondsRealtime(0.18f);
        CanvasGroup g = levelUpPanel.GetComponent<CanvasGroup>();
        yield return UIFactory.FadeGroup(g, 1f, 0f, 0.15f);
        levelUpPanel.SetActive(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        System.Action<UpgradeDef> cb = onPick;
        onPick = null;
        if (cb != null)
            cb(def);
    }

    // Dipakai AutoPilot: memilih kartu sesuai urutan prioritas.
    public void AutoPick(UpgradeId[] priority)
    {
        if (!choosing || currentChoices == null)
            return;
        int best = 0;
        int bestRank = int.MaxValue;
        for (int i = 0; i < currentChoices.Count; i++)
        {
            int rank = System.Array.IndexOf(priority, currentChoices[i].id);
            if (rank < 0)
                rank = 1000;
            if (rank < bestRank)
            {
                bestRank = rank;
                best = i;
            }
        }
        choiceOpenTime = -10f;
        Pick(best);
    }

    // ---------------------------------------------------------------- pause

    private void BuildPause()
    {
        GameDatabase db = GameDatabase.I;
        pausePanel = PanelRoot("Pause", new Color(0.03f, 0.02f, 0.07f, 0.7f));
        Transform root = pausePanel.transform;

        Image box = UIFactory.Image(root, "Box", db.rounded, UIFactory.PanelDark);
        box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 820f));
        Text title = UIFactory.Text(box.transform, "Title", "PAUSED", 92, UIFactory.Orange, TextAnchor.MiddleCenter, true, 4f);
        title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(680f, 110f));

        Button resume = UIFactory.Button(box.transform, "Resume", "RESUME", UIFactory.Orange, () => GameManager.I.Resume(), new Vector2(440f, 86f));
        resume.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(440f, 86f));
        Button restart = UIFactory.Button(box.transform, "Restart", "RESTART", new Color(0.4f, 0.55f, 0.95f), () => GameManager.I.Restart(), new Vector2(440f, 86f));
        restart.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -272f), new Vector2(440f, 86f));
        Button menu = UIFactory.Button(box.transform, "Menu", "MAIN MENU", new Color(0.55f, 0.38f, 0.8f), () => GameManager.I.GoToMenu(), new Vector2(440f, 86f));
        menu.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -374f), new Vector2(440f, 86f));

        Button music = UIFactory.Button(box.transform, "Music", "MUSIC: ON", new Color(0.25f, 0.6f, 0.45f), ToggleMusic, new Vector2(210f, 64f), 28);
        music.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-115f, -476f), new Vector2(210f, 64f));
        musicLabel = music.GetComponentInChildren<Text>();
        Button sfx = UIFactory.Button(box.transform, "Sfx", "SFX: ON", new Color(0.25f, 0.6f, 0.45f), ToggleSfx, new Vector2(210f, 64f), 28);
        sfx.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(115f, -476f), new Vector2(210f, 64f));
        sfxLabel = sfx.GetComponentInChildren<Text>();

        Text controls = UIFactory.Text(box.transform, "Controls",
            "<color=#FFB347>WASD / Arrows</color>  move\n<color=#FFB347>Mouse</color>  aim auto-attack\n<color=#FFB347>SPACE / Shift</color>  dash (invincible)\n<color=#FFB347>V</color>  AI Vision     <color=#FFB347>F10</color>  Autopilot",
            27, UIFactory.Cream, TextAnchor.UpperCenter, false, 1.5f);
        controls.lineSpacing = 1.15f;
        controls.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -570f), new Vector2(640f, 200f));
    }

    private void ToggleMusic()
    {
        AudioManager.SetMusicOn(!AudioManager.MusicOn);
        musicLabel.text = AudioManager.MusicOn ? "MUSIC: ON" : "MUSIC: OFF";
    }

    private void ToggleSfx()
    {
        AudioManager.SetSfxOn(!AudioManager.SfxOn);
        sfxLabel.text = AudioManager.SfxOn ? "SFX: ON" : "SFX: OFF";
    }

    public void ShowPause(bool show)
    {
        musicLabel.text = AudioManager.MusicOn ? "MUSIC: ON" : "MUSIC: OFF";
        sfxLabel.text = AudioManager.SfxOn ? "SFX: ON" : "SFX: OFF";
        pausePanel.SetActive(show);
        if (show)
        {
            pausePanel.GetComponent<CanvasGroup>().alpha = 1f;
            StartCoroutine(UIFactory.PopScale(pausePanel.transform.Find("Box"), 0.85f, 1f, 0.25f));
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(pausePanel.transform.Find("Box/Resume").gameObject);
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // ---------------------------------------------------------------- game over / victory

    private void AddStat(Transform parent, string label, string value, float y)
    {
        Text l = UIFactory.Text(parent, "Label", label, 32, new Color(0.8f, 0.78f, 0.9f), TextAnchor.MiddleLeft, false, 1.5f);
        l.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-150f, y), new Vector2(300f, 44f));
        Text v = UIFactory.Text(parent, "Value", value, 36, Color.white, TextAnchor.MiddleRight, true, 2f);
        v.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(150f, y), new Vector2(300f, 44f));
    }

    public void ShowGameOver(RunStats stats)
    {
        GameDatabase db = GameDatabase.I;
        if (gameOverPanel != null)
            Destroy(gameOverPanel);
        gameOverPanel = PanelRoot("GameOver", new Color(0.12f, 0.02f, 0.04f, 0.78f));
        Transform root = gameOverPanel.transform;

        Image box = UIFactory.Image(root, "Box", db.rounded, UIFactory.PanelDark);
        box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 760f));
        Text title = UIFactory.Text(box.transform, "Title", "CAPY FAINTED...", 84, new Color(1f, 0.45f, 0.35f), TextAnchor.MiddleCenter, true, 4f);
        title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(760f, 110f));
        Text sub = UIFactory.Text(box.transform, "Sub", "The dungeon was too spicy this time.", 30, UIFactory.Cream, TextAnchor.MiddleCenter, false);
        sub.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 44f));

        PlayerController p = PlayerController.I;
        AddStat(box.transform, "Time", CapyMath.FormatTime(stats.time), -240f);
        AddStat(box.transform, "Rooms cleared", stats.roomsCleared + " / 5", -295f);
        AddStat(box.transform, "Enemies defeated", stats.kills.ToString(), -350f);
        AddStat(box.transform, "Level reached", p != null ? p.Level.Level.ToString() : "-", -405f);

        Button retry = UIFactory.Button(box.transform, "Retry", "TRY AGAIN", UIFactory.Orange, () => GameManager.I.Restart(), new Vector2(460f, 90f), 44);
        retry.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(460f, 90f));
        Button menu = UIFactory.Button(box.transform, "Menu", "MAIN MENU", new Color(0.55f, 0.38f, 0.8f), () => GameManager.I.GoToMenu(), new Vector2(460f, 80f), 36);
        menu.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(460f, 80f));

        gameOverPanel.SetActive(true);
        StartCoroutine(UIFactory.FadeGroup(gameOverPanel.GetComponent<CanvasGroup>(), 0f, 1f, 0.4f));
        StartCoroutine(UIFactory.PopScale(box.transform, 0.7f, 1f, 0.4f));
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(retry.gameObject);
    }

    public void ShowVictory(RunStats stats, string rank, float bestTime, bool newBest)
    {
        GameDatabase db = GameDatabase.I;
        if (victoryPanel != null)
            Destroy(victoryPanel);
        victoryPanel = PanelRoot("Victory", new Color(0.1f, 0.07f, 0.02f, 0.7f));
        Transform root = victoryPanel.transform;

        Image glow = UIFactory.Image(root, "Glow", db.glow, new Color(1f, 0.8f, 0.3f, 0.35f), false);
        glow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1400f, 500f));
        glow.gameObject.AddComponent<Spinner>();

        Image box = UIFactory.Image(root, "Box", db.rounded, UIFactory.PanelDark);
        box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(860f, 860f));
        Text title = UIFactory.Text(box.transform, "Title", "VICTORY!", 120, new Color(1f, 0.82f, 0.3f), TextAnchor.MiddleCenter, true, 5f);
        title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(840f, 140f));
        Text sub = UIFactory.Text(box.transform, "Sub", "The Golden Yuzu is back home. Capy is the hero!", 30, UIFactory.Cream, TextAnchor.MiddleCenter, false);
        sub.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(840f, 44f));

        Color rankColor = rank == "S" ? new Color(1f, 0.8f, 0.25f) : rank == "A" ? new Color(0.5f, 0.9f, 0.5f) : rank == "B" ? new Color(0.45f, 0.75f, 1f) : new Color(0.75f, 0.7f, 0.8f);
        Image rankBg = UIFactory.Image(box.transform, "Rank", db.circle, rankColor, false);
        rankBg.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -290f), new Vector2(170f, 170f));
        Text rankText = UIFactory.Text(rankBg.transform, "Letter", rank, 120, Color.white, TextAnchor.MiddleCenter, true, 4f);
        rankText.rectTransform.Stretch(0f, 0f, 0f, 10f);
        Text rankLabel = UIFactory.Text(box.transform, "RankLabel", "RANK", 26, rankColor, TextAnchor.MiddleCenter, true, 2f);
        rankLabel.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -392f), new Vector2(300f, 36f));

        PlayerController p = PlayerController.I;
        AddStat(box.transform, "Time  (" + GameSettings.DifficultyName.ToLower() + ")", CapyMath.FormatTime(stats.time), -450f);
        AddStat(box.transform, "Best time", CapyMath.FormatTime(bestTime) + (newBest ? "  NEW!" : ""), -500f);
        AddStat(box.transform, "Enemies defeated", stats.kills.ToString(), -550f);
        AddStat(box.transform, "Damage taken", stats.damageTaken.ToString(), -600f);
        AddStat(box.transform, "Level reached", p != null ? p.Level.Level.ToString() : "-", -650f);

        Button again = UIFactory.Button(box.transform, "Again", "PLAY AGAIN", UIFactory.Orange, () => GameManager.I.Restart(), new Vector2(380f, 86f), 40);
        again.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 50f), new Vector2(380f, 86f));
        Button menu = UIFactory.Button(box.transform, "Menu", "MAIN MENU", new Color(0.55f, 0.38f, 0.8f), () => GameManager.I.GoToMenu(), new Vector2(380f, 86f), 40);
        menu.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(200f, 50f), new Vector2(380f, 86f));

        victoryPanel.SetActive(true);
        StartCoroutine(UIFactory.FadeGroup(victoryPanel.GetComponent<CanvasGroup>(), 0f, 1f, 0.5f));
        StartCoroutine(UIFactory.PopScale(box.transform, 0.6f, 1f, 0.45f));
        StartCoroutine(UIFactory.PopScale(rankBg.transform, 2.5f, 1f, 0.6f));
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(again.gameObject);
    }

    // ================================================================== fade

    private void BuildFade()
    {
        fadeCanvas = UIFactory.Canvas("Fade", 100);
        fadeCanvas.transform.SetParent(transform, false);
        fadeImage = UIFactory.Image(fadeCanvas.transform, "Black", null, Color.black, false);
        fadeImage.rectTransform.Stretch();
        fadeImage.raycastTarget = false;
    }

    public void FadeIn(float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(1f, 0f, duration, null));
    }

    public void FadeOut(float duration, System.Action done)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(fadeImage.color.a, 1f, duration, done));
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, System.Action done)
    {
        fadeImage.raycastTarget = to > 0.5f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, to);
        fadeRoutine = null;
        if (done != null)
            done();
    }
}
