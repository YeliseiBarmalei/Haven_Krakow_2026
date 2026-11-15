using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class PlayerMovementAudioControl : MonoBehaviour
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
        GameAudio.SetPlayerMovementEnabled(!GameAudio.PlayerMovementEnabled);
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        label.text = GameAudio.PlayerMovementEnabled ? "Player Movement Sound: On" : "Player Movement Sound: Off";
    }
}
