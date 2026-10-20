using System.IO;
using UnityEditor;
using UnityEngine;

// Generator tekstur dan sprite prosedural (lantai, ikon UI, ikon skill, tekstur efek).
// Semua digambar dengan signed distance function lalu disimpan sebagai PNG di Assets/Art.
public static class CapyTextures
{
    public const string TexFolder = "Assets/Art/Textures";
    public const string UIFolder = "Assets/Art/UI";

    // ================================================================== kanvas

    private class Pix
    {
        public readonly int W;
        public readonly int H;
        public readonly Color[] px;

        public Pix(int w, int h, Color fill)
        {
            W = w;
            H = h;
            px = new Color[w * h];
            for (int i = 0; i < px.Length; i++)
                px[i] = fill;
        }

        public void Paint(System.Func<Vector2, float> sdf, Color color, float aa = -1f)
        {
            if (aa <= 0f)
                aa = 1.4f / W;
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / W, (y + 0.5f) / H);
                    float d = sdf(p);
                    float cov = Mathf.Clamp01(0.5f - d / aa);
                    if (cov <= 0f)
                        continue;
                    Blend(y * W + x, color, cov);
                }
            }
        }

        public void Blend(int i, Color c, float cov)
        {
            float a = c.a * cov;
            Color d = px[i];
            float outA = a + d.a * (1f - a);
            if (outA < 0.00001f)
            {
                px[i] = new Color(0f, 0f, 0f, 0f);
                return;
            }
            Color o = (c * a + d * d.a * (1f - a)) / outA;
            o.a = outA;
            px[i] = o;
        }

        // Garis luar gelap + isi, supaya ikon terbaca di atas latar apa pun.
        public void Shape(System.Func<Vector2, float> sdf, Color fill, Color outline, float width)
        {
            Paint(p => sdf(p) - width, outline);
            Paint(sdf, fill);
        }

        public Texture2D ToTexture()
        {
            Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }

    // ================================================================== SDF

    private static float Dot2(Vector2 v) { return Vector2.Dot(v, v); }

    private static float Circle(Vector2 p, Vector2 c, float r) { return Vector2.Distance(p, c) - r; }

    private static float Ring(Vector2 p, Vector2 c, float r, float w) { return Mathf.Abs(Circle(p, c, r)) - w; }

    private static float Box(Vector2 p, Vector2 c, Vector2 half, float round)
    {
        Vector2 d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(round, round);
        return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - round;
    }

    private static float Seg(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        Vector2 pa = p - a;
        Vector2 ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.000001f, Vector2.Dot(ba, ba)));
        return (pa - ba * h).magnitude - r;
    }

    private static float Poly(Vector2 p, float r, params Vector2[] pts)
    {
        float d = float.MaxValue;
        for (int i = 0; i < pts.Length - 1; i++)
            d = Mathf.Min(d, Seg(p, pts[i], pts[i + 1], r));
        return d;
    }

    private static float Heart(Vector2 p, Vector2 c, float s)
    {
        Vector2 q = (p - c) / s + new Vector2(0f, 0.55f);
        q.x = Mathf.Abs(q.x);
        float d;
        if (q.y + q.x > 1f)
            d = Mathf.Sqrt(Dot2(q - new Vector2(0.25f, 0.75f))) - Mathf.Sqrt(2f) / 4f;
        else
            d = Mathf.Sqrt(Mathf.Min(Dot2(q - new Vector2(0f, 1f)), Dot2(q - 0.5f * Mathf.Max(q.x + q.y, 0f) * Vector2.one))) * Mathf.Sign(q.x - q.y);
        return d * s;
    }

    private static float Diamond(Vector2 p, Vector2 c, Vector2 half)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x - c.x) / half.x, Mathf.Abs(p.y - c.y) / half.y);
        return (q.x + q.y - 1f) * Mathf.Min(half.x, half.y) * 0.7f;
    }

    private static float SmoothMin(float a, float b, float k)
    {
        float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
        return Mathf.Lerp(b, a, h) - k * h * (1f - h);
    }

    private static float Crescent(Vector2 p, Vector2 c, float r, Vector2 bite)
    {
        return Mathf.Max(Circle(p, c, r), -Circle(p, c + bite, r * 0.92f));
    }

    private static float Arrow(Vector2 p, Vector2 from, Vector2 to, float w)
    {
        Vector2 dir = (to - from).normalized;
        Vector2 side = new Vector2(-dir.y, dir.x);
        float shaft = Seg(p, from, to - dir * 0.02f, w);
        float headL = Seg(p, to, to - dir * 0.16f + side * 0.12f, w);
        float headR = Seg(p, to, to - dir * 0.16f - side * 0.12f, w);
        return Mathf.Min(shaft, Mathf.Min(headL, headR));
    }

    private static float Chevron(Vector2 p, Vector2 tip, Vector2 dir, float size, float w)
    {
        Vector2 side = new Vector2(-dir.y, dir.x);
        return Mathf.Min(Seg(p, tip, tip - dir * size + side * size, w), Seg(p, tip, tip - dir * size - side * size, w));
    }

    // ================================================================== noise (bisa diulang tanpa sambungan)

    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)0x7fffffff;
        }
    }

    private static int Wrap(int v, int period)
    {
        int m = v % period;
        return m < 0 ? m + period : m;
    }

    private static float ValueNoise(float x, float y, int period, int seed)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0;
        float fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float a = Hash(Wrap(x0, period), Wrap(y0, period), seed);
        float b = Hash(Wrap(x0 + 1, period), Wrap(y0, period), seed);
        float c = Hash(Wrap(x0, period), Wrap(y0 + 1, period), seed);
        float d = Hash(Wrap(x0 + 1, period), Wrap(y0 + 1, period), seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    private static float Fbm(float u, float v, int freq, int octaves, int seed)
    {
        float sum = 0f;
        float amp = 0.5f;
        float norm = 0f;
        for (int o = 0; o < octaves; o++)
        {
            sum += ValueNoise(u * freq, v * freq, freq, seed + o * 17) * amp;
            norm += amp;
            freq *= 2;
            amp *= 0.5f;
        }
        return sum / norm;
    }

    // Voronoi yang bisa diulang: mengembalikan jarak ke titik terdekat (f1) dan kedua (f2).
    private static void Voronoi(float u, float v, int cells, int seed, out float f1, out float f2, out int cellId)
    {
        float x = u * cells;
        float y = v * cells;
        int cx = Mathf.FloorToInt(x);
        int cy = Mathf.FloorToInt(y);
        f1 = 9f;
        f2 = 9f;
        cellId = 0;
        for (int j = -1; j <= 1; j++)
        {
            for (int i = -1; i <= 1; i++)
            {
                int gx = cx + i;
                int gy = cy + j;
                int wx = Wrap(gx, cells);
                int wy = Wrap(gy, cells);
                float px = gx + 0.15f + 0.7f * Hash(wx, wy, seed);
                float py = gy + 0.15f + 0.7f * Hash(wx, wy, seed + 7);
                float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                if (d < f1)
                {
                    f2 = f1;
                    f1 = d;
                    cellId = wx + wy * cells;
                }
                else if (d < f2)
                {
                    f2 = d;
                }
            }
        }
    }

    private static Color Tint(Color c, float k)
    {
        return new Color(c.r * k, c.g * k, c.b * k, c.a);
    }

    // ================================================================== simpan & impor

    private static string Save(Pix pix, string folder, string name)
    {
        CapyEditorUtil.EnsureFolder(folder);
        string path = folder + "/" + name + ".png";
        Texture2D t = pix.ToTexture();
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return path;
    }

    private static Texture2D ImportTexture(string path, bool repeat, bool mipmaps, bool alpha)
    {
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Default;
        ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        ti.mipmapEnabled = mipmaps;
        ti.alphaIsTransparency = alpha;
        ti.filterMode = FilterMode.Trilinear;
        ti.anisoLevel = repeat ? 4 : 1;
        ti.textureCompression = repeat ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Sprite ImportSprite(string path, Vector4 border)
    {
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spriteBorder = border;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ================================================================== tekstur efek

    public static Texture2D SoftCircle()
    {
        Pix p = new Pix(128, 128, new Color(1f, 1f, 1f, 0f));
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float r = Vector2.Distance(new Vector2((x + 0.5f) / 128f, (y + 0.5f) / 128f), new Vector2(0.5f, 0.5f)) * 2f;
                float a = Mathf.Clamp01(1f - r);
                p.px[y * 128 + x] = new Color(1f, 1f, 1f, a * a);
            }
        }
        return ImportTexture(Save(p, TexFolder, "SoftCircle"), false, true, true);
    }

    public static Texture2D RingTex()
    {
        Pix p = new Pix(256, 256, new Color(1f, 1f, 1f, 0f));
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float r = Vector2.Distance(new Vector2((x + 0.5f) / 256f, (y + 0.5f) / 256f), new Vector2(0.5f, 0.5f)) * 2f;
                float a = Mathf.Exp(-Mathf.Pow((r - 0.86f) / 0.06f, 2f)) + Mathf.Exp(-Mathf.Pow((r - 0.86f) / 0.2f, 2f)) * 0.25f;
                p.px[y * 256 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a) * (r < 1f ? 1f : 0f));
            }
        }
        return ImportTexture(Save(p, TexFolder, "Ring"), false, true, true);
    }

    public static Texture2D Spark()
    {
        Pix p = new Pix(64, 64, new Color(1f, 1f, 1f, 0f));
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float r = Vector2.Distance(new Vector2((x + 0.5f) / 64f, (y + 0.5f) / 64f), new Vector2(0.5f, 0.5f)) * 2f;
                p.px[y * 64 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Exp(-r * r * 6f) * 1.3f - 0.02f));
            }
        }
        return ImportTexture(Save(p, TexFolder, "Spark"), false, true, true);
    }

    public static Texture2D Star()
    {
        Pix p = new Pix(128, 128, new Color(1f, 1f, 1f, 0f));
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float u = ((x + 0.5f) / 128f - 0.5f) * 2f;
                float v = ((y + 0.5f) / 128f - 0.5f) * 2f;
                float a = Mathf.Exp(-Mathf.Abs(u) * 16f) * Mathf.Exp(-Mathf.Abs(v) * 2.5f) + Mathf.Exp(-Mathf.Abs(v) * 16f) * Mathf.Exp(-Mathf.Abs(u) * 2.5f);
                a += Mathf.Exp(-(u * u + v * v) * 25f);
                a *= Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
                p.px[y * 128 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            }
        }
        return ImportTexture(Save(p, TexFolder, "Star"), false, true, true);
    }

    public static Texture2D TelegraphCircle()
    {
        Pix p = new Pix(256, 256, new Color(1f, 1f, 1f, 0f));
        Vector2 c = new Vector2(0.5f, 0.5f);
        p.Paint(q => Circle(q, c, 0.49f), new Color(1f, 1f, 1f, 0.5f));
        p.Paint(q => Ring(q, c, 0.47f, 0.02f), new Color(1f, 1f, 1f, 1f));
        return ImportTexture(Save(p, TexFolder, "TelegraphCircle"), false, true, true);
    }

    public static Texture2D TelegraphRect()
    {
        Pix p = new Pix(128, 256, new Color(1f, 1f, 1f, 0f));
        p.Paint(q => Box(q, new Vector2(0.5f, 0.5f), new Vector2(0.48f, 0.49f), 0.02f), new Color(1f, 1f, 1f, 0.45f));
        p.Paint(q => Mathf.Abs(Box(q, new Vector2(0.5f, 0.5f), new Vector2(0.46f, 0.48f), 0.02f)) - 0.02f, new Color(1f, 1f, 1f, 1f));
        for (int i = 0; i < 4; i++)
        {
            float y = 0.2f + i * 0.2f;
            p.Paint(q => Chevron(new Vector2(q.x, q.y * 2f), new Vector2(0.5f, y * 2f + 0.06f), Vector2.up, 0.2f, 0.035f), new Color(1f, 1f, 1f, 0.7f));
        }
        return ImportTexture(Save(p, TexFolder, "TelegraphRect"), false, true, true);
    }

    public static Texture2D SpawnRune()
    {
        Pix p = new Pix(256, 256, new Color(1f, 1f, 1f, 0f));
        Vector2 c = new Vector2(0.5f, 0.5f);
        p.Paint(q => Ring(q, c, 0.46f, 0.016f), Color.white);
        p.Paint(q => Ring(q, c, 0.38f, 0.01f), Color.white);
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * Mathf.PI * 2f;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            p.Paint(q => Seg(q, c + d * 0.39f, c + d * 0.45f, 0.008f), Color.white);
        }
        Vector2[] tri1 = new Vector2[4];
        Vector2[] tri2 = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            float a1 = Mathf.PI * 0.5f + i * Mathf.PI * 2f / 3f;
            float a2 = -Mathf.PI * 0.5f + i * Mathf.PI * 2f / 3f;
            tri1[i] = c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * 0.34f;
            tri2[i] = c + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * 0.34f;
        }
        p.Paint(q => Poly(q, 0.009f, tri1), new Color(1f, 1f, 1f, 0.9f));
        p.Paint(q => Poly(q, 0.009f, tri2), new Color(1f, 1f, 1f, 0.9f));
        p.Paint(q => Circle(q, c, 0.05f), new Color(1f, 1f, 1f, 0.8f));
        return ImportTexture(Save(p, TexFolder, "SpawnRune"), false, true, true);
    }

    // ================================================================== lantai & dinding

    public static Texture2D Floor(string kind)
    {
        int n = 512;
        Pix p = new Pix(n, n, Color.white);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n;
                float v = (y + 0.5f) / n;
                p.px[y * n + x] = FloorColor(kind, u, v);
            }
        }
        return ImportTexture(Save(p, TexFolder, "Floor_" + kind), true, true, false);
    }

    private static Color FloorColor(string kind, float u, float v)
    {
        switch (kind)
        {
            case "Camp": return WoodPlanks(u, v, new Color(0.62f, 0.42f, 0.25f), 8, 3);
            case "Garden": return Grass(u, v);
            case "Barracks": return Flagstones(u, v, new Color(0.5f, 0.43f, 0.36f), 4, 2, 11);
            case "Library": return DiamondTiles(u, v);
            case "Cave": return CaveRock(u, v);
            case "Throne": return Marble(u, v);
            case "Corridor": return Flagstones(u, v, new Color(0.36f, 0.35f, 0.4f), 8, 4, 23);
            default: return Outside(u, v);
        }
    }

    private static Color WoodPlanks(float u, float v, Color baseColor, int planks, int seed)
    {
        float row = v * planks;
        int r = Mathf.FloorToInt(row);
        float fr = row - r;
        float offset = Hash(r, 0, seed);
        float uu = Mathf.Repeat(u + offset, 1f);
        int segment = Mathf.FloorToInt(uu * 2f);
        float fs = uu * 2f - segment;
        float tint = 0.82f + Hash(r, segment, seed + 1) * 0.3f;
        float grain = Fbm(u * 1f, v * planks, 4, 3, seed + 3);
        float lines = Mathf.Abs(Mathf.Sin((v * planks * 6f + grain * 6f) * Mathf.PI));
        Color c = Tint(baseColor, tint * (0.9f + grain * 0.2f) * (0.94f + lines * 0.06f));
        float gap = Mathf.Min(fr, 1f - fr);
        if (gap < 0.05f) c = Tint(c, 0.45f + gap * 8f);
        float end = Mathf.Min(fs, 1f - fs) * 2f / planks * planks;
        if (end < 0.012f) c = Tint(c, 0.55f);
        if (Mathf.Abs(fs - 0.05f) < 0.008f && Mathf.Abs(fr - 0.5f) < 0.06f) c = Tint(c, 0.5f);
        return c;
    }

    private static Color Grass(float u, float v)
    {
        float n1 = Fbm(u, v, 4, 4, 5);
        float n2 = Fbm(u, v, 32, 2, 9);
        Color a = new Color(0.32f, 0.56f, 0.26f);
        Color b = new Color(0.42f, 0.66f, 0.3f);
        Color c = Color.Lerp(a, b, n1);
        c = Tint(c, 0.86f + n2 * 0.25f);
        float blades = ValueNoise(u * 96f, v * 160f, 96, 3);
        if (blades > 0.78f) c = Tint(c, 0.82f);
        int gx = Mathf.FloorToInt(u * 24f);
        int gy = Mathf.FloorToInt(v * 24f);
        float h = Hash(gx, gy, 77);
        if (h > 0.93f)
        {
            Vector2 center = new Vector2((gx + 0.3f + Hash(gx, gy, 3) * 0.4f) / 24f, (gy + 0.3f + Hash(gx, gy, 4) * 0.4f) / 24f);
            float d = Vector2.Distance(new Vector2(u, v), center);
            if (d < 0.006f)
            {
                Color[] flowers = { new Color(1f, 0.95f, 0.85f), new Color(1f, 0.85f, 0.3f), new Color(1f, 0.6f, 0.75f) };
                c = flowers[Mathf.FloorToInt(Hash(gx, gy, 5) * 2.99f)];
            }
        }
        return c;
    }

    private static Color Flagstones(float u, float v, Color baseColor, int rows, int perRow, int seed)
    {
        float row = v * rows;
        int r = Mathf.FloorToInt(row);
        float fr = row - r;
        float uu = Mathf.Repeat(u + (r % 2) * (0.5f / perRow), 1f);
        float col = uu * perRow;
        int ci = Mathf.FloorToInt(col);
        float fc = col - ci;
        float tint = 0.8f + Hash(Wrap(ci, perRow), r, seed) * 0.35f;
        float speck = Fbm(u, v, 16, 3, seed + 2);
        Color c = Tint(baseColor, tint * (0.88f + speck * 0.24f));
        float edgeX = Mathf.Min(fc, 1f - fc) / perRow * rows;
        float edgeY = Mathf.Min(fr, 1f - fr);
        float edge = Mathf.Min(edgeX, edgeY);
        if (edge < 0.04f) c = Tint(baseColor, 0.35f);
        else if (edge < 0.1f)
        {
            bool topLeft = (fr > 0.5f && edgeY < 0.1f) || (fc < 0.5f && edgeX < 0.1f);
            c = Tint(c, topLeft ? 1.12f : 0.85f);
        }
        return c;
    }

    private static Color DiamondTiles(float u, float v)
    {
        float a = (u + v) * 4f;
        float b = (u - v) * 4f;
        int ia = Mathf.FloorToInt(a);
        int ib = Mathf.FloorToInt(b);
        float fa = a - ia;
        float fb = b - ib;
        bool alt = ((ia + ib) & 1) == 0;
        Color c = alt ? new Color(0.36f, 0.24f, 0.52f) : new Color(0.24f, 0.2f, 0.42f);
        float n = Fbm(u, v, 8, 3, 41);
        c = Tint(c, 0.9f + n * 0.2f);
        float edge = Mathf.Min(Mathf.Min(fa, 1f - fa), Mathf.Min(fb, 1f - fb));
        if (edge < 0.04f) c = new Color(0.78f, 0.62f, 0.3f);
        else if (edge < 0.08f) c = Tint(c, 0.75f);
        return c;
    }

    private static Color CaveRock(float u, float v)
    {
        float f1, f2;
        int id;
        Voronoi(u, v, 6, 31, out f1, out f2, out id);
        float n = Fbm(u, v, 8, 4, 33);
        Color c = Color.Lerp(new Color(0.16f, 0.18f, 0.26f), new Color(0.26f, 0.29f, 0.38f), n);
        c = Tint(c, 0.85f + Hash(id, 0, 35) * 0.3f);
        float crack = f2 - f1;
        if (crack < 0.05f) c = Tint(c, 0.45f + crack * 6f);
        return c;
    }

    private static Color Marble(float u, float v)
    {
        float tu = u * 2f;
        float tv = v * 2f;
        float fu = tu - Mathf.Floor(tu);
        float fv = tv - Mathf.Floor(tv);
        float n = Fbm(u, v, 4, 5, 51);
        float veins = Mathf.Pow(Mathf.Abs(Mathf.Sin((u * 6f + v * 3f + n * 4f) * Mathf.PI)), 0.25f);
        Color c = Color.Lerp(new Color(0.42f, 0.44f, 0.52f), new Color(0.6f, 0.62f, 0.7f), veins);
        c = Tint(c, 0.9f + n * 0.15f);
        float edge = Mathf.Min(Mathf.Min(fu, 1f - fu), Mathf.Min(fv, 1f - fv));
        if (edge < 0.012f) c = new Color(0.82f, 0.66f, 0.32f);
        else if (edge < 0.03f) c = Tint(c, 0.7f);
        else if (edge > 0.07f && edge < 0.08f) c = Tint(c, 0.85f);
        return c;
    }

    private static Color Outside(float u, float v)
    {
        float n = Fbm(u, v, 8, 4, 61);
        return Color.Lerp(new Color(0.07f, 0.06f, 0.1f), new Color(0.13f, 0.12f, 0.17f), n);
    }

    public static Texture2D StoneBlock()
    {
        int n = 128;
        Pix p = new Pix(n, n, Color.white);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n;
                float v = (y + 0.5f) / n;
                float d = Box(new Vector2(u, v), new Vector2(0.5f, 0.5f), new Vector2(0.47f, 0.47f), 0.08f);
                float noise = Fbm(u, v, 8, 3, 71);
                float k = 0.85f + noise * 0.3f;
                if (d > -0.015f) k *= 0.55f;
                else if (d > -0.06f) k *= (u + v > 1f) ? 0.88f : 1.1f;
                p.px[y * n + x] = new Color(k, k, k, 1f);
            }
        }
        return ImportTexture(Save(p, TexFolder, "StoneBlock"), true, true, false);
    }

    // ================================================================== sprite UI

    public static Sprite Rounded(string name, int size, float radiusPx, int border)
    {
        Pix p = new Pix(size, size, new Color(1f, 1f, 1f, 0f));
        float r = radiusPx / size;
        p.Paint(q => Box(q, new Vector2(0.5f, 0.5f), new Vector2(0.5f - 1f / size, 0.5f - 1f / size), r), Color.white);
        return ImportSprite(Save(p, UIFolder, name), new Vector4(border, border, border, border));
    }

    public static Sprite CircleSprite()
    {
        Pix p = new Pix(256, 256, new Color(1f, 1f, 1f, 0f));
        p.Paint(q => Circle(q, new Vector2(0.5f, 0.5f), 0.495f), Color.white);
        return ImportSprite(Save(p, UIFolder, "Circle"), Vector4.zero);
    }

    public static Sprite RingSprite()
    {
        Pix p = new Pix(256, 256, new Color(1f, 1f, 1f, 0f));
        p.Paint(q => Ring(q, new Vector2(0.5f, 0.5f), 0.47f, 0.025f), Color.white);
        return ImportSprite(Save(p, UIFolder, "RingUI"), Vector4.zero);
    }

    public static Sprite GlowSprite()
    {
        Pix p = new Pix(256, 256, new Color(1f, 1f, 1f, 0f));
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float r = Vector2.Distance(new Vector2((x + 0.5f) / 256f, (y + 0.5f) / 256f), new Vector2(0.5f, 0.5f)) * 2f;
                float a = Mathf.Clamp01(1f - r);
                p.px[y * 256 + x] = new Color(1f, 1f, 1f, a * a);
            }
        }
        return ImportSprite(Save(p, UIFolder, "Glow"), Vector4.zero);
    }

    public static Sprite VignetteSprite()
    {
        Pix p = new Pix(256, 256, new Color(1f, 1f, 1f, 0f));
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float u = ((x + 0.5f) / 256f - 0.5f) * 2f;
                float v = ((y + 0.5f) / 256f - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u + v * v) / 1.41421f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, r));
                p.px[y * 256 + x] = new Color(1f, 1f, 1f, a);
            }
        }
        return ImportSprite(Save(p, UIFolder, "Vignette"), Vector4.zero);
    }

    private static readonly Color Ink = new Color(0.1f, 0.06f, 0.14f, 1f);

    public static Sprite HeartSprite()
    {
        Pix p = new Pix(128, 128, new Color(1f, 1f, 1f, 0f));
        Vector2 c = new Vector2(0.5f, 0.47f);
        p.Shape(q => Heart(q, c, 0.62f), new Color(0.98f, 0.32f, 0.4f), Ink, 0.045f);
        p.Paint(q => Circle(q, new Vector2(0.36f, 0.62f), 0.07f), new Color(1f, 1f, 1f, 0.75f));
        return ImportSprite(Save(p, UIFolder, "Heart"), Vector4.zero);
    }

    public static Sprite CapyFace()
    {
        Pix p = new Pix(256, 256, new Color(1f, 1f, 1f, 0f));
        Color fur = new Color(0.74f, 0.5f, 0.3f);
        Color dark = new Color(0.52f, 0.33f, 0.18f);
        System.Func<Vector2, float> head = q => SmoothMin(Circle(q, new Vector2(0.5f, 0.44f), 0.31f), Box(q, new Vector2(0.5f, 0.34f), new Vector2(0.25f, 0.16f), 0.14f), 0.05f);
        p.Paint(q => Circle(q, new Vector2(0.27f, 0.68f), 0.075f) - 0.03f, Ink);
        p.Paint(q => Circle(q, new Vector2(0.73f, 0.68f), 0.075f) - 0.03f, Ink);
        p.Paint(q => Circle(q, new Vector2(0.27f, 0.68f), 0.075f), dark);
        p.Paint(q => Circle(q, new Vector2(0.73f, 0.68f), 0.075f), dark);
        p.Shape(head, fur, Ink, 0.03f);
        p.Paint(q => Box(q, new Vector2(0.5f, 0.3f), new Vector2(0.17f, 0.1f), 0.09f), dark);
        p.Paint(q => Box(q, new Vector2(0.5f, 0.355f), new Vector2(0.06f, 0.025f), 0.025f), Ink);
        p.Paint(q => Seg(q, new Vector2(0.44f, 0.26f), new Vector2(0.5f, 0.29f), 0.01f), Ink);
        p.Paint(q => Seg(q, new Vector2(0.56f, 0.26f), new Vector2(0.5f, 0.29f), 0.01f), Ink);
        p.Paint(q => Circle(q, new Vector2(0.38f, 0.47f), 0.042f), Ink);
        p.Paint(q => Circle(q, new Vector2(0.62f, 0.47f), 0.042f), Ink);
        p.Paint(q => Circle(q, new Vector2(0.393f, 0.485f), 0.014f), Color.white);
        p.Paint(q => Circle(q, new Vector2(0.633f, 0.485f), 0.014f), Color.white);
        p.Paint(q => Circle(q, new Vector2(0.29f, 0.37f), 0.04f), new Color(1f, 0.55f, 0.6f, 0.75f));
        p.Paint(q => Circle(q, new Vector2(0.71f, 0.37f), 0.04f), new Color(1f, 0.55f, 0.6f, 0.75f));
        p.Shape(q => Circle(q, new Vector2(0.5f, 0.82f), 0.1f), new Color(1f, 0.6f, 0.12f), Ink, 0.025f);
        p.Paint(q => Circle(q, new Vector2(0.47f, 0.85f), 0.025f), new Color(1f, 0.85f, 0.55f, 0.8f));
        p.Shape(q => Box(q, new Vector2(0.57f, 0.93f), new Vector2(0.055f, 0.018f), 0.015f), new Color(0.35f, 0.7f, 0.3f), Ink, 0.015f);
        return ImportSprite(Save(p, UIFolder, "CapyFace"), Vector4.zero);
    }

    private static Sprite Icon(string name, System.Action<Pix> draw)
    {
        Pix p = new Pix(128, 128, new Color(1f, 1f, 1f, 0f));
        draw(p);
        return ImportSprite(Save(p, UIFolder, name), Vector4.zero);
    }

    private static void W(Pix p, System.Func<Vector2, float> sdf)
    {
        p.Shape(sdf, Color.white, new Color(0.08f, 0.05f, 0.12f, 0.55f), 0.035f);
    }

    public static Sprite DashIcon()
    {
        return Icon("Icon_Dash", p =>
        {
            W(p, q => Mathf.Min(Chevron(q, new Vector2(0.62f, 0.5f), Vector2.right, 0.2f, 0.05f), Chevron(q, new Vector2(0.84f, 0.5f), Vector2.right, 0.2f, 0.05f)));
            W(p, q => Mathf.Min(Seg(q, new Vector2(0.12f, 0.64f), new Vector2(0.3f, 0.64f), 0.03f), Mathf.Min(Seg(q, new Vector2(0.06f, 0.5f), new Vector2(0.3f, 0.5f), 0.03f), Seg(q, new Vector2(0.12f, 0.36f), new Vector2(0.3f, 0.36f), 0.03f))));
        });
    }

    public static Sprite SkullIcon()
    {
        return Icon("Icon_Skull", p =>
        {
            System.Func<Vector2, float> skull = q => SmoothMin(Circle(q, new Vector2(0.5f, 0.56f), 0.3f), Box(q, new Vector2(0.5f, 0.3f), new Vector2(0.17f, 0.12f), 0.05f), 0.06f);
            p.Shape(skull, new Color(1f, 0.96f, 0.88f), Ink, 0.04f);
            p.Paint(q => Circle(q, new Vector2(0.39f, 0.52f), 0.08f), Ink);
            p.Paint(q => Circle(q, new Vector2(0.61f, 0.52f), 0.08f), Ink);
            p.Paint(q => Seg(q, new Vector2(0.44f, 0.24f), new Vector2(0.44f, 0.3f), 0.015f), Ink);
            p.Paint(q => Seg(q, new Vector2(0.56f, 0.24f), new Vector2(0.56f, 0.3f), 0.015f), Ink);
        });
    }

    public static Sprite ClockIcon()
    {
        return Icon("Icon_Clock", p =>
        {
            p.Shape(q => Circle(q, new Vector2(0.5f, 0.5f), 0.38f), new Color(1f, 0.96f, 0.88f), Ink, 0.045f);
            p.Paint(q => Seg(q, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.76f), 0.03f), Ink);
            p.Paint(q => Seg(q, new Vector2(0.5f, 0.5f), new Vector2(0.68f, 0.42f), 0.03f), Ink);
            p.Paint(q => Circle(q, new Vector2(0.5f, 0.5f), 0.045f), new Color(1f, 0.55f, 0.15f));
        });
    }

    public static Sprite GemIcon()
    {
        return Icon("Icon_Gem", p =>
        {
            p.Shape(q => Diamond(q, new Vector2(0.5f, 0.5f), new Vector2(0.28f, 0.4f)), new Color(0.4f, 0.85f, 1f), Ink, 0.04f);
            p.Paint(q => Mathf.Max(Diamond(q, new Vector2(0.5f, 0.5f), new Vector2(0.28f, 0.4f)), q.x - 0.5f), new Color(0.75f, 0.97f, 1f));
        });
    }

    // Ikon skill: urutan sama dengan enum UpgradeId.
    public static Sprite[] UpgradeIcons()
    {
        Sprite[] icons = new Sprite[17];
        Vector2 c = new Vector2(0.5f, 0.5f);

        icons[0] = Icon("Up_TwinSlash", p =>
        {
            W(p, q => Crescent(q, new Vector2(0.42f, 0.56f), 0.26f, new Vector2(-0.09f, -0.09f)));
            W(p, q => Crescent(q, new Vector2(0.6f, 0.4f), 0.26f, new Vector2(-0.09f, -0.09f)));
        });
        icons[1] = Icon("Up_WideSwing", p =>
        {
            W(p, q => Mathf.Min(Arrow(q, new Vector2(0.26f, 0.18f), new Vector2(0.26f, 0.8f), 0.04f),
                Mathf.Min(Arrow(q, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.84f), 0.04f), Arrow(q, new Vector2(0.74f, 0.18f), new Vector2(0.74f, 0.8f), 0.04f))));
        });
        icons[2] = Icon("Up_Diagonal", p =>
        {
            W(p, q => Mathf.Min(Arrow(q, new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.84f), 0.04f),
                Mathf.Min(Arrow(q, new Vector2(0.5f, 0.16f), new Vector2(0.18f, 0.72f), 0.04f), Arrow(q, new Vector2(0.5f, 0.16f), new Vector2(0.82f, 0.72f), 0.04f))));
        });
        icons[3] = Icon("Up_RearGuard", p =>
        {
            W(p, q => Mathf.Min(Arrow(q, new Vector2(0.36f, 0.3f), new Vector2(0.36f, 0.86f), 0.045f), Arrow(q, new Vector2(0.64f, 0.7f), new Vector2(0.64f, 0.14f), 0.045f)));
        });
        icons[4] = Icon("Up_Piercing", p =>
        {
            W(p, q => Ring(q, c, 0.22f, 0.04f));
            W(p, q => Arrow(q, new Vector2(0.12f, 0.12f), new Vector2(0.86f, 0.86f), 0.04f));
        });
        icons[5] = Icon("Up_Bouncy", p =>
        {
            W(p, q => Mathf.Min(Poly(q, 0.04f, new Vector2(0.12f, 0.3f), new Vector2(0.34f, 0.72f), new Vector2(0.56f, 0.3f), new Vector2(0.72f, 0.62f)),
                Arrow(q, new Vector2(0.72f, 0.62f), new Vector2(0.86f, 0.86f), 0.04f)));
        });
        icons[6] = Icon("Up_Sharp", p =>
        {
            System.Func<Vector2, float> sword = q => Mathf.Min(Seg(q, new Vector2(0.3f, 0.3f), new Vector2(0.8f, 0.8f), 0.055f),
                Mathf.Min(Seg(q, new Vector2(0.2f, 0.4f), new Vector2(0.4f, 0.2f), 0.04f), Seg(q, new Vector2(0.14f, 0.14f), new Vector2(0.3f, 0.3f), 0.04f)));
            W(p, sword);
            p.Paint(q => Circle(q, new Vector2(0.84f, 0.84f), 0.05f), new Color(1f, 1f, 0.7f));
        });
        icons[7] = Icon("Up_Zoomies", p =>
        {
            W(p, q => Mathf.Min(Chevron(q, new Vector2(0.58f, 0.5f), Vector2.right, 0.18f, 0.05f), Chevron(q, new Vector2(0.82f, 0.5f), Vector2.right, 0.18f, 0.05f)));
            W(p, q => Mathf.Min(Seg(q, new Vector2(0.1f, 0.66f), new Vector2(0.3f, 0.66f), 0.03f), Seg(q, new Vector2(0.1f, 0.34f), new Vector2(0.3f, 0.34f), 0.03f)));
        });
        icons[8] = Icon("Up_ChillVibes", p =>
        {
            W(p, q => Poly(q, 0.05f, new Vector2(0.6f, 0.88f), new Vector2(0.36f, 0.5f), new Vector2(0.64f, 0.5f), new Vector2(0.4f, 0.12f)));
        });
        icons[9] = Icon("Up_ThickFur", p =>
        {
            W(p, q => Heart(q, new Vector2(0.5f, 0.46f), 0.56f));
            p.Paint(q => Mathf.Min(Seg(q, new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.6f), 0.035f), Seg(q, new Vector2(0.38f, 0.48f), new Vector2(0.62f, 0.48f), 0.035f)), new Color(0.3f, 0.75f, 0.35f));
        });
        icons[10] = Icon("Up_YuzuSnack", p =>
        {
            p.Shape(q => Circle(q, new Vector2(0.5f, 0.45f), 0.3f), new Color(1f, 0.62f, 0.12f), Ink, 0.04f);
            p.Paint(q => Circle(q, new Vector2(0.4f, 0.55f), 0.07f), new Color(1f, 0.88f, 0.6f, 0.8f));
            p.Shape(q => Box(q, new Vector2(0.6f, 0.8f), new Vector2(0.12f, 0.045f), 0.04f), new Color(0.35f, 0.75f, 0.3f), Ink, 0.03f);
        });
        icons[11] = Icon("Up_YuzuOrbit", p =>
        {
            W(p, q => Ring(q, c, 0.32f, 0.018f));
            W(p, q => Circle(q, c, 0.13f));
            p.Shape(q => Circle(q, new Vector2(0.5f + 0.32f, 0.5f), 0.09f), new Color(1f, 0.62f, 0.12f), Ink, 0.03f);
            p.Shape(q => Circle(q, new Vector2(0.5f - 0.32f, 0.5f), 0.09f), new Color(1f, 0.62f, 0.12f), Ink, 0.03f);
        });
        icons[12] = Icon("Up_Spicy", p =>
        {
            System.Func<Vector2, float> flame = q => SmoothMin(SmoothMin(Circle(q, new Vector2(0.5f, 0.36f), 0.22f), Circle(q, new Vector2(0.5f, 0.55f), 0.14f), 0.1f),
                SmoothMin(Circle(q, new Vector2(0.55f, 0.7f), 0.08f), Circle(q, new Vector2(0.6f, 0.84f), 0.035f), 0.08f), 0.1f);
            W(p, flame);
            p.Paint(q => SmoothMin(Circle(q, new Vector2(0.5f, 0.32f), 0.11f), Circle(q, new Vector2(0.52f, 0.45f), 0.06f), 0.06f), new Color(1f, 0.6f, 0.2f));
        });
        icons[13] = Icon("Up_Onsen", p =>
        {
            W(p, q => Mathf.Max(Ring(q, new Vector2(0.5f, 0.42f), 0.28f, 0.04f), q.y - 0.42f));
            for (int i = 0; i < 3; i++)
            {
                float x = 0.32f + i * 0.18f;
                W(p, q =>
                {
                    float d = float.MaxValue;
                    for (int s = 0; s < 6; s++)
                    {
                        float y0 = 0.46f + s * 0.07f;
                        float y1 = y0 + 0.07f;
                        Vector2 a = new Vector2(x + Mathf.Sin(y0 * 22f) * 0.035f, y0);
                        Vector2 b = new Vector2(x + Mathf.Sin(y1 * 22f) * 0.035f, y1);
                        d = Mathf.Min(d, Seg(q, a, b, 0.025f));
                    }
                    return d;
                });
            }
        });
        icons[14] = Icon("Up_LuckyLeaf", p =>
        {
            System.Func<Vector2, float> clover = q => Mathf.Min(Mathf.Min(Circle(q, new Vector2(0.5f, 0.68f), 0.14f), Circle(q, new Vector2(0.5f, 0.34f), 0.14f)),
                Mathf.Min(Circle(q, new Vector2(0.33f, 0.51f), 0.14f), Circle(q, new Vector2(0.67f, 0.51f), 0.14f)));
            W(p, q => Mathf.Min(clover(q), Seg(q, new Vector2(0.5f, 0.5f), new Vector2(0.72f, 0.12f), 0.035f)));
        });
        icons[15] = Icon("Up_DashMaster", p =>
        {
            W(p, q => Mathf.Min(Chevron(q, new Vector2(0.5f, 0.5f), Vector2.right, 0.22f, 0.06f), Mathf.Min(Chevron(q, new Vector2(0.72f, 0.5f), Vector2.right, 0.22f, 0.06f), Chevron(q, new Vector2(0.94f, 0.5f), Vector2.right, 0.22f, 0.06f))));
        });
        icons[16] = Icon("Up_Munchies", p =>
        {
            System.Func<Vector2, float> cookie = q => Mathf.Max(Mathf.Max(Circle(q, c, 0.33f), -Circle(q, new Vector2(0.8f, 0.74f), 0.12f)), -Circle(q, new Vector2(0.86f, 0.56f), 0.08f));
            p.Shape(cookie, new Color(0.88f, 0.66f, 0.38f), Ink, 0.04f);
            p.Paint(q => Circle(q, new Vector2(0.38f, 0.55f), 0.05f), new Color(0.35f, 0.2f, 0.12f));
            p.Paint(q => Circle(q, new Vector2(0.55f, 0.35f), 0.05f), new Color(0.35f, 0.2f, 0.12f));
            p.Paint(q => Circle(q, new Vector2(0.4f, 0.32f), 0.035f), new Color(0.35f, 0.2f, 0.12f));
            p.Paint(q => Circle(q, new Vector2(0.6f, 0.58f), 0.035f), new Color(0.35f, 0.2f, 0.12f));
        });
        return icons;
    }
}
