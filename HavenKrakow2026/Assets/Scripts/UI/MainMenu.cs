using UnityEngine;

public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private string arenaScene = "EndlessArena";
    [SerializeField] private UnityEngine.UI.Text volumeFontSource;
    [SerializeField] private AudioClip uiClick;

    private void Start()
    {
        FadeTransition.Initialize();
        var canvas = volumeFontSource.canvas;
        foreach (var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            button.onClick.AddListener(() => GameAudio.Play(uiClick, .65f));
        VolumeControl.AddTo((RectTransform)canvas.transform, volumeFontSource.font,
            Vector2.right, new Vector2(-146f, 46f), uiClick);
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
