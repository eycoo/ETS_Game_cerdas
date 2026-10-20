using System.Collections.Generic;
using UnityEngine;

// AUTOPILOT (tombol F10) — teknik AI: UTILITY AI / CONTEXT STEERING.
// Capy dikendalikan AI. Setiap frame, 16 arah gerak + pilihan "diam" diberi skor:
//   skor = - bahaya (musuh dekat, proyektil yang mendekat, area telegraph, shockwave)
//          - penalti tembok + tarikan ke tujuan (pintu, peti, onsen, Golden Yuzu)
// Pilihan dengan skor tertinggi dijalankan. "Diam" berarti menyerang (aturan Archero).
// Dipakai juga untuk pengujian otomatis seluruh game.
public class AutoPilot : MonoBehaviour
{
    private const int Directions = 16;
    private const float Probe = 1.6f;

    private static readonly UpgradeId[] Priority =
    {
        UpgradeId.TwinSlash, UpgradeId.ChillVibes, UpgradeId.SharpSword, UpgradeId.WideSwing,
        UpgradeId.YuzuSnack, UpgradeId.DiagonalSlash, UpgradeId.YuzuOrbit, UpgradeId.BouncyBlade,
        UpgradeId.PiercingEdge, UpgradeId.ThickFur, UpgradeId.SpicyYuzu, UpgradeId.LuckyLeaf,
        UpgradeId.OnsenAura, UpgradeId.Munchies, UpgradeId.Zoomies, UpgradeId.DashMaster, UpgradeId.RearGuard
    };

    private PlayerController player;
    private readonly float[] scores = new float[Directions];
    private float stayScore;
    private int chosen = -1;
    private float upgradeTimer;
    private float objectiveTimer;
    private Vector3 objective;
    private bool hasObjective;
    private readonly List<Vector3> path = new List<Vector3>();
    private int pathIndex;
    private float repathTimer;
    private float decisionTimer;
    private Vector2 lastDecision;

