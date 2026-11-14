using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenu : MonoBehaviour
{
    [System.Serializable]
    private sealed class LevelEntry
    {
        public string scene;
        public string title;
        public bool availableFromStart;
        public Button button;
        public Text label;
    }

    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject levelsPanel;
    [SerializeField] private LevelEntry[] levels;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip backgroundMusic;

    private void Start()
    {
        GameAudio.ApplyVolume();
        GameAudio.PlayMusic(backgroundMusic);
        foreach (var button in menuCanvas.GetComponentsInChildren<Button>(true))
            button.onClick.AddListener(() => GameAudio.Play(uiClick, .65f));

        foreach (var level in levels)
            level.button.onClick.AddListener(() => OpenLevel(level));
    }

    public void Play()
    {
        if (FadeTransition.IsBusy)
            return;

        foreach (var level in levels)
        {
            var available = IsAvailable(level);
            level.button.interactable = available;
            level.label.text = available ? level.title : $"{level.title}  —  Locked";
        }
        menuPanel.SetActive(false);
        levelsPanel.SetActive(true);
    }

    public void Back()
    {
        if (FadeTransition.IsBusy)
            return;

        levelsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    private void OpenLevel(LevelEntry level)
    {
        if (IsAvailable(level))
            FadeTransition.Load(level.scene);
    }

    private static bool IsAvailable(LevelEntry level) =>
        level.availableFromStart || PlayerPrefs.GetInt(UnlockKey(level.scene)) == 1;

    public static void UnlockLevel(string scene)
    {
        PlayerPrefs.SetInt(UnlockKey(scene), 1);
        PlayerPrefs.Save();
    }

    private static string UnlockKey(string scene) => $"LevelUnlocked.{scene}";

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
