using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Synthesizer kecil untuk membuat semua efek suara dan musik secara prosedural (WAV).
// Tidak ada aset audio dari luar, jadi game bebas lisensi.
public static class CapyAudioGen
{
    public const string Folder = "Assets/Art/Audio";
    private const int SR = 44100;

    private enum Wave
    {
        Sine,
        Square,
        Saw,
        Triangle,
        Noise
    }

    private static System.Random rng = new System.Random(1234);

    private static float Rand()
    {
        return (float)rng.NextDouble() * 2f - 1f;
    }

    private static float[] Buf(float seconds)
    {
        return new float[Mathf.CeilToInt(SR * seconds)];
    }

    private static float Osc(Wave w, double phase, float duty = 0.5f)
    {
        double ph = phase - System.Math.Floor(phase);
        switch (w)
        {
            case Wave.Sine: return Mathf.Sin((float)(ph * System.Math.PI * 2.0));
            case Wave.Square: return ph < duty ? 0.8f : -0.8f;
            case Wave.Saw: return (float)(ph * 2.0 - 1.0) * 0.8f;
            case Wave.Triangle: return (float)(System.Math.Abs(ph - 0.5) * 4.0 - 1.0);
            default: return Rand();
        }
    }

    // Satu nada dengan selisih pitch (f0 -> f1), envelope attack/decay, dan vibrato opsional.
    private static void Tone(float[] b, float start, float dur, float f0, float f1, Wave w, float vol, float attack = 0.005f, float release = 0.05f, float vibrato = 0f, float vibRate = 6f, float duty = 0.5f)
    {
        int s0 = Mathf.Max(0, (int)(start * SR));
        int n = (int)(dur * SR);
        double phase = 0.0;
        for (int i = 0; i < n && s0 + i < b.Length; i++)
        {
            float t = i / (float)SR;
            float k = n > 1 ? i / (float)(n - 1) : 0f;
            float f = Mathf.Lerp(f0, f1, k);
            if (vibrato > 0f)
                f *= 1f + Mathf.Sin(t * vibRate * Mathf.PI * 2f) * vibrato;
            phase += f / SR;
            float env = Mathf.Min(1f, t / Mathf.Max(0.0001f, attack));
            float rel = Mathf.Clamp01((dur - t) / Mathf.Max(0.0001f, release));
            b[s0 + i] += Osc(w, phase, duty) * vol * env * rel;
        }
    }

