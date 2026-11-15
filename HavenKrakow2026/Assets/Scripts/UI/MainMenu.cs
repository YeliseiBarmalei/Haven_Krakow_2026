using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private string introScene = "Intro";
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private LevelEntry[] levels = System.Array.Empty<LevelEntry>();
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private RectTransform quitButton;
    [SerializeField] private GameObject menuCard;
    [SerializeField] private GameObject settingsCard;

    private Vector2 quitHome;
    private Vector2 quitVelocity;

    private void Start()
    {
        GameAudio.ApplyVolume();
        GameAudio.PlayMusic(backgroundMusic);
        if (quitButton != null)
            quitHome = quitButton.anchoredPosition;

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

    public void Play() => FadeTransition.Load(introScene);

    public void OpenSettings() => ShowSettings(true);

    public void CloseSettings() => ShowSettings(false);

    private void ShowSettings(bool open)
    {
        if (FadeTransition.IsBusy || menuCard == null || settingsCard == null)
            return;

        menuCard.SetActive(!open);
        settingsCard.SetActive(open);
    }

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

    private void Update()
    {
        if (quitButton == null || !quitButton.gameObject.activeInHierarchy ||
            !Application.isFocused || FadeTransition.IsBusy || Mouse.current == null)
            return;

        var cursor = Mouse.current.position.ReadValue();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(quitButton, cursor, null, out var localPoint))
            return;

        const float repelDistance = 55f;
        var rect = quitButton.rect;
        var nearest = new Vector2(Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax),
            Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax));
        var away = rect.center - localPoint;
        var proximity = 1f - Mathf.Clamp01(Vector2.Distance(localPoint, nearest) / repelDistance);
        var position = quitButton.anchoredPosition;
        if (position.y >= quitHome.y && away.y > 0f)
            away = away.x >= 0f ? Vector2.right : Vector2.left;

        var acceleration = (quitHome - position) * 28f;
        acceleration += (away.sqrMagnitude > .01f ? away.normalized : Vector2.down) * (8500f * proximity);

        var dt = Mathf.Min(Time.unscaledDeltaTime, .04f);
        quitVelocity = (quitVelocity + acceleration * dt) * Mathf.Exp(-8f * dt);
        var next = position + quitVelocity * dt;
        var canvasRect = ((RectTransform)menuCanvas.transform).rect;
        var horizontalLimit = Mathf.Max(0f, (canvasRect.width - rect.width) * .5f - 16f);
        var bottomLimit = Mathf.Min(quitHome.y, -(canvasRect.height - rect.height) * .5f + 16f);
        var clamped = new Vector2(
            Mathf.Clamp(next.x, -horizontalLimit, horizontalLimit),
            Mathf.Clamp(next.y, bottomLimit, quitHome.y));
        if (clamped.x != next.x)
            quitVelocity.x = 0f;
        if (clamped.y != next.y)
            quitVelocity.y = 0f;
        quitButton.anchoredPosition = clamped;
    }
}
