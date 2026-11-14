using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UpgradeMenu : MonoBehaviour
{
    public enum Upgrade { GrandmaHealth, SlimeHealth, ReturnDamage, Size }

    private Text title;
    private Button sizeButton;
    private Text sizeLabel;
    private Action<Upgrade> onChoose;
    private float previousTimeScale;

    public static UpgradeMenu Create(Text hud, Transform resultPanel)
    {
        var overlay = CreateRect("Upgrade Menu", resultPanel.parent, Vector2.zero, Vector2.one);
        overlay.SetSiblingIndex(resultPanel.GetSiblingIndex());
        overlay.gameObject.AddComponent<Image>().color = new Color(.02f, .04f, .08f, .84f);
        var menu = overlay.gameObject.AddComponent<UpgradeMenu>();

        var center = Vector2.one * .5f;
        var card = CreateRect("Upgrade Card", overlay, center, center);
        card.sizeDelta = new Vector2(440, 338);
        card.gameObject.AddComponent<Image>().color = new Color(.10f, .15f, .23f);
        menu.title = MakeText(CreateRect("Title", card, center, center), hud.font, 28, "Choose an upgrade");
        menu.title.rectTransform.anchoredPosition = new Vector2(0, 127);
        menu.title.rectTransform.sizeDelta = new Vector2(400, 55);

        menu.AddOption(card, hud.font, Upgrade.GrandmaHealth, "Grandma Health  +1 HP", 67);
        menu.AddOption(card, hud.font, Upgrade.SlimeHealth, "Slime Health  +1 HP", 9);
        menu.AddOption(card, hud.font, Upgrade.ReturnDamage, "Return Damage  +1", -49);
        menu.sizeButton = menu.AddOption(card, hud.font, Upgrade.Size, "Slime Size", -107);
        menu.sizeLabel = menu.sizeButton.GetComponentInChildren<Text>();
        overlay.gameObject.SetActive(false);
        return menu;
    }

    public void Open(int level, bool canGrow, float sizePercent, Action<Upgrade> choose)
    {
        title.text = $"Level {level}   |   Choose an upgrade";
        sizeButton.interactable = canGrow;
        sizeLabel.text = canGrow ? $"Slime Size  +{sizePercent:0.#}% of base" : "Slime Size  (maxed)";
        onChoose = choose;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        gameObject.SetActive(true);
    }

    private void Choose(Upgrade upgrade)
    {
        var callback = onChoose;
        if (callback == null)
            return;

        onChoose = null;
        gameObject.SetActive(false);
        Time.timeScale = previousTimeScale;
        callback(upgrade);
    }

    private void OnDestroy()
    {
        if (onChoose != null)
            Time.timeScale = previousTimeScale;
    }

    private Button AddOption(Transform parent, Font font, Upgrade upgrade, string label, float y)
    {
        var center = Vector2.one * .5f;
        var rect = CreateRect(upgrade.ToString(), parent, center, center);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(350, 46);
        var background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(.21f, .39f, .45f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => Choose(upgrade));
        MakeText(CreateRect("Label", rect, Vector2.zero, Vector2.one), font, 21, label);
        return button;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Text MakeText(RectTransform rect, Font font, int size, string label)
    {
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.text = label;
        return text;
    }
}
