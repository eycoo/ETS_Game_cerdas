using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnEntry
{
    public EnemyType type;
    public int count = 1;
}

[System.Serializable]
public class Wave
{
    public SpawnEntry[] entries;
}

public enum RoomReward
{
    None,
    Chest,
    HotSpring
}

// Satu ruangan dungeon. Saat Capy masuk: pintu tertutup, musuh muncul per gelombang,
// dan setelah semua kalah pintu keluar terbuka (pola Archero / Binding of Isaac).
public class RoomController : MonoBehaviour
{
    public enum RoomState
    {
        Waiting,
        Active,
        Cleared
    }

    [Header("Info")]
    public int roomNumber = 1;
    public string roomName = "Room";
    public string subtitle = "";
    public string aiTopic = "";
    public Color themeColor = Color.white;
    public Vector2 size = new Vector2(20f, 16f);
    public bool isBossRoom;
    public bool isSafeRoom;
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;

    [Header("Links")]
    public Door entryDoor;
    public Door exitDoor;
    public Transform[] spawnPoints;
    public Transform rewardPoint;
    public GameObject exitGuide;
    public GolemBossAI boss;

    [Header("Waves")]
    public Wave[] waves;
    public RoomReward reward;

    public RoomState State { get; private set; } = RoomState.Waiting;
    public NavGrid Grid { get; private set; }
    public int AliveCount => alive.Count;
    public int WaveIndex { get; private set; }
    public int WaveCount => waves != null ? waves.Length : 0;
    public Vector3 EntryPoint => transform.position + new Vector3(0f, 0f, -size.y * 0.5f + 1.2f);

    private readonly List<EnemyBase> alive = new List<EnemyBase>();
    private int pendingSpawns;

    public static readonly Color PortalColor = new Color(0.7f, 0.4f, 1f);

    private void Awake()
    {
        Grid = new NavGrid(transform.position, size, 0.5f);
        if (exitGuide != null)
            exitGuide.SetActive(false);
    }

    private void Start()
    {
        if (isSafeRoom)
        {
            State = RoomState.Cleared;
            if (exitDoor != null)
                exitDoor.Open();
            if (exitGuide != null)
                exitGuide.SetActive(true);
        }
    }

    public bool Contains(Vector3 p, float margin)
    {
        Vector3 c = transform.position;
        return Mathf.Abs(p.x - c.x) <= size.x * 0.5f - margin && Mathf.Abs(p.z - c.z) <= size.y * 0.5f - margin;
    }

    public Vector3 ClampInside(Vector3 p, float margin)
    {
        Vector3 c = transform.position;
        float hx = Mathf.Max(0.1f, size.x * 0.5f - margin);
        float hz = Mathf.Max(0.1f, size.y * 0.5f - margin);
        p.x = Mathf.Clamp(p.x, c.x - hx, c.x + hx);
        p.z = Mathf.Clamp(p.z, c.z - hz, c.z + hz);
        return p;
    }

    private void Update()
    {
        GameManager gm = GameManager.I;
        if (gm == null || PlayerController.I == null)
            return;

        if (Contains(PlayerController.I.transform.position, 0f) && gm.CurrentRoom != this && State != RoomState.Waiting)
            gm.OnRoomEntered(this);

        if (State != RoomState.Waiting || !gm.IsPlaying || isSafeRoom)
            return;

        if (Contains(PlayerController.I.transform.position, 1.6f))
            Activate();
    }

