using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class EnemyMovementAudioControl : MonoBehaviour
{
    [SerializeField] private Text label;

    private Button button;

    private void Awake() => button = GetComponent<Button>();

    private void OnEnable()
    {
        UpdateLabel();
        button.onClick.AddListener(ToggleSound);
    }

    private void OnDisable() => button.onClick.RemoveListener(ToggleSound);

    private void ToggleSound()
    {
        GameAudio.SetEnemyMovementEnabled(!GameAudio.EnemyMovementEnabled);
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        label.text = GameAudio.EnemyMovementEnabled ? "Enemy Movement: On" : "Enemy Movement: Off";
    }
}
