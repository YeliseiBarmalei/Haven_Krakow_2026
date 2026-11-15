#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

internal static class GameViewCursor
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        var texture = PlayerSettings.defaultCursor;
        if (texture != null)
            Cursor.SetCursor(texture, PlayerSettings.cursorHotspot, CursorMode.Auto);
    }
}
#endif
