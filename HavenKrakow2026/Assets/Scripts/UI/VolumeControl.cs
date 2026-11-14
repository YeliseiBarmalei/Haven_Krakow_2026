using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class VolumeControl
{
    public static void AddTo(RectTransform parent, Font font, Vector2 anchor, Vector2 position, AudioClip previewClip)
    {
        GameAudio.ApplyVolume();
        var root = Rect("Volume Control", parent, anchor, anchor, position, new Vector2(260f, 56f));
        Image(root, new Color(.07f, .12f, .18f, .96f), false);
        var label = Rect("Volume Label", root, Vector2.one * .5f, Vector2.one * .5f,
            new Vector2(0f, 15f), new Vector2(232f, 22f)).gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        var track = Rect("Volume Slider", root, Vector2.one * .5f, Vector2.one * .5f,
            new Vector2(0f, -11f), new Vector2(218f, 20f));
        Image(track, new Color(.20f, .28f, .35f), true);
        var fillArea = Rect("Fill Area", track, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-12f, -12f));
        var fill = Rect("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image(fill, new Color(.35f, .84f, .72f), false);
        var handleArea = Rect("Handle Area", track, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-12f, 0f));
        var handle = Rect("Volume Handle", handleArea, Vector2.zero, Vector2.up, Vector2.zero, new Vector2(14f, -2f));
        var handleImage = Image(handle, Color.white, true);
        var slider = track.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.SetValueWithoutNotify(GameAudio.Volume);
        label.text = $"Volume  {Mathf.RoundToInt(slider.value * 100f)}%";
        slider.onValueChanged.AddListener(value =>
        {
            GameAudio.SetVolume(value);
            label.text = $"Volume  {Mathf.RoundToInt(value * 100f)}%";
        });
        var events = track.gameObject.AddComponent<EventTrigger>();
        var release = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        release.callback.AddListener(_ => GameAudio.Play(previewClip, .65f));
        events.triggers.Add(release);
    }

    private static Image Image(RectTransform rect, Color tint, bool raycast)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = tint;
        image.raycastTarget = raycast;
        return image;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