    private void Activate()
    {
        State = RoomState.Active;
        GameManager.I.OnRoomEntered(this);
        if (entryDoor != null)
            entryDoor.Close();
        if (exitDoor != null)
            exitDoor.Close();
        Grid.Build(0.4f);

        if (isBossRoom)
            StartCoroutine(BossRoutine());
        else
            StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        GameUI.I.ShowBanner("ROOM " + roomNumber + "   " + roomName.ToUpper(), subtitle, themeColor, 2.6f);
        yield return new WaitForSeconds(1.1f);

        for (WaveIndex = 0; WaveIndex < WaveCount; WaveIndex++)
        {
            if (WaveIndex > 0)
            {
                GameUI.I.ShowToast("WAVE " + (WaveIndex + 1) + " / " + WaveCount, themeColor);
                yield return new WaitForSeconds(0.9f);
            }

            yield return StartCoroutine(SpawnWave(waves[WaveIndex]));
            while (alive.Count > 0 || pendingSpawns > 0)
            {
                alive.RemoveAll(e => e == null);
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.35f);
        Clear();
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        List<EnemyType> types = new List<EnemyType>();
        foreach (SpawnEntry entry in wave.entries)
        {
            for (int i = 0; i < entry.count; i++)
                types.Add(entry.type);
        }

        List<Vector3> positions = new List<Vector3>();
        foreach (EnemyType type in types)
        {
            Vector3 pos = PickSpawnPoint(positions);
            positions.Add(pos);
            Telegraph.Spawn(pos, 0.95f, 0.75f, PortalColor);
            pendingSpawns++;
        }
        AudioManager.Sfx("spawn", 0.55f);

        yield return new WaitForSeconds(0.75f);

        for (int i = 0; i < types.Count; i++)
        {
            GameObject prefab = GameDatabase.I.GetEnemy(types[i]);
            pendingSpawns--;
            if (prefab == null)
                continue;

            Vector3 pos = positions[i];
            if (types[i] == EnemyType.Bat)
                pos.y = 1.5f;
            Quaternion rot = Quaternion.LookRotation(CapyMath.DirectionTo(pos, PlayerController.I.transform.position));
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            EnemyBase e = go.GetComponent<EnemyBase>();
            RegisterEnemy(e);
            Transform visual = go.transform.Find("Visual");
            if (visual != null)
                visual.rotation = rot;
            VFX.Poof(positions[i] + Vector3.up * 0.4f, PortalColor.WithAlpha(0.6f), 6, 0.8f);
            yield return new WaitForSeconds(0.04f);
        }
    }

    private Vector3 PickSpawnPoint(List<Vector3> used)
    {
        Vector3 player = PlayerController.I.transform.position;
        Vector3 best = transform.position;
        float bestScore = float.MinValue;

        if (spawnPoints != null)
        {
            foreach (Transform sp in spawnPoints)
            {
                if (sp == null)
                    continue;
                Vector3 p = sp.position + new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
                if (!Grid.IsWalkable(p))
                    p = sp.position;

                float score = Mathf.Min(CapyMath.FlatDistance(p, player), 9f) + Random.Range(0f, 3f);
                foreach (Vector3 u in used)
                {
                    if (CapyMath.FlatDistance(u, p) < 1.6f)
                        score -= 10f;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }
        }

        best.y = 0f;
        return best;
    }

    public void RegisterEnemy(EnemyBase e)
    {
        if (e == null)
            return;
        e.Room = this;
        float hp = (e.IsBoss ? 1f : healthMultiplier) * GameSettings.EnemyHealthMultiplier;
        float dmg = (e.IsBoss ? 1f : damageMultiplier) * GameSettings.EnemyDamageMultiplier;
        e.ApplyRoomScaling(hp, dmg);
        if (!alive.Contains(e))
            alive.Add(e);
    }

    public void OnEnemyDied(EnemyBase e)
    {
        alive.Remove(e);
    }

    private void Clear()
    {
        State = RoomState.Cleared;
        if (exitDoor != null)
            exitDoor.Open();
        if (exitGuide != null)
            exitGuide.SetActive(true);

        XPGem.CollectAll();
        GameUI.I.ShowToast("ROOM CLEARED!", new Color(0.55f, 1f, 0.6f), true);
        AudioManager.Sfx("room_clear", 0.9f);
        GameManager.I.OnRoomCleared(this);
        SpawnReward();
    }

    private void SpawnReward()
    {
        GameDatabase db = GameDatabase.I;
        Vector3 pos = rewardPoint != null ? rewardPoint.position : transform.position;
        GameObject prefab = null;
        if (reward == RoomReward.Chest) prefab = db.chest;
        if (reward == RoomReward.HotSpring) prefab = db.hotSpring;
        if (prefab == null)
            return;

        Instantiate(prefab, pos, Quaternion.Euler(0f, 180f, 0f));
        VFX.Poof(pos + Vector3.up * 0.5f, new Color(1f, 1f, 1f, 0.7f), 16, 1.2f);
        VFX.Sparkle(pos + Vector3.up, new Color(1f, 0.85f, 0.35f), 24);
    }

    private IEnumerator BossRoutine()
    {
        GameManager gm = GameManager.I;
        gm.BeginCutscene();
        AudioManager.Music(MusicTrack.None, 0.8f);
        GameUI.I.SetCinematicBars(true);
        yield return new WaitForSeconds(0.6f);

        if (boss != null)
        {
            CameraRig.I.Focus(boss.transform.position + Vector3.forward * 0.5f, -7f);
            yield return new WaitForSeconds(1.0f);
            RegisterEnemy(boss);
            gm.Boss = boss;
            yield return StartCoroutine(boss.WakeUp());
            GameUI.I.ShowBossTitle("GOLEM KING", "Guardian of the Golden Yuzu");
            AudioManager.Music(MusicTrack.Boss, 0.4f);
            yield return new WaitForSeconds(1.8f);
        }

        CameraRig.I.ReleaseFocus();
        GameUI.I.SetCinematicBars(false);
        if (boss != null)
            GameUI.I.ShowBossBar(boss);
        gm.EndCutscene();

        while (boss != null && boss.IsAlive)
            yield return null;

        State = RoomState.Cleared;
        gm.OnRoomCleared(this);
        if (exitGuide != null)
            exitGuide.SetActive(true);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.6f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.05f, new Vector3(size.x, 0.1f, size.y));
    }
}
