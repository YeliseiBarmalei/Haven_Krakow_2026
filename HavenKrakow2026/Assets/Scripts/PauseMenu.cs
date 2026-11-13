using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private string mainMenuScene = "MainMenu";

    private bool paused;
    private float previousTimeScale;

    private void Start()
    {
        FadeTransition.Initialize();
    }

    private void Update()
    {
        if (!FadeTransition.IsBusy && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            SetPaused(!paused);
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
