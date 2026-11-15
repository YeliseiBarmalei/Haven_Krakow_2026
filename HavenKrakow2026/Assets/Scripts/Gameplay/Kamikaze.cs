using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public sealed class Kamikaze : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float startingSpeed = .9f;
    [SerializeField, Min(.1f)] private float acceleration = 1.25f;
    [SerializeField, Min(.1f)] private float maximumSpeed = 5f;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField, Min(.1f)] private float walkFramesPerSecond = 8f;
    [SerializeField, Min(.5f)] private float explosionDamage = 2f;
    [SerializeField, Min(.1f)] private float explosionVisualScale = 1.8f;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField, Range(0f, 1f)] private float explosionVolume = .55f;
    [SerializeField] private AudioClip runSound;
    [SerializeField, Range(0f, 1f)] private float runVolume = .12f;

    private Rigidbody2D body;
    private Collider2D shape;
    private SpriteRenderer visual;
    private AudioSource runAudio;
    private ActorHealth grandma;
    private LevelController arena;
    private float speed;
    private float walkFrame;

    public bool IsActive { get; private set; }
    public Bounds VisualBounds => visual.bounds;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<Collider2D>();
        visual = GetComponent<SpriteRenderer>();
        if (runSound != null)
        {
            runAudio = gameObject.AddComponent<AudioSource>();
            runAudio.playOnAwake = false;
            runAudio.loop = true;
            runAudio.clip = runSound;
        }
    }

    public void Begin(Vector2 entry, ActorHealth target, LevelController level)
    {
        grandma = target;
        arena = level;
        speed = Mathf.Min(startingSpeed, maximumSpeed);
        body.position = entry;
        transform.position = entry;
        walkFrame = 0f;
        FaceRunDirection(grandma.Center.x - entry.x);
        AnimateRun(0f);
        IsActive = true;
    }

    private void Update()
    {
        if (!IsActive || runAudio == null)
            return;

        runAudio.volume = runVolume * GameAudio.EffectsVolume;
        if (Time.timeScale == 0f || FadeTransition.IsBusy)
        {
            runAudio.Pause();
            return;
        }

        runAudio.pitch = arena.ArenaSpeed;
        runAudio.UnPause();
        if (!runAudio.isPlaying)
            runAudio.Play();
    }

    public void Tick(float step, bool dealDamage = true)
    {
        if (!IsActive)
            return;

        speed = Mathf.MoveTowards(speed, maximumSpeed, acceleration * step);
        var direction = grandma.Center - body.position;
        var position = Vector2.MoveTowards(body.position, grandma.Center, speed * step);
        FaceRunDirection(direction.x);
        AnimateRun(step);

        var radius = shape.bounds.extents.x;
        var point = new Vector3(position.x, position.y, grandma.HitBounds.center.z);
        if (grandma.HitBounds.SqrDistance(point) <= radius * radius)
        {
            Explode();
            if (dealDamage)
                grandma.TakeHit(explosionDamage, direction);
            return;
        }

        body.MovePosition(position);
    }

    private void FaceRunDirection(float horizontalDirection)
    {
        if (Mathf.Abs(horizontalDirection) > .01f)
            visual.flipX = horizontalDirection > 0f;
    }

    private void AnimateRun(float step)
    {
        if (walkFrames == null || walkFrames.Length == 0)
            return;

        visual.sprite = walkFrames[(int)walkFrame];
        var pace = Mathf.Lerp(.65f, 1.65f, speed / maximumSpeed);
        walkFrame = Mathf.Repeat(walkFrame + step * walkFramesPerSecond * pace, walkFrames.Length);
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
        if (runAudio != null)
            runAudio.Stop();
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
