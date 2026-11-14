using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ArenaHud : MonoBehaviour
{
    [System.Serializable]
    private sealed class Bar
    {
        public RectTransform track;
        public RectTransform fill;

        public void Set(float value, int max)
        {
            fill.anchorMax = new Vector2(max > 0 ? Mathf.Clamp01(value / max) : 0f, 1f);
        }
    }

    private sealed class EnemyBar
    {
        public Shooter Shooter;
        public Bar Bar;
    }

    private readonly List<EnemyBar> enemyBars = new();
    [SerializeField] private Camera arenaCamera;
    [SerializeField] private Bar enemyBarTemplate;
    [SerializeField] private Bar slimeHealth, grandmaHealth, progress;
    [SerializeField] private Text slimeValue, grandmaValue, progressValue, waveValue;
    [SerializeField] private Text slowLabel, swapLabel, parryLabel;
    [SerializeField] private Bar slowBar, swapBar, parryBar;
    [SerializeField] private Image slowTint;
    [SerializeField] private Image[] slowEdges;

    public void SetSlowMotion(float remaining, float recharge, float duration, float cooldown)
    {
        var active = remaining > 0f;
        slowTint.enabled = active;
        foreach (var edge in slowEdges)
            edge.enabled = active;

        var strength = Mathf.Clamp01(remaining * 5f);
        slowTint.canvasRenderer.SetAlpha(strength);
        foreach (var edge in slowEdges)
            edge.canvasRenderer.SetAlpha(strength);

        slowLabel.text = active ? $"{remaining:0.0}s" :
            recharge > 0f ? $"{Mathf.CeilToInt(recharge)}s" : "SPACE";
        var fraction = active ? remaining / duration : 1f - recharge / cooldown;
        slowBar.fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    public void SetSwap(float recharge, float cooldown, bool hasTarget)
    {
        swapLabel.text = recharge > 0f ? $"{Mathf.CeilToInt(recharge)}s" : hasTarget ? "E" : "NO SHOT";
        swapBar.fill.anchorMax = new Vector2(recharge > 0f ? Mathf.Clamp01(1f - recharge / cooldown) : hasTarget ? 1f : 0f, 1f);
    }

    public void SetParry(float remaining, float recharge, float duration, float cooldown)
    {
        parryLabel.text = remaining > 0f ? "ACTIVE" :
            recharge > 0f ? $"{Mathf.CeilToInt(recharge)}s" : "Q";
        var fraction = remaining > 0f ? remaining / duration : cooldown > 0f ? 1f - recharge / cooldown : 1f;
        parryBar.fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    public void SetStatus(int wave, int totalWaves, float slimeHp, int slimeMax, int grandmaHp, int grandmaMax,
        int level, int pieces, int requiredPieces, int damage)
    {
        slimeHealth.Set(slimeHp, slimeMax);
        grandmaHealth.Set(grandmaHp, grandmaMax);
        progress.Set(pieces, requiredPieces);
        slimeValue.text = $"{slimeHp:0.#}/{slimeMax} HP";
        grandmaValue.text = $"{grandmaHp}/{grandmaMax} HP";
        progressValue.text = $"Lv. {level}    Pieces {pieces}/{requiredPieces}    Return dmg {damage}";
        waveValue.text = $"WAVE {wave} / {totalWaves}";
    }

    public void TrackEnemy(Shooter shooter)
    {
        var track = Instantiate(enemyBarTemplate.track, transform);
        var fillIndex = enemyBarTemplate.fill.GetSiblingIndex();
        var bar = new Bar { track = track, fill = (RectTransform)track.GetChild(fillIndex) };
        if (shooter.IsElite)
        {
            var badge = new GameObject("Elite Skull", typeof(RectTransform)).AddComponent<Text>();
            var rect = badge.rectTransform;
            rect.SetParent(track, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
            rect.anchoredPosition = new Vector2(-12f, 0f);
            rect.sizeDelta = new Vector2(22f, 22f);
            badge.font = waveValue.font;
            badge.fontSize = 18;
            badge.text = "☠";
            badge.alignment = TextAnchor.MiddleCenter;
            badge.color = new Color(1f, .78f, .38f);
            badge.raycastTarget = false;
        }
        enemyBars.Add(new EnemyBar { Shooter = shooter, Bar = bar });
    }

    public void ClearEnemyBars()
    {
        foreach (var entry in enemyBars)
            Destroy(entry.Bar.track.gameObject);
        enemyBars.Clear();
    }

    private void LateUpdate()
    {
        var root = (RectTransform)transform;
        for (var i = enemyBars.Count - 1; i >= 0; i--)
        {
            var entry = enemyBars[i];
            if (entry.Shooter == null || !entry.Shooter.gameObject.activeSelf)
            {
                Destroy(entry.Bar.track.gameObject);
                enemyBars.RemoveAt(i);
                continue;
            }

            var track = entry.Bar.track;
            var actor = entry.Shooter.Health;
            var world = (Vector2)entry.Shooter.transform.position + Vector2.up * (actor.Radius + .16f);
            var screen = arenaCamera.WorldToScreenPoint(world);
            var visible = entry.Shooter.CanBeHit && screen.z > 0f && arenaCamera.pixelRect.Contains(screen);
            track.gameObject.SetActive(visible);
            if (!visible)
                continue;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var point);
            var halfWidth = track.sizeDelta.x * .5f + 4f;
            var halfHeight = track.sizeDelta.y * .5f + 4f;
            track.anchoredPosition = new Vector2(
                Mathf.Clamp(point.x, root.rect.xMin + halfWidth, root.rect.xMax - halfWidth),
                Mathf.Clamp(point.y, root.rect.yMin + halfHeight, root.rect.yMax - halfHeight));
            entry.Bar.Set(actor.Current, actor.Max);
        }
    }
}
