using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FadeTransition : MonoBehaviour
{
    private const float Duration = 0.25f;
    private static FadeTransition instance;
    private Image overlay;

    public static bool IsBusy => instance != null && instance.overlay.raycastTarget;

    public static void Initialize()
    {
        if (instance == null)
            CreateOverlay(1f).StartCoroutine(instance.Reveal());
    }

    private static FadeTransition CreateOverlay(float alpha)
    {
        var root = new GameObject("Fade Transition", typeof(Canvas), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        DontDestroyOnLoad(root);

        instance = root.AddComponent<FadeTransition>();
        var shade = new GameObject("Black Overlay", typeof(Image));
        shade.transform.SetParent(root.transform, false);
        instance.overlay = shade.GetComponent<Image>();
        instance.overlay.color = new Color(0f, 0f, 0f, alpha);
        var rect = instance.overlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return instance;
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

        var transition = instance != null ? instance : CreateOverlay(0f);
        transition.StartCoroutine(transition.SwitchScene(sceneName));
    }

    private IEnumerator SwitchScene(string sceneName)
    {
        overlay.raycastTarget = true;
        yield return FadeTo(1f);
        yield return SceneManager.LoadSceneAsync(sceneName);
        Time.timeScale = 1f;
        yield return Reveal();
    }

    private IEnumerator Reveal()
    {
        yield return FadeTo(0f);
        overlay.raycastTarget = false;
    }

    private IEnumerator FadeTo(float target)
    {
        var initial = overlay.color.a;
        var elapsed = 0f;
        while (elapsed < Duration)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            overlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(initial, target, elapsed / Duration));
        }

        overlay.color = new Color(0f, 0f, 0f, target);
    }
}
