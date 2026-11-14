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
    [Min(.01f)] public float shieldRingWidth = .11f;
    public Color shieldColor = new Color(.25f, .92f, .62f);
    public Color shieldHitColor = new Color(.77f, 1f, .94f);
    public Material shieldMaterial;
}
