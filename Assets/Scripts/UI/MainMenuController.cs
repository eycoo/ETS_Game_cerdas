using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu utama: judul, tombol Play / How to Play / Quit, dan latar diorama yang berputar pelan.
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float orbitSpeed = 5f;

    private GameObject howToPanel;
    private GameObject mainGroup;
    private Image fade;
    private RectTransform titleRoot;
    private bool loading;
    private Button playButton;
    private Text difficultyLabel;

    private void Start()
    {
        Time.timeScale = 1f;
        CapyInput.UseOverride = false;
        UIFactory.EnsureEventSystem();
        AudioManager.Music(MusicTrack.Menu, 1.5f);
        BuildUI();
        StartCoroutine(FadeRoutine(1f, 0f, 1f, null));
    }

    private void Update()
    {
        // Kamera bergoyang pelan ke kiri-kanan supaya diorama terasa hidup.
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.12f) * orbitSpeed * 4f, 0f);

        if (titleRoot != null)
        {
            float s = 1f + Mathf.Sin(Time.time * 1.6f) * 0.015f;
            titleRoot.localScale = new Vector3(s, s, 1f);
            titleRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 0.9f) * 1.2f);
        }

        if (howToPanel != null && howToPanel.activeSelf && CapyInput.PausePressed())
            ShowHowTo(false);
    }

    private void BuildUI()
    {
        GameDatabase db = GameDatabase.I;
        Canvas canvas = UIFactory.Canvas("MenuCanvas", 10);
        Transform root = canvas.transform;

        Image vignette = UIFactory.Image(root, "Vignette", db.vignette, new Color(0.02f, 0.01f, 0.06f, 0.75f), false);
        vignette.rectTransform.Stretch();

        mainGroup = UIFactory.Rect("Main", root).Stretch().gameObject;
        Transform main = mainGroup.transform;

        Image glow = UIFactory.Image(main, "TitleGlow", db.glow, new Color(1f, 0.6f, 0.2f, 0.3f), false);
        glow.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(470f, -260f), new Vector2(1000f, 520f));

        titleRoot = UIFactory.Rect("Title", main).Place(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(470f, -250f), new Vector2(900f, 360f));
        Text capy = UIFactory.Text(titleRoot, "Capy", "CAPY", 190, UIFactory.Orange, TextAnchor.MiddleCenter, true, 7f);
        capy.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 75f), new Vector2(900f, 210f));
        Text conquest = UIFactory.Text(titleRoot, "Conquest", "CONQUEST", 132, UIFactory.Cream, TextAnchor.MiddleCenter, true, 6f);
        conquest.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -85f), new Vector2(900f, 160f));

        Text tagline = UIFactory.Text(main, "Tagline", "Conquer the dungeon. Outsmart the smart monsters.", 32, new Color(1f, 0.9f, 0.75f), TextAnchor.MiddleCenter, false, 2f);
        tagline.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(470f, -470f), new Vector2(900f, 50f));

        playButton = UIFactory.Button(main, "Play", "PLAY", UIFactory.Orange, Play, new Vector2(460f, 104f), 56);
        playButton.GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(470f, -585f), new Vector2(460f, 104f));
        Button difficulty = UIFactory.Button(main, "Difficulty", "", new Color(0.24f, 0.62f, 0.55f), CycleDifficulty, new Vector2(460f, 68f), 32);
        difficulty.GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(470f, -683f), new Vector2(460f, 68f));
        difficultyLabel = difficulty.GetComponentInChildren<Text>();
        difficultyLabel.text = "MODE: " + GameSettings.DifficultyName;
        Button how = UIFactory.Button(main, "HowTo", "HOW TO PLAY", new Color(0.36f, 0.56f, 0.95f), () => ShowHowTo(true), new Vector2(460f, 80f), 38);
        how.GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(470f, -772f), new Vector2(460f, 80f));
        Button quit = UIFactory.Button(main, "Quit", "QUIT", new Color(0.55f, 0.38f, 0.8f), Quit, new Vector2(460f, 80f), 38);
        quit.GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(470f, -862f), new Vector2(460f, 80f));

        float best = PlayerPrefs.GetFloat(GameManager.BestTimeKey, 0f);
        Text bestText = UIFactory.Text(main, "Best", best > 0f ? "Best clear time  " + CapyMath.FormatTime(best) : "No clear yet. Be the first!", 28, new Color(1f, 0.85f, 0.45f), TextAnchor.MiddleCenter, true, 2f);
        bestText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(470f, -945f), new Vector2(700f, 40f));

        Text footer = UIFactory.Text(main, "Footer", "Game Cerdas ITS  -  Farras  -  Mumtaz  -  Kayla", 24, new Color(0.85f, 0.82f, 0.95f, 0.85f), TextAnchor.LowerRight, false, 1.5f);
        footer.rectTransform.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(900f, 36f));

        BuildHowTo(root);

        GameObject fadeGo = UIFactory.Rect("Fade", root).Stretch().gameObject;
        fade = fadeGo.AddComponent<Image>();
        fade.color = Color.black;
        fade.raycastTarget = false;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
    }

    private void BuildHowTo(Transform root)
    {
        GameDatabase db = GameDatabase.I;
        howToPanel = UIFactory.Rect("HowTo", root).Stretch().gameObject;
        Image dim = UIFactory.Image(howToPanel.transform, "Dim", null, new Color(0.02f, 0.01f, 0.06f, 0.8f), false);
        dim.rectTransform.Stretch();
        dim.raycastTarget = true;

        Image box = UIFactory.Image(howToPanel.transform, "Box", db.rounded, UIFactory.PanelDark);
        box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 900f));
        Text title = UIFactory.Text(box.transform, "Title", "HOW TO PLAY", 84, UIFactory.Orange, TextAnchor.MiddleCenter, true, 4f);
        title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(1400f, 100f));

        string goal =
            "<color=#FFB347>GOAL</color>\n" +
            "Clear 5 rooms, level up, pick skills,\nbeat the Golem King and grab the Golden Yuzu.\n\n" +
            "<color=#FFB347>CONTROLS</color>";
        Text g = UIFactory.Text(box.transform, "Goal", goal, 30, UIFactory.Cream, TextAnchor.UpperLeft, false, 1.5f);
        g.lineSpacing = 1.1f;
        g.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -150f), new Vector2(680f, 240f));

        string keys = "WASD / Arrows\nStand still\nSPACE / Shift\n1 / 2 / 3\nV\nF10\nESC";
        string actions = "Move\nAuto-attack nearest enemy\nDash (invincible)\nPick a skill card\nAI Vision (see the AI think)\nAutopilot (AI plays for you)\nPause";
        Text k = UIFactory.Text(box.transform, "Keys", keys, 29, new Color(1f, 0.82f, 0.45f), TextAnchor.UpperLeft, false, 1.5f);
        k.lineSpacing = 1.12f;
        k.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -362f), new Vector2(240f, 380f));
        Text l = UIFactory.Text(box.transform, "Actions", actions, 29, UIFactory.Cream, TextAnchor.UpperLeft, false, 1.5f);
        l.lineSpacing = 1.12f;
        l.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -362f), new Vector2(460f, 380f));

        string right =
            "<color=#FFB347>SMART ENEMIES</color>\n" +
            "<color=#8CFF73>Slime</color>  Steering: seek + separation, splits in two\n" +
            "<color=#FFD966>Skeleton</color>  Finite State Machine + A* pathfinding\n" +
            "<color=#FFC266>Archer</color>  FSM: reposition, aim (red laser), shoot\n" +
            "<color=#D9A6FF>Wizard</color>  Behavior Tree: teleport, summon, cast\n" +
            "<color=#BF8CFF>Eye Bat</color>  Flocking (Boids), swoops at you\n" +
            "<color=#80E6FF>Golem King</color>  Behavior Tree boss, 2 phases\n\n" +
            "<color=#FFB347>TIPS</color>\n" +
            "Red zones on the floor = incoming attack.\n" +
            "Dash through shockwaves and arrows.\n" +
            "A Golem that charges into a wall gets stunned!";
        Text r = UIFactory.Text(box.transform, "Right", right, 28, UIFactory.Cream, TextAnchor.UpperLeft, false, 1.5f);
        r.lineSpacing = 1.12f;
        r.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(790f, -150f), new Vector2(660f, 620f));

        Button back = UIFactory.Button(box.transform, "Back", "BACK", UIFactory.Orange, () => ShowHowTo(false), new Vector2(360f, 84f), 40);
        back.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 84f));

        howToPanel.SetActive(false);
    }

    private void ShowHowTo(bool show)
    {
        howToPanel.SetActive(show);
        mainGroup.SetActive(!show);
        if (EventSystem.current != null)
        {
            GameObject target = show ? howToPanel.transform.Find("Box/Back").gameObject : playButton.gameObject;
            EventSystem.current.SetSelectedGameObject(target);
        }
        if (show)
            StartCoroutine(UIFactory.PopScale(howToPanel.transform.Find("Box"), 0.85f, 1f, 0.25f));
    }

    // Easy / Normal / Hard mengubah HP dan damage musuh (lihat GameSettings).
    private void CycleDifficulty()
    {
        GameSettings.Difficulty = (GameSettings.Difficulty + 1) % 3;
        difficultyLabel.text = "MODE: " + GameSettings.DifficultyName;
    }

    private void Play()
    {
        if (loading)
            return;
        loading = true;
        AudioManager.Sfx("select");
        StartCoroutine(FadeRoutine(0f, 1f, 0.6f, () => SceneManager.LoadScene(GameManager.GameSceneName)));
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, System.Action done)
    {
        float t = 0f;
        fade.raycastTarget = true;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        fade.color = new Color(0f, 0f, 0f, to);
        fade.raycastTarget = to > 0.5f;
        if (done != null)
            done();
    }
}
