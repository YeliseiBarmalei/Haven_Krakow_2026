using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private AudioClip uiClick;

    private bool paused;
    private float previousTimeScale;

    private void Start()
    {
        FadeTransition.Initialize();
        var label = panel.GetComponentInChildren<UnityEngine.UI.Text>(true);
        foreach (var button in panel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            button.onClick.AddListener(() => GameAudio.Play(uiClick, .65f));
        VolumeControl.AddTo((RectTransform)panel.transform, label.font,
            Vector2.right, new Vector2(-146f, 46f), uiClick);
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
        if (paused == value)
            return;

        if (value)
            previousTimeScale = Time.timeScale;

        paused = value;
        panel.SetActive(value);
        Time.timeScale = value ? 0f : previousTimeScale;
    }
}
