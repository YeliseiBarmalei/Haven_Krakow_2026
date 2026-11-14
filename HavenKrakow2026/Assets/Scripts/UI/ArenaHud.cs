using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ArenaHud : MonoBehaviour
{
    private static readonly Color Panel = new Color32(15, 25, 37, 233);
    private static readonly Color Track = new Color32(43, 57, 72, 255);
    private static readonly Color TextColor = new Color32(235, 246, 250, 255);
    private static readonly Color SlimeColor = new Color32(92, 224, 193, 255);
    private static readonly Color GrandmaColor = new Color32(244, 162, 190, 255);
    private static readonly Color EnemyColor = new Color32(255, 119, 104, 255);
    private static readonly Color ProgressColor = new Color32(247, 205, 120, 255);

    private sealed class Bar
    {
        public RectTransform Track;
        public RectTransform Fill;

        public void Set(int value, int max)
        {
            Fill.anchorMax = new Vector2(max > 0 ? Mathf.Clamp01((float)value / max) : 0f, 1f);
        }
    }

    private sealed class EnemyBar
    {
        public Shooter Shooter;
        public Bar Bar;
    }

    private readonly List<EnemyBar> enemyBars = new();
    private Camera arenaCamera;
    private Bar slimeHealth, grandmaHealth, progress;
    private Text slimeValue, grandmaValue, progressValue, waveValue;
    private Text slowLabel;
    private Bar slowBar;
    private Image slowTint;
    private Image[] slowEdges;

    public void SetSlowMotion(float remaining, float recharge, float duration, float cooldown)
    {
        var active = remaining > 0f;
        slowTint.enabled = active;
        foreach (var edge in slowEdges)
            edge.enabled = active;

        var strength = Mathf.Clamp01(remaining * 5f);
        slowTint.color = new Color(.21f, .44f, .82f, .19f * strength);
        foreach (var edge in slowEdges)
            edge.color = new Color(.49f, .79f, 1f, .8f * strength);

        slowLabel.text = active ? $"SLOW MOTION   {remaining:0.0}s" :
            recharge > 0f ? $"SLOW MOTION   {Mathf.CeilToInt(recharge)}s" : "SPACE   ·   SLOW MOTION";
        var fraction = active ? remaining / duration : 1f - recharge / cooldown;
        slowBar.Fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    public static ArenaHud Create(Text source, Camera camera)
    {
        var root = Rect("Arena HUD", source.transform.parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.SetAsFirstSibling();
        var hud = root.gameObject.AddComponent<ArenaHud>();
        hud.arenaCamera = camera;
        hud.Build(root, source.font);
        source.enabled = false;
        return hud;
    }

    public void SetStatus(int wave, int slimeHp, int slimeMax, int grandmaHp, int grandmaMax,
        int level, int pieces, int requiredPieces, int damage)
    {
        slimeHealth.Set(slimeHp, slimeMax);
        grandmaHealth.Set(grandmaHp, grandmaMax);
        progress.Set(pieces, requiredPieces);
        slimeValue.text = $"{slimeHp}/{slimeMax} HP";
        grandmaValue.text = $"{grandmaHp}/{grandmaMax} HP";
        progressValue.text = $"Lv. {level}    Pieces {pieces}/{requiredPieces}    Return dmg {damage}";
        waveValue.text = $"WAVE {wave}";
    }

    public void TrackEnemy(Shooter shooter)
    {
        var bar = MakeBar(transform, "Enemy Health", Vector2.one * .5f, Vector2.zero, new Vector2(52f, 7f), EnemyColor);
        bar.Track.gameObject.SetActive(false);
        enemyBars.Add(new EnemyBar { Shooter = shooter, Bar = bar });
    }

    public void ClearEnemyBars()
    {
        foreach (var entry in enemyBars)
            Destroy(entry.Bar.Track.gameObject);
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
                Destroy(entry.Bar.Track.gameObject);
                enemyBars.RemoveAt(i);
                continue;
            }

            var track = entry.Bar.Track;
            var actor = entry.Shooter.Health;
            var world = (Vector2)entry.Shooter.transform.position + Vector2.up * (actor.Radius + .16f);
            var screen = arenaCamera.WorldToScreenPoint(world);
            var visible = screen.z > 0f && arenaCamera.pixelRect.Contains(screen);
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

    private void Build(RectTransform root, Font font)
    {
        slowTint = MakeSlowOverlay(root, "Time Tint", Vector2.zero, Vector2.one, Vector2.zero);
        slowEdges = new[]
        {
            MakeSlowOverlay(root, "Top Edge", new Vector2(0, 1), Vector2.one, new Vector2(0, 8)),
            MakeSlowOverlay(root, "Bottom Edge", Vector2.zero, Vector2.right, new Vector2(0, 8)),
            MakeSlowOverlay(root, "Left Edge", Vector2.zero, Vector2.up, new Vector2(8, 0)),
            MakeSlowOverlay(root, "Right Edge", Vector2.right, Vector2.one, new Vector2(8, 0))
        };
        var skill = Card(root, "Slow Motion", Vector2.zero, new Vector2(135f, 46f), new Vector2(234f, 56f));
        slowLabel = Label(skill, font, "Slow Motion Status", new Vector2(0f, 7f), new Vector2(212f, 24f), 16, TextAnchor.MiddleCenter);
        slowBar = MakeBar(skill, "Slow Motion Charge", Vector2.one * .5f, new Vector2(0f, -13f), new Vector2(200f, 6f), new Color(.49f, .79f, 1f));
        SetSlowMotion(0f, 0f, 1f, 1f);

        var left = new Vector2(0f, 1f);
        var right = Vector2.one;
        var top = new Vector2(.5f, 1f);

        var player = Card(root, "Slime", left, new Vector2(166f, -70f), new Vector2(296f, 104f));
        slimeHealth = HealthCard(player, font, "Slime", SlimeColor, out slimeValue);
        progressValue = Label(player, font, "Progress", new Vector2(0f, -16f), new Vector2(264f, 21f), 16);
        progress = MakeBar(player, "Upgrade Progress", Vector2.one * .5f, new Vector2(0f, -36f), new Vector2(264f, 7f), ProgressColor);

        var grandmaCard = Card(root, "Grandma", right, new Vector2(-140f, -49f), new Vector2(244f, 62f));
        grandmaHealth = HealthCard(grandmaCard, font, "Grandma", GrandmaColor, out grandmaValue);

        var wave = Card(root, "Wave", top, new Vector2(0f, -36f), new Vector2(144f, 36f));
        waveValue = Label(wave, font, "Wave Number", Vector2.zero, wave.sizeDelta, 18, TextAnchor.MiddleCenter);
        waveValue.fontStyle = FontStyle.Bold;
    }

    private static Image MakeSlowOverlay(Transform parent, string name, Vector2 min, Vector2 max, Vector2 size)
    {
        var rect = Rect(name, parent, min, max, Vector2.zero, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static Bar HealthCard(RectTransform card, Font font, string name, Color tint, out Text value)
    {
        var width = card.sizeDelta.x - 32f;
        var y = card.sizeDelta.y * .5f - 19f;
        var title = Label(card, font, name + " Name", new Vector2(0f, y), new Vector2(width, 22f), 19);
        title.text = name;
        title.fontStyle = FontStyle.Bold;
        value = Label(card, font, name + " HP", new Vector2(0f, y), new Vector2(width, 22f), 16, TextAnchor.MiddleRight);
        return MakeBar(card, name + " Health", Vector2.one * .5f, new Vector2(0f, y - 21f), new Vector2(width, 12f), tint);
    }

    private static Bar MakeBar(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color tint)
    {
        var track = Rect(name, parent, anchor, anchor, offset, size);
        ImageOn(track, Track);
        var fill = Rect("Fill", track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        ImageOn(fill, tint);
        return new Bar { Track = track, Fill = fill };
    }

    private static RectTransform Card(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        var card = Rect(name + " Panel", parent, anchor, anchor, offset, size);
        ImageOn(card, Panel);
        var shadow = card.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 0, 0, 120);
        shadow.effectDistance = new Vector2(0f, -3f);
        return card;
    }

    private static void ImageOn(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static Text Label(Transform parent, Font font, string name, Vector2 offset, Vector2 size, int fontSize,
        TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var rect = Rect(name, parent, Vector2.one * .5f, Vector2.one * .5f, offset, size);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.color = TextColor;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offset, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
        return rect;
    }
}
