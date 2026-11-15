using UnityEngine;

public static class GameAudio
{
    private const string VolumeKey = "MasterVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string EffectsVolumeKey = "EffectsVolume";
    private const string EnemyMovementKey = "EnemyMovementSound";
    private const float MusicGain = .65f;
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
            transition.volume = EffectsVolume;
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
        music.volume = MusicGain * MusicVolume;
        music.pitch = playbackSpeed;
        music.Play();
    }

    public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .8f));
    public static float MusicVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
    public static float EffectsVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsVolumeKey, 1f));
    public static bool EnemyMovementEnabled => PlayerPrefs.GetInt(EnemyMovementKey, 1) != 0;

    public static void ApplyVolume()
    {
        AudioListener.volume = Volume;
        if (music != null)
            music.volume = MusicGain * MusicVolume;
        if (source != null)
            source.volume = EffectsVolume;
        if (transition != null)
            transition.volume = EffectsVolume;
    }

    public static void SetVolume(float value) => SaveVolume(VolumeKey, value);
    public static void SetMusicVolume(float value) => SaveVolume(MusicVolumeKey, value);
    public static void SetEffectsVolume(float value) => SaveVolume(EffectsVolumeKey, value);

    public static void SetEnemyMovementEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(EnemyMovementKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static void SaveVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        ApplyVolume();
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
            source.volume = EffectsVolume;
        }

        source.PlayOneShot(clip, volume);
    }
}
