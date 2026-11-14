using UnityEngine;

public static class GameAudio
{
    private const string VolumeKey = "MasterVolume";
    private static AudioSource source;

    public static float Volume => PlayerPrefs.GetFloat(VolumeKey, .8f);

    public static void ApplyVolume()
    {
        AudioListener.volume = Mathf.Clamp01(Volume);
    }

    public static void SetVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        PlayerPrefs.Save();
    }

    public static void Play(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        if (source == null)
        {
            source = new GameObject("Game Audio").AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        source.PlayOneShot(clip, volume);
    }
}
