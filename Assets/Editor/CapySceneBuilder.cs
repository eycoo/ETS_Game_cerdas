using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static CapyEditorUtil;

// Membangun scene game (Capy Camp + 5 ruangan dungeon) dan scene menu utama dari kode.
public static class CapySceneBuilder
{
    public const string GameScenePath = "Assets/Scenes/CapyConquest.unity";
    public const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    public const string ProfilePath = "Assets/Art/PostProcess.asset";

    private class Theme
    {
        public Material floor;
        public Material wall;
        public Material wallAlt;
        public Material cap;
        public Color fill;
        public Color torch;
        public bool logs;
    }

    private static Material Tiled(string name, Texture2D tex, Vector2 size, float tileWorld)
    {
        return CapyArt.Lit(name, Color.white, 0.12f, 0f, tex, new Vector2(size.x / tileWorld, size.y / tileWorld));
    }

    // ================================================================== game scene

    public static void BuildGameScene(GameDatabase db, GameObject playerPrefab)
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Random.InitState(7);

        Texture2D stone = AssetDatabase.LoadAssetAtPath<Texture2D>(CapyTextures.TexFolder + "/StoneBlock.png");
        Theme camp = MakeTheme("Camp", new Color(0.62f, 0.44f, 0.28f), new Color(0.55f, 0.38f, 0.22f), CapyArt.WoodDark, new Color(1f, 0.75f, 0.45f), new Color(1f, 0.7f, 0.4f), stone);
        camp.logs = true;
        Theme garden = MakeTheme("Garden", new Color(0.55f, 0.6f, 0.55f), new Color(0.48f, 0.55f, 0.48f), CapyArt.Moss, new Color(0.55f, 1f, 0.55f), new Color(1f, 0.78f, 0.45f), stone);
        Theme barracks = MakeTheme("Barracks", new Color(0.6f, 0.52f, 0.45f), new Color(0.52f, 0.45f, 0.4f), CapyArt.StoneDark, new Color(1f, 0.65f, 0.35f), new Color(1f, 0.62f, 0.32f), stone);
        Theme library = MakeTheme("Library", new Color(0.52f, 0.44f, 0.66f), new Color(0.45f, 0.38f, 0.6f), CapyArt.RobeDark, new Color(0.75f, 0.5f, 1f), new Color(0.85f, 0.6f, 1f), stone);
        Theme cave = MakeTheme("Cave", new Color(0.32f, 0.36f, 0.5f), new Color(0.27f, 0.3f, 0.43f), CapyArt.StoneDark, new Color(0.4f, 0.7f, 1f), new Color(0.5f, 0.8f, 1f), stone);
        Theme throne = MakeTheme("Throne", new Color(0.55f, 0.58f, 0.66f), new Color(0.48f, 0.5f, 0.6f), CapyArt.Gold, new Color(0.6f, 0.75f, 1f), new Color(1f, 0.7f, 0.4f), stone);
        Theme corridor = MakeTheme("Corridor", new Color(0.42f, 0.42f, 0.48f), new Color(0.36f, 0.36f, 0.42f), CapyArt.StoneDark, Color.white, new Color(1f, 0.72f, 0.42f), stone);

        Transform level = new GameObject("Level").transform;

        // Lantai luar (gelap) di bawah semuanya.
        Texture2D outsideTex = AssetDatabase.LoadAssetAtPath<Texture2D>(CapyTextures.TexFolder + "/Floor_Outside.png");
        GameObject outside = GameObject.CreatePrimitive(PrimitiveType.Quad);
        outside.name = "OutsideGround";
        Object.DestroyImmediate(outside.GetComponent<Collider>());
        outside.transform.SetParent(level, false);
        outside.transform.position = new Vector3(0f, -0.04f, 60f);
        outside.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        outside.transform.localScale = new Vector3(120f, 220f, 1f);
        outside.GetComponent<MeshRenderer>().sharedMaterial = CapyArt.Lit("Floor_Outside", Color.white, 0.05f, 0f, outsideTex, new Vector2(24f, 44f));

        // Tata letak: ruangan berjajar ke utara, dihubungkan lorong 6 unit.
        Vector2[] sizes =
        {
            new Vector2(14f, 11f),
            new Vector2(20f, 16f),
            new Vector2(20f, 16f),
            new Vector2(20f, 16f),
            new Vector2(22f, 18f),
            new Vector2(24f, 20f)
        };
        float[] centers = new float[sizes.Length];
        centers[0] = 0f;
        for (int i = 1; i < sizes.Length; i++)
            centers[i] = centers[i - 1] + sizes[i - 1].y * 0.5f + sizes[i].y * 0.5f + 8f;

        Theme[] themes = { camp, garden, barracks, library, cave, throne };
        string[] floorKinds = { "Camp", "Garden", "Barracks", "Library", "Cave", "Throne" };
        RoomController[] rooms = new RoomController[sizes.Length];
        Door[] southDoors = new Door[sizes.Length];
        Door[] northDoors = new Door[sizes.Length];

        for (int i = 0; i < sizes.Length; i++)
        {
            Texture2D floorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(CapyTextures.TexFolder + "/Floor_" + floorKinds[i] + ".png");
            themes[i].floor = Tiled("Floor_" + floorKinds[i], floorTex, sizes[i], floorKinds[i] == "Throne" ? 6f : 5f);
            Transform roomRoot = new GameObject(i == 0 ? "Room0_CapyCamp" : "Room" + i).transform;
            roomRoot.SetParent(level, false);
            roomRoot.position = new Vector3(0f, 0f, centers[i]);
            BuildShell(roomRoot, sizes[i], themes[i], i > 0, i < sizes.Length - 1, out southDoors[i], out northDoors[i]);
            rooms[i] = roomRoot.gameObject.AddComponent<RoomController>();
            rooms[i].size = sizes[i];
            rooms[i].roomNumber = i;
        }

        Texture2D corridorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(CapyTextures.TexFolder + "/Floor_Corridor.png");
        for (int i = 0; i < sizes.Length - 1; i++)
        {
            float z0 = centers[i] + sizes[i].y * 0.5f + 1f;
            float z1 = centers[i + 1] - sizes[i + 1].y * 0.5f - 1f;
            corridor.floor = Tiled("Floor_Corridor", corridorTex, new Vector2(4f, z1 - z0), 4f);
            BuildCorridor(level, z0, z1, corridor, i);
        }

