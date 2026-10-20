using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState
{
    Intro,
    Playing,
    LevelUp,
    Paused,
    Cutscene,
    GameOver,
    Victory
}

public class RunStats
{
    public float time;
    public int kills;
    public int damageTaken;
    public int damageDealt;
    public int roomsCleared;
}

// Pengatur alur permainan: state game, waktu (pause, hit stop, slow motion),
// antrean pilihan upgrade, menang/kalah, dan tombol bantuan presentasi.
public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public const string GameSceneName = "CapyConquest";
    public const string MenuSceneName = "MainMenu";
    public const string BestTimeKey = "capy_best_time";

    [SerializeField] private PlayerController player;
    [SerializeField] private RoomController[] rooms;

    public GameState State { get; private set; } = GameState.Intro;
    public bool IsPlaying => State == GameState.Playing;
    public PlayerController Player => player;
    public RoomController CurrentRoom { get; private set; }
    public RoomController[] Rooms => rooms;
    public RunStats Stats { get; } = new RunStats();
    public bool AIVision { get; private set; }
    public bool GodMode { get; private set; }
    public EnemyBase Boss { get; set; }

    private float hitStopUntil;
    private float slowMoUntil;
    private float slowMoScale = 1f;
    private readonly Queue<bool> pendingUpgrades = new Queue<bool>();
    private bool sceneChanging;

    // Pengali kecepatan untuk pengujian otomatis (1 = normal).
    public static float TestSpeed = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        I = null;
        TestSpeed = 1f;
    }

    private void Awake()
    {
        I = this;
        Time.timeScale = 1f;

        Physics.IgnoreLayerCollision(Layers.Player, Layers.Enemy, true);
        for (int layer = 0; layer < 32; layer++)
            Physics.IgnoreLayerCollision(Layers.Flying, layer, true);

        if (player == null)
            player = FindAnyObjectByType<PlayerController>();

        if (rooms == null || rooms.Length == 0)
        {
            rooms = FindObjectsByType<RoomController>();
            System.Array.Sort(rooms, (a, b) => a.roomNumber.CompareTo(b.roomNumber));
        }
    }

    private void Start()
    {
        VFX.Ensure();
        GameUI.Ensure();
        WorldUI.Ensure();
        AIDebugDraw.Ensure();
        AudioManager.Music(MusicTrack.Dungeon);
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        State = GameState.Intro;
        GameUI.I.FadeIn(0.9f);
        yield return new WaitForSecondsRealtime(0.5f);
        GameUI.I.ShowBanner("CAPY CONQUEST", "The Golem King stole the Golden Yuzu. Take it back!", new Color(1f, 0.72f, 0.25f), 3.4f);
        State = GameState.Playing;
        yield return new WaitForSecondsRealtime(1.0f);
        GameUI.I.ShowHint("<b>WASD</b> move   <b>Mouse</b> aim auto-attack   <b>SPACE</b> dash   <b>V</b> AI Vision   <b>ESC</b> pause", 9f);
    }

    private void Update()
    {
        if (!sceneChanging)
        {
            if (CapyInput.PausePressed())
            {
                if (State == GameState.Playing)
                    Pause();
                else if (State == GameState.Paused)
                    Resume();
            }

            if (CapyInput.VisionPressed() && State != GameState.GameOver && State != GameState.Victory)
                ToggleAIVision();

            HandleDebugKeys();
        }

        if (State == GameState.Playing)
        {
            Stats.time += Time.deltaTime;

            if (pendingUpgrades.Count > 0 && player != null && !player.Health.IsDead)
                ShowNextUpgrade();
        }

        UpdateTimeScale();
    }

    private void UpdateTimeScale()
    {
        float scale = 1f;

        if (State == GameState.LevelUp || State == GameState.Paused)
        {
            scale = 0f;
        }
        else if (Time.unscaledTime < hitStopUntil)
        {
            scale = 0.04f;
        }
        else if (Time.unscaledTime < slowMoUntil)
        {
            scale = slowMoScale;
        }

        Time.timeScale = scale * TestSpeed;
    }

    // ---------------------------------------------------------------- waktu

    public void HitStop(float duration)
    {
        if (State != GameState.Playing)
            return;
        hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + duration);
    }

    public void SlowMotion(float scale, float duration)
    {
        slowMoScale = scale;
        slowMoUntil = Time.unscaledTime + duration;
    }

    // ---------------------------------------------------------------- state

    public void Pause()
    {
        State = GameState.Paused;
        GameUI.I.ShowPause(true);
        AudioManager.Sfx("click");
    }

    public void Resume()
    {
        if (State != GameState.Paused)
            return;
        State = GameState.Playing;
        GameUI.I.ShowPause(false);
        AudioManager.Sfx("click");
    }

    public void BeginCutscene()
    {
        if (State == GameState.Playing)
            State = GameState.Cutscene;
    }

    public void EndCutscene()
    {
        if (State == GameState.Cutscene)
            State = GameState.Playing;
    }

    public void ToggleAIVision()
    {
        AIVision = !AIVision;
        GameUI.I.SetAIVision(AIVision);
        AudioManager.Sfx("click");
    }

    // ---------------------------------------------------------------- upgrade

    public void QueueUpgrade(bool treasure)
    {
        pendingUpgrades.Enqueue(treasure);
    }

    private void ShowNextUpgrade()
    {
        bool treasure = pendingUpgrades.Dequeue();
        List<UpgradeDef> choices = UpgradeLibrary.RollChoices(player, 3);
        if (choices.Count == 0)
            return;

        State = GameState.LevelUp;
        AudioManager.Sfx(treasure ? "chest" : "levelup");
        GameUI.I.ShowUpgradeChoice(choices, treasure, PickUpgrade);
    }

    private void PickUpgrade(UpgradeDef def)
    {
        UpgradeLibrary.Apply(player, def);
        player.Health.SetInvulnerable(0.8f);
        VFX.Sparkle(player.transform.position + Vector3.up, def.color, 24);
        AudioManager.Sfx("select");
        State = GameState.Playing;
    }

    // ---------------------------------------------------------------- event

    public void OnRoomEntered(RoomController room)
    {
        CurrentRoom = room;
    }

    public void OnRoomCleared(RoomController room)
    {
        Stats.roomsCleared++;
    }

    public void OnEnemyKilled(EnemyBase enemy)
    {
        Stats.kills++;
        if (player != null && player.Stats.HealOnKill > 0)
            player.Health.Heal(player.Stats.HealOnKill, false);
    }

    public void OnPlayerDied()
    {
        if (State == GameState.GameOver || State == GameState.Victory)
            return;
        State = GameState.GameOver;
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        AudioManager.Music(MusicTrack.None, 0.4f);
        AudioManager.Sfx("gameover");
        SlowMotion(0.3f, 1.4f);
        yield return new WaitForSecondsRealtime(1.7f);
        GameUI.I.ShowGameOver(Stats);
    }

    public void OnGoldenYuzuCollected()
    {
        if (State == GameState.Victory || State == GameState.GameOver)
            return;
        State = GameState.Victory;
        StartCoroutine(VictoryRoutine());
    }

    private IEnumerator VictoryRoutine()
    {
        AudioManager.Music(MusicTrack.Victory, 0.3f);
        player.Visual.Celebrate();
        yield return new WaitForSecondsRealtime(1.6f);

        float best = PlayerPrefs.GetFloat(BestTimeKey, 0f);
        bool newBest = best <= 0f || Stats.time < best;
        if (newBest)
        {
            PlayerPrefs.SetFloat(BestTimeKey, Stats.time);
            PlayerPrefs.Save();
            best = Stats.time;
        }

        GameUI.I.ShowVictory(Stats, ComputeRank(), best, newBest);
    }

    public string ComputeRank()
    {
        int score = 0;
        if (Stats.time < 300f) score += 2;
        else if (Stats.time < 480f) score += 1;
        if (Stats.damageTaken < 60) score += 2;
        else if (Stats.damageTaken < 160) score += 1;

        if (score >= 4) return "S";
        if (score == 3) return "A";
        if (score == 2) return "B";
        return "C";
    }

    // ---------------------------------------------------------------- scene

    public void Restart()
    {
        LoadScene(GameSceneName);
    }

    public void GoToMenu()
    {
        LoadScene(MenuSceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (sceneChanging)
            return;
        sceneChanging = true;
        CapyInput.UseOverride = false;
        GameUI.I.FadeOut(0.5f, () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        });
    }

    // ---------------------------------------------------------------- bantuan presentasi

    private void HandleDebugKeys()
    {
        if (CapyInput.KeyPressed(Key.F1))
        {
            GodMode = !GodMode;
            GameUI.I.ShowToast(GodMode ? "GOD MODE ON" : "GOD MODE OFF", new Color(1f, 0.85f, 0.3f));
        }

        if (State != GameState.Playing)
            return;

        if (CapyInput.KeyPressed(Key.F2))
            KillRoomEnemies();

        if (CapyInput.KeyPressed(Key.F3))
            player.Level.AddXP(player.Level.XPToNext - player.Level.XP);

        if (CapyInput.KeyPressed(Key.F4))
            WarpToNextRoom();

        if (CapyInput.KeyPressed(Key.F10))
        {
            AutoPilot pilot = player.GetComponent<AutoPilot>();
            if (pilot == null)
                pilot = player.gameObject.AddComponent<AutoPilot>();
            pilot.enabled = !pilot.enabled;
            GameUI.I.ShowToast(pilot.enabled ? "AUTOPILOT ON (Utility AI)" : "AUTOPILOT OFF", new Color(0.5f, 0.85f, 1f));
        }
    }

    public void KillRoomEnemies()
    {
        List<EnemyBase> copy = new List<EnemyBase>(EnemyBase.Active);
        foreach (EnemyBase enemy in copy)
        {
            if (enemy != null && enemy.IsAlive)
                enemy.Kill();
        }
    }

    public void WarpToNextRoom()
    {
        RoomController next = null;
        foreach (RoomController room in rooms)
        {
            if (room.State != RoomController.RoomState.Cleared && room != CurrentRoom)
            {
                next = room;
                break;
            }
        }

        if (CurrentRoom != null && CurrentRoom.State == RoomController.RoomState.Active)
        {
            KillRoomEnemies();
            next = CurrentRoom;
        }

        if (next == null)
            return;

        player.TeleportTo(next.EntryPoint);
        CameraRig.I.SnapToTarget();
    }
}
