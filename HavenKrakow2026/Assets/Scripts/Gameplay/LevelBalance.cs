using UnityEngine;

[CreateAssetMenu(menuName = "Game/Level Balance")]
public sealed class LevelBalance : ScriptableObject
{
    [Header("Progression")]
    [Min(1f)] public float pieceInterval = 10f;
    [Min(1)] public int startingUpgradeCost = 2;

    [Header("Slow Motion")]
    [Min(.01f)] public float slowDuration = 3f;
    [Min(.01f)] public float slowCooldown = 18f;

    [Header("Swap")]
    [Min(0f)] public float swapCooldown = 12f;
    [Range(0f, 1f)] public float swapSoundVolume = .25f;

    [Header("Parry Shield")]
    [Min(.02f)] public float shieldDuration = .25f;
    [Min(0f)] public float shieldMissCooldown = 15f;
    [Min(0f)] public float shieldSuccessCooldown = 3f;
    [Min(0f)] public float shieldPadding = 0f;
    [Min(0f)] public float shieldReturnBoost = 8f;
    public Sprite shieldSprite;
    public Color shieldColor = new Color(1f, 1f, 1f, .42f);
    public Color shieldHitColor = new Color(1f, 1f, 1f, .68f);

    [Header("Lightning")]
    public Sprite lightningWarningIcon;
    public AudioClip lightningSound;
    [Range(0f, 1f)] public float lightningSoundVolume = .25f;
}