        // ---------------- isi tiap ruangan
        DecorateCamp(rooms[0].transform, sizes[0], camp);
        DecorateGarden(rooms[1].transform, sizes[1], garden);
        DecorateBarracks(rooms[2].transform, sizes[2], barracks);
        DecorateLibrary(rooms[3].transform, sizes[3], library);
        DecorateCave(rooms[4].transform, sizes[4], cave);
        GameObject boss = DecorateThrone(rooms[5].transform, sizes[5], throne, db);

        Physics.SyncTransforms();

        string[] names = { "Capy Camp", "Slime Garden", "Skeleton Barracks", "Wizard Library", "Bat Cave", "Golem Throne" };
        string[] subtitles =
        {
            "",
            "Enemy AI: STEERING  -  seek + separation + wander",
            "Enemy AI: FINITE STATE MACHINE  +  A* PATHFINDING",
            "Enemy AI: BEHAVIOR TREE  -  teleport, summon, cast",
            "Enemy AI: FLOCKING (BOIDS)  -  separation, alignment, cohesion",
            "Boss AI: BEHAVIOR TREE with two phases"
        };
        Color[] colors =
        {
            new Color(1f, 0.8f, 0.5f),
            new Color(0.55f, 1f, 0.45f),
            new Color(1f, 0.75f, 0.4f),
            new Color(0.85f, 0.6f, 1f),
            new Color(0.5f, 0.8f, 1f),
            new Color(1f, 0.45f, 0.35f)
        };

        for (int i = 0; i < rooms.Length; i++)
        {
            RoomController rc = rooms[i];
            rc.roomName = names[i];
            rc.subtitle = subtitles[i];
            rc.themeColor = colors[i];
            rc.entryDoor = southDoors[i];
            rc.exitDoor = northDoors[i];
            rc.isSafeRoom = i == 0;
            rc.isBossRoom = i == rooms.Length - 1;

            Transform[] chevrons;
            if (i < rooms.Length - 1)
            {
                GameObject guide = CapyArt.Chevrons(rc.transform, new Vector3(0f, 0f, sizes[i].y * 0.5f - 4.2f), 3, out chevrons);
                ExitGuide eg = guide.AddComponent<ExitGuide>();
                SetRefArray(eg, "chevrons", chevrons);
                rc.exitGuide = guide;
            }

            GameObject reward = Group("RewardPoint", rc.transform, new Vector3(0f, 0f, 0.5f));
            rc.rewardPoint = reward.transform;
            if (i > 0 && !rc.isBossRoom)
                rc.spawnPoints = MakeSpawnPoints(rc.transform, sizes[i]);
        }

        rooms[1].waves = new[] { W(E(EnemyType.SlimeSmall, 3), E(EnemyType.SlimeBig, 2)), W(E(EnemyType.SlimeBig, 3), E(EnemyType.SlimeSmall, 3)) };
        rooms[2].waves = new[] { W(E(EnemyType.SkeletonWarrior, 3)), W(E(EnemyType.SkeletonWarrior, 2), E(EnemyType.SkeletonArcher, 2)), W(E(EnemyType.SkeletonWarrior, 3), E(EnemyType.SkeletonArcher, 2)) };
        rooms[3].waves = new[] { W(E(EnemyType.Wizard, 1), E(EnemyType.SkeletonWarrior, 2)), W(E(EnemyType.Wizard, 2), E(EnemyType.SlimeBig, 2)), W(E(EnemyType.Wizard, 1), E(EnemyType.SkeletonArcher, 2), E(EnemyType.SkeletonWarrior, 2)) };
        rooms[4].waves = new[] { W(E(EnemyType.Bat, 6)), W(E(EnemyType.Bat, 6), E(EnemyType.SkeletonArcher, 2)), W(E(EnemyType.Bat, 8), E(EnemyType.Wizard, 1)) };
        float[] hpScale = { 1f, 1f, 1.6f, 2f, 2.6f, 1f };
        float[] dmgScale = { 1f, 1f, 1.1f, 1.2f, 1.3f, 1f };
        for (int i = 0; i < rooms.Length; i++)
        {
            rooms[i].healthMultiplier = hpScale[i];
            rooms[i].damageMultiplier = dmgScale[i];
        }
        rooms[2].reward = RoomReward.Chest;
        rooms[4].reward = RoomReward.HotSpring;
        rooms[5].boss = boss.GetComponent<GolemBossAI>();

        // ---------------- player
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(1.5f, 0f, -2.5f);

        // ---------------- sistem
        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        SetRef(gm, "player", player.GetComponent<PlayerController>());
        SetRefArray(gm, "rooms", rooms);

        GameObject camGo = MakeCamera(34f, new Color(0.05f, 0.045f, 0.09f));
        CameraRig rig = camGo.AddComponent<CameraRig>();
        SetRef(rig, "target", player.transform);
        camGo.transform.position = player.transform.position + new Vector3(0f, 20f, -14f);
        camGo.transform.rotation = Quaternion.Euler(56f, 0f, 0f);

