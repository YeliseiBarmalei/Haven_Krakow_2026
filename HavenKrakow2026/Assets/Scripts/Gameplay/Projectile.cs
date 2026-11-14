using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private Color deflectedTint = new Color(.46f, 1f, .73f);
    [SerializeField, Min(0f)] private float returnSpeedPerSlimeSpeed = .6f;

    private SpriteRenderer visual;
    private Vector2 previousPosition;
    private Vector2 bouncePosition;
    private float bounceProgress = -1f;
    public Vector2 Position { get; private set; }
    public float Radius { get; private set; }
    public Vector2 Velocity { get; set; }
    public bool IsDeflected { get; private set; }

    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        Radius = GetComponent<CircleCollider2D>().radius * transform.lossyScale.x;
        Position = previousPosition = transform.position;
    }

    public void AdvanceTo(Vector2 position)
    {
        previousPosition = Position;
        Position = position;
        bounceProgress = -1f;
    }

    public void Deflect(Vector2 impact, float hitTime, float slimeSpeed, float arenaSpeed)
    {
        var incomingSpeed = Velocity.magnitude;
        Velocity = -Velocity.normalized * (incomingSpeed + slimeSpeed * returnSpeedPerSlimeSpeed);
        IsDeflected = true;
        visual.color = deflectedTint;

        AdvanceTo(impact + Velocity * (Time.fixedDeltaTime * arenaSpeed * (1f - hitTime)));
        bouncePosition = impact;
        bounceProgress = hitTime;
    }

    private void LateUpdate()
    {
        // Match the interpolated Rigidbody2D on the slime.
        var progress = Mathf.Clamp01((float)((Time.timeAsDouble - Time.fixedTimeAsDouble) / Time.fixedDeltaTime));
        if (bounceProgress < 0f)
            transform.position = Vector2.Lerp(previousPosition, Position, progress);
        else if (progress < bounceProgress)
            transform.position = Vector2.Lerp(previousPosition, bouncePosition, progress / bounceProgress);
        else
            transform.position = Vector2.Lerp(bouncePosition, Position, Mathf.InverseLerp(bounceProgress, 1f, progress));
    }
}
