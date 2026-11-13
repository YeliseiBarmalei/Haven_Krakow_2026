using UnityEngine;

public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private string sandboxScene = "Sandbox";

    private void Start()
    {
        FadeTransition.Initialize();
    }

    public void Play()
    {
        FadeTransition.Load(sandboxScene);
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
