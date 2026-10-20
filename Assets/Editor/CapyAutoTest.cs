using System.IO;
using UnityEditor;
using UnityEngine;

// Uji otomatis: AutoPilot memainkan game di Play Mode, lalu durasi tiap ruangan,
// damage yang diterima, level, dan hasil akhir dicatat ke Temp/autotest.log.
public static class CapyAutoTest
{
    private const string LogPath = "Temp/autotest.log";

    private static float speed = 3f;
    private static bool god;
    private static double nextStatus;
    private static int lastRoom = -1;
    private static float roomStart;
    private static int roomDamageStart;
    private static GameState lastState;
    private static float resultTime;
    private static bool finished;
    private static bool started;
    private static readonly System.Collections.Generic.List<float> shotTimes = new System.Collections.Generic.List<float>();
    private static readonly System.Collections.Generic.List<string> shotNames = new System.Collections.Generic.List<string>();
    public static bool CaptureShots;

    private static void Schedule(float gameTime, string name)
    {
        if (!CaptureShots)
            return;
        shotTimes.Add(gameTime);
        shotNames.Add(name);
    }

    public static void Begin(float testSpeed, bool godMode)
    {
        Begin(testSpeed, godMode, false, 1);
    }

    // humanLike: AutoPilot bereaksi lambat dan tanpa dash. difficulty: 0 Easy, 1 Normal, 2 Hard.
    public static void Begin(float testSpeed, bool godMode, bool humanLike, int difficulty)
    {
        AutoPilot.HumanLike = humanLike;
        GameSettings.Difficulty = difficulty;
        speed = testSpeed;
        god = godMode;
        started = false;
        finished = false;
        lastRoom = -1;
        shotTimes.Clear();
        shotNames.Clear();
        Directory.CreateDirectory("Temp/AutoShots");
        File.WriteAllText(LogPath, "=== autotest speed " + speed + " god " + god + " humanLike " + humanLike + " mode " + GameSettings.DifficultyName + " " + System.DateTime.Now + "\n");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    public static void Stop()
    {
        EditorApplication.update -= Tick;
    }

    private static void Write(string line)
    {
        File.AppendAllText(LogPath, line + "\n");
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            if (started)
                Stop();
            return;
        }

        GameManager gm = GameManager.I;
        PlayerController p = PlayerController.I;
        if (gm == null || p == null)
            return;

        if (!started)
        {
            started = true;
            AutoPilot pilot = p.GetComponent<AutoPilot>();
            if (pilot == null)
                pilot = p.gameObject.AddComponent<AutoPilot>();
            pilot.enabled = true;
            GameManager.TestSpeed = speed;
            if (god && !gm.GodMode)
            {
                System.Reflection.PropertyInfo prop = typeof(GameManager).GetProperty("GodMode");
                prop.GetSetMethod(true).Invoke(gm, new object[] { true });
            }
            Write("started");
        }

        int room = gm.CurrentRoom != null ? gm.CurrentRoom.roomNumber : -1;
        if (room != lastRoom && gm.CurrentRoom != null && gm.CurrentRoom.State == RoomController.RoomState.Active)
        {
            lastRoom = room;
            roomStart = gm.Stats.time;
            roomDamageStart = gm.Stats.damageTaken;
            Write(string.Format("[t={0:0.0}] ENTER room {1} {2} lv={3} hp={4:0}", gm.Stats.time, room, gm.CurrentRoom.roomName, p.Level.Level, p.Health.Current));
            Schedule(gm.Stats.time + (room == 5 ? 1.5f : 4.5f), "room" + room + "_a");
            Schedule(gm.Stats.time + (room == 5 ? 9f : 12f), "room" + room + "_b");
        }
        if (lastRoom >= 0 && gm.CurrentRoom != null && gm.CurrentRoom.roomNumber == lastRoom && gm.CurrentRoom.State == RoomController.RoomState.Cleared && roomStart >= 0f)
        {
            Write(string.Format("[t={0:0.0}] CLEAR room {1} in {2:0.0}s, damage taken {3}, lv={4}, hp={5:0}/{6:0}", gm.Stats.time, lastRoom, gm.Stats.time - roomStart, gm.Stats.damageTaken - roomDamageStart, p.Level.Level, p.Health.Current, p.Health.Max));
            roomStart = -1f;
            Schedule(gm.Stats.time + 1.8f, "room" + lastRoom + "_clear");
        }

        if (gm.State != lastState)
        {
            if (gm.State == GameState.GameOver || gm.State == GameState.Victory)
            {
                string ups = "";
                foreach (System.Collections.Generic.KeyValuePair<UpgradeId, int> kv in p.Stats.AllLevels())
                    ups += kv.Key + ":" + kv.Value + " ";
                resultTime = Time.unscaledTime;
                Write(string.Format("[t={0:0.0}] RESULT {1} kills={2} dmgTaken={3} lv={4} upgrades={5}", gm.Stats.time, gm.State, gm.Stats.kills, gm.Stats.damageTaken, p.Level.Level, ups));
                if (gm.Boss != null)
                    Write("boss hp left " + gm.Boss.Health.ToString("0") + "/" + gm.Boss.MaxHealth);
                finished = true;
                shotTimes.Add(-1f);
                shotNames.Add("result_" + gm.State);
            }
            lastState = gm.State;
        }

        for (int i = shotTimes.Count - 1; i >= 0; i--)
        {
            bool due = shotTimes[i] < 0f ? (finished && Time.unscaledTime > resultTime + 3f) : gm.Stats.time >= shotTimes[i];
            if (!due)
                continue;
            ScreenCapture.CaptureScreenshot("Temp/AutoShots/" + shotNames[i] + ".png");
            Write("  shot " + shotNames[i]);
            shotTimes.RemoveAt(i);
            shotNames.RemoveAt(i);
            break;
        }

        if (EditorApplication.timeSinceStartup >= nextStatus && !finished)
        {
            nextStatus = EditorApplication.timeSinceStartup + 3.0;
            string bossInfo = gm.Boss != null && gm.Boss.IsAlive ? " boss=" + gm.Boss.Health.ToString("0") + " (" + gm.Boss.AIState + ")" : "";
            Write(string.Format("  t={0:0.0} {1} hp={2:0} lv={3} enemies={4}{5}", gm.Stats.time, gm.State, p.Health.Current, p.Level.Level, EnemyBase.Active.Count, bossInfo));
        }
    }
}
