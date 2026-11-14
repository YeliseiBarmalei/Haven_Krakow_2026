using UnityEngine;

public static class GameAudio
{
    private const string VolumeKey = "MasterVolume";
    private static AudioSource source;
    private static AudioSource music;
    private static AudioSource transition;
    private static float playbackSpeed = 1f;

    public static void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = speed;
        if (source != null)
            source.pitch = speed;
        if (music != null)
            music.pitch = speed;
    }

    public static void PlayTransition(AudioClip clip)
    {
        if (clip == null)
            return;

        if (transition == null)
        {
            transition = new GameObject("Time Shift Audio").AddComponent<AudioSource>();
            transition.playOnAwake = false;
            transition.spatialBlend = 0f;
        }

        transition.PlayOneShot(clip, .8f);
    }

    public static void PlayMusic(AudioClip clip)
    {
        if (clip == null || music != null)
            return;

        var player = new GameObject("Background Music");
        Object.DontDestroyOnLoad(player);
        music = player.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.clip = clip;
        music.loop = true;
        music.spatialBlend = 0f;
        music.volume = .65f;
        music.pitch = playbackSpeed;
        music.Play();
    }

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
            source.pitch = playbackSpeed;
        }

        source.PlayOneShot(clip, volume);
    }
}
