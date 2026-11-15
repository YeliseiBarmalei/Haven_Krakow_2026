using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class ArenaHud : MonoBehaviour
{
    private const float FadeSpeed = 6f;
    private const float LockedSkillAlpha = .62f;

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
        public RectTransform Badge;
    }

    private sealed class FadePanel
    {
        public readonly RectTransform Rect;
        public readonly CanvasGroup Group;
        public readonly float BaseAlpha;
        public Rect ScreenRect;
        public bool Occluded;
        public bool Available = true;

        public FadePanel(Transform panel)
        {
            Rect = (RectTransform)panel;
            Group = Rect.GetComponent<CanvasGroup>();
            if (Group == null)
                Group = Rect.gameObject.AddComponent<CanvasGroup>();
            BaseAlpha = Group.alpha;
        }

        public bool Overlaps(Rect rect) => ScreenRect.width > 0f && ScreenRect.Overlaps(rect);
    }

    private readonly List<EnemyBar> enemyBars = new();
    private readonly List<Kamikaze> runners = new();
    private readonly Vector3[] panelCorners = new Vector3[4];
    [SerializeField] private Camera arenaCamera;
    [SerializeField] private Bar enemyBarTemplate;
    [SerializeField] private Bar slimeHealth, grandmaHealth, progress;
    [SerializeField] private Text slimeValue, grandmaValue, progressValue, waveValue;
    [SerializeField] private Text slowLabel, swapLabel, parryLabel;
    [SerializeField] private Bar slowBar, swapBar, parryBar;
    [SerializeField] private Image slowTint;
    [SerializeField] private Image[] slowEdges;
    [SerializeField] private Font eliteBadgeFont;
    [SerializeField] private GameObject skillTooltip;
    [SerializeField] private Text skillTooltipText;
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private RectTransform pauseButton;
    [SerializeField, Range(.1f, .95f)] private float occludedPanelAlpha = .45f;
    private Text hoveredSkill;
    private FadePanel[] fadePanels;
    private FadePanel slowPanel, swapPanel, parryPanel;

    public RectTransform PauseButton => pauseButton;

    private void Awake()
    {
        slowPanel = new FadePanel(slowLabel.transform.parent);
        swapPanel = new FadePanel(swapLabel.transform.parent);
        parryPanel = new FadePanel(parryLabel.transform.parent);
        fadePanels = new[]
        {
            new FadePanel(slimeHealth.track.parent),
            new FadePanel(grandmaHealth.track.parent),
            new FadePanel(waveValue.transform.parent),
            slowPanel,
            swapPanel,
            parryPanel
        };
    }

    public void SetPracticeSkills(bool slowAvailable, bool swapAvailable, bool parryAvailable)
    {
        slowPanel.Available = slowAvailable;
        swapPanel.Available = swapAvailable;
        parryPanel.Available = parryAvailable;
    }

    private void UpdateSkillTooltip()
    {
        if (skillTooltip == null || skillTooltipText == null)
            return;

        Text hovered = null;
        var canInspect = Time.timeScale > 0f || (pauseOverlay != null && pauseOverlay.activeInHierarchy);
        if (Application.isFocused && canInspect && !FadeTransition.IsBusy && Mouse.current != null)
        {
            var cursor = Mouse.current.position.ReadValue();
            if (ContainsSkill(slowLabel, cursor))
                hovered = slowLabel;
            else if (ContainsSkill(swapLabel, cursor))
                hovered = swapLabel;
            else if (ContainsSkill(parryLabel, cursor))
                hovered = parryLabel;
        }

        if (hovered == hoveredSkill)
        {
            if (hovered != null)
                PositionSkillTooltip(hovered);
            return;
        }

        hoveredSkill = hovered;
        skillTooltip.SetActive(hovered != null);
        if (hovered == null)
            return;

        if (hovered == slowLabel)
            skillTooltipText.text = "<b>Space  ·  Slime-Devistan</b>\nSlows bullets and enemies while you move at full speed.";
        else if (hovered == swapLabel)
            skillTooltipText.text = "<b>Q  ·  Swap</b>\nTrade places with the nearest eligible bullet. It keeps flying in the same direction.";
        else
            skillTooltipText.text = "<b>E  ·  Parry</b>\nBriefly shield Grandma and reflect incoming bullets. A successful parry recharges faster.";

        skillTooltip.transform.SetAsLastSibling();
        PositionSkillTooltip(hovered);
    }

    private void PositionSkillTooltip(Text hovered)
    {
        const float gap = 10f;
        const float edgeInset = 4f;
        var tooltip = (RectTransform)skillTooltip.transform;
        var root = (RectTransform)tooltip.parent;
        var card = (RectTransform)hovered.transform.parent;
        var leftCard = (RectTransform)slowLabel.transform.parent;
        var rightCard = (RectTransform)parryLabel.transform.parent;
        var rowLeft = root.InverseTransformPoint(leftCard.TransformPoint(leftCard.rect.min)).x;
        var rowRight = root.InverseTransformPoint(rightCard.TransformPoint(rightCard.rect.max)).x;
        var centerY = root.InverseTransformPoint(card.TransformPoint(card.rect.center)).y;

        var left = rowLeft - gap - tooltip.rect.xMax;
        var right = rowRight + gap - tooltip.rect.xMin;
        var minX = root.rect.xMin + edgeInset - tooltip.rect.xMin;
        var maxX = root.rect.xMax - edgeInset - tooltip.rect.xMax;
        var preferLeft = hovered == slowLabel;
        var x = preferLeft ? left : right;
        if (preferLeft && left < minX && right <= maxX)
            x = right;
        else if (!preferLeft && right > maxX && left >= minX)
            x = left;

        tooltip.localPosition = new Vector3(
            Mathf.Clamp(x, minX, maxX),
            Mathf.Clamp(centerY, root.rect.yMin + edgeInset - tooltip.rect.yMin,
                root.rect.yMax - edgeInset - tooltip.rect.yMax),
            tooltip.localPosition.z);
    }

    private static bool ContainsSkill(Text label, Vector2 cursor)
    {
        var panel = (RectTransform)label.transform.parent;
        return panel.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(panel, cursor);
    }

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

        slowLabel.text = active ? $"Space  {remaining:0.0}s" :
            recharge > 0f ? $"Space  {Mathf.CeilToInt(recharge)}s" : "Space";
        var fraction = active ? remaining / duration : cooldown > 0f ? 1f - recharge / cooldown : 1f;
        slowBar.fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    public void SetSwap(float recharge, float cooldown, bool hasTarget)
    {
        swapLabel.text = recharge > 0f ? $"Q  {Mathf.CeilToInt(recharge)}s" : hasTarget ? "Q" : "Q  ·  No shot";
        swapBar.fill.anchorMax = new Vector2(recharge > 0f ? Mathf.Clamp01(1f - recharge / cooldown) : hasTarget ? 1f : 0f, 1f);
    }

    public void SetParry(float remaining, float recharge, float duration, float cooldown)
    {
        parryLabel.text = remaining > 0f ? $"E  {remaining:0.0}s" :
            recharge > 0f ? $"E  {Mathf.CeilToInt(recharge)}s" : "E";
        var fraction = remaining > 0f ? remaining / duration : cooldown > 0f ? 1f - recharge / cooldown : 1f;
        parryBar.fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    public void SetStatus(int wave, int totalWaves, float slimeHp, int slimeMax, float grandmaHp, int grandmaMax,
        int level, int pieces, int requiredPieces, float damage)
    {
        slimeHealth.Set(slimeHp, slimeMax);
        grandmaHealth.Set(grandmaHp, grandmaMax);
        progress.Set(pieces, requiredPieces);
        slimeValue.text = $"{slimeHp:0.#}/{slimeMax} HP";
        grandmaValue.text = $"{grandmaHp:0.#}/{grandmaMax} HP";
        progressValue.text = $"Lv. {level}    Cakes {pieces}/{requiredPieces}    Return dmg {damage:0.#}";
        waveValue.text = $"WAVE {wave} / {totalWaves}";
    }

    public void TrackEnemy(Shooter shooter)
    {
        var track = Instantiate(enemyBarTemplate.track, transform);
        var fillIndex = enemyBarTemplate.fill.GetSiblingIndex();
        var bar = new Bar { track = track, fill = (RectTransform)track.GetChild(fillIndex) };
        var entry = new EnemyBar { Shooter = shooter, Bar = bar };
        if (shooter.IsElite)
        {
            var badge = new GameObject("Elite Skull", typeof(RectTransform)).AddComponent<Text>();
            var rect = badge.rectTransform;
            rect.SetParent(track, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
            rect.anchoredPosition = new Vector2(-17f, 0f);
            rect.sizeDelta = new Vector2(32f, 32f);
            badge.font = eliteBadgeFont != null ? eliteBadgeFont : waveValue.font;
            badge.fontSize = 28;
            badge.text = "☠";
            badge.alignment = TextAnchor.MiddleCenter;
            badge.color = new Color(1f, .22f, .22f);
            badge.raycastTarget = false;
            entry.Badge = rect;
        }
        enemyBars.Add(entry);
    }

    public void ClearEnemyBars()
    {
        foreach (var entry in enemyBars)
            Destroy(entry.Bar.track.gameObject);
        enemyBars.Clear();
    }

    public void TrackEnemy(Kamikaze runner) => runners.Add(runner);

    public Rect SlimePanelScreenRect() => PanelScreenRect((RectTransform)slimeHealth.track.parent);

    private Rect PanelScreenRect(RectTransform panel)
    {
        panel.GetWorldCorners(panelCorners);
        var min = RectTransformUtility.WorldToScreenPoint(null, panelCorners[0]);
        var max = RectTransformUtility.WorldToScreenPoint(null, panelCorners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void LateUpdate()
    {
        UpdateSkillTooltip();
        var root = (RectTransform)transform;
        foreach (var panel in fadePanels)
        {
            panel.ScreenRect = panel.Rect.gameObject.activeInHierarchy
                ? PanelScreenRect(panel.Rect)
                : Rect.zero;
            panel.Occluded = false;
        }

        for (var i = runners.Count - 1; i >= 0; i--)
        {
            var runner = runners[i];
            if (runner == null || !runner.IsActive)
                runners.RemoveAt(i);
            else
                MarkOccludedPanels(runner.VisualBounds);
        }

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
            var sprite = entry.Shooter.VisualBounds;
            MarkOccludedPanels(sprite);
            var world = new Vector3(sprite.center.x, sprite.max.y + .12f, sprite.center.z);
            var screen = arenaCamera.WorldToScreenPoint(world);
            var visible = entry.Shooter.CanBeHit && screen.z > 0f && arenaCamera.pixelRect.Contains(screen);
            if (!visible)
            {
                track.gameObject.SetActive(false);
                continue;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var point);
            var halfWidth = track.sizeDelta.x * .5f + 4f;
            var halfHeight = track.sizeDelta.y * .5f + 4f;
            var leftInset = halfWidth;
            if (entry.Badge != null)
            {
                leftInset += -entry.Badge.anchoredPosition.x - entry.Badge.rect.xMin;
                halfHeight = Mathf.Max(halfHeight, entry.Badge.rect.height * .5f + 4f);
            }
            track.anchoredPosition = new Vector2(
                Mathf.Clamp(point.x, root.rect.xMin + leftInset, root.rect.xMax - halfWidth),
                Mathf.Clamp(point.y, root.rect.yMin + halfHeight, root.rect.yMax - halfHeight));
            track.GetWorldCorners(panelCorners);
            var min = RectTransformUtility.WorldToScreenPoint(null, panelCorners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(null, panelCorners[2]);
            if (entry.Badge != null)
            {
                entry.Badge.GetWorldCorners(panelCorners);
                min = Vector2.Min(min, RectTransformUtility.WorldToScreenPoint(null, panelCorners[0]));
                max = Vector2.Max(max, RectTransformUtility.WorldToScreenPoint(null, panelCorners[2]));
            }
            var barRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            var overlapsPanel = false;
            foreach (var panel in fadePanels)
                overlapsPanel |= panel.Overlaps(barRect);
            track.gameObject.SetActive(!overlapsPanel);
            entry.Bar.Set(actor.Current, actor.Max);
        }

        foreach (var panel in fadePanels)
        {
            var visibility = panel.Available ? 1f : LockedSkillAlpha;
            if (panel.Occluded)
                visibility = Mathf.Min(visibility, occludedPanelAlpha);
            panel.Group.alpha = Mathf.MoveTowards(panel.Group.alpha, panel.BaseAlpha * visibility,
                FadeSpeed * Time.unscaledDeltaTime);
        }
    }

    private void MarkOccludedPanels(Bounds sprite)
    {
        var min = arenaCamera.WorldToScreenPoint(sprite.min);
        var max = arenaCamera.WorldToScreenPoint(sprite.max);
        if (min.z <= 0f || max.z <= 0f)
            return;

        var screenBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        foreach (var panel in fadePanels)
        {
            if (panel.Overlaps(screenBounds))
                panel.Occluded = true;
        }
    }
}
