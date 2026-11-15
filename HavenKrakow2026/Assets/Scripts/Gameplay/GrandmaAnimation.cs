using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class GrandmaAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] searchingFrames;
    [SerializeField, Min(.1f)] private float idleFramesPerSecond = 4f;
    [SerializeField, Min(.1f)] private float searchingFramesPerSecond = 8f;

    private SpriteRenderer visual;
    private float idleFrame;

    public float SearchDuration => searchingFrames.Length / searchingFramesPerSecond;

    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
    }

    public void Animate(float step, float timeUntilThrow = float.PositiveInfinity)
    {
        var searchDuration = SearchDuration;
        if (timeUntilThrow <= searchDuration && searchingFrames.Length > 0)
        {
            var frame = Mathf.FloorToInt((searchDuration - timeUntilThrow) * searchingFramesPerSecond);
            visual.sprite = searchingFrames[Mathf.Clamp(frame, 0, searchingFrames.Length - 1)];
            return;
        }

        if (idleFrames.Length == 0)
            return;

        visual.sprite = idleFrames[(int)idleFrame];
        idleFrame = Mathf.Repeat(idleFrame + step * idleFramesPerSecond, idleFrames.Length);
    }
}
