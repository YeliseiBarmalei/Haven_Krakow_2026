using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UpgradeMenu : MonoBehaviour
{
    public enum Upgrade { GrandmaHealth, SlimeHealth, ReturnDamage, Size }

    [SerializeField] private Text title;
    [SerializeField] private Button sizeButton;
    [SerializeField] private Text sizeLabel;
    private Action<Upgrade> onChoose;
    private float previousTimeScale;

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

    public void ChooseGrandmaHealth() => Choose(Upgrade.GrandmaHealth);
    public void ChooseSlimeHealth() => Choose(Upgrade.SlimeHealth);
    public void ChooseReturnDamage() => Choose(Upgrade.ReturnDamage);
    public void ChooseSize() => Choose(Upgrade.Size);

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

}
