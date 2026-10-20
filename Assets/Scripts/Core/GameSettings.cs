using UnityEngine;

// Pengaturan yang dipilih di menu utama dan disimpan di PlayerPrefs.
public static class GameSettings
{
    private const string DifficultyKey = "capy_difficulty";

    public static readonly string[] DifficultyNames = { "EASY", "NORMAL", "HARD" };
    private static readonly float[] HealthMultipliers = { 0.7f, 1f, 1.4f };
    private static readonly float[] DamageMultipliers = { 0.6f, 1f, 1.3f };

    // 0 = Easy, 1 = Normal, 2 = Hard.
    public static int Difficulty
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(DifficultyKey, 1), 0, 2); }
        set
        {
            PlayerPrefs.SetInt(DifficultyKey, Mathf.Clamp(value, 0, 2));
            PlayerPrefs.Save();
        }
    }

    public static string DifficultyName => DifficultyNames[Difficulty];
    public static float EnemyHealthMultiplier => HealthMultipliers[Difficulty];
    public static float EnemyDamageMultiplier => DamageMultipliers[Difficulty];
}
