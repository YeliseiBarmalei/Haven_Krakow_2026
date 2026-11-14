using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public sealed class VolumeControl : MonoBehaviour, IPointerUpHandler
{
    [SerializeField] private Text label;
    [SerializeField] private AudioClip previewClip;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        slider.SetValueWithoutNotify(GameAudio.Volume);
        UpdateLabel(slider.value);
        slider.onValueChanged.AddListener(SetVolume);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(SetVolume);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        GameAudio.Play(previewClip, .65f);
    }

    private void SetVolume(float value)
    {
        GameAudio.SetVolume(value);
        UpdateLabel(value);
    }

    private void UpdateLabel(float value)
    {
        label.text = $"Volume  {Mathf.RoundToInt(value * 100f)}%";
    }
}
