using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static CapyEditorUtil;

// Membuat semua prefab (Player, musuh, proyektil, pickup) dan mengisi GameDatabase.
public static class CapyPrefabBuilder
{
    public const string PrefabFolder = "Assets/Prefabs";
    public const string DatabasePath = "Assets/Resources/GameDatabase.asset";

    private static PhysicsMaterial frictionless;

    public static GameDatabase LoadOrCreateDatabase()
    {
        EnsureFolder("Assets/Resources");
        GameDatabase db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<GameDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        return db;
    }

    public static void BuildPrefabs(GameDatabase db)
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(PrefabFolder + "/Enemies");
        EnsureFolder(PrefabFolder + "/Projectiles");
        EnsureFolder(PrefabFolder + "/Pickups");

        string pmPath = "Assets/Art/Frictionless.asset";
        frictionless = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pmPath);
        if (frictionless == null)
        {
            frictionless = new PhysicsMaterial("Frictionless");
            frictionless.dynamicFriction = 0f;
            frictionless.staticFriction = 0f;
            frictionless.bounciness = 0f;
            frictionless.frictionCombine = PhysicsMaterialCombine.Minimum;
            frictionless.bounceCombine = PhysicsMaterialCombine.Minimum;
            AssetDatabase.CreateAsset(frictionless, pmPath);
        }

        db.swordWave = SwordWave(db);
        db.magicOrb = MagicOrb(db);
        db.arrow = Arrow();
        db.rock = Rock();
        db.orbitYuzu = OrbitYuzuPrefab();

        db.xpGem = XPGemPrefab();
        db.yuzuHeal = YuzuHealPrefab();
        db.chest = ChestPrefab();
        db.hotSpring = HotSpringPrefab();
        db.goldenYuzu = GoldenYuzuPrefab();

        db.slimeBig = SlimePrefab("SlimeBig", true);
        db.slimeSmall = SlimePrefab("SlimeSmall", false);
        db.skeletonWarrior = SkeletonWarriorPrefab();
        db.skeletonArcher = SkeletonArcherPrefab();
        db.wizard = WizardPrefab();
        db.bat = BatPrefab();
        db.golemBoss = GolemPrefab();

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
    }

    // ================================================================== player

    public static GameObject PlayerPrefab()
    {
        GameObject root = new GameObject("Player");
        root.layer = Layers.Player;
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.radius = 0.45f;
        col.height = 1.6f;
        col.center = new Vector3(0f, 0.8f, 0f);
        col.sharedMaterial = frictionless;

        Transform visual = CapyArt.Capybara(root.transform);
        CapyArt.BlobShadow(root.transform, 1.5f, false);

        root.AddComponent<PlayerController>();
        root.AddComponent<PlayerHealth>();
        root.AddComponent<PlayerCombat>();
        PlayerVisual pv = root.AddComponent<PlayerVisual>();
        root.AddComponent<OrbitYuzu>();

        Transform body = visual.Find("Body");
        SetRef(pv, "visualRoot", visual);
        SetRef(pv, "body", body);
        SetRef(pv, "swordPivot", body.Find("SwordPivot"));
        SetRef(pv, "footL", body.Find("FootL"));
        SetRef(pv, "footR", body.Find("FootR"));
        SetRef(pv, "scarfTail", body.Find("ScarfTail"));
        SetRef(pv, "yuzu", body.Find("Yuzu"));
        return SavePrefab(root, PrefabFolder + "/Player.prefab");
    }

    // ================================================================== musuh

    private static GameObject EnemyRoot(string name, float radius, float height, bool flying, float mass)
    {
        GameObject root = new GameObject(name);
        root.layer = flying ? Layers.Flying : Layers.Enemy;
        if (!flying)
        {
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.radius = radius;
            col.height = Mathf.Max(height, radius * 2f);
            col.center = new Vector3(0f, col.height * 0.5f, 0f);
            col.sharedMaterial = frictionless;
        }
        return root;
    }

    private static void Stats(EnemyBase e, string displayName, float hp, float speed, float contact, int xp, float radius, float kb, float barHeight, Color blood, Transform visual, float yuzu = 0.05f)
    {
        SetString(e, "displayName", displayName);
        SetFloat(e, "maxHealth", hp);
        SetFloat(e, "moveSpeed", speed);
        SetFloat(e, "contactDamage", contact);
        SetInt(e, "xpValue", xp);
        SetFloat(e, "radius", radius);
        SetFloat(e, "knockbackResist", kb);
        SetFloat(e, "healthBarHeight", barHeight);
        SetColor(e, "bloodColor", blood);
        SetFloat(e, "yuzuDropChance", yuzu);
        SetRef(e, "visualRoot", visual);
    }

    private static GameObject SlimePrefab(string name, bool big)
    {
        float scale = big ? 1f : 0.55f;
        GameObject root = EnemyRoot(name, big ? 0.6f : 0.35f, big ? 1.1f : 0.6f, false, big ? 2f : 1f);
        Transform visual = CapyArt.Slime(root.transform, scale);
        CapyArt.BlobShadow(root.transform, big ? 1.6f : 0.9f, false);
        SlimeAI ai = root.AddComponent<SlimeAI>();
        Stats(ai, big ? "Slime" : "Slimelet", big ? 34f : 14f, big ? 4.2f : 4.8f, big ? 10f : 7f, big ? 2 : 1, big ? 0.6f : 0.35f, big ? 0.2f : 0f, big ? 1.45f : 0.9f, new Color(0.45f, 0.85f, 0.4f), visual, big ? 0.06f : 0.03f);
        SetBool(ai, "isBig", big);
        SetRef(ai, "body", visual.Find("Body"));
        SetFloat(ai, "hopInterval", big ? 0.85f : 0.6f);
        SetFloat(ai, "hopHeight", big ? 0.9f : 0.6f);
        SetFloat(ai, "hopDuration", big ? 0.5f : 0.38f);
        return SavePrefab(root, PrefabFolder + "/Enemies/" + name + ".prefab");
    }

    private static GameObject SkeletonWarriorPrefab()
    {
        GameObject root = EnemyRoot("SkeletonWarrior", 0.45f, 1.9f, false, 3f);
        Transform visual = CapyArt.SkeletonWarrior(root.transform);
        CapyArt.BlobShadow(root.transform, 1.2f, false);
        SkeletonAI ai = root.AddComponent<SkeletonAI>();
        Stats(ai, "Skeleton", 58f, 3.1f, 8f, 3, 0.45f, 0.3f, 2.35f, new Color(0.92f, 0.88f, 0.8f), visual);
        Transform body = visual.Find("Body");
        SetRef(ai, "body", body);
        SetRef(ai, "swordPivot", body.Find("SwordPivot"));
        SetRef(ai, "legL", body.Find("LegL"));
        SetRef(ai, "legR", body.Find("LegR"));
        return SavePrefab(root, PrefabFolder + "/Enemies/SkeletonWarrior.prefab");
    }

    private static GameObject SkeletonArcherPrefab()
    {
        GameObject root = EnemyRoot("SkeletonArcher", 0.42f, 1.9f, false, 2.5f);
        Transform visual = CapyArt.SkeletonArcher(root.transform);
        CapyArt.BlobShadow(root.transform, 1.15f, false);
        SkeletonArcherAI ai = root.AddComponent<SkeletonArcherAI>();
        Stats(ai, "Skeleton Archer", 40f, 3.3f, 6f, 3, 0.42f, 0.2f, 2.3f, new Color(0.92f, 0.88f, 0.8f), visual);
        Transform body = visual.Find("Body");
        SetRef(ai, "body", body);
        SetRef(ai, "bowPivot", body.Find("BowPivot"));
        return SavePrefab(root, PrefabFolder + "/Enemies/SkeletonArcher.prefab");
    }

    private static GameObject WizardPrefab()
    {
        GameObject root = EnemyRoot("Wizard", 0.55f, 2.2f, false, 2f);
        Transform visual = CapyArt.Wizard(root.transform);
        CapyArt.BlobShadow(root.transform, 1.5f, false);
        WizardAI ai = root.AddComponent<WizardAI>();
        Stats(ai, "Wizard", 70f, 2.6f, 8f, 5, 0.55f, 0.3f, 2.75f, new Color(0.65f, 0.4f, 0.9f), visual, 0.12f);
        Transform body = visual.Find("Body");
        SetRef(ai, "body", body);
        SetRef(ai, "staffOrb", body.Find("Staff/Orb"));
        return SavePrefab(root, PrefabFolder + "/Enemies/Wizard.prefab");
    }

    private static GameObject BatPrefab()
    {
        GameObject root = EnemyRoot("EyeBat", 0.5f, 1f, true, 1f);
        Transform visual = CapyArt.Bat(root.transform);
        CapyArt.BlobShadow(root.transform, 1f, true);
        BatAI ai = root.AddComponent<BatAI>();
        Stats(ai, "Eye Bat", 20f, 5.5f, 9f, 1, 0.5f, 0f, 0.95f, new Color(0.6f, 0.38f, 0.75f), visual, 0.04f);
        Transform body = visual.Find("Body");
        SetRef(ai, "body", body);
        SetRef(ai, "wingL", body.Find("WingL"));
        SetRef(ai, "wingR", body.Find("WingR"));
        return SavePrefab(root, PrefabFolder + "/Enemies/EyeBat.prefab");
    }

    private static GameObject GolemPrefab()
    {
        GameObject root = EnemyRoot("GolemKing", 1.5f, 5f, false, 60f);
        Renderer[] glow;
        Light coreLight;
        Transform visual = CapyArt.Golem(root.transform, out glow, out coreLight);
        CapyArt.BlobShadow(root.transform, 4.2f, false);
        GolemBossAI ai = root.AddComponent<GolemBossAI>();
        Stats(ai, "Golem King", 3000f, 2.2f, 15f, 0, 1.5f, 1f, 5.8f, new Color(0.6f, 0.62f, 0.68f), visual, 0f);
        Transform body = visual.Find("Body");
        SetRef(ai, "body", body);
        SetRef(ai, "armL", body.Find("ArmL"));
        SetRef(ai, "armR", body.Find("ArmR"));
        SetRef(ai, "legL", body.Find("LegL"));
        SetRef(ai, "legR", body.Find("LegR"));
        SetRef(ai, "head", body.Find("Head"));
        SetRefArray(ai, "glowRenderers", glow);
        SetRef(ai, "enragedMaterial", CapyArt.CrystalRed);
        SetRef(ai, "dormantMaterial", CapyArt.CrystalDim);
        SetRef(ai, "awakeMaterial", CapyArt.CrystalCyan);
        SetRef(ai, "coreLight", coreLight);
        return SavePrefab(root, PrefabFolder + "/Enemies/GolemKing.prefab");
    }

    // ================================================================== proyektil

    private static Projectile ProjectileRoot(GameObject root, float radius, float lifetime, float knockback, Color impact)
    {
        Projectile p = root.AddComponent<Projectile>();
        SetFloat(p, "radius", radius);
        SetFloat(p, "lifetime", lifetime);
        SetFloat(p, "knockback", knockback);
        SetColor(p, "impactColor", impact);
        return p;
    }

    private static GameObject SwordWave(GameDatabase db)
    {
        GameObject root = new GameObject("SwordWave");
        Transform visual = Group("Visual", root.transform, Vector3.zero).transform;
        Material glow = CapyArt.Fx("FX_SwordWave", db.softCircle, new Color(1.4f, 2.4f, 3.2f, 1f), true, false, 20);
        Material core = CapyArt.Fx("FX_SwordWaveCore", db.softCircle, new Color(3f, 3f, 3f, 1f), true, false, 21);
        GameObject outer = new GameObject("Glow", typeof(MeshFilter), typeof(MeshRenderer));
        outer.transform.SetParent(visual, false);
        outer.transform.localScale = new Vector3(1.05f, 1f, 1.05f);
        outer.GetComponent<MeshFilter>().sharedMesh = CapyArt.Crescent;
        outer.GetComponent<MeshRenderer>().sharedMaterial = glow;
        GameObject inner = new GameObject("Core", typeof(MeshFilter), typeof(MeshRenderer));
        inner.transform.SetParent(visual, false);
        inner.transform.localPosition = new Vector3(0f, 0.01f, 0.04f);
        inner.transform.localScale = new Vector3(0.8f, 1f, 0.85f);
        inner.GetComponent<MeshFilter>().sharedMesh = CapyArt.Crescent;
        inner.GetComponent<MeshRenderer>().sharedMaterial = core;
        NoShadows(root);
        ProjectileRoot(root, 0.55f, 1.05f, 3.5f, new Color(0.7f, 0.95f, 1f));
        return SavePrefab(root, PrefabFolder + "/Projectiles/SwordWave.prefab");
    }

    private static GameObject MagicOrb(GameDatabase db)
    {
        GameObject root = new GameObject("MagicOrb");
        Transform visual = Group("Visual", root.transform, Vector3.zero).transform;
        Part(PrimitiveType.Sphere, "Core", visual, Vector3.zero, Vector3.one * 0.34f, CapyArt.OrbGlow);
        GameObject halo = CapyArt.FxQuad(visual, "Halo", CapyArt.Additive, Vector3.zero, 1.1f, db.softCircle, new Color(1f, 0.45f, 1f, 0.7f));
        halo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        TrailRenderer tr = root.AddComponent<TrailRenderer>();
        tr.time = 0.25f;
        tr.widthMultiplier = 0.32f;
        tr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        tr.sharedMaterial = CapyArt.Fx("FX_OrbTrail", db.softCircle, new Color(1.8f, 0.7f, 2.2f, 0.8f), true, false, 15);
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.minVertexDistance = 0.08f;
        NoShadows(root);
        Projectile p = ProjectileRoot(root, 0.3f, 3.5f, 0f, new Color(0.9f, 0.5f, 1f));
        SetFloat(p, "homingTime", 0.6f);
        SetFloat(p, "homingTurnRate", 70f);
        return SavePrefab(root, PrefabFolder + "/Projectiles/MagicOrb.prefab");
    }

    private static GameObject Arrow()
    {
        GameObject root = new GameObject("Arrow");
        Transform visual = Group("Visual", root.transform, Vector3.zero).transform;
        Part(PrimitiveType.Cylinder, "Shaft", visual, Vector3.zero, new Vector3(0.05f, 0.4f, 0.05f), CapyArt.Wood, new Vector3(90f, 0f, 0f));
        Part(PrimitiveType.Cube, "Tip", visual, new Vector3(0f, 0f, 0.42f), new Vector3(0.11f, 0.11f, 0.11f), CapyArt.Steel, new Vector3(0f, 45f, 0f));
        Part(PrimitiveType.Cube, "FletchA", visual, new Vector3(0f, 0f, -0.34f), new Vector3(0.02f, 0.16f, 0.16f), CapyArt.Cloth);
        Part(PrimitiveType.Cube, "FletchB", visual, new Vector3(0f, 0f, -0.34f), new Vector3(0.16f, 0.02f, 0.16f), CapyArt.Cloth);
        TrailRenderer tr = root.AddComponent<TrailRenderer>();
        tr.time = 0.12f;
        tr.widthMultiplier = 0.12f;
        tr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        tr.sharedMaterial = CapyArt.Fx("FX_ArrowTrail", null, new Color(1f, 0.9f, 0.8f, 0.5f), true, false, 14);
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ProjectileRoot(root, 0.22f, 2.5f, 2f, new Color(1f, 0.9f, 0.7f));
        return SavePrefab(root, PrefabFolder + "/Projectiles/Arrow.prefab");
    }

    private static GameObject Rock()
    {
        GameObject root = new GameObject("Rock");
        Transform spinner = Group("Spinner", root.transform, Vector3.zero).transform;
        Part(PrimitiveType.Sphere, "A", spinner, Vector3.zero, new Vector3(0.9f, 0.7f, 0.8f), CapyArt.GolemStone);
        Part(PrimitiveType.Cube, "B", spinner, new Vector3(0.2f, 0.15f, 0f), new Vector3(0.55f, 0.5f, 0.5f), CapyArt.GolemDark, new Vector3(20f, 30f, 10f));
        Part(PrimitiveType.Cube, "Moss", spinner, new Vector3(0f, 0.33f, 0f), new Vector3(0.45f, 0.1f, 0.4f), CapyArt.Moss);
        CapyArt.BlobShadow(root.transform, 1.3f, true);
        LobProjectile lob = root.AddComponent<LobProjectile>();
        SetRef(lob, "spinner", spinner);
        return SavePrefab(root, PrefabFolder + "/Projectiles/Rock.prefab");
    }

    private static GameObject OrbitYuzuPrefab()
    {
        GameObject root = new GameObject("OrbitYuzu");
        Part(PrimitiveType.Sphere, "Fruit", root.transform, Vector3.zero, Vector3.one * 0.46f, CapyArt.Yuzu);
        Part(PrimitiveType.Sphere, "Leaf", root.transform, new Vector3(0.1f, 0.24f, 0f), new Vector3(0.22f, 0.05f, 0.12f), CapyArt.Leaf, new Vector3(0f, 0f, 20f));
        GameObject glow = Part(PrimitiveType.Sphere, "Glow", root.transform, Vector3.zero, Vector3.one * 0.6f, CapyArt.Fx("FX_YuzuGlow", null, new Color(1f, 0.6f, 0.15f, 0.25f), true, false, 6));
        glow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return SavePrefab(root, PrefabFolder + "/Projectiles/OrbitYuzu.prefab");
    }

    // ================================================================== pickup

    private static GameObject XPGemPrefab()
    {
        GameObject root = new GameObject("XPGem");
        Transform visual = Group("Visual", root.transform, Vector3.zero).transform;
        GameObject gem = new GameObject("Gem", typeof(MeshFilter), typeof(MeshRenderer));
        gem.transform.SetParent(visual, false);
        gem.transform.localScale = new Vector3(0.22f, 0.3f, 0.22f);
        gem.GetComponent<MeshFilter>().sharedMesh = CapyArt.Octahedron;
        gem.GetComponent<MeshRenderer>().sharedMaterial = CapyArt.Glow("XPGem", new Color(0.45f, 1.8f, 2.6f));
        NoShadows(root);
        XPGem x = root.AddComponent<XPGem>();
        SetRef(x, "visual", visual);
        return SavePrefab(root, PrefabFolder + "/Pickups/XPGem.prefab");
    }

    private static GameObject YuzuHealPrefab()
    {
        GameObject root = new GameObject("YuzuHeal");
        Transform visual = Group("Visual", root.transform, Vector3.zero).transform;
        Part(PrimitiveType.Sphere, "Fruit", visual, Vector3.zero, Vector3.one * 0.45f, CapyArt.Yuzu);
        Part(PrimitiveType.Sphere, "Leaf", visual, new Vector3(0.1f, 0.24f, 0f), new Vector3(0.24f, 0.05f, 0.12f), CapyArt.Leaf, new Vector3(0f, 0f, 20f));
        Part(PrimitiveType.Cylinder, "Stem", visual, new Vector3(0f, 0.24f, 0f), new Vector3(0.04f, 0.04f, 0.04f), CapyArt.WoodDark);
        GameObject ring = CapyArt.FxQuad(root.transform, "HealRing", CapyArt.Additive, new Vector3(0f, -0.4f, 0f), 1.2f, GameDatabase.I != null ? GameDatabase.I.ringTexture : null, new Color(0.4f, 1f, 0.5f, 0.7f));
        ring.AddComponent<Spinner>();
        YuzuPickup y = root.AddComponent<YuzuPickup>();
        SetRef(y, "visual", visual);
        return SavePrefab(root, PrefabFolder + "/Pickups/YuzuHeal.prefab");
    }

    private static GameObject ChestPrefab()
    {
        GameObject root = new GameObject("TreasureChest");
        Part(PrimitiveType.Cube, "Base", root.transform, new Vector3(0f, 0.32f, 0f), new Vector3(1.1f, 0.64f, 0.75f), CapyArt.ChestWood);
        for (int s = -1; s <= 1; s += 2)
            Part(PrimitiveType.Cube, "Band", root.transform, new Vector3(0.45f * s, 0.32f, 0f), new Vector3(0.08f, 0.66f, 0.78f), CapyArt.Gold);
        Part(PrimitiveType.Cube, "Lock", root.transform, new Vector3(0f, 0.55f, 0.39f), new Vector3(0.18f, 0.22f, 0.06f), CapyArt.Gold);
        Transform lid = Group("Lid", root.transform, new Vector3(0f, 0.64f, -0.375f)).transform;
        Part(PrimitiveType.Cube, "LidBox", lid, new Vector3(0f, 0.14f, 0.375f), new Vector3(1.12f, 0.28f, 0.77f), CapyArt.ChestWood);
        for (int s = -1; s <= 1; s += 2)
            Part(PrimitiveType.Cube, "LidBand", lid, new Vector3(0.45f * s, 0.14f, 0.375f), new Vector3(0.08f, 0.3f, 0.8f), CapyArt.Gold);
        Part(PrimitiveType.Cube, "Treasure", root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 0.08f, 0.6f), CapyArt.GoldGlow);
        Light l = CapyArt.PointLight(root.transform, new Vector3(0f, 1.2f, 0.4f), new Color(1f, 0.82f, 0.4f), 5f, 2.5f);
        CapyArt.BlobShadow(root.transform, 1.6f, false);
        TreasureChest c = root.AddComponent<TreasureChest>();
        SetRef(c, "lid", lid);
        SetRef(c, "glow", l);
        return SavePrefab(root, PrefabFolder + "/Pickups/TreasureChest.prefab");
    }

    private static GameObject HotSpringPrefab()
    {
        GameObject root = new GameObject("HotSpringReward");
        CapyArt.HotSpringVisual(root.transform, Vector3.zero, 1.6f);
        CapyArt.PointLight(root.transform, new Vector3(0f, 1.5f, 0f), new Color(0.5f, 0.85f, 1f), 6f, 2f);
        HotSpring h = root.AddComponent<HotSpring>();
        SetFloat(h, "healPercent", 1f);
        SetFloat(h, "radius", 1.7f);
        return SavePrefab(root, PrefabFolder + "/Pickups/HotSpringReward.prefab");
    }

    private static GameObject GoldenYuzuPrefab()
    {
        GameObject root = new GameObject("GoldenYuzu");
        Part(PrimitiveType.Sphere, "Fruit", root.transform, Vector3.zero, Vector3.one * 0.8f, CapyArt.GoldGlow);
        Part(PrimitiveType.Sphere, "Leaf", root.transform, new Vector3(0.16f, 0.42f, 0f), new Vector3(0.38f, 0.07f, 0.2f), CapyArt.Leaf, new Vector3(0f, 0f, 20f));
        Part(PrimitiveType.Cylinder, "Stem", root.transform, new Vector3(0f, 0.43f, 0f), new Vector3(0.07f, 0.06f, 0.07f), CapyArt.WoodDark);
        for (int i = 0; i < 5; i++)
        {
            float a = i / 5f * Mathf.PI * 2f;
            Part(PrimitiveType.Cube, "Crown", root.transform, new Vector3(Mathf.Cos(a) * 0.22f, 0.5f, Mathf.Sin(a) * 0.22f), new Vector3(0.1f, 0.18f, 0.1f), CapyArt.Gold, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
        }
        CapyArt.PointLight(root.transform, Vector3.zero, new Color(1f, 0.8f, 0.35f), 7f, 4f);
        NoShadows(root);
        root.AddComponent<GoldenYuzu>();
        return SavePrefab(root, PrefabFolder + "/Pickups/GoldenYuzu.prefab");
    }
}
