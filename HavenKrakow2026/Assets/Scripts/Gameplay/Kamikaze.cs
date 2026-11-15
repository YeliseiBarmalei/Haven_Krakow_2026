using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public sealed class Kamikaze : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float startingSpeed = .9f;
    [SerializeField, Min(.1f)] private float acceleration = 1.25f;
    [SerializeField, Min(.1f)] private float maximumSpeed = 5f;
    [SerializeField, Min(.5f)] private float explosionDamage = 2f;
    [SerializeField, Min(.1f)] private float explosionVisualScale = 1.8f;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField, Range(0f, 1f)] private float explosionVolume = .55f;

    private Rigidbody2D body;
    private Collider2D shape;
    private SpriteRenderer visual;
    private ActorHealth grandma;
    private LevelController arena;
    private float speed;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<Collider2D>();
        visual = GetComponent<SpriteRenderer>();
    }

    public void Begin(Vector2 entry, ActorHealth target, LevelController level)
    {
        grandma = target;
        arena = level;
        speed = Mathf.Min(startingSpeed, maximumSpeed);
        body.position = entry;
        transform.position = entry;
        IsActive = true;
    }

    public void Tick(float step)
    {
        if (!IsActive)
            return;

        speed = Mathf.MoveTowards(speed, maximumSpeed, acceleration * step);
        var direction = grandma.Center - body.position;
        var position = Vector2.MoveTowards(body.position, grandma.Center, speed * step);
        if (Mathf.Abs(direction.x) > .01f)
            visual.flipX = direction.x < 0f;

        var radius = shape.bounds.extents.x;
        var point = new Vector3(position.x, position.y, grandma.HitBounds.center.z);
        if (grandma.HitBounds.SqrDistance(point) <= radius * radius)
        {
            Explode();
            grandma.TakeHit(explosionDamage, direction);
            return;
        }

        body.MovePosition(position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryRam(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryRam(other);
    }

    private void TryRam(Collider2D other)
    {
        if (!IsActive || !other.TryGetComponent<SlimeController>(out var slime) || !slime.CanRam)
            return;

        Explode();
        arena.RegisterKamikazeDefeat();
    }

    public void Stop()
    {
        IsActive = false;
        body.linearVelocity = Vector2.zero;
    }

    private void Explode()
    {
        Stop();
        ImpactBurst.SpawnExplosion(visual, body.position, explosionVisualScale);
        GameAudio.Play(explosionSound, explosionVolume);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
