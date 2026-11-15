using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenu : MonoBehaviour
{
    private const string TutorialSeenKey = "Tutorial.PracticeCompleted";
    [System.Serializable]
    private sealed class LevelEntry
    {
        public string scene;
        public string title;
        public bool availableFromStart;
        public Button button;
        public Text label;
    }

    [SerializeField] private string tutorialLevel = "Level01";
    [SerializeField] private string tutorialScene = "Tutorial";
    [SerializeField] private string levelSelectScene = "LevelSelect";
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private LevelEntry[] levels = System.Array.Empty<LevelEntry>();
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
        {
            level.button.onClick.AddListener(() => OpenLevel(level));
            var available = IsAvailable(level);
            level.button.interactable = available;
            level.label.text = available ? level.title : $"{level.title}  —  Locked";
        }
    }

    public void Play() => FadeTransition.Load(levelSelectScene);

    public void Back() => FadeTransition.Load(mainMenuScene);

    private void OpenLevel(LevelEntry level)
    {
        if (FadeTransition.IsBusy || !IsAvailable(level))
            return;

        if (level.scene == tutorialLevel && PlayerPrefs.GetInt(TutorialSeenKey) == 0)
        {
            Tutorial();
            return;
        }

        FadeTransition.Load(level.scene);
    }

    public void Tutorial() => FadeTransition.Load(tutorialScene);

    public static void CompleteTutorial()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();
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
