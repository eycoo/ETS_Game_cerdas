using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MusicTrack
{
    None,
    Menu,
    Dungeon,
    Boss,
    Victory
}

// Semua suara dibuat secara prosedural (lihat Editor/CapyAudioGen.cs) dan diputar dari sini.
// Objek ini dibuat otomatis dan bertahan antar-scene.
public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    public static bool MusicOn { get; private set; } = true;
    public static bool SfxOn { get; private set; } = true;

    private const float MusicVolume = 0.42f;
    private const float SfxVolume = 0.75f;

    private AudioSource musicA;
    private AudioSource musicB;
    private bool aIsActive;
    private MusicTrack currentTrack = MusicTrack.None;
    private Coroutine fadeRoutine;
    private readonly List<AudioSource> pool = new List<AudioSource>();
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    private int roundRobin;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        I = null;
    }

    private static AudioManager Ensure()
    {
        if (I != null)
            return I;

        GameObject go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        I = go.AddComponent<AudioManager>();
        I.Init();
        return I;
    }

    private void Init()
    {
        MusicOn = PlayerPrefs.GetInt("capy_music", 1) == 1;
        SfxOn = PlayerPrefs.GetInt("capy_sfx", 1) == 1;

        musicA = CreateSource(true);
        musicB = CreateSource(true);
        for (int i = 0; i < 20; i++)
            pool.Add(CreateSource(false));

        GameDatabase db = GameDatabase.I;
        if (db != null && db.sfx != null)
        {
            foreach (AudioClip clip in db.sfx)
            {
                if (clip != null)
                    clips[clip.name] = clip;
            }
        }
    }

    private AudioSource CreateSource(bool loop)
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        src.volume = 0f;
        return src;
    }

    public static void Sfx(string name, float volume = 1f, float pitch = 1f, float jitter = 0.06f)
    {
        AudioManager m = Ensure();
        if (!SfxOn)
            return;

        AudioClip clip;
        if (!m.clips.TryGetValue(name, out clip))
            return;

        // Suara yang sama tidak boleh menumpuk dalam satu frame (misal 6 musuh kena sekaligus).
        float now = Time.unscaledTime;
        float last;
        if (m.lastPlayed.TryGetValue(name, out last) && now - last < 0.04f)
            return;
        m.lastPlayed[name] = now;

        AudioSource src = m.GetFreeSource();
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume * SfxVolume);
        src.pitch = pitch * (1f + Random.Range(-jitter, jitter));
        src.Play();
    }

    private AudioSource GetFreeSource()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].isPlaying)
                return pool[i];
        }
        roundRobin = (roundRobin + 1) % pool.Count;
        return pool[roundRobin];
    }

    public static void Music(MusicTrack track, float fade = 1.2f)
    {
        AudioManager m = Ensure();
        if (m.currentTrack == track)
            return;
        m.currentTrack = track;

        AudioClip clip = null;
        GameDatabase db = GameDatabase.I;
        if (db != null)
        {
            switch (track)
            {
                case MusicTrack.Menu: clip = db.musicMenu; break;
                case MusicTrack.Dungeon: clip = db.musicDungeon; break;
                case MusicTrack.Boss: clip = db.musicBoss; break;
                case MusicTrack.Victory: clip = db.musicVictory; break;
            }
        }

        if (m.fadeRoutine != null)
            m.StopCoroutine(m.fadeRoutine);
        m.fadeRoutine = m.StartCoroutine(m.Crossfade(clip, fade, track != MusicTrack.Victory));
    }

    private IEnumerator Crossfade(AudioClip clip, float duration, bool loop)
    {
        AudioSource from = aIsActive ? musicA : musicB;
        AudioSource to = aIsActive ? musicB : musicA;
        aIsActive = !aIsActive;

        float fromStart = from.volume;
        if (clip != null)
        {
            to.clip = clip;
            to.loop = loop;
            to.volume = 0f;
            to.Play();
        }

        float target = MusicOn ? MusicVolume : 0f;
        float t = 0f;
        duration = Mathf.Max(0.05f, duration);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            from.volume = Mathf.Lerp(fromStart, 0f, k);
            if (clip != null)
                to.volume = Mathf.Lerp(0f, target, k);
            yield return null;
        }

        from.Stop();
        if (clip != null)
            to.volume = target;
        fadeRoutine = null;
    }

    public static void SetMusicOn(bool on)
    {
        AudioManager m = Ensure();
        MusicOn = on;
        PlayerPrefs.SetInt("capy_music", on ? 1 : 0);
        AudioSource active = m.aIsActive ? m.musicB : m.musicA;
        if (m.fadeRoutine == null)
            active.volume = on ? MusicVolume : 0f;
    }

    public static void SetSfxOn(bool on)
    {
        Ensure();
        SfxOn = on;
        PlayerPrefs.SetInt("capy_sfx", on ? 1 : 0);
    }
}
