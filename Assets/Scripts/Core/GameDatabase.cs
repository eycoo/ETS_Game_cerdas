using UnityEngine;

public enum EnemyType
{
    SlimeBig,
    SlimeSmall,
    SkeletonWarrior,
    SkeletonArcher,
    Wizard,
    Bat,
    GolemBoss
}

// Satu tempat untuk semua prefab, material, font, sprite, dan audio.
// Asset-nya ada di Assets/Resources/GameDatabase.asset dan diisi oleh menu "Capy Conquest/Build All".
[CreateAssetMenu(menuName = "Capy Conquest/Game Database")]
public class GameDatabase : ScriptableObject
{
    private static GameDatabase instance;

    public static GameDatabase I
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<GameDatabase>("GameDatabase");
            return instance;
        }
    }

    [Header("Enemies")]
    public GameObject slimeBig;
    public GameObject slimeSmall;
    public GameObject skeletonWarrior;
    public GameObject skeletonArcher;
    public GameObject wizard;
    public GameObject bat;
    public GameObject golemBoss;

    [Header("Projectiles")]
    public GameObject swordWave;
    public GameObject magicOrb;
    public GameObject arrow;
    public GameObject rock;
    public GameObject orbitYuzu;

    [Header("Pickups")]
    public GameObject xpGem;
    public GameObject yuzuHeal;
    public GameObject chest;
    public GameObject hotSpring;
    public GameObject goldenYuzu;

    [Header("Effects")]
    public Material flashMaterial;
    public Material decalMaterial;
    public Material additiveMaterial;
    public Material debrisMaterial;
    public Material lineMaterial;
    public Texture2D softCircle;
    public Texture2D ringTexture;
    public Texture2D sparkTexture;
    public Texture2D starTexture;
    public Texture2D telegraphCircle;
    public Texture2D telegraphRect;
    public Texture2D spawnRune;
    public Mesh cubeMesh;

    [Header("UI")]
    public Font titleFont;
    public Font bodyFont;
    public Sprite rounded;
    public Sprite roundedSmall;
    public Sprite circle;
    public Sprite ring;
    public Sprite glow;
    public Sprite vignette;
    public Sprite heart;
    public Sprite capyFace;
    public Sprite dashIcon;
    public Sprite skullIcon;
    public Sprite clockIcon;
    public Sprite gemIcon;
    public Sprite[] upgradeIcons;

    [Header("Audio")]
    public AudioClip[] sfx;
    public AudioClip musicMenu;
    public AudioClip musicDungeon;
    public AudioClip musicBoss;
    public AudioClip musicVictory;

    public GameObject GetEnemy(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.SlimeBig: return slimeBig;
            case EnemyType.SlimeSmall: return slimeSmall;
            case EnemyType.SkeletonWarrior: return skeletonWarrior;
            case EnemyType.SkeletonArcher: return skeletonArcher;
            case EnemyType.Wizard: return wizard;
            case EnemyType.Bat: return bat;
            case EnemyType.GolemBoss: return golemBoss;
        }
        return null;
    }

    public Sprite GetUpgradeIcon(int index)
    {
        if (upgradeIcons == null || index < 0 || index >= upgradeIcons.Length)
            return circle;
        return upgradeIcons[index];
    }
}
