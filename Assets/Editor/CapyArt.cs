using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static CapyEditorUtil;

// Palet material dan model 3D (dari primitif Unity) untuk semua karakter dan properti level.
// Gaya: mainan plastik lembut seperti gambar konsep di folder ETS.
public static class CapyArt
{
    public const string MatFolder = "Assets/Art/Materials";
    public const string MeshFolder = "Assets/Art/Meshes";

    private static Shader lit;
    private static Shader unlit;
    private static Shader fx;
    private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    // ---- palet
    public static Material Capy, CapyDark, CapyBelly, Nose, Black, White, Blush, Yuzu, Leaf, Scarf, ScarfDark;
    public static Material Steel, DarkSteel, Gold, Wood, WoodDark, Bone, Helmet, Iron, RedEye, Leather, HoodGreen;
    public static Material SlimeMat, SlimeDark, Robe, RobeDark, VoidMat, WizEye, OrbGlow;
    public static Material BatLid, BatWing, EyeWhite, Iris;
    public static Material GolemStone, GolemDark, Moss, CrystalCyan, CrystalRed, CrystalDim, CrystalPurple;
    public static Material Stone, StoneDark, StoneLight, MushRed, MushSpot, BushMat, BushDark, Water;
    public static Material CarpetRed, CarpetGold, CandleWax, Flame, FlameRed, ChestWood, GoldGlow, GreenGlow, Straw, Cloth, ClothBlue, Paper;
    public static Material[] Books;
    public static Material Decal, Additive, Line, Flash, Shadow;

    public static Mesh Crescent;
    public static Texture2D SoftCircleTex;
    public static Mesh Octahedron;

    public static void Init(Texture2D stone, Texture2D softCircle)
    {
        lit = Shader.Find("Universal Render Pipeline/Lit");
        unlit = Shader.Find("Universal Render Pipeline/Unlit");
        fx = Shader.Find("Capy/Unlit Transparent");
        SoftCircleTex = softCircle;
        cache.Clear();
        EnsureFolder(MatFolder);
        EnsureFolder(MeshFolder);

        Capy = Lit("Capy", new Color(0.74f, 0.5f, 0.3f), 0.3f);
        CapyDark = Lit("CapyDark", new Color(0.52f, 0.33f, 0.19f), 0.3f);
        CapyBelly = Lit("CapyBelly", new Color(0.86f, 0.66f, 0.45f), 0.3f);
        Nose = Lit("Nose", new Color(0.22f, 0.13f, 0.09f), 0.5f);
        Black = Lit("Black", new Color(0.04f, 0.03f, 0.05f), 0.8f);
        White = Glow("WhiteShine", new Color(1.1f, 1.1f, 1.1f));
        Blush = Lit("Blush", new Color(1f, 0.55f, 0.6f), 0.4f);
        Yuzu = Lit("Yuzu", new Color(1f, 0.58f, 0.1f), 0.55f);
        Leaf = Lit("Leaf", new Color(0.32f, 0.68f, 0.3f), 0.4f);
        Scarf = Lit("Scarf", new Color(0.26f, 0.6f, 0.36f), 0.25f);
        ScarfDark = Lit("ScarfDark", new Color(0.18f, 0.42f, 0.26f), 0.25f);
        Steel = Lit("Steel", new Color(0.78f, 0.8f, 0.86f), 0.8f, 0.7f);
        DarkSteel = Lit("DarkSteel", new Color(0.25f, 0.25f, 0.3f), 0.6f, 0.6f);
        Gold = Lit("Gold", new Color(1f, 0.76f, 0.3f), 0.75f, 0.8f);
        Wood = Lit("Wood", new Color(0.55f, 0.35f, 0.18f), 0.2f);
        WoodDark = Lit("WoodDark", new Color(0.35f, 0.22f, 0.12f), 0.2f);
        Bone = Lit("Bone", new Color(0.92f, 0.88f, 0.8f), 0.35f);
        Helmet = Lit("Helmet", new Color(0.2f, 0.2f, 0.24f), 0.55f, 0.5f);
        Iron = Lit("Iron", new Color(0.38f, 0.38f, 0.42f), 0.6f, 0.6f);
        RedEye = Glow("RedEye", new Color(3.2f, 0.35f, 0.25f));
        Leather = Lit("Leather", new Color(0.42f, 0.27f, 0.17f), 0.3f);
        HoodGreen = Lit("HoodGreen", new Color(0.24f, 0.32f, 0.22f), 0.2f);
        SlimeMat = Lit("Slime", new Color(0.4f, 0.82f, 0.36f), 0.85f);
        SlimeDark = Lit("SlimeDark", new Color(0.16f, 0.38f, 0.16f), 0.6f);
        Robe = Lit("Robe", new Color(0.42f, 0.24f, 0.62f), 0.25f);
        RobeDark = Lit("RobeDark", new Color(0.26f, 0.14f, 0.4f), 0.25f);
        VoidMat = Lit("Void", new Color(0.02f, 0.01f, 0.03f), 0.1f);
        WizEye = Glow("WizEye", new Color(2.2f, 1.4f, 3.2f));
        OrbGlow = Glow("OrbGlow", new Color(2.4f, 1.1f, 2.8f));
        BatLid = Lit("BatLid", new Color(0.46f, 0.28f, 0.56f), 0.35f);
        BatWing = Lit("BatWing", new Color(0.3f, 0.17f, 0.38f), 0.25f);
        EyeWhite = Lit("EyeWhite", new Color(0.97f, 0.95f, 0.95f), 0.7f);
        Iris = Glow("Iris", new Color(1.8f, 0.9f, 0.15f));
        GolemStone = Lit("GolemStone", new Color(0.56f, 0.58f, 0.64f), 0.2f);
        GolemDark = Lit("GolemDark", new Color(0.38f, 0.4f, 0.47f), 0.2f);
        Moss = Lit("Moss", new Color(0.42f, 0.66f, 0.28f), 0.2f);
        CrystalCyan = Glow("CrystalCyan", new Color(0.45f, 1.7f, 2.4f));
        CrystalRed = Glow("CrystalRed", new Color(2.8f, 0.45f, 0.3f));
        CrystalDim = Glow("CrystalDim", new Color(0.1f, 0.25f, 0.32f));
        CrystalPurple = Glow("CrystalPurple", new Color(1.5f, 0.7f, 2.5f));
        Stone = Lit("PropStone", new Color(0.46f, 0.46f, 0.52f), 0.15f);
        StoneDark = Lit("PropStoneDark", new Color(0.3f, 0.3f, 0.36f), 0.15f);
        StoneLight = Lit("PropStoneLight", new Color(0.64f, 0.63f, 0.66f), 0.15f);
        MushRed = Lit("MushRed", new Color(0.92f, 0.28f, 0.3f), 0.4f);
        MushSpot = Lit("MushSpot", new Color(1f, 0.95f, 0.88f), 0.4f);
        BushMat = Lit("Bush", new Color(0.27f, 0.55f, 0.26f), 0.2f);
        BushDark = Lit("BushDark", new Color(0.18f, 0.4f, 0.2f), 0.2f);
        Water = Lit("Water", new Color(0.35f, 0.75f, 0.9f), 0.95f);
        CarpetRed = Lit("CarpetRed", new Color(0.6f, 0.13f, 0.18f), 0.1f);
        CarpetGold = Lit("CarpetGold", new Color(0.85f, 0.65f, 0.25f), 0.3f);
        CandleWax = Lit("Candle", new Color(0.98f, 0.94f, 0.82f), 0.3f);
        Flame = Glow("Flame", new Color(3.2f, 1.6f, 0.35f));
        FlameRed = Glow("FlameRed", new Color(3f, 0.8f, 0.2f));
        ChestWood = Lit("ChestWood", new Color(0.6f, 0.36f, 0.18f), 0.3f);
        GoldGlow = Glow("GoldGlow", new Color(2.4f, 1.7f, 0.45f));
        GreenGlow = Glow("GreenGlow", new Color(0.5f, 2.2f, 0.7f));
        Straw = Lit("Straw", new Color(0.86f, 0.72f, 0.4f), 0.15f);
        Cloth = Lit("ClothRed", new Color(0.72f, 0.18f, 0.2f), 0.15f);
        ClothBlue = Lit("ClothBlue", new Color(0.22f, 0.36f, 0.7f), 0.15f);
        Paper = Lit("Paper", new Color(0.95f, 0.9f, 0.78f), 0.2f);
        Books = new[]
        {
            Lit("Book0", new Color(0.72f, 0.2f, 0.22f), 0.3f),
            Lit("Book1", new Color(0.22f, 0.42f, 0.72f), 0.3f),
            Lit("Book2", new Color(0.26f, 0.6f, 0.36f), 0.3f),
            Lit("Book3", new Color(0.82f, 0.66f, 0.22f), 0.3f),
            Lit("Book4", new Color(0.5f, 0.3f, 0.65f), 0.3f)
        };

        Decal = Fx("FX_Decal", softCircle, Color.white, false, false, 0);
        Additive = Fx("FX_Additive", softCircle, Color.white, true, false, 10);
        Line = Fx("FX_Lines", Texture2D.whiteTexture, Color.white, false, true, 100);
        Flash = Glow("FX_Flash", new Color(1f, 0.96f, 0.9f));
        Shadow = Fx("FX_Shadow", softCircle, new Color(0f, 0f, 0f, 0.42f), false, false, -10);

        Crescent = BuildCrescent();
        Octahedron = BuildOctahedron();
        AssetDatabase.SaveAssets();
    }

