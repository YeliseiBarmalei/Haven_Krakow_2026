using UnityEngine;

public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private string arenaScene = "Level01";
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip backgroundMusic;

    private void Start()
    {
        GameAudio.ApplyVolume();
        GameAudio.PlayMusic(backgroundMusic);
        foreach (var button in menuCanvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            button.onClick.AddListener(() => GameAudio.Play(uiClick, .65f));
    }

    public void Play()
    {
        FadeTransition.Load(arenaScene);
    }

    public void Quit()
    {
        if (FadeTransition.IsBusy)
            return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