    // Nada dengan decay eksponensial (pluck / bell).
    private static void Pluck(float[] b, float start, float dur, float f, Wave w, float vol, float decay, float duty = 0.5f)
    {
        int s0 = Mathf.Max(0, (int)(start * SR));
        int n = (int)(dur * SR);
        double phase = 0.0;
        for (int i = 0; i < n && s0 + i < b.Length; i++)
        {
            float t = i / (float)SR;
            phase += f / SR;
            float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t * decay);
            float rel = Mathf.Clamp01((dur - t) / 0.02f);
            b[s0 + i] += Osc(w, phase, duty) * vol * env * rel;
        }
    }

    // Derau dengan low-pass satu kutub (cutoff 0..1, makin kecil makin "berat").
    private static void Noise(float[] b, float start, float dur, float vol, float attack, float release, float cutStart, float cutEnd)
    {
        int s0 = Mathf.Max(0, (int)(start * SR));
        int n = (int)(dur * SR);
        float lp = 0f;
        for (int i = 0; i < n && s0 + i < b.Length; i++)
        {
            float t = i / (float)SR;
            float k = n > 1 ? i / (float)(n - 1) : 0f;
            float cut = Mathf.Lerp(cutStart, cutEnd, k);
            lp += (Rand() - lp) * cut;
            float env = Mathf.Min(1f, t / Mathf.Max(0.0001f, attack));
            float rel = Mathf.Clamp01((dur - t) / Mathf.Max(0.0001f, release));
            b[s0 + i] += lp * vol * env * rel;
        }
    }

    private static void LowPass(float[] b, float cut)
    {
        float lp = 0f;
        for (int i = 0; i < b.Length; i++)
        {
            lp += (b[i] - lp) * cut;
            b[i] = lp;
        }
    }

    private static void Echo(float[] b, float delay, float feedback)
    {
        int d = (int)(delay * SR);
        for (int i = d; i < b.Length; i++)
            b[i] += b[i - d] * feedback;
    }

    private static void Normalize(float[] b, float peak)
    {
        float max = 0.0001f;
        for (int i = 0; i < b.Length; i++)
            max = Mathf.Max(max, Mathf.Abs(b[i]));
        float k = peak / max;
        for (int i = 0; i < b.Length; i++)
            b[i] = Mathf.Clamp(b[i] * k, -1f, 1f);
    }

    private static void SoftClip(float[] b)
    {
        for (int i = 0; i < b.Length; i++)
            b[i] = (float)System.Math.Tanh(b[i]);
    }

    private static string WriteWav(string name, float[] data)
    {
        CapyEditorUtil.EnsureFolder(Folder);
        string path = Folder + "/" + name + ".wav";
        using (FileStream fs = new FileStream(path, FileMode.Create))
        using (BinaryWriter w = new BinaryWriter(fs))
        {
            int byteCount = data.Length * 2;
            w.Write(new char[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + byteCount);
            w.Write(new char[] { 'W', 'A', 'V', 'E' });
            w.Write(new char[] { 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);
            w.Write((short)1);
            w.Write(SR);
            w.Write(SR * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(new char[] { 'd', 'a', 't', 'a' });
            w.Write(byteCount);
            for (int i = 0; i < data.Length; i++)
                w.Write((short)(Mathf.Clamp(data[i], -1f, 1f) * 32000f));
        }
        return path;
    }

    private static AudioClip Finish(string name, float[] b, float peak, bool music)
    {
        Normalize(b, peak);
        string path = WriteWav(name, b);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        AudioImporter imp = (AudioImporter)AssetImporter.GetAtPath(path);
        AudioImporterSampleSettings s = imp.defaultSampleSettings;
        if (music)
        {
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
        }
        else
        {
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.ADPCM;
        }
        imp.defaultSampleSettings = s;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static float Note(string name)
    {
        // Contoh: "A4", "C#5", "Bb3"
        string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        string n = name.Replace("Db", "C#").Replace("Eb", "D#").Replace("Gb", "F#").Replace("Ab", "G#").Replace("Bb", "A#");
        int octave = n[n.Length - 1] - '0';
        string key = n.Substring(0, n.Length - 1);
        int idx = System.Array.IndexOf(names, key);
        int midi = (octave + 1) * 12 + idx;
        return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }

    // ================================================================== SFX

    public static List<AudioClip> GenerateSfx()
    {
        List<AudioClip> clips = new List<AudioClip>();
        float[] b;

        b = Buf(0.22f);
        Noise(b, 0f, 0.2f, 0.7f, 0.02f, 0.12f, 0.08f, 0.5f);
        Tone(b, 0.02f, 0.12f, 1400f, 2100f, Wave.Sine, 0.25f, 0.005f, 0.08f);
        clips.Add(Finish("swing", b, 0.8f, false));

        b = Buf(0.16f);
        Tone(b, 0f, 0.12f, 210f, 70f, Wave.Sine, 1f, 0.002f, 0.06f);
        Noise(b, 0f, 0.04f, 0.6f, 0.001f, 0.03f, 0.6f, 0.2f);
        clips.Add(Finish("hit", b, 0.85f, false));

        b = Buf(0.3f);
        Tone(b, 0f, 0.12f, 240f, 70f, Wave.Sine, 1f, 0.002f, 0.06f);
        Noise(b, 0f, 0.05f, 0.7f, 0.001f, 0.04f, 0.7f, 0.3f);
        Pluck(b, 0.01f, 0.28f, 1250f, Wave.Triangle, 0.5f, 18f);
        Pluck(b, 0.01f, 0.28f, 1875f, Wave.Sine, 0.3f, 22f);
        clips.Add(Finish("crit", b, 0.9f, false));

        b = Buf(0.3f);
        Tone(b, 0f, 0.16f, 620f, 120f, Wave.Sine, 0.9f, 0.002f, 0.08f);
        Noise(b, 0f, 0.22f, 0.6f, 0.002f, 0.15f, 0.35f, 0.05f);
        clips.Add(Finish("enemy_die", b, 0.85f, false));

        b = Buf(0.22f);
        Tone(b, 0f, 0.2f, 200f, 420f, Wave.Sine, 0.8f, 0.01f, 0.1f, 0.05f, 18f);
        clips.Add(Finish("slime_hop", b, 0.7f, false));

        b = Buf(0.35f);
        Tone(b, 0f, 0.22f, 420f, 160f, Wave.Square, 0.5f, 0.003f, 0.1f);
        Tone(b, 0f, 0.22f, 210f, 80f, Wave.Sine, 0.8f, 0.003f, 0.1f);
        Noise(b, 0f, 0.12f, 0.5f, 0.001f, 0.08f, 0.5f, 0.1f);
        LowPass(b, 0.5f);
        clips.Add(Finish("player_hurt", b, 0.9f, false));

        b = Buf(0.28f);
        Noise(b, 0f, 0.26f, 0.9f, 0.04f, 0.16f, 0.04f, 0.4f);
        clips.Add(Finish("dash", b, 0.75f, false));

        b = Buf(0.12f);
        Pluck(b, 0f, 0.12f, 1567f, Wave.Sine, 0.7f, 30f);
        Pluck(b, 0.03f, 0.09f, 2349f, Wave.Sine, 0.45f, 35f);
        clips.Add(Finish("xp", b, 0.7f, false));

        b = Buf(1.0f);
        string[] lvl = { "C5", "E5", "G5", "C6", "E6" };
        for (int i = 0; i < lvl.Length; i++)
        {
            Pluck(b, i * 0.07f, 0.5f, Note(lvl[i]), Wave.Square, 0.25f, 6f, 0.3f);
            Pluck(b, i * 0.07f, 0.5f, Note(lvl[i]) * 2f, Wave.Sine, 0.2f, 8f);
        }
        Tone(b, 0.35f, 0.6f, Note("C6"), Note("C6"), Wave.Triangle, 0.3f, 0.01f, 0.4f, 0.01f, 7f);
        Echo(b, 0.11f, 0.3f);
        LowPass(b, 0.6f);
        clips.Add(Finish("levelup", b, 0.85f, false));

        b = Buf(0.8f);
        string[] heal = { "G5", "B5", "D6", "G6" };
        for (int i = 0; i < heal.Length; i++)
            Pluck(b, i * 0.06f, 0.5f, Note(heal[i]), Wave.Sine, 0.4f, 7f);
        Echo(b, 0.09f, 0.35f);
        clips.Add(Finish("heal", b, 0.7f, false));

        b = Buf(0.7f);
        Noise(b, 0f, 0.6f, 0.9f, 0.05f, 0.3f, 0.03f, 0.02f);
        Tone(b, 0.45f, 0.2f, 90f, 50f, Wave.Sine, 0.8f, 0.002f, 0.1f);
        clips.Add(Finish("door", b, 0.85f, false));

        b = Buf(0.8f);
        Tone(b, 0f, 0.7f, 220f, 880f, Wave.Sine, 0.5f, 0.05f, 0.25f, 0.06f, 14f);
        Tone(b, 0.05f, 0.65f, 330f, 1320f, Wave.Triangle, 0.25f, 0.05f, 0.25f, 0.04f, 11f);
        Noise(b, 0f, 0.7f, 0.25f, 0.2f, 0.3f, 0.1f, 0.4f);
        Echo(b, 0.12f, 0.3f);
        clips.Add(Finish("spawn", b, 0.7f, false));

        b = Buf(0.7f);
        Tone(b, 0f, 0.65f, 300f, 950f, Wave.Sine, 0.6f, 0.05f, 0.1f, 0.03f, 20f);
        Tone(b, 0f, 0.65f, 450f, 1400f, Wave.Triangle, 0.2f, 0.05f, 0.1f, 0.03f, 23f);
        clips.Add(Finish("magic_charge", b, 0.6f, false));

        b = Buf(0.35f);
        Tone(b, 0f, 0.25f, 1100f, 300f, Wave.Square, 0.35f, 0.002f, 0.15f, 0f, 0f, 0.25f);
        Tone(b, 0f, 0.25f, 550f, 150f, Wave.Sine, 0.6f, 0.002f, 0.15f);
        Noise(b, 0f, 0.15f, 0.3f, 0.001f, 0.1f, 0.6f, 0.2f);
        LowPass(b, 0.55f);
        Echo(b, 0.08f, 0.25f);
        clips.Add(Finish("magic_cast", b, 0.75f, false));

        b = Buf(0.3f);
        Pluck(b, 0f, 0.25f, 520f, Wave.Triangle, 0.7f, 22f);
        Tone(b, 0f, 0.12f, 700f, 240f, Wave.Sine, 0.4f, 0.001f, 0.08f);
        Noise(b, 0f, 0.1f, 0.4f, 0.001f, 0.08f, 0.3f, 0.6f);
        clips.Add(Finish("arrow", b, 0.75f, false));

        b = Buf(0.5f);
        Noise(b, 0f, 0.45f, 0.4f, 0.3f, 0.1f, 0.05f, 0.15f);
        Tone(b, 0f, 0.45f, 180f, 260f, Wave.Saw, 0.12f, 0.2f, 0.1f);
        LowPass(b, 0.3f);
        clips.Add(Finish("bow_draw", b, 0.5f, false));

        b = Buf(0.4f);
        for (int i = 0; i < 9; i++)
        {
            float t = i * 0.038f + (float)rng.NextDouble() * 0.01f;
            Noise(b, t, 0.025f, 0.8f, 0.001f, 0.02f, 0.7f, 0.5f);
            Pluck(b, t, 0.04f, 900f + (float)rng.NextDouble() * 900f, Wave.Triangle, 0.25f, 60f);
        }
        clips.Add(Finish("rattle", b, 0.65f, false));

        b = Buf(0.9f);
        Tone(b, 0f, 0.7f, 110f, 30f, Wave.Sine, 1f, 0.002f, 0.4f);
        Noise(b, 0f, 0.8f, 0.8f, 0.002f, 0.6f, 0.25f, 0.02f);
        SoftClip(b);
        clips.Add(Finish("slam", b, 0.95f, false));

        b = Buf(0.4f);
        Tone(b, 0f, 0.2f, 140f, 50f, Wave.Sine, 0.9f, 0.002f, 0.1f);
        Noise(b, 0f, 0.3f, 0.7f, 0.001f, 0.2f, 0.4f, 0.05f);
        clips.Add(Finish("rock_land", b, 0.85f, false));

        b = Buf(1.4f);
        Tone(b, 0f, 1.3f, 85f, 70f, Wave.Saw, 0.6f, 0.15f, 0.5f, 0.03f, 9f);
        Tone(b, 0f, 1.3f, 128f, 100f, Wave.Saw, 0.35f, 0.15f, 0.5f, 0.04f, 7f);
        Noise(b, 0f, 1.3f, 0.5f, 0.2f, 0.5f, 0.12f, 0.05f);
        LowPass(b, 0.18f);
        SoftClip(b);
        clips.Add(Finish("roar", b, 0.95f, false));

        b = Buf(1.1f);
        Noise(b, 0f, 1.05f, 1f, 0.3f, 0.4f, 0.02f, 0.03f);
        Tone(b, 0f, 1.05f, 45f, 55f, Wave.Sine, 0.5f, 0.3f, 0.4f);
        clips.Add(Finish("rumble", b, 0.85f, false));

        b = Buf(0.7f);
        Noise(b, 0f, 0.65f, 0.9f, 0.05f, 0.3f, 0.03f, 0.25f);
        Tone(b, 0f, 0.6f, 60f, 140f, Wave.Saw, 0.3f, 0.05f, 0.3f);
        LowPass(b, 0.3f);
        clips.Add(Finish("charge", b, 0.85f, false));

        b = Buf(0.3f);
        Noise(b, 0f, 0.28f, 0.8f, 0.05f, 0.15f, 0.05f, 0.3f);
        clips.Add(Finish("throw", b, 0.7f, false));

        b = Buf(0.45f);
        Tone(b, 0f, 0.4f, 300f, 1300f, Wave.Sine, 0.6f, 0.01f, 0.15f, 0.08f, 30f);
        Tone(b, 0f, 0.4f, 1300f, 300f, Wave.Sine, 0.3f, 0.01f, 0.15f, 0.08f, 25f);
        Echo(b, 0.07f, 0.3f);
        clips.Add(Finish("teleport", b, 0.65f, false));

        b = Buf(0.9f);
        Tone(b, 0f, 0.8f, 110f, 110f, Wave.Saw, 0.3f, 0.3f, 0.3f, 0.01f, 5f);
        Tone(b, 0f, 0.8f, 164.8f, 164.8f, Wave.Saw, 0.25f, 0.3f, 0.3f, 0.01f, 5.5f);
        Tone(b, 0f, 0.8f, 233f, 233f, Wave.Saw, 0.2f, 0.3f, 0.3f, 0.01f, 6f);
        LowPass(b, 0.12f);
        Echo(b, 0.15f, 0.3f);
        clips.Add(Finish("summon", b, 0.7f, false));

        b = Buf(0.35f);
        Tone(b, 0f, 0.12f, 1900f, 2700f, Wave.Square, 0.3f, 0.002f, 0.05f, 0.04f, 40f, 0.3f);
        Tone(b, 0.14f, 0.14f, 2100f, 2900f, Wave.Square, 0.3f, 0.002f, 0.06f, 0.04f, 40f, 0.3f);
        LowPass(b, 0.45f);
        clips.Add(Finish("bat_screech", b, 0.6f, false));

        b = Buf(1.4f);
        string[] chest = { "G4", "C5", "E5", "G5", "C6", "E6", "G6" };
        for (int i = 0; i < chest.Length; i++)
            Pluck(b, i * 0.06f, 0.8f, Note(chest[i]), Wave.Triangle, 0.35f, 5f);
        for (int i = 0; i < 12; i++)
            Pluck(b, 0.4f + i * 0.05f, 0.3f, 2000f + (float)rng.NextDouble() * 3000f, Wave.Sine, 0.08f, 20f);
        Echo(b, 0.13f, 0.35f);
        clips.Add(Finish("chest", b, 0.8f, false));

        b = Buf(0.8f);
        Noise(b, 0f, 0.5f, 0.9f, 0.005f, 0.35f, 0.25f, 0.05f);
        for (int i = 0; i < 10; i++)
        {
            float t = 0.05f + (float)rng.NextDouble() * 0.6f;
            float f = 500f + (float)rng.NextDouble() * 900f;
            Tone(b, t, 0.05f, f, f * 1.6f, Wave.Sine, 0.25f, 0.002f, 0.03f);
        }
        clips.Add(Finish("splash", b, 0.75f, false));

        b = Buf(0.05f);
        Pluck(b, 0f, 0.05f, 1050f, Wave.Sine, 0.8f, 60f);
        clips.Add(Finish("click", b, 0.6f, false));

        b = Buf(0.04f);
        Pluck(b, 0f, 0.04f, 1500f, Wave.Sine, 0.6f, 90f);
        clips.Add(Finish("hover", b, 0.35f, false));

        b = Buf(0.25f);
        Pluck(b, 0f, 0.12f, Note("E5"), Wave.Square, 0.3f, 18f, 0.3f);
        Pluck(b, 0.08f, 0.17f, Note("A5"), Wave.Square, 0.3f, 14f, 0.3f);
        LowPass(b, 0.5f);
        clips.Add(Finish("select", b, 0.7f, false));

        b = Buf(1.6f);
        string[] fan = { "C5", "E5", "G5" };
        for (int i = 0; i < fan.Length; i++)
            Pluck(b, i * 0.11f, 0.3f, Note(fan[i]), Wave.Square, 0.3f, 8f, 0.25f);
        Tone(b, 0.33f, 1.0f, Note("C6"), Note("C6"), Wave.Square, 0.25f, 0.01f, 0.5f, 0.012f, 6f, 0.25f);
        Tone(b, 0.33f, 1.0f, Note("E5"), Note("E5"), Wave.Triangle, 0.3f, 0.01f, 0.5f);
        Tone(b, 0.33f, 1.0f, Note("G5"), Note("G5"), Wave.Triangle, 0.25f, 0.01f, 0.5f);
        LowPass(b, 0.45f);
        Echo(b, 0.14f, 0.3f);
        clips.Add(Finish("room_clear", b, 0.85f, false));

        b = Buf(2.4f);
        string[] sad = { "G4", "F4", "E4", "C4" };
        for (int i = 0; i < sad.Length; i++)
        {
            float d = i == 3 ? 1.1f : 0.4f;
            Tone(b, i * 0.42f, d, Note(sad[i]), Note(sad[i]) * (i == 3 ? 0.98f : 1f), Wave.Triangle, 0.6f, 0.01f, 0.3f, 0.01f, 5f);
            Tone(b, i * 0.42f, d, Note(sad[i]) * 0.5f, Note(sad[i]) * 0.5f, Wave.Sine, 0.4f, 0.01f, 0.3f);
        }
        Echo(b, 0.2f, 0.3f);
        clips.Add(Finish("gameover", b, 0.8f, false));

        b = Buf(2.2f);
        Noise(b, 0f, 2.1f, 1f, 0.01f, 1.6f, 0.3f, 0.01f);
        Tone(b, 0f, 2f, 90f, 25f, Wave.Sine, 1f, 0.01f, 1f);
        Tone(b, 0f, 1.5f, 60f, 30f, Wave.Saw, 0.3f, 0.01f, 1f);
        LowPass(b, 0.3f);
        SoftClip(b);
        clips.Add(Finish("boss_die", b, 0.95f, false));

        return clips;
    }

    // ================================================================== musik

    private class Track
    {
        public float bpm;
        public string[] chords;      // satu akor per bar, contoh "Am"
        public string[] melody;      // not per 1/8, "-" = jeda, "~" = tahan
        public bool drums;
        public bool fastHats;
        public float leadVol = 0.22f;
        public float arpVol = 0.1f;
        public float bassVol = 0.3f;
        public Wave lead = Wave.Square;
        public float leadDuty = 0.3f;
        public bool bellLead;
    }

    private static readonly Dictionary<string, string[]> ChordNotes = new Dictionary<string, string[]>
    {
        { "Am", new[] { "A3", "C4", "E4" } },
        { "F", new[] { "F3", "A3", "C4" } },
        { "C", new[] { "C4", "E4", "G4" } },
        { "G", new[] { "G3", "B3", "D4" } },
        { "E", new[] { "E3", "G#3", "B3" } },
        { "Dm", new[] { "D4", "F4", "A4" } },
        { "Bb", new[] { "A#3", "D4", "F4" } },
        { "A", new[] { "A3", "C#4", "E4" } },
        { "Gm", new[] { "G3", "A#3", "D4" } }
    };

    private static float[] Render(Track tr, int loops)
    {
        float beat = 60f / tr.bpm;
        float eighth = beat * 0.5f;
        int bars = tr.chords.Length;
        float barLen = beat * 4f;
        float total = barLen * bars * loops;
        float[] b = Buf(total + 0.01f);
        float[] lead = Buf(total + 1.5f);

        for (int loop = 0; loop < loops; loop++)
        {
            for (int bar = 0; bar < bars; bar++)
            {
                float t0 = (loop * bars + bar) * barLen;
                string[] notes = ChordNotes[tr.chords[bar]];
                float root = Note(notes[0]) * 0.5f;

                // Bass: delapanan, akar dan oktaf bergantian.
                for (int s = 0; s < 8; s++)
                {
                    float f = (s % 4 == 2) ? root * 2f : root;
                    if (!tr.drums && s % 2 == 1)
                        continue;
                    Pluck(b, t0 + s * eighth, eighth * 0.95f, f, Wave.Triangle, tr.bassVol, tr.drums ? 7f : 3f);
                }

                // Arpeggio: 1/16 berputar di nada akor.
                for (int s = 0; s < 16; s++)
                {
                    int idx = (s % 4 == 3) ? 1 : s % 4 % 3;
                    float f = Note(notes[idx]) * (s >= 8 && loop % 2 == 1 ? 2f : 1f);
                    if (tr.bellLead)
                        Pluck(b, t0 + s * eighth * 0.5f, eighth, f * 2f, Wave.Sine, tr.arpVol, 9f);
                    else
                        Pluck(b, t0 + s * eighth * 0.5f, eighth * 0.5f, f, Wave.Square, tr.arpVol, 14f, 0.25f);
                }

                // Pad lembut.
                for (int k = 0; k < 3; k++)
                    Tone(b, t0, barLen, Note(notes[k]), Note(notes[k]), Wave.Triangle, 0.035f, 0.2f, 0.3f, 0.004f, 4f);

                if (tr.drums)
                {
                    for (int q = 0; q < 4; q++)
                    {
                        float bt = t0 + q * beat;
                        if (q == 0 || q == 2 || (tr.fastHats && q == 3))
                        {
                            Tone(b, bt, 0.18f, 150f, 45f, Wave.Sine, 0.65f, 0.001f, 0.08f);
                            if (tr.fastHats && q == 2)
                                Tone(b, bt + eighth, 0.15f, 150f, 45f, Wave.Sine, 0.45f, 0.001f, 0.08f);
                        }
                        if (q == 1 || q == 3)
                        {
                            Noise(b, bt, 0.16f, 0.35f, 0.001f, 0.12f, 0.6f, 0.3f);
                            Tone(b, bt, 0.08f, 240f, 160f, Wave.Triangle, 0.2f, 0.001f, 0.05f);
                        }
                        int hats = tr.fastHats ? 4 : 2;
                        for (int h = 0; h < hats; h++)
                        {
                            float ht = bt + h * beat / hats;
                            Noise(b, ht, 0.03f, 0.1f * (h % 2 == 0 ? 1f : 0.6f), 0.001f, 0.025f, 0.95f, 0.95f);
                        }
                    }
                }

                // Melodi.
                for (int s = 0; s < 8; s++)
                {
                    int mi = bar * 8 + s;
                    if (mi >= tr.melody.Length)
                        continue;
                    string m = tr.melody[mi];
                    if (m == "-" || m == "~")
                        continue;
                    int len = 1;
                    while (mi + len < tr.melody.Length && tr.melody[mi + len] == "~" && len < 8)
                        len++;
                    float f = Note(m);
                    float st = t0 + s * eighth;
                    float dur = eighth * len * 0.92f;
                    if (tr.bellLead)
                    {
                        Pluck(lead, st, dur + 0.5f, f, Wave.Sine, tr.leadVol, 3.5f);
                        Pluck(lead, st, dur + 0.3f, f * 2f, Wave.Sine, tr.leadVol * 0.3f, 6f);
                    }
                    else
                    {
                        float vol = loop % 2 == 1 ? tr.leadVol : tr.leadVol * 0.9f;
                        Tone(lead, st, dur, f, f, tr.lead, vol, 0.006f, 0.05f, 0.006f, 5.5f, tr.leadDuty);
                        if (loop % 2 == 1)
                            Tone(lead, st, dur, f * 0.5f, f * 0.5f, Wave.Triangle, vol * 0.5f, 0.006f, 0.05f);
                    }
                }
            }
        }

        LowPass(lead, 0.35f);
        Echo(lead, beat * 0.75f, 0.28f);
        for (int i = 0; i < b.Length; i++)
            b[i] += lead[i];
        // Ekor gema dari akhir lagu disambung ke awal supaya loop mulus.
        for (int i = b.Length; i < lead.Length; i++)
            b[i - b.Length] += lead[i];
        LowPass(b, 0.55f);
        return b;
    }

    public static AudioClip[] GenerateMusic()
    {
        Track dungeon = new Track();
        dungeon.bpm = 112f;
        dungeon.chords = new[] { "Am", "F", "C", "G", "Am", "F", "G", "E" };
        dungeon.melody = new[]
        {
            "A4", "-", "C5", "E5", "D5", "C5", "B4", "C5",
            "A4", "~", "F4", "A4", "C5", "~", "A4", "-",
            "G4", "-", "C5", "E5", "G5", "~", "E5", "D5",
            "D5", "~", "B4", "G4", "B4", "D5", "~", "-",
            "E5", "-", "A5", "G5", "E5", "C5", "D5", "E5",
            "F5", "~", "E5", "D5", "C5", "-", "A4", "C5",
            "B4", "-", "D5", "G5", "F5", "E5", "D5", "B4",
            "G#4", "~", "B4", "E5", "G#5", "~", "E5", "-"
        };
        dungeon.drums = true;

        Track boss = new Track();
        boss.bpm = 140f;
        boss.chords = new[] { "Dm", "Bb", "C", "A", "Dm", "Bb", "Gm", "A" };
        boss.melody = new[]
        {
            "D5", "D5", "F5", "D5", "A5", "~", "G5", "F5",
            "F5", "~", "D5", "-", "A#4", "D5", "F5", "-",
            "E5", "E5", "G5", "E5", "C6", "~", "A#5", "A5",
            "A5", "~", "E5", "-", "C#5", "E5", "A5", "-",
            "D6", "~", "C6", "A5", "F5", "-", "A5", "D6",
            "D6", "~", "A#5", "F5", "D5", "F5", "A#5", "-",
            "G5", "-", "A#5", "D6", "C6", "A#5", "A5", "G5",
            "A5", "~", "C#6", "~", "E6", "~", "A5", "-"
        };
        boss.drums = true;
        boss.fastHats = true;
        boss.leadVol = 0.2f;
        boss.lead = Wave.Saw;
        boss.bassVol = 0.34f;

        Track menu = new Track();
        menu.bpm = 84f;
        menu.chords = new[] { "C", "Am", "F", "G", "C", "Am", "Dm", "G" };
        menu.melody = new[]
        {
            "E5", "-", "G5", "-", "C6", "~", "B5", "G5",
            "A5", "~", "~", "-", "E5", "-", "G5", "-",
            "F5", "-", "A5", "-", "C6", "~", "A5", "F5",
            "G5", "~", "~", "-", "D5", "~", "~", "-",
            "E5", "-", "G5", "-", "C6", "~", "D6", "E6",
            "C6", "~", "~", "-", "A5", "-", "B5", "C6",
            "D6", "-", "A5", "-", "F5", "~", "A5", "-",
            "G5", "-", "B5", "-", "D6", "~", "~", "-"
        };
        menu.drums = false;
        menu.bellLead = true;
        menu.leadVol = 0.28f;
        menu.arpVol = 0.06f;
        menu.bassVol = 0.25f;

        AudioClip dungeonClip = Finish("music_dungeon", Render(dungeon, 2), 0.8f, true);
        AudioClip bossClip = Finish("music_boss", Render(boss, 2), 0.82f, true);
        AudioClip menuClip = Finish("music_menu", Render(menu, 2), 0.75f, true);

        // Jingle kemenangan.
        float[] v = Buf(4.5f);
        string[] fan = { "G4", "C5", "E5", "G5" };
        for (int i = 0; i < fan.Length; i++)
            Pluck(v, i * 0.14f, 0.3f, Note(fan[i]), Wave.Square, 0.3f, 6f, 0.25f);
        string[] chord = { "C5", "E5", "G5", "C6" };
        for (int i = 0; i < chord.Length; i++)
            Tone(v, 0.6f, 2.4f, Note(chord[i]), Note(chord[i]), i == 3 ? Wave.Square : Wave.Triangle, 0.22f, 0.02f, 1.2f, 0.01f, 5.5f, 0.25f);
        Tone(v, 0.6f, 2.4f, Note("C3"), Note("C3"), Wave.Triangle, 0.4f, 0.02f, 1.2f);
        for (int i = 0; i < 16; i++)
            Pluck(v, 0.7f + i * 0.09f, 0.4f, Note(chord[i % 4]) * 2f, Wave.Sine, 0.12f, 9f);
        LowPass(v, 0.5f);
        Echo(v, 0.18f, 0.35f);
        AudioClip victoryClip = Finish("music_victory", v, 0.85f, true);

        return new[] { menuClip, dungeonClip, bossClip, victoryClip };
    }
}
