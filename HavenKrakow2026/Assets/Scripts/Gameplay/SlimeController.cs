using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SlimeController : MonoBehaviour
{
    private static readonly Color[] AimColors =
    {
        new Color32(94, 231, 138, 255),
        new Color32(255, 168, 67, 255),
        new Color32(255, 91, 91, 255)
    };

    [SerializeField, Min(.1f)] private float dragLimit = 2.5f;
    [SerializeField, Min(.1f)] private float launchSpeed = 16f;
    [SerializeField, Min(0f)] private float stopSpeed = .25f;
    [SerializeField] private Transform aimShaft;
    [SerializeField] private Transform aimHead;
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(0.1f)] private float hitCooldown = 1f;
    [SerializeField] private Color flightTint = new Color(.45f, .85f, 1f);
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField, Min(1f)] private float idleFramesPerSecond = 8f;
    [SerializeField] private Sprite flightSprite;
    [SerializeField] private AudioClip slideSound;
    [SerializeField] private AudioClip damageSound;

    [Header("Slime-Devistan Afterimages")]
    [SerializeField, Range(0, 24)] private int afterimageCount = 14;
    [SerializeField, Min(.01f)] private float afterimageInterval = .025f;
    [SerializeField, Min(.01f)] private float afterimageLifetime = .48f;
    [SerializeField, Min(.01f)] private float afterimageSpacing = .13f;
    [SerializeField] private Color afterimageCyan = new Color(.18f, 1f, .85f, .72f);
    [SerializeField] private Color afterimageViolet = new Color(.74f, .32f, 1f, .65f);
    [SerializeField] private AudioClip slowMotionMovementSound;
    [SerializeField, Range(0f, 1f)] private float slowMotionMovementVolume = .55f;

    private AudioSource slowMotionMovementSource;

    private struct Afterimage
    {
        public SpriteRenderer Renderer;
        public Color Tint;
        public float BornAt;
    }

    private Afterimage[] afterimages;
    private Transform afterimageRoot;
    private int nextAfterimage;
    private float nextAfterimageTime;
    private Vector2 lastAfterimagePosition;
    private bool emitAfterimages;

    private Rigidbody2D body;
    private CircleCollider2D shape;
    private Camera view;
    private Vector2 dragStart;
    private Vector2 shaftSize;
    private SpriteRenderer aimHeadVisual;
    private SpriteRenderer stopMarker;
    private readonly SpriteRenderer[] powerTicks = new SpriteRenderer[9];
    private readonly RaycastHit2D[] stopHits = new RaycastHit2D[16];
    private readonly ContactFilter2D stopFilter = new ContactFilter2D { useTriggers = false };
    private Vector2 incomingVelocity;
    private bool aiming;
    private bool launched;
    private bool fullPowerLaunch;
    private SpriteRenderer visual;
    private Color restingTint;
    private float nextHitTime;
    private float idleTime;
    private float damageFlashUntil;

    public Vector2 Position => body.position;
    public float Radius => shape.radius * transform.lossyScale.x;
    public int Pieces { get; private set; }
    public float Health { get; private set; }
    public bool IsInFlight => launched;
    public bool CanRam => CanLaunch && launched && fullPowerLaunch;
    public int MaxHealth => maxHealth;
    public bool CanLaunch { get; set; } = true;
    public bool CanTakeShooterContactDamage { get; set; } = true;
    public RectTransform PauseButton { private get; set; }
    public bool CanDamageGrandma { get; set; } = true;
    public event System.Action PieceCollected;
    public event System.Action HealthChanged;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<CircleCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        restingTint = visual.color;
        Health = maxHealth;
        view = Camera.main;
        var shaftVisual = aimShaft.GetComponent<SpriteRenderer>();
        shaftSize = shaftVisual.sprite.bounds.size;
        aimHeadVisual = aimHead.GetComponent<SpriteRenderer>();
        stopMarker = new GameObject("Estimated Stop").AddComponent<SpriteRenderer>();
        stopMarker.transform.SetParent(transform, false);
        stopMarker.sprite = shaftVisual.sprite;
        stopMarker.sharedMaterial = shaftVisual.sharedMaterial;
        stopMarker.sortingLayerID = shaftVisual.sortingLayerID;
        stopMarker.sortingOrder = shaftVisual.sortingOrder + 1;
        shaftVisual.color = new Color32(24, 33, 46, 210);
        for (var i = 0; i < powerTicks.Length; i++)
        {
            var tick = new GameObject($"Power Tick {i + 1}").AddComponent<SpriteRenderer>();
            tick.transform.SetParent(aimShaft, false);
            tick.sprite = shaftVisual.sprite;
            tick.sharedMaterial = shaftVisual.sharedMaterial;
            tick.sortingLayerID = shaftVisual.sortingLayerID;
            tick.sortingOrder = shaftVisual.sortingOrder + 1;
            tick.color = AimColors[i / 3];
            powerTicks[i] = tick;
        }
        HideAim();
    }

    public void CollectPiece()
    {
        Pieces++;
        PieceCollected?.Invoke();
    }

    public void SetSlowMotionAfterimages(bool active)
    {
        if (emitAfterimages == active)
            return;

        emitAfterimages = active;
        lastAfterimagePosition = transform.position;
        nextAfterimageTime = Time.time;
    }

    private void LateUpdate()
    {
        UpdateSlowMotionMovementAudio();
        var now = Time.time;
        if (afterimages != null)
        {
            foreach (var image in afterimages)
            {
                if (!image.Renderer.enabled)
                    continue;

                var remaining = 1f - (now - image.BornAt) / afterimageLifetime;
                image.Renderer.enabled = remaining > 0f;
                var tint = image.Tint;
                tint.a *= Mathf.Clamp01(remaining) * Mathf.Clamp01(remaining);
                image.Renderer.color = tint;
            }
        }

        var position = (Vector2)transform.position;
        if (!emitAfterimages || afterimageCount == 0 || Time.timeScale <= 0f ||
            now < nextAfterimageTime || !visual.enabled || visual.sprite == null ||
            (position - lastAfterimagePosition).sqrMagnitude < afterimageSpacing * afterimageSpacing)
            return;

        if (afterimages == null)
            CreateAfterimages();

        ref var imageSlot = ref afterimages[nextAfterimage % afterimages.Length];
        var ghost = imageSlot.Renderer;
        imageSlot.Tint = nextAfterimage % 2 == 0 ? afterimageCyan : afterimageViolet;
        imageSlot.BornAt = now;
        ghost.sprite = visual.sprite;
        ghost.flipX = visual.flipX;
        ghost.flipY = visual.flipY;
        ghost.color = imageSlot.Tint;
        ghost.transform.SetPositionAndRotation(transform.position, transform.rotation);
        ghost.transform.localScale = transform.lossyScale;
        ghost.enabled = true;

        nextAfterimage++;
        nextAfterimageTime = now + afterimageInterval;
        lastAfterimagePosition = position;
    }

    private void UpdateSlowMotionMovementAudio()
    {
        if (slowMotionMovementSound == null)
            return;

        var speed = body.linearVelocity.magnitude;
        var audible = GameAudio.PlayerMovementEnabled && emitAfterimages && Time.timeScale > 0f &&
            !FadeTransition.IsBusy && speed > stopSpeed;
        if (audible && slowMotionMovementSource == null)
        {
            slowMotionMovementSource = gameObject.AddComponent<AudioSource>();
            slowMotionMovementSource.playOnAwake = false;
            slowMotionMovementSource.loop = true;
            slowMotionMovementSource.spatialBlend = 0f;
            slowMotionMovementSource.clip = slowMotionMovementSound;
            slowMotionMovementSource.volume = 0f;
        }

        if (slowMotionMovementSource == null)
            return;

        var targetVolume = audible
            ? slowMotionMovementVolume * GameAudio.EffectsVolume * Mathf.Lerp(.45f, 1f, Mathf.Clamp01(speed / launchSpeed))
            : 0f;
        slowMotionMovementSource.volume = Mathf.MoveTowards(
            slowMotionMovementSource.volume, targetVolume, Time.unscaledDeltaTime * 8f);

        if (audible && !slowMotionMovementSource.isPlaying)
            slowMotionMovementSource.Play();
        else if (!audible && slowMotionMovementSource.volume == 0f && slowMotionMovementSource.isPlaying)
            slowMotionMovementSource.Stop();
    }

    private void CreateAfterimages()
    {
        afterimageRoot = new GameObject("Slime-Devistan Afterimages").transform;
        afterimages = new Afterimage[afterimageCount];
        for (var i = 0; i < afterimages.Length; i++)
        {
            var ghost = new GameObject("Afterimage").AddComponent<SpriteRenderer>();
            ghost.transform.SetParent(afterimageRoot, false);
            ghost.sharedMaterial = visual.sharedMaterial;
            ghost.sortingLayerID = visual.sortingLayerID;
            ghost.sortingOrder = visual.sortingOrder - 1;
            ghost.enabled = false;
            afterimages[i].Renderer = ghost;
        }
    }

    private void OnDisable()
    {
        emitAfterimages = false;
        if (slowMotionMovementSource != null)
        {
            slowMotionMovementSource.Stop();
            slowMotionMovementSource.volume = 0f;
        }
        if (afterimages == null)
            return;

        foreach (var image in afterimages)
            if (image.Renderer != null)
                image.Renderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (afterimageRoot != null)
            Destroy(afterimageRoot.gameObject);
    }

    public void UpgradeHealth()
    {
        maxHealth++;
        Health++;
        HealthChanged?.Invoke();
    }

    public void StopLaunch()
    {
        body.linearVelocity = Vector2.zero;
        launched = false;
        fullPowerLaunch = false;
        visual.color = restingTint;
        idleTime = 0f;
        UpdateIdleSprite();
        visual.flipX = false;
    }

    public void KeepInside(Bounds bounds)
    {
        var position = Position;
        body.position = new Vector2(
            Mathf.Clamp(position.x, bounds.min.x + Radius, bounds.max.x - Radius),
            Mathf.Clamp(position.y, bounds.min.y + Radius, bounds.max.y - Radius));
    }

    public void SwapTo(Vector2 position)
    {
        StopLaunch();
        HideAim();
        body.position = position;
        transform.position = position;
    }

    private void Update()
    {
        if (!launched)
        {
            idleTime += Time.deltaTime;
            UpdateIdleSprite();
            visual.color = Time.time < damageFlashUntil ? new Color(1f, .42f, .45f) : restingTint;
        }

        if (!CanLaunch || FadeTransition.IsBusy || Time.timeScale == 0 || !Application.isFocused ||
            view == null || Mouse.current is not { } mouse || Keyboard.current?.escapeKey.wasPressedThisFrame == true)
        {
            HideAim();
            return;
        }

        var cursor = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (PauseButton != null && PauseButton.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(PauseButton, cursor))
                return;

            aiming = true;
            dragStart = cursor;
        }

        if (!aiming)
            return;

        var worldUnitsPerPixel = 2f * view.orthographicSize / view.pixelHeight;
        var rawPull = (cursor - dragStart) * worldUnitsPerPixel;
        var pull = Vector2.ClampMagnitude(rawPull, dragLimit);
        var fullPower = rawPull.sqrMagnitude >= dragLimit * dragLimit;
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (pull.sqrMagnitude > .01f)
            {
                body.linearVelocity = pull * (launchSpeed / dragLimit);
                launched = true;
                fullPowerLaunch = fullPower;
                if (GameAudio.PlayerMovementEnabled)
                    GameAudio.Play(slideSound, .68f);
                visual.color = flightTint;
                visual.sprite = flightSprite;
                visual.flipX = pull.x < 0f;
            }
            HideAim();
            return;
        }

        if (!mouse.leftButton.isPressed)
        {
            HideAim();
            return;
        }

        ShowAim(pull, fullPower);
    }

    private void UpdateIdleSprite()
    {
        if (idleFrames == null || idleFrames.Length < 2)
        {
            visual.sprite = idleSprite;
            return;
        }

        var last = idleFrames.Length - 1;
        var cycleLength = last * 2;
        idleTime %= cycleLength / idleFramesPerSecond;
        var frame = Mathf.FloorToInt(idleTime * idleFramesPerSecond);
        visual.sprite = idleFrames[Mathf.Min(frame, cycleLength - frame)];
    }

    private void FixedUpdate()
    {
        incomingVelocity = body.linearVelocity;
        if (launched && incomingVelocity.sqrMagnitude <= stopSpeed * stopSpeed)
            StopLaunch();
        else if (launched && Mathf.Abs(incomingVelocity.x) > stopSpeed)
            visual.flipX = incomingVelocity.x < 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var hitEnemy = CheckEnemyContact(collision);
        if (!CanLaunch || !launched || !collision.collider.TryGetComponent<ActorHealth>(out var actor))
            return;

        if (collision.contactCount > 0)
            SlidePastActor(collision.GetContact(0).normal);
        if (!hitEnemy && CanDamageGrandma)
            actor.TakeHit(.5f, incomingVelocity);
    }

    private void SlidePastActor(Vector2 normal)
    {
        var speed = incomingVelocity.magnitude;
        if (speed <= stopSpeed || Vector2.Dot(incomingVelocity, normal) >= 0f)
            return;

        var tangent = Vector2.Perpendicular(normal);
        var slideSpeed = Vector2.Dot(incomingVelocity, tangent);
        var direction = slideSpeed < 0f ? -1f : 1f;
        body.linearVelocity = tangent * (direction * Mathf.Max(Mathf.Abs(slideSpeed), speed * .55f)) +
            normal * (speed * .12f);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckEnemyContact(collision);
    }

    private bool CheckEnemyContact(Collision2D collision)
    {
        if (!collision.collider.TryGetComponent<Shooter>(out _))
            return false;

        if (CanTakeShooterContactDamage && CanLaunch && !launched && Time.time >= nextHitTime)
        {
            nextHitTime = Time.time + hitCooldown;
            TakeHit();
        }

        return true;
    }

    public void TakeHit(Vector2 direction = default)
    {
        if (launched)
            return;

        ApplyDamage(1f, direction);
    }

    public void TakeHazardDamage(float damage)
    {
        ApplyDamage(damage, Vector2.down);
    }

    private void ApplyDamage(float damage, Vector2 direction)
    {
        if (!CanLaunch || Health <= 0f)
            return;

        Health = Mathf.Max(0f, Health - damage);
        ImpactBurst.Spawn(visual, Position, direction, new Color(.24f, .94f, .89f));
        GameAudio.Play(damageSound, .85f);
        damageFlashUntil = Time.time + .16f;
        visual.color = new Color(1f, .42f, .45f);
        HealthChanged?.Invoke();
    }

    public bool Heal()
    {
        if (Health <= 0 || Health >= MaxHealth)
            return false;

        Health = Mathf.Min(MaxHealth, Health + 1f);
        HealthChanged?.Invoke();
        return true;
    }

    private void ShowAim(Vector2 pull, bool fullPower)
    {
        var length = pull.magnitude;
        var direction = pull.normalized;
        var start = (Vector2)transform.position + direction * Radius;
        var rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        var scale = transform.lossyScale.x;
        if (length <= .1f)
        {
            aimShaft.gameObject.SetActive(false);
            aimHead.gameObject.SetActive(false);
            stopMarker.enabled = false;
            return;
        }

        aimShaft.SetPositionAndRotation(start + pull * .5f, rotation);
        aimShaft.localScale = new Vector3(length / (shaftSize.x * scale), .075f / (shaftSize.y * scale), 1f);
        var tickLength = dragLimit / powerTicks.Length;
        for (var i = 0; i < powerTicks.Length; i++)
        {
            var offset = i * tickLength;
            var width = Mathf.Clamp(length - offset, 0f, tickLength * .76f);
            var tick = powerTicks[i];
            tick.enabled = width > 0f;
            tick.transform.localPosition = new Vector3(((offset + width * .5f) / length - .5f) * shaftSize.x, 0f, 0f);
            tick.transform.localScale = new Vector3(width / length, 1.65f, 1f);
        }

        var colorIndex = Mathf.Min((int)(length / dragLimit * AimColors.Length), AimColors.Length - 1);
        aimHeadVisual.color = fullPower ? Color.white : AimColors[colorIndex];
        aimHead.SetPositionAndRotation(start + pull, rotation * Quaternion.Euler(0, 0, -90));
        var headSize = fullPower ? 1.25f : 1f;
        aimHead.localScale = new Vector3(.28f * headSize / scale, .29f * headSize / scale, 1f);
        aimShaft.gameObject.SetActive(true);
        aimHead.gameObject.SetActive(true);
        ShowStopMarker(pull);
    }

    private void ShowStopMarker(Vector2 pull)
    {
        // Damping sets the stopping distance on an unobstructed slide.
        var speed = pull.magnitude * launchSpeed / dragLimit;
        var distance = Mathf.Max(0f, speed - stopSpeed) / Mathf.Max(.01f, body.linearDamping);
        var direction = pull.normalized;
        var count = shape.Cast(direction, stopFilter, stopHits, distance);
        for (var i = 0; i < count; i++)
            distance = Mathf.Min(distance, stopHits[i].distance);

        stopMarker.transform.SetPositionAndRotation((Vector2)transform.position + direction * distance, Quaternion.Euler(0, 0, 45f));
        stopMarker.transform.localScale = new Vector3(.12f / (shaftSize.x * transform.lossyScale.x),
            .12f / (shaftSize.y * transform.lossyScale.y), 1f);
        stopMarker.color = new Color(aimHeadVisual.color.r, aimHeadVisual.color.g, aimHeadVisual.color.b, .7f);
        stopMarker.enabled = distance > Radius;
    }

    private void HideAim()
    {
        aiming = false;
        aimShaft.gameObject.SetActive(false);
        aimHead.gameObject.SetActive(false);
        stopMarker.enabled = false;
    }
}
