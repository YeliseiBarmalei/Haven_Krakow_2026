using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FadeTransition : MonoBehaviour
{
    private const float Duration = 0.25f;
    [SerializeField] private Image overlay;

    private static FadeTransition instance;

    public static bool IsBusy => instance != null && instance.overlay.raycastTarget;

    private void Awake()
    {
        instance = this;
        SetAlpha(1f);
        overlay.enabled = true;
        overlay.raycastTarget = true;
    }

    private IEnumerator Start()
    {
        yield return FadeTo(0f);
        overlay.raycastTarget = false;
        overlay.enabled = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void Load(string sceneName)
    {
        if (IsBusy)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is missing from the build scene list.");
            return;
        }

        if (instance != null)
            instance.StartCoroutine(instance.SwitchScene(sceneName));
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator SwitchScene(string sceneName)
    {
        SetAlpha(0f);
        overlay.enabled = true;
        overlay.raycastTarget = true;
        yield return FadeTo(1f);

        var load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;
        while (load.progress < .9f)
            yield return null;

        Time.timeScale = 1f;
        load.allowSceneActivation = true;
    }

    private IEnumerator FadeTo(float target)
    {
        var initial = overlay.color.a;
        var elapsed = 0f;
        while (elapsed < Duration)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(initial, target, elapsed / Duration));
        }

        SetAlpha(target);
    }

    private void SetAlpha(float alpha)
    {
        var color = overlay.color;
        color.a = alpha;
        overlay.color = color;
    }
}
