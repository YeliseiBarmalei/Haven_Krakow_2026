using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UpgradeMenu : MonoBehaviour
{
    public enum Upgrade { GrandmaHealth, SlimeHealth, ReturnDamage, Cooldowns }

    [SerializeField] private Text title;
    [SerializeField] private Button cooldownButton;
    [SerializeField] private Text cooldownLabel;
    private Action<Upgrade> onChoose;
    private float previousTimeScale;

    public void Open(int level, int cooldownUpgrades, int cooldownLimit,
        float slowAndSwapReduction, float parryMissReduction, Action<Upgrade> choose)
    {
        title.text = $"Level {level}   |   Choose an upgrade";
        cooldownButton.interactable = cooldownUpgrades < cooldownLimit;
        cooldownLabel.text = cooldownButton.interactable
            ? $"Shorter Cooldowns  ({cooldownUpgrades}/{cooldownLimit})\n" +
              $"<size=15>Space / Q -{slowAndSwapReduction:0.#}s  ·  Missed parry -{parryMissReduction:0.#}s</size>"
            : "Shorter Cooldowns  (maxed)";
        onChoose = choose;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        gameObject.SetActive(true);
    }

    public void ChooseGrandmaHealth() => Choose(Upgrade.GrandmaHealth);
    public void ChooseSlimeHealth() => Choose(Upgrade.SlimeHealth);
    public void ChooseReturnDamage() => Choose(Upgrade.ReturnDamage);
    public void ChooseCooldowns()
    {
        if (cooldownButton.interactable)
            Choose(Upgrade.Cooldowns);
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

}