    // ================================================================== material

    private static Material GetOrCreate(string name, Shader shader)
    {
        Material m;
        if (cache.TryGetValue(name, out m))
            return m;
        string path = MatFolder + "/" + name + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else if (m.shader != shader)
        {
            m.shader = shader;
        }
        cache[name] = m;
        return m;
    }

    public static Material Lit(string name, Color c, float smooth = 0.3f, float metal = 0f, Texture tex = null, Vector2 tiling = default(Vector2))
    {
        Material m = GetOrCreate(name, lit);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        m.SetTexture("_BaseMap", tex);
        m.SetTextureScale("_BaseMap", tiling == default(Vector2) ? Vector2.one : tiling);
        m.DisableKeyword("_EMISSION");
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    public static Material Glow(string name, Color hdr)
    {
        Material m = GetOrCreate(name, unlit);
        m.SetColor("_BaseColor", hdr);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    public static Material Fx(string name, Texture tex, Color color, bool additive, bool zAlways, int queueOffset)
    {
        Material m = GetOrCreate(name, fx);
        m.SetTexture("_MainTex", tex);
        m.SetColor("_Color", color);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZTest", zAlways ? (float)CompareFunction.Always : (float)CompareFunction.LessEqual);
        m.renderQueue = 3000 + queueOffset;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ================================================================== mesh

    private static Mesh SaveMesh(Mesh mesh, string name)
    {
        string path = MeshFolder + "/" + name + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            existing.Clear();
            existing.vertices = mesh.vertices;
            existing.uv = mesh.uv;
            existing.triangles = mesh.triangles;
            existing.normals = mesh.normals;
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            return existing;
        }
        mesh.name = name;
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    // Bulan sabit datar untuk gelombang tebasan pedang.
    private static Mesh BuildCrescent()
    {
        const int seg = 24;
        const float arc = 75f;
        List<Vector3> v = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> t = new List<int>();
        for (int i = 0; i <= seg; i++)
        {
            float k = i / (float)seg * 2f - 1f;
            float a = k * arc * Mathf.Deg2Rad;
            float thick = (1f - k * k) * 0.36f + 0.01f;
            Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            v.Add(dir * (0.95f - thick) + Vector3.back * 0.55f);
            v.Add(dir * 0.95f + Vector3.back * 0.55f);
            uv.Add(new Vector2(0.5f, 0.05f));
            uv.Add(new Vector2(0.5f, 0.6f));
        }
        for (int i = 0; i < seg; i++)
        {
            int a = i * 2;
            t.Add(a); t.Add(a + 1); t.Add(a + 2);
            t.Add(a + 1); t.Add(a + 3); t.Add(a + 2);
        }
        Mesh m = new Mesh();
        m.SetVertices(v);
        m.SetUVs(0, uv);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return SaveMesh(m, "Crescent");
    }

    private static Mesh BuildOctahedron()
    {
        Vector3[] p =
        {
            new Vector3(0f, 1f, 0f), new Vector3(0f, -1f, 0f),
            new Vector3(0.6f, 0f, 0f), new Vector3(-0.6f, 0f, 0f),
            new Vector3(0f, 0f, 0.6f), new Vector3(0f, 0f, -0.6f)
        };
        int[][] faces =
        {
            new[] { 0, 4, 2 }, new[] { 0, 2, 5 }, new[] { 0, 5, 3 }, new[] { 0, 3, 4 },
            new[] { 1, 2, 4 }, new[] { 1, 5, 2 }, new[] { 1, 3, 5 }, new[] { 1, 4, 3 }
        };
        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        foreach (int[] f in faces)
        {
            for (int k = 0; k < 3; k++)
            {
                t.Add(v.Count);
                v.Add(p[f[k]]);
                uv.Add(Vector2.zero);
            }
        }
        Mesh m = new Mesh();
        m.SetVertices(v);
        m.SetUVs(0, uv);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return SaveMesh(m, "Octahedron");
    }

    // ================================================================== helper

    public static GameObject BlobShadow(Transform parent, float size, bool follow)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Shadow";
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(parent, false);
        q.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        q.transform.localScale = new Vector3(size, size, 1f);
        MeshRenderer r = q.GetComponent<MeshRenderer>();
        r.sharedMaterial = Shadow;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (follow)
        {
            BlobShadow bs = q.AddComponent<BlobShadow>();
            SetFloat(bs, "size", size);
        }
        return q;
    }

    public static GameObject FxQuad(Transform parent, string name, Material mat, Vector3 pos, float size, Texture tex, Color color)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(parent, false);
        q.transform.localPosition = pos;
        q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        q.transform.localScale = new Vector3(size, size, 1f);
        Material m = Fx(name + "_Mat", tex != null ? tex : SoftCircleTex, color, mat == Additive, false, mat == Additive ? 5 : 0);
        MeshRenderer r = q.GetComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return q;
    }

    public static Light PointLight(Transform parent, Vector3 pos, Color color, float range, float intensity)
    {
        GameObject go = new GameObject("Light");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = range;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
        return l;
    }

    // ================================================================== karakter

    // Capybara: badan bulat, jeruk di kepala, syal hijau, pedang di tangan kanan.
    public static Transform Capybara(Transform parent)
    {
        Transform visual = Group("Visual", parent, Vector3.zero).transform;
        visual.localScale = Vector3.one * 0.95f;
        Transform body = Group("Body", visual, Vector3.zero).transform;

        Part(PrimitiveType.Sphere, "Torso", body, new Vector3(0f, 0.62f, 0f), new Vector3(1.12f, 1f, 1.02f), Capy);
        Part(PrimitiveType.Sphere, "Belly", body, new Vector3(0f, 0.55f, 0.28f), new Vector3(0.78f, 0.72f, 0.5f), CapyBelly);
        Part(PrimitiveType.Sphere, "Tail", body, new Vector3(0f, 0.5f, -0.5f), Vector3.one * 0.15f, CapyDark);
        Part(PrimitiveType.Sphere, "Head", body, new Vector3(0f, 1.28f, 0.06f), new Vector3(0.98f, 0.86f, 0.94f), Capy);
        Part(PrimitiveType.Sphere, "Muzzle", body, new Vector3(0f, 1.17f, 0.42f), new Vector3(0.66f, 0.5f, 0.5f), CapyDark);
        Part(PrimitiveType.Sphere, "Nose", body, new Vector3(0f, 1.3f, 0.66f), new Vector3(0.3f, 0.1f, 0.1f), Nose);
        Part(PrimitiveType.Cube, "MouthL", body, new Vector3(-0.035f, 1.09f, 0.66f), new Vector3(0.08f, 0.022f, 0.02f), Nose, new Vector3(0f, 0f, -25f));
        Part(PrimitiveType.Cube, "MouthR", body, new Vector3(0.035f, 1.09f, 0.66f), new Vector3(0.08f, 0.022f, 0.02f), Nose, new Vector3(0f, 0f, 25f));
        for (int s = -1; s <= 1; s += 2)
        {
            Part(PrimitiveType.Sphere, s < 0 ? "EyeL" : "EyeR", body, new Vector3(0.22f * s, 1.42f, 0.42f), Vector3.one * 0.13f, Black);
            Part(PrimitiveType.Sphere, "Shine", body, new Vector3(0.2f * s + 0.02f, 1.455f, 0.48f), Vector3.one * 0.045f, White);
            Part(PrimitiveType.Sphere, "Cheek", body, new Vector3(0.34f * s, 1.23f, 0.38f), new Vector3(0.15f, 0.08f, 0.05f), Blush);
            Part(PrimitiveType.Sphere, "Ear", body, new Vector3(0.32f * s, 1.67f, -0.02f), new Vector3(0.2f, 0.2f, 0.12f), CapyDark);
            Part(PrimitiveType.Sphere, s < 0 ? "FootL" : "FootR", body, new Vector3(0.26f * s, 0.1f, 0.1f), new Vector3(0.3f, 0.2f, 0.42f), CapyDark);
        }

        Transform yuzu = Group("Yuzu", body, new Vector3(0f, 1.83f, 0.04f)).transform;
        Part(PrimitiveType.Sphere, "Fruit", yuzu, Vector3.zero, Vector3.one * 0.36f, Yuzu);
        Part(PrimitiveType.Cylinder, "Stem", yuzu, new Vector3(0f, 0.19f, 0f), new Vector3(0.04f, 0.03f, 0.04f), WoodDark);
        Part(PrimitiveType.Sphere, "Leaf", yuzu, new Vector3(0.09f, 0.21f, 0f), new Vector3(0.2f, 0.04f, 0.1f), Leaf, new Vector3(0f, 0f, 20f));

        Part(PrimitiveType.Cylinder, "Scarf", body, new Vector3(0f, 0.98f, 0.02f), new Vector3(0.92f, 0.08f, 0.88f), Scarf);
        Part(PrimitiveType.Sphere, "Knot", body, new Vector3(0.18f, 0.95f, 0.42f), Vector3.one * 0.16f, Scarf);
        Transform tail = Group("ScarfTail", body, new Vector3(0.22f, 0.95f, 0.42f)).transform;
        Part(PrimitiveType.Cube, "Cloth", tail, new Vector3(0f, -0.22f, 0f), new Vector3(0.17f, 0.42f, 0.06f), Scarf);
        Part(PrimitiveType.Cube, "Stripe", tail, new Vector3(0f, -0.36f, 0.005f), new Vector3(0.18f, 0.05f, 0.065f), ScarfDark);

        Part(PrimitiveType.Sphere, "ArmL", body, new Vector3(-0.5f, 0.68f, 0.16f), new Vector3(0.24f, 0.34f, 0.24f), Capy);

        Transform sword = Group("SwordPivot", body, new Vector3(0.5f, 0.72f, 0.2f)).transform;
        sword.localRotation = Quaternion.Euler(-35f, 0f, -12f);
        Part(PrimitiveType.Sphere, "Hand", sword, Vector3.zero, Vector3.one * 0.26f, Capy);
        Part(PrimitiveType.Cylinder, "Grip", sword, new Vector3(0f, 0.02f, 0f), new Vector3(0.06f, 0.09f, 0.06f), WoodDark);
        Part(PrimitiveType.Sphere, "Pommel", sword, new Vector3(0f, -0.09f, 0f), Vector3.one * 0.08f, Gold);
        Part(PrimitiveType.Cube, "Guard", sword, new Vector3(0f, 0.12f, 0f), new Vector3(0.3f, 0.06f, 0.08f), Gold);
        Part(PrimitiveType.Cube, "Blade", sword, new Vector3(0f, 0.5f, 0f), new Vector3(0.11f, 0.7f, 0.035f), Steel);
        Part(PrimitiveType.Cube, "Tip", sword, new Vector3(0f, 0.85f, 0f), new Vector3(0.078f, 0.078f, 0.035f), Steel, new Vector3(0f, 0f, 45f));
        return visual;
    }

    public static Transform Slime(Transform parent, float scale)
    {
        Transform visual = Group("Visual", parent, Vector3.zero).transform;
        visual.localScale = Vector3.one * scale;
        Transform body = Group("Body", visual, Vector3.zero).transform;
        Part(PrimitiveType.Sphere, "Base", body, new Vector3(0f, 0.26f, 0f), new Vector3(1.42f, 0.52f, 1.42f), SlimeMat);
        Part(PrimitiveType.Sphere, "Blob", body, new Vector3(0f, 0.52f, 0f), new Vector3(1.3f, 1f, 1.3f), SlimeMat);
        Part(PrimitiveType.Sphere, "Tip", body, new Vector3(0f, 1.02f, -0.02f), new Vector3(0.3f, 0.42f, 0.3f), SlimeMat, new Vector3(-12f, 0f, 0f));
        for (int s = -1; s <= 1; s += 2)
        {
            Part(PrimitiveType.Sphere, "Eye", body, new Vector3(0.22f * s, 0.63f, 0.57f), new Vector3(0.13f, 0.16f, 0.08f), Black);
            Part(PrimitiveType.Sphere, "Shine", body, new Vector3(0.22f * s + 0.025f, 0.67f, 0.61f), Vector3.one * 0.045f, White);
            Part(PrimitiveType.Sphere, "Blush", body, new Vector3(0.38f * s, 0.5f, 0.49f), new Vector3(0.14f, 0.07f, 0.05f), Blush);
            Part(PrimitiveType.Cube, "Mouth", body, new Vector3(0.03f * s, 0.48f, 0.635f), new Vector3(0.07f, 0.022f, 0.02f), SlimeDark, new Vector3(0f, 0f, 30f * s));
        }
        Part(PrimitiveType.Sphere, "Gloss", body, new Vector3(-0.32f, 0.84f, 0.36f), new Vector3(0.22f, 0.13f, 0.1f), White, new Vector3(0f, 0f, 35f));
        return visual;
    }

    private static Transform SkeletonBase(Transform parent, out Transform body)
    {
        Transform visual = Group("Visual", parent, Vector3.zero).transform;
        body = Group("Body", visual, Vector3.zero).transform;
        Part(PrimitiveType.Cube, "Pelvis", body, new Vector3(0f, 0.8f, 0f), new Vector3(0.42f, 0.14f, 0.24f), Bone);
        Part(PrimitiveType.Cylinder, "Spine", body, new Vector3(0f, 1.02f, -0.04f), new Vector3(0.07f, 0.22f, 0.07f), Bone);
        for (int i = 0; i < 3; i++)
            Part(PrimitiveType.Cylinder, "Rib", body, new Vector3(0f, 1.0f + i * 0.14f, 0f), new Vector3(0.46f - i * 0.03f, 0.03f, 0.32f), Bone);
        Part(PrimitiveType.Cube, "Sternum", body, new Vector3(0f, 1.14f, 0.15f), new Vector3(0.06f, 0.3f, 0.04f), Bone);
        Part(PrimitiveType.Cylinder, "Collar", body, new Vector3(0f, 1.38f, 0f), new Vector3(0.07f, 0.31f, 0.07f), Bone, new Vector3(0f, 0f, 90f));
        Part(PrimitiveType.Sphere, "Skull", body, new Vector3(0f, 1.72f, 0.02f), new Vector3(0.5f, 0.48f, 0.5f), Bone);
        Part(PrimitiveType.Cube, "Jaw", body, new Vector3(0f, 1.52f, 0.08f), new Vector3(0.3f, 0.12f, 0.26f), Bone);
        for (int s = -1; s <= 1; s += 2)
        {
            Part(PrimitiveType.Sphere, "Socket", body, new Vector3(0.1f * s, 1.73f, 0.2f), new Vector3(0.13f, 0.13f, 0.07f), Black);
            Part(PrimitiveType.Sphere, "Eye", body, new Vector3(0.1f * s, 1.73f, 0.235f), Vector3.one * 0.07f, RedEye);
            Transform leg = Group(s < 0 ? "LegL" : "LegR", body, new Vector3(0.13f * s, 0.78f, 0f)).transform;
            Part(PrimitiveType.Capsule, "Bone", leg, new Vector3(0f, -0.36f, 0f), new Vector3(0.09f, 0.36f, 0.09f), Bone);
            Part(PrimitiveType.Cube, "Foot", leg, new Vector3(0f, -0.74f, 0.06f), new Vector3(0.13f, 0.06f, 0.22f), Bone);
        }
        return visual;
    }

    public static Transform SkeletonWarrior(Transform parent)
    {
        Transform body;
        Transform visual = SkeletonBase(parent, out body);
        Part(PrimitiveType.Sphere, "Helmet", body, new Vector3(0f, 1.86f, 0f), new Vector3(0.58f, 0.36f, 0.58f), Helmet);
        Part(PrimitiveType.Cylinder, "Rim", body, new Vector3(0f, 1.81f, 0f), new Vector3(0.6f, 0.02f, 0.6f), Iron);
        for (int s = -1; s <= 1; s += 2)
            Part(PrimitiveType.Capsule, "Horn", body, new Vector3(0.27f * s, 2.0f, 0f), new Vector3(0.08f, 0.15f, 0.08f), Bone, new Vector3(0f, 0f, -35f * s));
        Part(PrimitiveType.Capsule, "ArmL", body, new Vector3(-0.36f, 1.16f, 0.06f), new Vector3(0.07f, 0.24f, 0.07f), Bone, new Vector3(20f, 0f, 15f));
        Part(PrimitiveType.Cylinder, "ShieldRim", body, new Vector3(-0.44f, 1.05f, 0.25f), new Vector3(0.6f, 0.03f, 0.6f), Iron, new Vector3(90f, 0f, 0f));
        Part(PrimitiveType.Cylinder, "Shield", body, new Vector3(-0.44f, 1.05f, 0.27f), new Vector3(0.55f, 0.035f, 0.55f), Wood, new Vector3(90f, 0f, 0f));
        Part(PrimitiveType.Cube, "ShieldBand", body, new Vector3(-0.44f, 1.05f, 0.3f), new Vector3(0.56f, 0.06f, 0.02f), Iron);
        Part(PrimitiveType.Sphere, "Boss", body, new Vector3(-0.44f, 1.05f, 0.31f), Vector3.one * 0.13f, Iron);

        Transform sword = Group("SwordPivot", body, new Vector3(0.36f, 1.12f, 0.12f)).transform;
        Part(PrimitiveType.Capsule, "Arm", sword, new Vector3(0f, -0.05f, 0f), new Vector3(0.07f, 0.2f, 0.07f), Bone);
        Part(PrimitiveType.Cylinder, "Grip", sword, new Vector3(0f, 0.08f, 0.06f), new Vector3(0.05f, 0.08f, 0.05f), Leather);
        Part(PrimitiveType.Cube, "Guard", sword, new Vector3(0f, 0.16f, 0.06f), new Vector3(0.24f, 0.05f, 0.06f), Iron);
        Part(PrimitiveType.Cube, "Blade", sword, new Vector3(0f, 0.48f, 0.06f), new Vector3(0.1f, 0.6f, 0.035f), DarkSteel);
        return visual;
    }

    public static Transform SkeletonArcher(Transform parent)
    {
        Transform body;
        Transform visual = SkeletonBase(parent, out body);
        Part(PrimitiveType.Sphere, "Hood", body, new Vector3(0f, 1.8f, -0.04f), new Vector3(0.6f, 0.52f, 0.6f), HoodGreen);
        Part(PrimitiveType.Sphere, "HoodTip", body, new Vector3(0f, 1.98f, -0.2f), new Vector3(0.26f, 0.3f, 0.26f), HoodGreen, new Vector3(-30f, 0f, 0f));
        Part(PrimitiveType.Capsule, "ArmR", body, new Vector3(0.34f, 1.16f, 0.1f), new Vector3(0.07f, 0.22f, 0.07f), Bone, new Vector3(50f, 0f, -10f));
        Part(PrimitiveType.Cylinder, "Quiver", body, new Vector3(0.12f, 1.2f, -0.22f), new Vector3(0.14f, 0.25f, 0.14f), Leather, new Vector3(20f, 0f, 0f));
        for (int i = 0; i < 3; i++)
            Part(PrimitiveType.Cube, "Fletch", body, new Vector3(0.08f + i * 0.04f, 1.5f, -0.3f), new Vector3(0.03f, 0.1f, 0.06f), Cloth, new Vector3(20f, 0f, 0f));

        Transform bow = Group("BowPivot", body, new Vector3(-0.36f, 1.15f, 0.28f)).transform;
        Part(PrimitiveType.Capsule, "Arm", bow, new Vector3(0.08f, 0f, -0.12f), new Vector3(0.07f, 0.2f, 0.07f), Bone, new Vector3(70f, 0f, 0f));
        Part(PrimitiveType.Capsule, "LimbUp", bow, new Vector3(0f, 0.24f, 0.04f), new Vector3(0.05f, 0.24f, 0.05f), Wood, new Vector3(-18f, 0f, 0f));
        Part(PrimitiveType.Capsule, "LimbDown", bow, new Vector3(0f, -0.24f, 0.04f), new Vector3(0.05f, 0.24f, 0.05f), Wood, new Vector3(18f, 0f, 0f));
        Part(PrimitiveType.Cylinder, "String", bow, new Vector3(0f, 0f, -0.05f), new Vector3(0.012f, 0.44f, 0.012f), Paper);
        return visual;
    }

    public static Transform Wizard(Transform parent)
    {
        Transform visual = Group("Visual", parent, Vector3.zero).transform;
        Transform body = Group("Body", visual, new Vector3(0f, 0.15f, 0f)).transform;
        Part(PrimitiveType.Cylinder, "RobeHem", body, new Vector3(0f, 0.22f, 0f), new Vector3(1.15f, 0.22f, 1.15f), Robe);
        Part(PrimitiveType.Cylinder, "Trim", body, new Vector3(0f, 0.02f, 0f), new Vector3(1.19f, 0.035f, 1.19f), Gold);
        Part(PrimitiveType.Cylinder, "RobeMid", body, new Vector3(0f, 0.6f, 0f), new Vector3(0.95f, 0.22f, 0.95f), Robe);
        Part(PrimitiveType.Capsule, "Torso", body, new Vector3(0f, 1.05f, 0f), new Vector3(0.75f, 0.5f, 0.7f), Robe);
        Part(PrimitiveType.Cylinder, "Belt", body, new Vector3(0f, 0.86f, 0f), new Vector3(0.78f, 0.04f, 0.74f), Gold);
        Part(PrimitiveType.Cube, "Gem", body, new Vector3(0f, 1.12f, 0.36f), Vector3.one * 0.12f, OrbGlow, new Vector3(0f, 0f, 45f));
        Part(PrimitiveType.Sphere, "Hood", body, new Vector3(0f, 1.62f, 0f), new Vector3(0.82f, 0.8f, 0.82f), Robe);
        Part(PrimitiveType.Sphere, "HoodTip", body, new Vector3(0f, 2.0f, -0.08f), new Vector3(0.3f, 0.42f, 0.3f), Robe, new Vector3(-20f, 0f, 0f));
        Part(PrimitiveType.Sphere, "HoodTip2", body, new Vector3(0f, 2.22f, -0.2f), new Vector3(0.14f, 0.24f, 0.14f), RobeDark, new Vector3(-38f, 0f, 0f));
        Part(PrimitiveType.Sphere, "Face", body, new Vector3(0f, 1.58f, 0.2f), new Vector3(0.56f, 0.54f, 0.45f), VoidMat);
        for (int s = -1; s <= 1; s += 2)
        {
            Part(PrimitiveType.Sphere, "Eye", body, new Vector3(0.1f * s, 1.6f, 0.42f), new Vector3(0.09f, 0.06f, 0.04f), WizEye);
            Part(PrimitiveType.Capsule, "Sleeve", body, new Vector3(0.42f * s, 1.0f, 0.12f), new Vector3(0.22f, 0.28f, 0.22f), RobeDark, new Vector3(20f, 0f, -15f * s));
        }
        Transform staff = Group("Staff", body, new Vector3(0.5f, 0f, 0.2f)).transform;
        Part(PrimitiveType.Cylinder, "Pole", staff, new Vector3(0f, 1.05f, 0f), new Vector3(0.06f, 1.05f, 0.06f), Wood);
        Part(PrimitiveType.Cylinder, "Ring", staff, new Vector3(0f, 2.1f, 0f), new Vector3(0.26f, 0.02f, 0.26f), Gold);
        Part(PrimitiveType.Sphere, "Orb", staff, new Vector3(0f, 2.24f, 0f), Vector3.one * 0.24f, OrbGlow);
        PointLight(staff, new Vector3(0f, 2.24f, 0f), new Color(0.85f, 0.5f, 1f), 4.5f, 2.5f);
        return visual;
    }

    public static Transform Bat(Transform parent)
    {
        Transform visual = Group("Visual", parent, Vector3.zero).transform;
        Transform body = Group("Body", visual, Vector3.zero).transform;
        Part(PrimitiveType.Sphere, "Eyeball", body, Vector3.zero, Vector3.one * 0.8f, EyeWhite);
        Part(PrimitiveType.Sphere, "Lid", body, new Vector3(0f, 0.14f, -0.05f), new Vector3(0.86f, 0.64f, 0.86f), BatLid);
        Part(PrimitiveType.Sphere, "Iris", body, new Vector3(0f, -0.03f, 0.3f), new Vector3(0.42f, 0.42f, 0.2f), Iris);
        Part(PrimitiveType.Sphere, "Pupil", body, new Vector3(0f, -0.03f, 0.385f), new Vector3(0.17f, 0.21f, 0.06f), Black);
        Part(PrimitiveType.Sphere, "Shine", body, new Vector3(0.09f, 0.05f, 0.4f), Vector3.one * 0.06f, White);
        for (int s = -1; s <= 1; s += 2)
        {
            Part(PrimitiveType.Sphere, "Horn", body, new Vector3(0.22f * s, 0.42f, -0.02f), new Vector3(0.13f, 0.2f, 0.13f), Bone, new Vector3(0f, 0f, -20f * s));
            Transform wing = Group(s < 0 ? "WingL" : "WingR", body, new Vector3(0.36f * s, 0.05f, -0.05f)).transform;
            Part(PrimitiveType.Cube, "Arm", wing, new Vector3(0.35f * s, 0.1f, 0f), new Vector3(0.72f, 0.07f, 0.07f), BatLid, new Vector3(0f, 0f, 10f * s));
            Part(PrimitiveType.Cube, "Membrane", wing, new Vector3(0.38f * s, -0.05f, 0f), new Vector3(0.72f, 0.34f, 0.04f), BatWing);
            for (int k = 0; k < 3; k++)
                Part(PrimitiveType.Sphere, "Scallop", wing, new Vector3((0.15f + k * 0.25f) * s, -0.2f, 0f), new Vector3(0.24f, 0.14f, 0.04f), BatWing);
        }
        return visual;
    }

    public static Transform Golem(Transform parent, out Renderer[] glow, out Light coreLight)
    {
        List<Renderer> glows = new List<Renderer>();
        Transform visual = Group("Visual", parent, Vector3.zero).transform;
        Transform body = Group("Body", visual, Vector3.zero).transform;

        for (int s = -1; s <= 1; s += 2)
        {
            Transform leg = Group(s < 0 ? "LegL" : "LegR", body, new Vector3(0.75f * s, 1.7f, 0f)).transform;
            Part(PrimitiveType.Cube, "Leg", leg, new Vector3(0f, -0.8f, 0f), new Vector3(0.9f, 1.6f, 1f), GolemDark);
            Part(PrimitiveType.Cube, "Foot", leg, new Vector3(0f, -1.55f, 0.12f), new Vector3(1.05f, 0.32f, 1.25f), GolemDark);
        }
        Part(PrimitiveType.Cube, "Pelvis", body, new Vector3(0f, 1.85f, 0f), new Vector3(2f, 0.5f, 1.2f), GolemDark);
        Part(PrimitiveType.Cube, "Torso", body, new Vector3(0f, 3f, 0f), new Vector3(3f, 2f, 1.7f), GolemStone);
        Part(PrimitiveType.Cube, "ChestPlate", body, new Vector3(0f, 3.1f, 0.86f), new Vector3(1.1f, 1.1f, 0.06f), GolemDark);
        glows.Add(Part(PrimitiveType.Cube, "Core", body, new Vector3(0f, 3.1f, 0.92f), new Vector3(0.5f, 0.5f, 0.12f), CrystalCyan, new Vector3(0f, 0f, 45f)).GetComponent<Renderer>());
        Part(PrimitiveType.Cube, "MossBack", body, new Vector3(0.6f, 3.95f, -0.3f), new Vector3(1.2f, 0.12f, 1f), Moss);

        Transform head = Group("Head", body, new Vector3(0f, 4.05f, 0.1f)).transform;
        Part(PrimitiveType.Cube, "Skull", head, new Vector3(0f, 0.45f, 0f), new Vector3(1.25f, 0.95f, 1.1f), GolemStone);
        Part(PrimitiveType.Cube, "Brow", head, new Vector3(0f, 0.72f, 0.5f), new Vector3(1.3f, 0.14f, 0.2f), GolemDark);
        glows.Add(Part(PrimitiveType.Cube, "Eyes", head, new Vector3(0f, 0.5f, 0.56f), new Vector3(0.85f, 0.16f, 0.06f), CrystalCyan).GetComponent<Renderer>());
        Part(PrimitiveType.Cube, "Moss", head, new Vector3(0f, 0.95f, 0f), new Vector3(1.05f, 0.12f, 0.9f), Moss);

        for (int s = -1; s <= 1; s += 2)
        {
            Part(PrimitiveType.Cube, "Shoulder", body, new Vector3(1.9f * s, 3.75f, 0f), new Vector3(1.15f, 1f, 1.3f), GolemDark);
            Part(PrimitiveType.Cube, "ShoulderMoss", body, new Vector3(1.9f * s, 4.28f, 0f), new Vector3(1f, 0.1f, 1.1f), Moss);
            glows.Add(Part(PrimitiveType.Cube, "Spike", body, new Vector3(2.15f * s, 4.5f, -0.1f), new Vector3(0.25f, 0.75f, 0.25f), CrystalCyan, new Vector3(0f, 0f, -25f * s)).GetComponent<Renderer>());
            glows.Add(Part(PrimitiveType.Cube, "Spike2", body, new Vector3(1.75f * s, 4.42f, 0.25f), new Vector3(0.18f, 0.45f, 0.18f), CrystalCyan, new Vector3(15f, 0f, -10f * s)).GetComponent<Renderer>());

            Transform arm = Group(s < 0 ? "ArmL" : "ArmR", body, new Vector3(1.95f * s, 3.55f, 0f)).transform;
            Part(PrimitiveType.Cube, "Upper", arm, new Vector3(0f, -0.85f, 0f), new Vector3(0.85f, 1.4f, 0.85f), GolemStone);
            Part(PrimitiveType.Cube, "Band", arm, new Vector3(0f, -1.6f, 0.1f), new Vector3(1.42f, 0.2f, 1.42f), GolemDark);
            Part(PrimitiveType.Cube, "Fist", arm, new Vector3(0f, -2.05f, 0.1f), new Vector3(1.35f, 1.15f, 1.35f), GolemStone);
        }

        coreLight = PointLight(body, new Vector3(0f, 3.1f, 1.6f), new Color(0.5f, 0.9f, 1f), 8f, 4f);
        glow = glows.ToArray();
        return visual;
    }

    // ================================================================== properti level

    public static GameObject WallTorch(Transform parent, Vector3 pos, Color lightColor, float yaw)
    {
        GameObject t = Group("Torch", parent, pos);
        t.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Part(PrimitiveType.Cube, "Bracket", t.transform, new Vector3(0f, 0f, 0f), new Vector3(0.16f, 0.4f, 0.16f), Iron);
        Part(PrimitiveType.Cylinder, "Stick", t.transform, new Vector3(0f, 0.25f, 0.18f), new Vector3(0.1f, 0.25f, 0.1f), Wood, new Vector3(30f, 0f, 0f));
        Part(PrimitiveType.Cylinder, "Cup", t.transform, new Vector3(0f, 0.48f, 0.3f), new Vector3(0.22f, 0.07f, 0.22f), Iron);
        GameObject flame = Part(PrimitiveType.Sphere, "Flame", t.transform, new Vector3(0f, 0.68f, 0.3f), new Vector3(0.26f, 0.38f, 0.26f), Flame);
        Light l = PointLight(t.transform, new Vector3(0f, 0.9f, 0.6f), lightColor, 9f, 3.2f);
        LightFlicker f = l.gameObject.AddComponent<LightFlicker>();
        SetFloat(f, "baseIntensity", 3.2f);
        SetRef(f, "flame", flame.transform);
        NoShadows(t);
        return t;
    }

    public static GameObject Brazier(Transform parent, Vector3 pos, Color lightColor, float scale = 1f)
    {
        GameObject t = Group("Brazier", parent, pos);
        t.transform.localScale = Vector3.one * scale;
        Part(PrimitiveType.Cylinder, "Base", t.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.6f, 0.08f, 0.6f), Iron);
        Part(PrimitiveType.Cylinder, "Pole", t.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.14f, 0.45f, 0.14f), Iron);
        Part(PrimitiveType.Cylinder, "Bowl", t.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.75f, 0.12f, 0.75f), Iron);
        Part(PrimitiveType.Sphere, "Coals", t.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.6f, 0.15f, 0.6f), FlameRed);
        GameObject flame = Part(PrimitiveType.Sphere, "Flame", t.transform, new Vector3(0f, 1.42f, 0f), new Vector3(0.42f, 0.6f, 0.42f), Flame);
        Light l = PointLight(t.transform, new Vector3(0f, 1.8f, 0f), lightColor, 10f, 3.6f);
        LightFlicker f = l.gameObject.AddComponent<LightFlicker>();
        SetFloat(f, "baseIntensity", 3.6f);
        SetRef(f, "flame", flame.transform);
        Obstacle(t, new Vector3(0f, 0.6f, 0f), new Vector3(0.75f, 1.2f, 0.75f));
        return t;
    }

    // Collider kotak + layer Obstacle (menghalangi gerak, proyektil, dan dipakai grid A*).
    public static void Obstacle(GameObject go, Vector3 center, Vector3 size)
    {
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.center = center;
        bc.size = size;
        go.layer = Layers.Obstacle;
    }

    public static GameObject Mushroom(Transform parent, Vector3 pos, float s, bool collider)
    {
        GameObject t = Group("Mushroom", parent, pos);
        t.transform.localScale = Vector3.one * s;
        t.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Part(PrimitiveType.Cylinder, "Stem", t.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.24f, 0.3f, 0.24f), MushSpot);
        Part(PrimitiveType.Sphere, "Cap", t.transform, new Vector3(0f, 0.64f, 0f), new Vector3(0.82f, 0.46f, 0.82f), MushRed);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 1.7f;
            Part(PrimitiveType.Sphere, "Spot", t.transform, new Vector3(Mathf.Cos(a) * 0.25f, 0.8f, Mathf.Sin(a) * 0.25f), new Vector3(0.12f, 0.06f, 0.12f), MushSpot);
        }
        if (collider)
            Obstacle(t, new Vector3(0f, 0.5f, 0f), new Vector3(0.7f, 1f, 0.7f));
        return t;
    }

    public static GameObject Bush(Transform parent, Vector3 pos, float s, bool collider)
    {
        GameObject t = Group("Bush", parent, pos);
        t.transform.localScale = Vector3.one * s;
        Part(PrimitiveType.Sphere, "A", t.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.2f, 0.9f, 1.1f), BushMat);
        Part(PrimitiveType.Sphere, "B", t.transform, new Vector3(0.45f, 0.35f, 0.2f), new Vector3(0.8f, 0.7f, 0.8f), BushDark);
        Part(PrimitiveType.Sphere, "C", t.transform, new Vector3(-0.4f, 0.32f, -0.15f), new Vector3(0.75f, 0.62f, 0.75f), BushMat);
        Part(PrimitiveType.Sphere, "Berry", t.transform, new Vector3(0.1f, 0.85f, 0.35f), Vector3.one * 0.12f, MushRed);
        if (collider)
            Obstacle(t, new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 1f, 1.3f));
        return t;
    }

    public static GameObject Flower(Transform parent, Vector3 pos, Material petal)
    {
        GameObject t = Group("Flower", parent, pos);
        Part(PrimitiveType.Cylinder, "Stem", t.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.03f, 0.15f, 0.03f), Leaf);
        Part(PrimitiveType.Sphere, "Petals", t.transform, new Vector3(0f, 0.32f, 0f), new Vector3(0.2f, 0.07f, 0.2f), petal);
        Part(PrimitiveType.Sphere, "Center", t.transform, new Vector3(0f, 0.35f, 0f), Vector3.one * 0.07f, Yuzu);
        NoShadows(t);
        return t;
    }

    public static GameObject Crate(Transform parent, Vector3 pos, float s, float yaw)
    {
        GameObject t = Group("Crate", parent, pos);
        t.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Part(PrimitiveType.Cube, "Box", t.transform, new Vector3(0f, s * 0.5f, 0f), Vector3.one * s, Wood);
        Part(PrimitiveType.Cube, "BandA", t.transform, new Vector3(0f, s * 0.5f, 0f), new Vector3(s * 1.02f, s * 0.12f, s * 1.02f), WoodDark);
        Part(PrimitiveType.Cube, "BandB", t.transform, new Vector3(0f, s * 0.5f, 0f), new Vector3(s * 0.12f, s * 1.02f, s * 1.02f), WoodDark);
        Obstacle(t, new Vector3(0f, s * 0.5f, 0f), Vector3.one * s);
        return t;
    }

    public static GameObject Barrel(Transform parent, Vector3 pos)
    {
        GameObject t = Group("Barrel", parent, pos);
        Part(PrimitiveType.Cylinder, "Body", t.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 0.55f, 0.8f), Wood);
        Part(PrimitiveType.Cylinder, "HoopA", t.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.84f, 0.04f, 0.84f), Iron);
        Part(PrimitiveType.Cylinder, "HoopB", t.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.84f, 0.04f, 0.84f), Iron);
        Part(PrimitiveType.Cylinder, "Lid", t.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.72f, 0.02f, 0.72f), WoodDark);
        Obstacle(t, new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 1.1f, 0.8f));
        return t;
    }

    public static GameObject Pillar(Transform parent, Vector3 pos, Material mat, Material capMat, float height = 3.2f)
    {
        GameObject t = Group("Pillar", parent, pos);
        Part(PrimitiveType.Cube, "Base", t.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.3f, 0.3f, 1.3f), capMat);
        Part(PrimitiveType.Cylinder, "Column", t.transform, new Vector3(0f, height * 0.5f, 0f), new Vector3(0.9f, height * 0.5f, 0.9f), mat);
        Part(PrimitiveType.Cube, "Cap", t.transform, new Vector3(0f, height, 0f), new Vector3(1.3f, 0.3f, 1.3f), capMat);
        BoxCollider bc = t.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, height * 0.5f, 0f);
        bc.size = new Vector3(1f, height, 1f);
        t.layer = Layers.Obstacle;
        return t;
    }

    public static GameObject WeaponRack(Transform parent, Vector3 pos, float yaw)
    {
        GameObject t = Group("WeaponRack", parent, pos);
        t.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Part(PrimitiveType.Cube, "PostL", t.transform, new Vector3(-1.1f, 0.7f, 0f), new Vector3(0.14f, 1.4f, 0.14f), WoodDark);
        Part(PrimitiveType.Cube, "PostR", t.transform, new Vector3(1.1f, 0.7f, 0f), new Vector3(0.14f, 1.4f, 0.14f), WoodDark);
        Part(PrimitiveType.Cube, "Bar", t.transform, new Vector3(0f, 1.25f, 0f), new Vector3(2.4f, 0.12f, 0.14f), Wood);
        Part(PrimitiveType.Cube, "Bar2", t.transform, new Vector3(0f, 0.4f, 0f), new Vector3(2.4f, 0.1f, 0.14f), Wood);
        for (int i = 0; i < 4; i++)
        {
            float x = -0.75f + i * 0.5f;
            Part(PrimitiveType.Cube, "Blade", t.transform, new Vector3(x, 0.85f, 0.09f), new Vector3(0.09f, 1.1f, 0.03f), Steel, new Vector3(0f, 0f, i % 2 == 0 ? 4f : -4f));
            Part(PrimitiveType.Cube, "Guard", t.transform, new Vector3(x, 0.32f, 0.09f), new Vector3(0.24f, 0.05f, 0.05f), Gold);
        }
        Obstacle(t, new Vector3(0f, 0.7f, 0f), new Vector3(2.4f, 1.4f, 0.4f));
        return t;
    }

    public static GameObject Dummy(Transform parent, Vector3 pos)
    {
        GameObject t = Group("TrainingDummy", parent, pos);
        Part(PrimitiveType.Cylinder, "Post", t.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.12f, 0.6f, 0.12f), WoodDark);
        Part(PrimitiveType.Capsule, "Body", t.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.55f, 0.45f, 0.5f), Straw);
        Part(PrimitiveType.Sphere, "Head", t.transform, new Vector3(0f, 1.7f, 0f), Vector3.one * 0.42f, Straw);
        Part(PrimitiveType.Cylinder, "Arms", t.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.08f, 0.55f, 0.08f), WoodDark, new Vector3(0f, 0f, 90f));
        Part(PrimitiveType.Cube, "Target", t.transform, new Vector3(0f, 1.1f, 0.26f), new Vector3(0.3f, 0.3f, 0.02f), Cloth);
        Obstacle(t, new Vector3(0f, 0.8f, 0f), new Vector3(0.6f, 1.6f, 0.6f));
        return t;
    }

    public static GameObject Banner(Transform parent, Vector3 pos, Material cloth)
    {
        GameObject t = Group("Banner", parent, pos);
        Part(PrimitiveType.Cylinder, "Rod", t.transform, new Vector3(0f, 0f, 0f), new Vector3(0.06f, 0.55f, 0.06f), Gold, new Vector3(0f, 0f, 90f));
        Part(PrimitiveType.Cube, "Cloth", t.transform, new Vector3(0f, -0.75f, 0f), new Vector3(0.9f, 1.5f, 0.05f), cloth);
        Part(PrimitiveType.Cube, "Tip", t.transform, new Vector3(0f, -1.55f, 0f), new Vector3(0.45f, 0.45f, 0.05f), cloth, new Vector3(0f, 0f, 45f));
        Part(PrimitiveType.Cube, "Emblem", t.transform, new Vector3(0f, -0.65f, 0.03f), new Vector3(0.35f, 0.35f, 0.02f), Gold, new Vector3(0f, 0f, 45f));
        NoShadows(t);
        return t;
    }

    public static GameObject Bookshelf(Transform parent, Vector3 pos, float yaw, float width)
    {
        GameObject t = Group("Bookshelf", parent, pos);
        t.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Part(PrimitiveType.Cube, "Frame", t.transform, new Vector3(0f, 1.3f, 0f), new Vector3(width, 2.6f, 0.7f), WoodDark);
        int books = Mathf.RoundToInt(width / 0.42f);
        for (int row = 0; row < 3; row++)
        {
            Part(PrimitiveType.Cube, "Shelf", t.transform, new Vector3(0f, 0.25f + row * 0.8f, 0.05f), new Vector3(width - 0.1f, 0.06f, 0.62f), Wood);
            for (int b = 0; b < books; b++)
            {
                float h = 0.45f + ((b * 7 + row * 3) % 5) * 0.05f;
                float x = -width * 0.5f + 0.25f + b * ((width - 0.5f) / Mathf.Max(1, books - 1));
                Part(PrimitiveType.Cube, "Book", t.transform, new Vector3(x, 0.28f + row * 0.8f + h * 0.5f, 0.12f), new Vector3(0.28f, h, 0.45f), Books[(b + row * 2) % Books.Length], new Vector3(0f, 0f, (b % 4 == 3) ? 8f : 0f));
            }
        }
        Obstacle(t, new Vector3(0f, 1.3f, 0f), new Vector3(width, 2.6f, 0.7f));
        return t;
    }

    public static GameObject Candle(Transform parent, Vector3 pos, bool light)
    {
        GameObject t = Group("Candle", parent, pos);
        Part(PrimitiveType.Cylinder, "Wax", t.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.12f, 0.15f, 0.12f), CandleWax);
        Part(PrimitiveType.Sphere, "Flame", t.transform, new Vector3(0f, 0.36f, 0f), new Vector3(0.08f, 0.13f, 0.08f), Flame);
        if (light)
        {
            Light l = PointLight(t.transform, new Vector3(0f, 0.6f, 0f), new Color(1f, 0.75f, 0.45f), 5f, 1.4f);
            LightFlicker f = l.gameObject.AddComponent<LightFlicker>();
            SetFloat(f, "baseIntensity", 1.4f);
        }
        NoShadows(t);
        return t;
    }

    public static GameObject Desk(Transform parent, Vector3 pos, float yaw)
    {
        GameObject t = Group("Desk", parent, pos);
        t.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Part(PrimitiveType.Cube, "Top", t.transform, new Vector3(0f, 0.85f, 0f), new Vector3(2.2f, 0.12f, 1.1f), Wood);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Part(PrimitiveType.Cube, "Leg", t.transform, new Vector3(0.95f * sx, 0.42f, 0.42f * sz), new Vector3(0.12f, 0.84f, 0.12f), WoodDark);
        Part(PrimitiveType.Sphere, "CrystalBall", t.transform, new Vector3(-0.5f, 1.15f, 0f), Vector3.one * 0.42f, OrbGlow);
        Part(PrimitiveType.Cylinder, "Stand", t.transform, new Vector3(-0.5f, 0.95f, 0f), new Vector3(0.3f, 0.05f, 0.3f), Gold);
        Part(PrimitiveType.Cube, "Scroll", t.transform, new Vector3(0.45f, 0.93f, 0.1f), new Vector3(0.6f, 0.03f, 0.45f), Paper, new Vector3(0f, 15f, 0f));
        Candle(t.transform, new Vector3(0.85f, 0.91f, -0.3f), false);
        Obstacle(t, new Vector3(0f, 0.5f, 0f), new Vector3(2.2f, 1f, 1.1f));
        return t;
    }

    public static GameObject FloatingBook(Transform parent, Vector3 pos)
    {
        GameObject t = Group("FloatingBook", parent, pos);
        Transform b = Group("Book", t.transform, Vector3.zero).transform;
        b.localRotation = Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
        Part(PrimitiveType.Cube, "Cover", b, Vector3.zero, new Vector3(0.5f, 0.08f, 0.38f), Books[Random.Range(0, Books.Length)]);
        Part(PrimitiveType.Cube, "Pages", b, new Vector3(0.02f, 0f, 0f), new Vector3(0.46f, 0.06f, 0.34f), Paper);
        Bobbing bob = t.AddComponent<Bobbing>();
        SetFloat(bob, "amplitude", 0.2f);
        SetFloat(bob, "speed", 1.5f);
        SetFloat(bob, "spinSpeed", 20f);
        return t;
    }

    public static GameObject Stalagmite(Transform parent, Vector3 pos, float s)
    {
        GameObject t = Group("Stalagmite", parent, pos);
        t.transform.localScale = new Vector3(s, s * Random.Range(0.9f, 1.4f), s);
        Part(PrimitiveType.Cylinder, "Base", t.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.3f, 0.9f), StoneDark);
        Part(PrimitiveType.Cylinder, "Mid", t.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.62f, 0.3f, 0.62f), StoneDark);
        Part(PrimitiveType.Cylinder, "Top", t.transform, new Vector3(0f, 1.35f, 0f), new Vector3(0.36f, 0.25f, 0.36f), Stone);
        Part(PrimitiveType.Sphere, "Tip", t.transform, new Vector3(0f, 1.62f, 0f), new Vector3(0.3f, 0.4f, 0.3f), Stone);
        Obstacle(t, new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 1.2f, 0.9f));
        return t;
    }

    public static GameObject Rock(Transform parent, Vector3 pos, float s, bool collider)
    {
        GameObject t = Group("Rock", parent, pos);
        t.transform.localScale = Vector3.one * s;
        t.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Part(PrimitiveType.Sphere, "A", t.transform, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.75f, 1f), StoneDark);
        Part(PrimitiveType.Sphere, "B", t.transform, new Vector3(0.4f, 0.25f, 0.2f), new Vector3(0.7f, 0.5f, 0.7f), Stone);
        if (collider)
            Obstacle(t, new Vector3(0f, 0.4f, 0f), new Vector3(1.3f, 0.8f, 1.1f));
        return t;
    }

    public static GameObject CrystalCluster(Transform parent, Vector3 pos, float s, Material mat, Color lightColor, bool light, bool collider)
    {
        GameObject t = Group("Crystals", parent, pos);
        t.transform.localScale = Vector3.one * s;
        Part(PrimitiveType.Cube, "C1", t.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.35f, 1.3f, 0.35f), mat, new Vector3(8f, 20f, 10f));
        Part(PrimitiveType.Cube, "C2", t.transform, new Vector3(0.3f, 0.35f, 0.15f), new Vector3(0.25f, 0.75f, 0.25f), mat, new Vector3(-10f, 40f, -30f));
        Part(PrimitiveType.Cube, "C3", t.transform, new Vector3(-0.28f, 0.3f, -0.1f), new Vector3(0.22f, 0.6f, 0.22f), mat, new Vector3(15f, -20f, 28f));
        Part(PrimitiveType.Sphere, "Rock", t.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.9f, 0.25f, 0.8f), StoneDark);
        if (light)
            PointLight(t.transform, new Vector3(0f, 1.2f, 0f), lightColor, 6f, 2.2f);
        if (collider)
            Obstacle(t, new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 1.2f, 0.9f));
        NoShadows(t);
        return t;
    }

    public static GameObject YuzuPile(Transform parent, Vector3 pos)
    {
        GameObject t = Group("YuzuPile", parent, pos);
        int n = 0;
        for (int layer = 0; layer < 3; layer++)
        {
            int count = 6 - layer * 2;
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f + layer;
                float r = 0.45f - layer * 0.18f;
                Part(PrimitiveType.Sphere, "Yuzu" + n++, t.transform, new Vector3(Mathf.Cos(a) * r, 0.2f + layer * 0.3f, Mathf.Sin(a) * r), Vector3.one * 0.38f, layer == 2 ? GoldGlow : Yuzu);
            }
        }
        return t;
    }

    public static GameObject Throne(Transform parent, Vector3 pos)
    {
        GameObject t = Group("Throne", parent, pos);
        Part(PrimitiveType.Cube, "Seat", t.transform, new Vector3(0f, 0.9f, 0f), new Vector3(2.6f, 0.6f, 1.8f), GolemDark);
        Part(PrimitiveType.Cube, "Back", t.transform, new Vector3(0f, 2.6f, -0.75f), new Vector3(2.6f, 3.2f, 0.4f), GolemDark);
        Part(PrimitiveType.Cube, "BackTrim", t.transform, new Vector3(0f, 4.25f, -0.75f), new Vector3(2.9f, 0.25f, 0.5f), Gold);
        Part(PrimitiveType.Cube, "Gem", t.transform, new Vector3(0f, 3.3f, -0.52f), new Vector3(0.5f, 0.5f, 0.1f), CrystalCyan, new Vector3(0f, 0f, 45f));
        for (int s = -1; s <= 1; s += 2)
        {
            Part(PrimitiveType.Cube, "Arm", t.transform, new Vector3(1.35f * s, 1.45f, 0f), new Vector3(0.4f, 0.5f, 1.8f), GolemStone);
            Part(PrimitiveType.Cube, "ArmTrim", t.transform, new Vector3(1.35f * s, 1.72f, 0f), new Vector3(0.45f, 0.08f, 1.85f), Gold);
        }
        Obstacle(t, new Vector3(0f, 1.5f, -0.2f), new Vector3(2.9f, 3f, 2f));
        return t;
    }

    public static GameObject HotSpringVisual(Transform parent, Vector3 pos, float radius)
    {
        GameObject t = Group("HotSpring", parent, pos);
        int rocks = Mathf.RoundToInt(radius * 7f);
        for (int i = 0; i < rocks; i++)
        {
            float a = i / (float)rocks * Mathf.PI * 2f;
            float s = 0.55f + (i * 37 % 10) * 0.03f;
            Part(PrimitiveType.Sphere, "Rock", t.transform, new Vector3(Mathf.Cos(a) * radius, 0.18f, Mathf.Sin(a) * radius), new Vector3(s * 1.2f, s * 0.6f, s), i % 3 == 0 ? StoneLight : Stone, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
        }
        Part(PrimitiveType.Cylinder, "Water", t.transform, new Vector3(0f, 0.14f, 0f), new Vector3(radius * 2f, 0.05f, radius * 2f), Water);
        FxQuad(t.transform, "WaterGlow", Additive, new Vector3(0f, 0.2f, 0f), radius * 2.2f, null, new Color(0.4f, 0.8f, 1f, 0.25f));
        return t;
    }

    public static GameObject Chevrons(Transform parent, Vector3 pos, int count, out Transform[] items)
    {
        GameObject t = Group("ExitGuide", parent, pos);
        items = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            Transform c = Group("Chevron", t.transform, new Vector3(0f, 0.06f, i * 1.1f)).transform;
            Part(PrimitiveType.Cube, "L", c, new Vector3(-0.28f, 0f, -0.28f), new Vector3(0.9f, 0.04f, 0.18f), GreenGlow, new Vector3(0f, -45f, 0f));
            Part(PrimitiveType.Cube, "R", c, new Vector3(0.28f, 0f, -0.28f), new Vector3(0.9f, 0.04f, 0.18f), GreenGlow, new Vector3(0f, 45f, 0f));
            items[i] = c;
        }
        NoShadows(t);
        return t;
    }
}