    // Mode uji "mirip manusia": keputusan hanya tiap 0,3 detik, tanpa dash,
    // dan proyektil baru diperhatikan dari jarak dekat. Dipakai untuk memperkirakan kesulitan.
    public static bool HumanLike;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        HumanLike = false;
    }

    public string Mode { get; private set; } = "";

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        CapyInput.UseOverride = true;
        CapyInput.MoveOverride = Vector2.zero;
    }

    private void OnDisable()
    {
        CapyInput.UseOverride = false;
        CapyInput.MoveOverride = Vector2.zero;
    }

    private void Update()
    {
        GameManager gm = GameManager.I;
        if (gm == null || player == null)
            return;

        if (gm.State == GameState.LevelUp)
        {
            upgradeTimer += Time.unscaledDeltaTime;
            if (upgradeTimer > 0.9f)
            {
                upgradeTimer = 0f;
                GameUI.I.AutoPick(Priority);
            }
            return;
        }
        upgradeTimer = 0f;

        if (!gm.IsPlaying || player.Health.IsDead)
        {
            CapyInput.MoveOverride = Vector2.zero;
            return;
        }

        if (HumanLike)
        {
            decisionTimer -= Time.deltaTime;
            if (decisionTimer > 0f)
            {
                CapyInput.MoveOverride = lastDecision;
                return;
            }
            decisionTimer = 0.3f;
        }

        Vector3 pos = player.transform.position;
        bool enemies = AnyEnemy();
        UpdateObjective(enemies);

        float hereDanger = Danger(pos);
        float bestScore = float.MinValue;
        chosen = -1;

        Vector3 goalDir = Vector3.zero;
        if (!enemies && hasObjective)
            goalDir = NextWaypoint(pos);

        for (int i = 0; i < Directions; i++)
        {
            Vector3 dir = DirectionVector(i);
            Vector3 p = pos + dir * Probe;
            float s = -Danger(p) * 1.0f;

            RaycastHit hit;
            if (Physics.SphereCast(pos + Vector3.up * 0.6f, 0.35f, dir, out hit, Probe, Layers.ObstacleMask, QueryTriggerInteraction.Ignore))
                s -= 4f * (1f - hit.distance / Probe);

            if (goalDir != Vector3.zero)
                s += Vector3.Dot(dir, goalDir) * 3f;
            else if (enemies)
                s += KeepRangeBonus(p);

            scores[i] = s;
            if (s > bestScore)
            {
                bestScore = s;
                chosen = i;
            }
        }

        // Pilihan diam: menyerang. Lebih menarik jika ada musuh dan posisi sekarang aman.
        stayScore = enemies ? 1.2f - hereDanger * 1.2f + KeepRangeBonus(pos) : -10f;
        if (!enemies && !hasObjective)
            stayScore = 0f;

        if (stayScore >= bestScore)
        {
            chosen = -1;
            Mode = enemies ? "ATTACK (stand still)" : "IDLE";
            CapyInput.MoveOverride = Vector2.zero;
        }
        else
        {
            Vector3 d = DirectionVector(chosen);
            Mode = enemies ? (hereDanger > 0.5f ? "EVADE" : "REPOSITION") : "GO TO OBJECTIVE";
            CapyInput.MoveOverride = new Vector2(d.x, d.z);
            if (!HumanLike && hereDanger > 3.5f && player.DashCooldown01 >= 1f)
                CapyInput.DashOverride = true;
        }
        lastDecision = CapyInput.MoveOverride;
    }

    private static Vector3 DirectionVector(int i)
    {
        float a = i / (float)Directions * Mathf.PI * 2f;
        return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
    }

    private bool AnyEnemy()
    {
        foreach (EnemyBase e in EnemyBase.Active)
        {
            if (e != null && e.IsTargetable)
                return true;
        }
        RoomController room = GameManager.I.CurrentRoom;
        return room != null && room.State == RoomController.RoomState.Active && !room.isBossRoom;
    }

    // Nilai bahaya di titik p (semakin besar semakin berbahaya).
    private float Danger(Vector3 p)
    {
        float danger = 0f;

        foreach (EnemyBase e in EnemyBase.Active)
        {
            if (e == null || !e.IsAlive || e.IsSpawning)
                continue;
            float safe = e.IsBoss ? 5.5f : (e is BatAI ? 2.4f : 2.8f);
            float d = CapyMath.FlatDistance(p, e.transform.position);
            if (d < safe)
                danger += (safe - d) / safe * (e.IsBoss ? 4f : 2.5f);
        }

        foreach (Projectile pr in Projectile.EnemyActive)
        {
            if (pr == null)
                continue;
            if (HumanLike && CapyMath.FlatDistance(pr.transform.position, player.transform.position) > 4f)
                continue;
            Vector3 a = pr.transform.position.Flat();
            Vector3 b = a + pr.Direction * pr.Speed * 0.7f;
            float d = DistanceToSegment(p.Flat(), a, b);
            if (d < 1.1f)
                danger += (1.1f - d) * 3f;
        }

        foreach (Telegraph tg in Telegraph.Active)
        {
            if (tg != null && tg.IsDanger && tg.Contains(p, 0.6f))
                danger += 2.5f + tg.Progress * 3f;
        }

        foreach (Shockwave sw in Shockwave.Active)
        {
            if (sw == null)
                continue;
            float d = CapyMath.FlatDistance(p, sw.transform.position);
            float future = sw.CurrentRadius + sw.Speed * 0.25f;
            if (Mathf.Abs(d - future) < 1.2f)
                danger += 4f;
        }

        RoomController room = GameManager.I.CurrentRoom;
        if (room != null && !room.Contains(p, 0.9f) && room.State == RoomController.RoomState.Active)
            danger += 2f;

        return danger;
    }

    // Skor kecil untuk berada di jarak serang yang nyaman (tidak terlalu dekat, tidak terlalu jauh).
    private float KeepRangeBonus(Vector3 p)
    {
        EnemyBase target = EnemyBase.FindNearest(p, 30f, null);
        if (target == null)
            return 0f;
        float d = CapyMath.FlatDistance(p, target.transform.position);
        float ideal = target.IsBoss ? 7f : 5f;
        return -Mathf.Abs(d - ideal) * 0.12f;
    }

    private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
        return Vector3.Distance(p, a + ab * t);
    }

    // ------------------------------------------------------------ tujuan

    private void UpdateObjective(bool enemies)
    {
        objectiveTimer -= Time.deltaTime;
        if (objectiveTimer > 0f)
            return;
        objectiveTimer = 0.4f;

        hasObjective = false;
        if (enemies)
            return;

        GoldenYuzu yuzu = FindAnyObjectByType<GoldenYuzu>();
        if (yuzu != null && yuzu.gameObject.activeInHierarchy)
        {
            SetObjective(yuzu.transform.position);
            return;
        }

        foreach (TreasureChest chest in FindObjectsByType<TreasureChest>())
        {
            if (!chest.IsOpened)
            {
                SetObjective(chest.transform.position);
                return;
            }
        }

        foreach (HotSpring spring in FindObjectsByType<HotSpring>())
        {
            if (spring.IsAvailable && player.Health.Current < player.Health.Max * 0.95f)
            {
                SetObjective(spring.transform.position);
                return;
            }
        }

        // Menuju ruangan berikutnya yang belum selesai.
        RoomController[] rooms = GameManager.I.Rooms;
        foreach (RoomController room in rooms)
        {
            if (room.State != RoomController.RoomState.Cleared)
            {
                SetObjective(room.transform.position);
                return;
            }
        }
    }

    private void SetObjective(Vector3 p)
    {
        if (!hasObjective || CapyMath.FlatDistance(p, objective) > 0.5f)
        {
            path.Clear();
            pathIndex = 0;
            repathTimer = 0f;
        }
        objective = p;
        hasObjective = true;
    }

    // Jika berada di ruangan yang sudah bersih, jalan ke pintu utara lewat A* lalu lurus ke tujuan.
    private Vector3 NextWaypoint(Vector3 pos)
    {
        RoomController room = GameManager.I.CurrentRoom;
        Vector3 target = objective;

        if (room != null && room.Contains(pos, 0f) && !room.Contains(objective, 0f))
        {
            Vector3 door = room.transform.position + new Vector3(0f, 0f, room.size.y * 0.5f - 0.4f);
            if (objective.z < room.transform.position.z)
                door = room.transform.position + new Vector3(0f, 0f, -room.size.y * 0.5f + 0.4f);
            target = CapyMath.FlatDistance(pos, door) < 0.8f ? door + Vector3.forward * 3f : door;

            if (room.Grid != null && room.Grid.IsBuilt && !CapyMath.HasLineOfSight(pos, door, 0.35f))
            {
                repathTimer -= Time.deltaTime;
                if (repathTimer <= 0f || pathIndex >= path.Count)
                {
                    room.Grid.FindPath(pos, door, path);
                    pathIndex = path.Count > 1 ? 1 : 0;
                    repathTimer = 0.5f;
                }
                if (pathIndex < path.Count && CapyMath.FlatDistance(pos, path[pathIndex]) < 0.5f)
                    pathIndex++;
                if (pathIndex < path.Count)
                    target = path[pathIndex];
            }
        }

        return CapyMath.DirectionTo(pos, target);
    }

    public void DrawAIDebug()
    {
        Vector3 pos = player.transform.position + Vector3.up * 0.1f;
        float max = 0.01f;
        for (int i = 0; i < Directions; i++)
            max = Mathf.Max(max, Mathf.Abs(scores[i]));

        for (int i = 0; i < Directions; i++)
        {
            float n = scores[i] / max;
            Color c = n >= 0f ? new Color(0.4f, 1f, 0.5f, 0.85f) : new Color(1f, 0.35f, 0.3f, 0.85f);
            float len = 0.7f + Mathf.Abs(n) * 1.3f;
            AIDebugDraw.Line(pos, pos + DirectionVector(i) * len, c, i == chosen ? 0.14f : 0.05f);
        }
        if (chosen < 0)
            AIDebugDraw.Circle(pos, 0.7f, new Color(0.4f, 0.9f, 1f, 0.9f), 0.08f, 24);
        if (hasObjective)
            AIDebugDraw.Line(pos, objective + Vector3.up * 0.1f, new Color(1f, 0.9f, 0.4f, 0.5f), 0.05f);
    }
}
