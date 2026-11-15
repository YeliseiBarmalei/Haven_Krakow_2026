using UnityEngine;
using UnityEngine.UI;

public sealed class IntroCutscene : MonoBehaviour
{
    [SerializeField] private Text dialogue;
    [SerializeField] private Text nextLabel;
    [SerializeField, TextArea] private string[] lines;
    [SerializeField, Min(1f)] private float charactersPerSecond = 38f;
    [SerializeField] private string nextScene = "LevelSelect";
    [SerializeField] private string firstRunScene = "Tutorial";
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip backgroundMusic;

    private int lineIndex;
    private float characterProgress;

    private void Start()
    {
        GameAudio.ApplyVolume();
        GameAudio.PlayMusic(backgroundMusic);
        ShowLine();
    }

    private void Update()
    {
        if (FadeTransition.IsBusy || lines.Length == 0)
            return;

        var line = lines[lineIndex];
        var previous = (int)characterProgress;
        characterProgress = Mathf.Min(line.Length, characterProgress + Time.unscaledDeltaTime * charactersPerSecond);
        var visible = (int)characterProgress;
        if (visible != previous)
            dialogue.text = line.Substring(0, visible);
    }

    public void Next()
    {
        if (FadeTransition.IsBusy)
            return;

        GameAudio.Play(uiClick, .65f);
        if (lines.Length == 0)
        {
            FinishIntro();
            return;
        }

        var line = lines[lineIndex];
        if (characterProgress < line.Length)
        {
            characterProgress = line.Length;
            dialogue.text = line;
            return;
        }

        if (lineIndex == lines.Length - 1)
            FinishIntro();
        else
        {
            lineIndex++;
            ShowLine();
        }
    }

    public void Skip()
    {
        if (FadeTransition.IsBusy)
            return;

        GameAudio.Play(uiClick, .65f);
        FinishIntro();
    }

    private void FinishIntro() =>
        FadeTransition.Load(MainMenu.HasCompletedTutorial ? nextScene : firstRunScene);

    private void ShowLine()
    {
        characterProgress = 0f;
        dialogue.text = string.Empty;
        nextLabel.text = lineIndex >= lines.Length - 1 ? "Continue" : "Next";
    }
}
