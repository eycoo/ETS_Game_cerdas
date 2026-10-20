using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Menu "Capy Conquest" di editor: membangun ulang seluruh aset, prefab, scene, dan build Windows.
public static class CapyMenu
{
    public const string BuildPath = "Build/CapyConquest/CapyConquest.exe";

    [MenuItem("Capy Conquest/1. Build All (art + audio + prefabs + scenes)")]
    public static void BuildAll()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Capy Conquest", "Generating art...", 0.1f);
            GameDatabase db = GenerateArt();
            EditorUtility.DisplayProgressBar("Capy Conquest", "Generating audio...", 0.4f);
            GenerateAudio(db);
            EditorUtility.DisplayProgressBar("Capy Conquest", "Building prefabs and scenes...", 0.7f);
            BuildContent(db);
            ConfigureProject(db);
            Debug.Log("[CapyBuilder] Build All selesai.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("Capy Conquest/2. Rebuild Prefabs + Scenes only")]
    public static void RebuildContent()
    {
        GameDatabase db = CapyPrefabBuilder.LoadOrCreateDatabase();
        Texture2D stone = AssetDatabase.LoadAssetAtPath<Texture2D>(CapyTextures.TexFolder + "/StoneBlock.png");
        CapyArt.Init(stone, db.softCircle);
        AssignMaterials(db);
        BuildContent(db);
        ConfigureProject(db);
        Debug.Log("[CapyBuilder] Prefab dan scene dibangun ulang.");
    }

    [MenuItem("Capy Conquest/3. Build Windows Player")]
    public static void BuildWindows()
    {
        BuildPlayerOptions opts = new BuildPlayerOptions();
        opts.scenes = new[] { CapySceneBuilder.MenuScenePath, CapySceneBuilder.GameScenePath };
        opts.locationPathName = BuildPath;
        opts.target = BuildTarget.StandaloneWindows64;
        opts.targetGroup = BuildTargetGroup.Standalone;
        opts.options = BuildOptions.None;
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[CapyBuilder] Build result: " + report.summary.result + ", size " + (report.summary.totalSize / (1024 * 1024)) + " MB, errors " + report.summary.totalErrors);
    }

    public static GameDatabase GenerateArt()
    {
        GameDatabase db = CapyPrefabBuilder.LoadOrCreateDatabase();

        db.softCircle = CapyTextures.SoftCircle();
        db.ringTexture = CapyTextures.RingTex();
        db.sparkTexture = CapyTextures.Spark();
        db.starTexture = CapyTextures.Star();
        db.telegraphCircle = CapyTextures.TelegraphCircle();
        db.telegraphRect = CapyTextures.TelegraphRect();
        db.spawnRune = CapyTextures.SpawnRune();

        string[] floors = { "Camp", "Garden", "Barracks", "Library", "Cave", "Throne", "Corridor", "Outside" };
        foreach (string f in floors)
            CapyTextures.Floor(f);
        Texture2D stone = CapyTextures.StoneBlock();

        db.rounded = CapyTextures.Rounded("Rounded", 96, 30f, 34);
        db.roundedSmall = CapyTextures.Rounded("RoundedSmall", 32, 8f, 10);
        db.circle = CapyTextures.CircleSprite();
        db.ring = CapyTextures.RingSprite();
        db.glow = CapyTextures.GlowSprite();
        db.vignette = CapyTextures.VignetteSprite();
        db.heart = CapyTextures.HeartSprite();
        db.capyFace = CapyTextures.CapyFace();
        db.dashIcon = CapyTextures.DashIcon();
        db.skullIcon = CapyTextures.SkullIcon();
        db.clockIcon = CapyTextures.ClockIcon();
        db.gemIcon = CapyTextures.GemIcon();
        db.upgradeIcons = CapyTextures.UpgradeIcons();

        db.titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/LilitaOne-Regular.ttf");
        db.bodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/VarelaRound-Regular.ttf");

        CapyArt.Init(stone, db.softCircle);
        AssignMaterials(db);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        return db;
    }

    private static void AssignMaterials(GameDatabase db)
    {
        db.flashMaterial = CapyArt.Flash;
        db.decalMaterial = CapyArt.Decal;
        db.additiveMaterial = CapyArt.Additive;
        db.lineMaterial = CapyArt.Line;

        Shader particle = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
        if (particle == null)
            particle = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        string debrisPath = CapyArt.MatFolder + "/FX_Debris.mat";
        Material debris = AssetDatabase.LoadAssetAtPath<Material>(debrisPath);
        if (debris == null)
        {
            debris = new Material(particle);
            AssetDatabase.CreateAsset(debris, debrisPath);
        }
        debris.shader = particle;
        debris.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(debris);
        db.debrisMaterial = debris;

        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        db.cubeMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);
        EditorUtility.SetDirty(db);
    }

    public static void GenerateAudio(GameDatabase db)
    {
        List<AudioClip> sfx = CapyAudioGen.GenerateSfx();
        db.sfx = sfx.ToArray();
        AudioClip[] music = CapyAudioGen.GenerateMusic();
        db.musicMenu = music[0];
        db.musicDungeon = music[1];
        db.musicBoss = music[2];
        db.musicVictory = music[3];
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
    }

    private static void BuildContent(GameDatabase db)
    {
        CapyPrefabBuilder.BuildPrefabs(db);
        GameObject player = CapyPrefabBuilder.PlayerPrefab();
        AssetDatabase.SaveAssets();
        CapySceneBuilder.BuildGameScene(db, player);
        CapySceneBuilder.BuildMenuScene(db);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureProject(GameDatabase db)
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(CapySceneBuilder.MenuScenePath, true),
            new EditorBuildSettingsScene(CapySceneBuilder.GameScenePath, true)
        };

        PlayerSettings.productName = "Capy Conquest";
        PlayerSettings.companyName = "Kelompok Capy ITS";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.visibleInBackground = true;

        if (db.capyFace != null)
        {
            Texture2D icon = db.capyFace.texture;
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }
        AssetDatabase.SaveAssets();
    }
}
