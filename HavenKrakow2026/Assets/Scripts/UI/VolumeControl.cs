using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public sealed class VolumeControl : MonoBehaviour, IPointerUpHandler
{
    private enum Channel { Master, Music, Effects }

    [SerializeField] private Channel channel;
    [SerializeField] private Text label;
    [SerializeField] private AudioClip previewClip;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        slider.SetValueWithoutNotify(channel switch
        {
            Channel.Music => GameAudio.MusicVolume,
            Channel.Effects => GameAudio.EffectsVolume,
            _ => GameAudio.Volume
        });
        UpdateLabel(slider.value);
        slider.onValueChanged.AddListener(SetVolume);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(SetVolume);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (channel != Channel.Music && eventData.button == PointerEventData.InputButton.Left)
            GameAudio.Play(previewClip, .65f);
    }

    private void SetVolume(float value)
    {
        switch (channel)
        {
            case Channel.Music: GameAudio.SetMusicVolume(value); break;
            case Channel.Effects: GameAudio.SetEffectsVolume(value); break;
            default: GameAudio.SetVolume(value); break;
        }
        UpdateLabel(value);
    }

    private void UpdateLabel(float value)
    {
        var title = channel switch
        {
            Channel.Music => "Music",
            Channel.Effects => "Sound Effects",
            _ => "Master Volume"
        };
        label.text = $"{title}  {Mathf.RoundToInt(value * 100f)}%";
    }
}