        SetupLighting(new Color(0.75f, 0.78f, 1f), 1.05f, Quaternion.Euler(55f, -35f, 0f));
        MakeVolume();

        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, GameScenePath);
    }

    private static Wave W(params SpawnEntry[] entries)
    {
        Wave w = new Wave();
        w.entries = entries;
        return w;
    }

    private static SpawnEntry E(EnemyType type, int count)
    {
        SpawnEntry e = new SpawnEntry();
        e.type = type;
        e.count = count;
        return e;
    }

    private static Theme MakeTheme(string name, Color wall, Color wallAlt, Material cap, Color fill, Color torch, Texture2D stone)
    {
        Theme t = new Theme();
        t.wall = CapyArt.Lit("Wall_" + name, wall, 0.1f, 0f, stone, Vector2.one);
        t.wallAlt = CapyArt.Lit("WallAlt_" + name, wallAlt, 0.1f, 0f, stone, Vector2.one);
        t.cap = cap;
        t.fill = fill;
        t.torch = torch;
        return t;
    }

    private static Transform[] MakeSpawnPoints(Transform room, Vector2 size)
    {
        List<Transform> points = new List<Transform>();
        Transform parent = Group("SpawnPoints", room, Vector3.zero).transform;
        float hx = size.x * 0.5f - 2.2f;
        float hz = size.y * 0.5f - 2.2f;
        int nx = 5;
        int nz = 4;
        for (int iz = 0; iz < nz; iz++)
        {
            for (int ix = 0; ix < nx; ix++)
            {
                Vector3 local = new Vector3(Mathf.Lerp(-hx, hx, ix / (float)(nx - 1)), 0f, Mathf.Lerp(-hz + 3f, hz, iz / (float)(nz - 1)));
                Vector3 world = room.position + local;
                if (Physics.CheckSphere(world + Vector3.up * 0.7f, 0.9f, Layers.ObstacleMask, QueryTriggerInteraction.Ignore))
                    continue;
                points.Add(Group("SP", parent, local).transform);
            }
        }
        return points.ToArray();
    }

    // ================================================================== struktur ruangan

    private static void BuildShell(Transform room, Vector2 size, Theme theme, bool doorSouth, bool doorNorth, out Door south, out Door north)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        floor.name = "Floor";
        Object.DestroyImmediate(floor.GetComponent<Collider>());
        floor.transform.SetParent(room, false);
        floor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        floor.transform.localScale = new Vector3(size.x, size.y, 1f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = theme.floor;

        Transform walls = Group("Walls", room, Vector3.zero).transform;
        float hx = size.x * 0.5f;
        float hz = size.y * 0.5f;

        // Utara tinggi (latar), samping sedang, selatan rendah supaya tidak menutupi pandangan.
        WallLine(walls, new Vector3(-hx - 1f, 0f, hz + 0.5f), new Vector3(hx + 1f, 0f, hz + 0.5f), 3.2f, theme, doorNorth);
        WallLine(walls, new Vector3(-hx - 1f, 0f, -hz - 0.5f), new Vector3(hx + 1f, 0f, -hz - 0.5f), 1.1f, theme, doorSouth);
        WallLine(walls, new Vector3(-hx - 0.5f, 0f, -hz), new Vector3(-hx - 0.5f, 0f, hz), 2.4f, theme, false);
        WallLine(walls, new Vector3(hx + 0.5f, 0f, -hz), new Vector3(hx + 0.5f, 0f, hz), 2.4f, theme, false);

        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                float h = sz > 0 ? 3.6f : 1.6f;
                Part(PrimitiveType.Cube, "Corner", walls, new Vector3((hx + 0.5f) * sx, h * 0.5f, (hz + 0.5f) * sz), new Vector3(1.4f, h, 1.4f), theme.wallAlt);
                Part(PrimitiveType.Cube, "CornerCap", walls, new Vector3((hx + 0.5f) * sx, h + 0.08f, (hz + 0.5f) * sz), new Vector3(1.6f, 0.16f, 1.6f), theme.cap);
            }
        }

        south = doorSouth ? BuildDoor(room, new Vector3(0f, 0f, -hz - 0.5f), true, theme) : null;
        north = doorNorth ? BuildDoor(room, new Vector3(0f, 0f, hz + 0.5f), false, theme) : null;

        // Lampu pengisi warna tema.
        CapyArt.PointLight(room, new Vector3(0f, 6f, 0f), theme.fill, Mathf.Max(size.x, size.y) * 1.1f, 1.4f).name = "FillLight";
    }

    // Dinding dari balok-balok (variasi warna) + satu collider per segmen.
    private static void WallLine(Transform parent, Vector3 a, Vector3 b, float height, Theme theme, bool doorGap)
    {
        bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
        float length = alongX ? b.x - a.x : b.z - a.z;
        if (!doorGap)
        {
            WallSegment(parent, a, b, height, theme, alongX);
            return;
        }
        float mid = length * 0.5f;
        Vector3 dir = (b - a).normalized;
        WallSegment(parent, a, a + dir * (mid - 1.5f), height, theme, alongX);
        WallSegment(parent, a + dir * (mid + 1.5f), b, height, theme, alongX);
    }

    private static void WallSegment(Transform parent, Vector3 a, Vector3 b, float height, Theme theme, bool alongX)
    {
        float length = Vector3.Distance(a, b);
        if (length < 0.05f)
            return;
        GameObject seg = Group("Wall", parent, (a + b) * 0.5f);
        seg.layer = Layers.Obstacle;
        BoxCollider bc = seg.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, height * 0.5f, 0f);
        bc.size = alongX ? new Vector3(length, height, 1f) : new Vector3(1f, height, length);

        Vector3 dir = (b - a).normalized;
        if (theme.logs)
        {
            int logs = Mathf.Max(1, Mathf.RoundToInt(length / 0.55f));
            for (int i = 0; i < logs; i++)
            {
                float t = (i + 0.5f) / logs;
                Vector3 p = Vector3.Lerp(a, b, t) - seg.transform.localPosition;
                float h = height + ((i * 13) % 5) * 0.08f;
                Part(PrimitiveType.Cylinder, "Log", seg.transform, p + Vector3.up * h * 0.5f, new Vector3(0.55f, h * 0.5f, 0.55f), i % 2 == 0 ? CapyArt.Wood : CapyArt.WoodDark);
                Part(PrimitiveType.Sphere, "LogTip", seg.transform, p + Vector3.up * h, new Vector3(0.55f, 0.3f, 0.55f), CapyArt.WoodDark);
            }
            Part(PrimitiveType.Cube, "Rail", seg.transform, Vector3.up * height * 0.62f + (alongX ? Vector3.back : Vector3.left) * 0.3f, alongX ? new Vector3(length, 0.14f, 0.12f) : new Vector3(0.12f, 0.14f, length), CapyArt.WoodDark);
            return;
        }

        int blocks = Mathf.Max(1, Mathf.RoundToInt(length / 2f));
        float blockLen = length / blocks;
        for (int i = 0; i < blocks; i++)
        {
            Vector3 p = a + dir * (blockLen * (i + 0.5f)) - seg.transform.localPosition;
            float h = height + (((i * 7) % 3) - 1) * 0.06f;
            Vector3 scale = alongX ? new Vector3(blockLen - 0.04f, h, 1f) : new Vector3(1f, h, blockLen - 0.04f);
            Part(PrimitiveType.Cube, "Block", seg.transform, p + Vector3.up * h * 0.5f, scale, (i % 3 == 1) ? theme.wallAlt : theme.wall);
        }
        Vector3 capScale = alongX ? new Vector3(length + 0.1f, 0.16f, 1.2f) : new Vector3(1.2f, 0.16f, length + 0.1f);
        Part(PrimitiveType.Cube, "Cap", seg.transform, Vector3.up * (height + 0.08f), capScale, theme.cap);
    }

    private static Door BuildDoor(Transform room, Vector3 localPos, bool south, Theme theme)
    {
        GameObject d = Group(south ? "DoorSouth" : "DoorNorth", room, localPos);
        float frameH = south ? 2.4f : 3.6f;
        for (int s = -1; s <= 1; s += 2)
        {
            Part(PrimitiveType.Cube, "Post", d.transform, new Vector3(1.75f * s, frameH * 0.5f, 0f), new Vector3(0.6f, frameH, 1.3f), theme.wallAlt);
            Part(PrimitiveType.Cube, "PostCap", d.transform, new Vector3(1.75f * s, frameH + 0.1f, 0f), new Vector3(0.8f, 0.2f, 1.45f), theme.cap);
        }
        Part(PrimitiveType.Cube, "Lintel", d.transform, new Vector3(0f, frameH - 0.15f, 0f), new Vector3(3.2f, 0.3f, 1.1f), theme.wallAlt);

        Transform bars = Group("Bars", d.transform, Vector3.zero).transform;
        float barH = frameH - 0.3f;
        for (int i = 0; i < 6; i++)
        {
            float x = -1.25f + i * 0.5f;
            Part(PrimitiveType.Cylinder, "Bar", bars, new Vector3(x, barH * 0.5f, 0f), new Vector3(0.14f, barH * 0.5f, 0.14f), CapyArt.Iron);
            Part(PrimitiveType.Sphere, "Spike", bars, new Vector3(x, barH, 0f), new Vector3(0.14f, 0.22f, 0.14f), CapyArt.Iron);
        }
        Part(PrimitiveType.Cube, "CrossA", bars, new Vector3(0f, barH * 0.3f, 0f), new Vector3(2.9f, 0.1f, 0.1f), CapyArt.Iron);
        Part(PrimitiveType.Cube, "CrossB", bars, new Vector3(0f, barH * 0.75f, 0f), new Vector3(2.9f, 0.1f, 0.1f), CapyArt.Iron);

        d.layer = Layers.Obstacle;
        BoxCollider bc = d.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 1.5f, 0f);
        bc.size = new Vector3(3f, 3f, 0.6f);

        Light status = CapyArt.PointLight(d.transform, new Vector3(0f, frameH + 0.3f, south ? 0.8f : -0.8f), new Color(0.45f, 1f, 0.55f), 3.5f, 1.5f);
        Part(PrimitiveType.Sphere, "Gem", d.transform, new Vector3(0f, frameH + 0.05f, south ? 0.56f : -0.56f), Vector3.one * 0.28f, CapyArt.CrystalCyan);

        Door door = d.AddComponent<Door>();
        SetRef(door, "bars", bars);
        SetRef(door, "blocker", bc);
        SetFloat(door, "openDepth", barH + 0.4f);
        SetBool(door, "startOpen", south);
        SetRef(door, "statusLight", status);
        return door;
    }

    private static void BuildCorridor(Transform level, float z0, float z1, Theme theme, int index)
    {
        Transform c = Group("Corridor" + index, level, new Vector3(0f, 0f, (z0 + z1) * 0.5f)).transform;
        float len = z1 - z0;
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        floor.name = "Floor";
        Object.DestroyImmediate(floor.GetComponent<Collider>());
        floor.transform.SetParent(c, false);
        floor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        floor.transform.localScale = new Vector3(4f, len + 2f, 1f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = theme.floor;
        Part(PrimitiveType.Cube, "Carpet", c, new Vector3(0f, 0.012f, 0f), new Vector3(1.5f, 0.02f, len + 2f), CapyArt.CarpetRed);

        for (int s = -1; s <= 1; s += 2)
        {
            WallSegment(c, new Vector3(2.5f * s, 0f, -len * 0.5f - 1f), new Vector3(2.5f * s, 0f, len * 0.5f + 1f), 2.1f, theme, false);
            CapyArt.WallTorch(c, new Vector3(2.0f * s, 1.5f, 0f), theme.torch, s < 0 ? 90f : -90f);
        }
    }

    // ================================================================== dekorasi per ruangan

    private static void TorchesNorth(Transform room, Vector2 size, Theme theme, float spread)
    {
        float hz = size.y * 0.5f;
        CapyArt.WallTorch(room, new Vector3(-spread, 1.9f, hz - 0.05f), theme.torch, 180f);
        CapyArt.WallTorch(room, new Vector3(spread, 1.9f, hz - 0.05f), theme.torch, 180f);
    }

    private static void DecorateCamp(Transform room, Vector2 size, Theme theme)
    {
        Transform d = Group("Decor", room, Vector3.zero).transform;
        GameObject spring = CapyArt.HotSpringVisual(d, new Vector3(-3.2f, 0f, 0.8f), 1.7f);
        HotSpring hs = spring.AddComponent<HotSpring>();
        SetBool(hs, "decorative", true);
        CapyArt.Obstacle(spring, new Vector3(0f, 0.4f, 0f), new Vector3(3.6f, 0.8f, 3.6f));
        for (int i = 0; i < 3; i++)
        {
            GameObject y = Part(PrimitiveType.Sphere, "FloatingYuzu", spring.transform, new Vector3(-0.6f + i * 0.6f, 0.22f, -0.3f + (i % 2) * 0.6f), Vector3.one * 0.32f, CapyArt.Yuzu);
            Bobbing bob = y.AddComponent<Bobbing>();
            SetFloat(bob, "amplitude", 0.04f);
            SetFloat(bob, "speed", 2f);
            SetFloat(bob, "spinSpeed", 15f);
        }
        CapyArt.PointLight(d, new Vector3(-3.2f, 1.6f, 0.8f), new Color(0.5f, 0.85f, 1f), 6f, 2f);

        // Tenda.
        Transform tent = Group("Tent", d, new Vector3(4.2f, 0f, 1.6f)).transform;
        Part(PrimitiveType.Cube, "SideL", tent, new Vector3(-0.62f, 0.85f, 0f), new Vector3(0.08f, 2.1f, 2.6f), CapyArt.ClothBlue, new Vector3(0f, 0f, -38f));
        Part(PrimitiveType.Cube, "SideR", tent, new Vector3(0.62f, 0.85f, 0f), new Vector3(0.08f, 2.1f, 2.6f), CapyArt.Cloth, new Vector3(0f, 0f, 38f));
        Part(PrimitiveType.Cylinder, "Ridge", tent, new Vector3(0f, 1.66f, 0f), new Vector3(0.1f, 1.4f, 0.1f), CapyArt.WoodDark, new Vector3(90f, 0f, 0f));
        CapyArt.Obstacle(tent.gameObject, new Vector3(0f, 0.8f, 0f), new Vector3(2.4f, 1.6f, 2.6f));

        GameObject crate = CapyArt.Crate(d, new Vector3(5.4f, 0f, -1.4f), 0.9f, 12f);
        CapyArt.YuzuPile(crate.transform, new Vector3(0f, 0.9f, 0f)).transform.localScale = Vector3.one * 0.7f;
        CapyArt.Crate(d, new Vector3(4.4f, 0f, -2.6f), 0.7f, -20f);

        // Papan penunjuk ke utara.
        Transform sign = Group("Sign", d, new Vector3(2.2f, 0f, 3.8f)).transform;
        Part(PrimitiveType.Cylinder, "Post", sign, new Vector3(0f, 0.7f, 0f), new Vector3(0.12f, 0.7f, 0.12f), CapyArt.WoodDark);
        Part(PrimitiveType.Cube, "Board", sign, new Vector3(0f, 1.25f, 0f), new Vector3(1.1f, 0.42f, 0.08f), CapyArt.Wood);
        Part(PrimitiveType.Cube, "Arrow", sign, new Vector3(0f, 1.25f, 0.05f), new Vector3(0.5f, 0.1f, 0.03f), CapyArt.Paper);
        Part(PrimitiveType.Cube, "ArrowHead", sign, new Vector3(0.24f, 1.25f, 0.05f), new Vector3(0.18f, 0.18f, 0.03f), CapyArt.Paper, new Vector3(0f, 0f, 45f));

        for (int s = -1; s <= 1; s += 2)
            CapyArt.Brazier(d, new Vector3(5.6f * s, 0f, -3.8f), theme.torch, 0.8f);

        for (int i = 0; i < 4; i++)
        {
            float a = i / 4f * Mathf.PI * 2f + 0.4f;
            Part(PrimitiveType.Cylinder, "LogSeat", d, new Vector3(-3.2f + Mathf.Cos(a) * 2.7f, 0.22f, 0.8f + Mathf.Sin(a) * 2.7f), new Vector3(0.45f, 0.22f, 0.45f), CapyArt.Wood);
        }
        Material[] petals = { CapyArt.MushSpot, CapyArt.Blush, CapyArt.Yuzu };
        for (int i = 0; i < 10; i++)
            CapyArt.Flower(d, new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-4.8f, 4.8f)), petals[i % 3]);
        CapyArt.Mushroom(d, new Vector3(-6f, 0f, 4.2f), 0.9f, false);
        CapyArt.Mushroom(d, new Vector3(-5.3f, 0f, 4.6f), 0.6f, false);
        TorchesNorth(room, size, theme, 3.6f);
    }

    private static void DecorateGarden(Transform room, Vector2 size, Theme theme)
    {
        Transform d = Group("Decor", room, Vector3.zero).transform;
        // Pohon kecil.
        Transform tree = Group("Tree", d, new Vector3(-5.5f, 0f, 3f)).transform;
        Part(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 0.9f, 0f), new Vector3(0.45f, 0.9f, 0.45f), CapyArt.Wood);
        Part(PrimitiveType.Sphere, "LeavesA", tree, new Vector3(0f, 2.4f, 0f), new Vector3(2.4f, 1.8f, 2.4f), CapyArt.BushMat);
        Part(PrimitiveType.Sphere, "LeavesB", tree, new Vector3(0.6f, 2.9f, 0.3f), new Vector3(1.5f, 1.2f, 1.5f), CapyArt.BushDark);
        Part(PrimitiveType.Sphere, "Fruit", tree, new Vector3(-0.7f, 2.3f, 0.9f), Vector3.one * 0.3f, CapyArt.Yuzu);
        Part(PrimitiveType.Sphere, "Fruit2", tree, new Vector3(0.8f, 2.0f, 0.8f), Vector3.one * 0.3f, CapyArt.Yuzu);
        CapyArt.Obstacle(tree.gameObject, new Vector3(0f, 0.8f, 0f), new Vector3(0.9f, 1.6f, 0.9f));

        // Kolam (dekorasi, bisa dilompati slime).
        Part(PrimitiveType.Cylinder, "Pond", d, new Vector3(5f, 0.02f, -2.5f), new Vector3(3.6f, 0.02f, 2.6f), CapyArt.Water);
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * Mathf.PI * 2f;
            Part(PrimitiveType.Sphere, "PondStone", d, new Vector3(5f + Mathf.Cos(a) * 1.9f, 0.1f, -2.5f + Mathf.Sin(a) * 1.4f), new Vector3(0.5f, 0.25f, 0.4f), CapyArt.StoneLight);
        }

        CapyArt.Mushroom(d, new Vector3(-8f, 0f, 6f), 1.4f, true);
        CapyArt.Mushroom(d, new Vector3(-7f, 0f, 6.4f), 0.8f, false);
        CapyArt.Mushroom(d, new Vector3(8f, 0f, 6f), 1.2f, true);
        CapyArt.Mushroom(d, new Vector3(8.2f, 0f, -6f), 1.5f, true);
        CapyArt.Mushroom(d, new Vector3(7.3f, 0f, -6.4f), 0.7f, false);
        CapyArt.Bush(d, new Vector3(-8.3f, 0f, -5.8f), 1.2f, true);
        CapyArt.Bush(d, new Vector3(-8.5f, 0f, 0.5f), 1f, true);
        CapyArt.Bush(d, new Vector3(2.5f, 0f, 6.4f), 0.9f, true);
        CapyArt.Rock(d, new Vector3(1.5f, 0f, 1.5f), 0.9f, true);
        CapyArt.Rock(d, new Vector3(-2f, 0f, -3f), 0.7f, true);
        Material[] petals = { CapyArt.MushSpot, CapyArt.Blush, CapyArt.Yuzu, CapyArt.ClothBlue };
        for (int i = 0; i < 26; i++)
            CapyArt.Flower(d, new Vector3(Random.Range(-9f, 9f), 0f, Random.Range(-7f, 7f)), petals[i % petals.Length]);
        for (int i = 0; i < 5; i++)
            Part(PrimitiveType.Cylinder, "SlimePuddle", d, new Vector3(Random.Range(-7f, 7f), 0.01f, Random.Range(-6f, 6f)), new Vector3(Random.Range(0.6f, 1.2f), 0.01f, Random.Range(0.5f, 1f)), CapyArt.SlimeMat);

        TorchesNorth(room, size, theme, 4.5f);
        CapyArt.Brazier(d, new Vector3(-8.3f, 0f, -3f), theme.torch, 0.9f);
        CapyArt.Brazier(d, new Vector3(8.3f, 0f, 2f), theme.torch, 0.9f);
    }

    private static void DecorateBarracks(Transform room, Vector2 size, Theme theme)
    {
        Transform d = Group("Decor", room, Vector3.zero).transform;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                CapyArt.Pillar(d, new Vector3(4.5f * sx, 0f, 2.6f * sz), theme.wall, theme.wallAlt, 3f);

        CapyArt.Crate(d, new Vector3(-8f, 0f, -5.6f), 1.3f, 8f);
        CapyArt.Crate(d, new Vector3(-6.6f, 0f, -6f), 1f, -15f);
        CapyArt.Crate(d, new Vector3(-8f, 1.3f, -5.6f), 0.8f, 30f);
        CapyArt.Barrel(d, new Vector3(8f, 0f, 5.8f));
        CapyArt.Barrel(d, new Vector3(6.9f, 0f, 6.1f));
        CapyArt.Barrel(d, new Vector3(8.1f, 0f, 4.6f));
        CapyArt.WeaponRack(d, new Vector3(-4.5f, 0f, 7.4f), 0f);
        CapyArt.WeaponRack(d, new Vector3(4.5f, 0f, 7.4f), 0f);
        CapyArt.Dummy(d, new Vector3(6.5f, 0f, -4f));
        CapyArt.Dummy(d, new Vector3(-6.5f, 0f, 3.5f));
        float hz = size.y * 0.5f;
        CapyArt.Banner(room, new Vector3(-7.5f, 2.9f, hz - 0.02f), CapyArt.Cloth);
        CapyArt.Banner(room, new Vector3(7.5f, 2.9f, hz - 0.02f), CapyArt.Cloth);
        CapyArt.Banner(room, new Vector3(-1.6f - 0.9f, 2.9f, hz - 0.02f), CapyArt.ClothBlue);
        CapyArt.Banner(room, new Vector3(2.5f, 2.9f, hz - 0.02f), CapyArt.ClothBlue);
        Part(PrimitiveType.Cube, "Rug", d, new Vector3(0f, 0.012f, 0f), new Vector3(5f, 0.02f, 3.2f), CapyArt.CarpetRed);
        Part(PrimitiveType.Cube, "RugTrim", d, new Vector3(0f, 0.008f, 0f), new Vector3(5.4f, 0.02f, 3.6f), CapyArt.CarpetGold);
        TorchesNorth(room, size, theme, 6f);
        CapyArt.Brazier(d, new Vector3(-8.4f, 0f, 1f), theme.torch, 0.9f);
        CapyArt.Brazier(d, new Vector3(8.4f, 0f, -1f), theme.torch, 0.9f);
    }

    private static void DecorateLibrary(Transform room, Vector2 size, Theme theme)
    {
        Transform d = Group("Decor", room, Vector3.zero).transform;
        float hz = size.y * 0.5f;
        CapyArt.Bookshelf(d, new Vector3(-6f, 0f, hz - 0.45f), 0f, 4.4f);
        CapyArt.Bookshelf(d, new Vector3(6f, 0f, hz - 0.45f), 0f, 4.4f);
        CapyArt.Bookshelf(d, new Vector3(-5.5f, 0f, 0.3f), 90f, 3.6f);
        CapyArt.Bookshelf(d, new Vector3(5.5f, 0f, 0.3f), 90f, 3.6f);
        CapyArt.Desk(d, new Vector3(-7f, 0f, -5f), 15f);
        CapyArt.Desk(d, new Vector3(7f, 0f, -5f), -15f);

        Texture2D rune = AssetDatabase.LoadAssetAtPath<Texture2D>(CapyTextures.TexFolder + "/SpawnRune.png");
        GameObject circle = CapyArt.FxQuad(d, "MagicCircle", CapyArt.Additive, new Vector3(0f, 0.03f, 0.3f), 6f, rune, new Color(0.8f, 0.45f, 1f, 0.55f));
        Spinner sp = circle.AddComponent<Spinner>();
        SetFloat(sp, "speed", 12f);
        SerializedObject so = new SerializedObject(sp);
        so.FindProperty("axis").vector3Value = Vector3.forward;
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f;
            CapyArt.FloatingBook(d, new Vector3(Mathf.Cos(a) * 3.8f, 1.8f + (i % 2) * 0.5f, 0.3f + Mathf.Sin(a) * 3.2f));
        }
        for (int i = 0; i < 6; i++)
            CapyArt.Candle(d, new Vector3(Random.Range(-8.5f, 8.5f), 0f, Random.Range(-6.5f, -3.5f)), i < 2);
        CapyArt.CrystalCluster(d, new Vector3(-8.6f, 0f, 3.5f), 0.9f, CapyArt.CrystalPurple, new Color(0.8f, 0.5f, 1f), true, true);
        CapyArt.CrystalCluster(d, new Vector3(8.6f, 0f, 3.5f), 0.9f, CapyArt.CrystalPurple, new Color(0.8f, 0.5f, 1f), true, true);
        Part(PrimitiveType.Cube, "Rug", d, new Vector3(0f, 0.011f, -4.5f), new Vector3(3f, 0.02f, 2f), CapyArt.ClothBlue);
        TorchesNorth(room, size, theme, 2.8f);
    }

    private static void DecorateCave(Transform room, Vector2 size, Theme theme)
    {
        Transform d = Group("Decor", room, Vector3.zero).transform;
        Vector3[] stalagmites =
        {
            new Vector3(-6f, 0f, 3.5f), new Vector3(5.5f, 0f, 4.5f), new Vector3(-3f, 0f, -3.5f),
            new Vector3(4f, 0f, -2.5f), new Vector3(0.5f, 0f, 2f), new Vector3(-8.5f, 0f, -6f), new Vector3(8.5f, 0f, -6.5f)
        };
        foreach (Vector3 p in stalagmites)
            CapyArt.Stalagmite(d, p, Random.Range(0.8f, 1.2f));
        CapyArt.Rock(d, new Vector3(-9f, 0f, 1f), 1.2f, true);
        CapyArt.Rock(d, new Vector3(9f, 0f, 0f), 1.1f, true);
        CapyArt.Rock(d, new Vector3(2.5f, 0f, -6.5f), 0.8f, true);
        CapyArt.CrystalCluster(d, new Vector3(-9.2f, 0f, 7.2f), 1.3f, CapyArt.CrystalCyan, new Color(0.4f, 0.85f, 1f), true, true);
        CapyArt.CrystalCluster(d, new Vector3(9.2f, 0f, 7.2f), 1.1f, CapyArt.CrystalPurple, new Color(0.8f, 0.5f, 1f), true, true);
        CapyArt.CrystalCluster(d, new Vector3(-9.3f, 0f, -7.5f), 0.9f, CapyArt.CrystalPurple, new Color(0.8f, 0.5f, 1f), true, true);
        CapyArt.CrystalCluster(d, new Vector3(9.3f, 0f, -7.6f), 1f, CapyArt.CrystalCyan, new Color(0.4f, 0.85f, 1f), true, true);
        CapyArt.CrystalCluster(d, new Vector3(-1.5f, 0f, 6.8f), 0.7f, CapyArt.CrystalCyan, new Color(0.4f, 0.85f, 1f), false, false);
        for (int i = 0; i < 6; i++)
            Part(PrimitiveType.Cylinder, "Puddle", d, new Vector3(Random.Range(-8f, 8f), 0.01f, Random.Range(-7f, 7f)), new Vector3(Random.Range(1f, 2f), 0.01f, Random.Range(0.8f, 1.5f)), CapyArt.Lit("CaveWater", new Color(0.1f, 0.24f, 0.36f), 0.95f));
        for (int i = 0; i < 8; i++)
            CapyArt.Mushroom(d, new Vector3(Random.Range(-9f, 9f), 0f, Random.Range(-7.5f, 7.5f)), Random.Range(0.3f, 0.5f), false);
        TorchesNorth(room, size, theme, 5f);
    }

    private static GameObject DecorateThrone(Transform room, Vector2 size, Theme theme, GameDatabase db)
    {
        Transform d = Group("Decor", room, Vector3.zero).transform;
        float hz = size.y * 0.5f;
        Part(PrimitiveType.Cube, "Dais", d, new Vector3(0f, 0.2f, hz - 2.4f), new Vector3(7f, 0.4f, 4.2f), theme.wallAlt);
        Part(PrimitiveType.Cube, "DaisTrim", d, new Vector3(0f, 0.41f, hz - 2.4f), new Vector3(7.1f, 0.04f, 4.3f), CapyArt.Gold);
        CapyArt.Throne(d, new Vector3(0f, 0.4f, hz - 1.8f));
        CapyArt.YuzuPile(d, new Vector3(-2.6f, 0.4f, hz - 1.5f));
        CapyArt.YuzuPile(d, new Vector3(2.6f, 0.4f, hz - 1.5f));
        Part(PrimitiveType.Cube, "Carpet", d, new Vector3(0f, 0.012f, -2f), new Vector3(2.8f, 0.02f, size.y - 4.5f), CapyArt.CarpetRed);
        Part(PrimitiveType.Cube, "CarpetTrim", d, new Vector3(0f, 0.008f, -2f), new Vector3(3.2f, 0.02f, size.y - 4.4f), CapyArt.CarpetGold);

        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int k = -1; k <= 1; k++)
                CapyArt.Pillar(d, new Vector3(10f * sx, 0f, k * 5.5f), theme.wall, theme.wallAlt, 3.6f);
            CapyArt.Brazier(d, new Vector3(4.6f * sx, 0.4f, hz - 1.4f), theme.torch, 1f);
            CapyArt.Brazier(d, new Vector3(10.6f * sx, 0f, -8.6f), theme.torch, 0.9f);
            CapyArt.CrystalCluster(d, new Vector3(10.6f * sx, 0f, 8.6f), 1.3f, CapyArt.CrystalCyan, new Color(0.4f, 0.85f, 1f), true, true);
            CapyArt.Banner(room, new Vector3(7.5f * sx, 3f, hz - 0.02f), CapyArt.Cloth);
        }
        TorchesNorth(room, size, theme, 3.8f);

        GameObject boss = (GameObject)PrefabUtility.InstantiatePrefab(db.golemBoss);
        boss.transform.SetParent(room, false);
        boss.transform.localPosition = new Vector3(0f, 0f, hz - 6.5f);
        boss.transform.Find("Visual").localRotation = Quaternion.Euler(0f, 180f, 0f);
        return boss;
    }

    // ================================================================== lighting, kamera, post

    private static GameObject MakeCamera(float fov, Color background)
    {
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 220f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        camGo.AddComponent<AudioListener>();
        UniversalAdditionalCameraData data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
        return camGo;
    }

    private static void SetupLighting(Color sunColor, float intensity, Quaternion rotation)
    {
        GameObject sunGo = new GameObject("Moonlight");
        Light sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = sunColor;
        sun.intensity = intensity;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.75f;
        sunGo.transform.rotation = rotation;
        RenderSettings.sun = sun;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.46f, 0.46f, 0.6f);
        RenderSettings.ambientEquatorColor = new Color(0.33f, 0.3f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.17f, 0.22f);
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
    }

    public static VolumeProfile LoadOrCreateProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile != null)
            return profile;

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(0.85f);
        bloom.scatter.Override(0.68f);
        AssetDatabase.AddObjectToAsset(bloom, profile);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.3f);
        vignette.smoothness.Override(0.45f);
        AssetDatabase.AddObjectToAsset(vignette, profile);

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.15f);
        color.contrast.Override(12f);
        color.saturation.Override(14f);
        AssetDatabase.AddObjectToAsset(color, profile);

        Tonemapping tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.Neutral);
        AssetDatabase.AddObjectToAsset(tone, profile);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static void MakeVolume()
    {
        GameObject go = new GameObject("PostProcess");
        Volume v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.sharedProfile = LoadOrCreateProfile();
    }

    // ================================================================== menu scene

    public static void BuildMenuScene(GameDatabase db)
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Random.InitState(11);
        Transform world = new GameObject("Diorama").transform;

        Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(CapyTextures.TexFolder + "/Floor_Garden.png");
        GameObject ground = Part(PrimitiveType.Cylinder, "Ground", world, new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.1f, 30f), CapyArt.Lit("Floor_MenuGrass", Color.white, 0.1f, 0f, grassTex, new Vector2(6f, 6f)));
        Part(PrimitiveType.Cylinder, "Cliff", world, new Vector3(0f, -1.2f, 0f), new Vector3(30.4f, 1.1f, 30.4f), CapyArt.StoneDark);

        // Onsen dengan Capy berendam.
        GameObject spring = CapyArt.HotSpringVisual(world, new Vector3(0f, 0f, 0f), 2.4f);
        HotSpring hs = spring.AddComponent<HotSpring>();
        SetBool(hs, "decorative", true);
        Transform capyRoot = Group("CapyBathing", world, new Vector3(0f, -0.55f, 0.2f)).transform;
        capyRoot.localRotation = Quaternion.Euler(0f, 200f, 0f);
        Transform capy = CapyArt.Capybara(capyRoot);
        Object.DestroyImmediate(capy.Find("Body/SwordPivot").gameObject);
        Bobbing capyBob = capyRoot.gameObject.AddComponent<Bobbing>();
        SetFloat(capyBob, "amplitude", 0.05f);
        SetFloat(capyBob, "speed", 1.2f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 1.6f;
            GameObject y = Part(PrimitiveType.Sphere, "FloatingYuzu", world, new Vector3(Mathf.Cos(a) * 1.4f, 0.2f, Mathf.Sin(a) * 1.3f), Vector3.one * 0.34f, CapyArt.Yuzu);
            Bobbing bob = y.AddComponent<Bobbing>();
            SetFloat(bob, "amplitude", 0.04f);
            SetFloat(bob, "speed", 1.8f);
            SetFloat(bob, "spinSpeed", 12f);
        }
        CapyArt.PointLight(world, new Vector3(0f, 2f, 0f), new Color(0.5f, 0.85f, 1f), 7f, 2.5f);

        // Barisan karakter di belakang (seperti gambar konsep).
        Transform lineup = Group("Lineup", world, new Vector3(0f, 0f, 6.5f)).transform;
        AddMenuCritter(CapyArt.Slime(Group("Slime", lineup, new Vector3(1.2f, 0f, -1.8f)).transform, 0.9f), 1.3f);
        AddMenuCritter(CapyArt.Slime(Group("Slimelet", lineup, new Vector3(-0.2f, 0f, -2.9f)).transform, 0.5f), 0.9f);
        Transform skel = CapyArt.SkeletonWarrior(Group("Skeleton", lineup, new Vector3(-1.4f, 0f, 0.4f)).transform);
        skel.localRotation = Quaternion.Euler(0f, 160f, 0f);
        Transform wiz = CapyArt.Wizard(Group("Wizard", lineup, new Vector3(-3.6f, 0f, -0.6f)).transform);
        wiz.localRotation = Quaternion.Euler(0f, 150f, 0f);
        Transform bat = CapyArt.Bat(Group("Bat", lineup, new Vector3(3.2f, 2.6f, -0.8f)).transform);
        bat.localRotation = Quaternion.Euler(0f, 200f, 0f);
        Bobbing batBob = bat.parent.gameObject.AddComponent<Bobbing>();
        SetFloat(batBob, "amplitude", 0.3f);
        SetFloat(batBob, "speed", 2f);
        Renderer[] glow;
        Light core;
        Transform golem = CapyArt.Golem(Group("Golem", lineup, new Vector3(5.6f, 0f, 3.2f)).transform, out glow, out core);
        golem.localRotation = Quaternion.Euler(0f, 200f, 0f);

        // Lingkungan kemah.
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f + 0.3f;
            Vector3 p = new Vector3(Mathf.Cos(a) * 9.5f, 0f, Mathf.Sin(a) * 9.5f);
            if (i % 2 == 0)
                CapyArt.Brazier(world, p, new Color(1f, 0.7f, 0.4f), 1f);
            else
                CapyArt.Bush(world, p, 1.3f, false);
        }
        for (int i = 0; i < 9; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(4f, 12f);
            CapyArt.Mushroom(world, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), Random.Range(0.5f, 1.2f), false);
        }
        Material[] petals = { CapyArt.MushSpot, CapyArt.Blush, CapyArt.Yuzu, CapyArt.ClothBlue };
        for (int i = 0; i < 30; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(3.2f, 13f);
            CapyArt.Flower(world, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), petals[i % petals.Length]);
        }
        CapyArt.CrystalCluster(world, new Vector3(9f, 0f, 8f), 1.5f, CapyArt.CrystalCyan, new Color(0.4f, 0.85f, 1f), true, false);
        CapyArt.CrystalCluster(world, new Vector3(-10f, 0f, 6f), 1.2f, CapyArt.CrystalPurple, new Color(0.8f, 0.5f, 1f), true, false);

        MakeFireflies(world);

        // Kamera bergoyang pelan di sekitar diorama, objek berada di sisi kanan layar.
        Transform pivot = Group("CameraPivot", null, new Vector3(0f, 0f, 1f)).transform;
        GameObject camGo = MakeCamera(38f, new Color(0.04f, 0.035f, 0.08f));
        camGo.transform.SetParent(pivot, false);
        camGo.transform.localPosition = new Vector3(-1.5f, 5.2f, -13f);
        camGo.transform.LookAt(pivot.position + new Vector3(-5.2f, 1.4f, 0f));

        SetupLighting(new Color(0.7f, 0.75f, 1f), 0.9f, Quaternion.Euler(40f, 150f, 0f));
        MakeVolume();

        GameObject controller = new GameObject("MainMenu");
        MainMenuController mm = controller.AddComponent<MainMenuController>();
        SetRef(mm, "cameraPivot", pivot);

        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    private static void AddMenuCritter(Transform visual, float hop)
    {
        MenuCritter c = visual.parent.gameObject.AddComponent<MenuCritter>();
        SetRef(c, "body", visual.Find("Body"));
        SetFloat(c, "hopInterval", hop);
    }

    private static void MakeFireflies(Transform parent)
    {
        GameObject go = new GameObject("Fireflies");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.5f), new Color(0.6f, 1f, 0.6f));
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 14f;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(22f, 3f, 22f);
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 0.3f;
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = CapyArt.Fx("FX_Firefly", GameDatabase.I != null ? GameDatabase.I.softCircle : null, new Color(2f, 1.8f, 0.9f, 1f), true, false, 30);
    }
}
