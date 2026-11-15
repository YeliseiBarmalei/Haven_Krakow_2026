using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip backgroundMusic;

    private bool paused;
    private float previousTimeScale;

    private void Start()
    {
        GameAudio.ApplyVolume();
        GameAudio.PlayMusic(backgroundMusic);
        if (panel == null)
            return;

        foreach (var button in panel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            button.onClick.AddListener(() => GameAudio.Play(uiClick, .65f));
    }

    private void Update()
    {
        if (!FadeTransition.IsBusy && (paused || Time.timeScale > 0f) &&
            Keyboard.current?.escapeKey.wasPressedThisFrame == true)
        {
            GameAudio.Play(uiClick, .65f);
            SetPaused(!paused);
        }
    }

    private void OnDisable()
    {
        if (paused)
            SetPaused(false);
    }

    public void Resume()
    {
        if (!FadeTransition.IsBusy)
            SetPaused(false);
    }

    public void Restart()
    {
        FadeTransition.Load(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        FadeTransition.Load(mainMenuScene);
    }

    private void SetPaused(bool value)
    {
        if (paused == value || (value && panel == null))
            return;

        if (value)
            previousTimeScale = Time.timeScale;

        paused = value;
        if (value || Time.timeScale == 0f)
            Time.timeScale = value ? 0f : previousTimeScale;
        if (panel != null)
            panel.SetActive(value);
    }
}
