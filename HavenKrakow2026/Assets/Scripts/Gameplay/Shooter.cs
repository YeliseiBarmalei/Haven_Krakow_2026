using UnityEngine;

[RequireComponent(typeof(ActorHealth))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class Shooter : MonoBehaviour
{
    [SerializeField, Min(1)] private int shots = 5;
    [SerializeField, Min(.1f)] private float firstShotDelay = 2.4f;
    [SerializeField, Min(.1f)] private float shotInterval = 2.6f;
    [SerializeField, Min(.1f)] private float bulletSpeed = 2.6f;
    [SerializeField, Min(0f)] private float warningDuration = .7f;
    [SerializeField] private SpriteRenderer warning;
    [Header("Shot Animation")]
    [SerializeField] private Sprite aimFrame;
    [SerializeField] private Sprite[] shotFrames;
    [SerializeField, Min(.01f)] private float shotFrameDuration = .085f;
    [Header("Knockback")]
    [SerializeField, Min(0f)] private float knockbackMultiplier = 1f;
    [SerializeField, Min(.1f)] private float maxKnockbackSpeed = 3f;
    [Header("Movement")]
    [SerializeField, Min(.1f)] private float approachSpeed = 3.8f;
    [SerializeField, Min(.1f)] private float advancePerShot = .42f;
    [SerializeField, Min(.1f)] private float repositionSpeed = 1.9f;
    [SerializeField, Min(0f)] private float postShotDelay = 1.6f;
    [Header("Elite")]
    [SerializeField, Min(1f)] private float eliteScale = 1.2f;
    [SerializeField, Min(0)] private int eliteBonusHealth = 2;
    [SerializeField, Min(0f)] private float eliteQuickStepDelay = .25f;
    [SerializeField, Min(.1f)] private float eliteBurstInterval = .9f;
    [SerializeField, Min(.1f)] private float eliteRecovery = 4.5f;
    [SerializeField, Min(.1f)] private float eliteRestingInterval = 3.4f;
    private int eliteShotIndex;
    public bool IsElite { get; private set; }

    private ActorHealth health;
    private Rigidbody2D body;
    private Vector2 warningSize;
    private int shotsLeft;
    private float cooldown;
    private Vector2 approachTarget;
    private bool approaching;
    private float repositionTime;
    private float postShotTime;
    private Vector2 repositionVelocity;
    private Vector2 lastShotOrigin;
    private Vector2 lastShotSide;
    private bool stepLeft;
    private float motionScale = 1f;
    private float normalDamping;
    private Color warningTint;
    private SpriteRenderer visual;
    private Sprite idleFrame;
    private float shotAnimationRemaining;

    public bool HasShots => isActiveAndEnabled && (shotsLeft > 0 || postShotTime > 0f || repositionTime > 0f);
    public bool CanBeHit => isActiveAndEnabled && !approaching;
    public ActorHealth Health => health;
    public float WallClearance => health.Radius + .12f;
    public Vector2 VisualExtents => GetComponent<SpriteRenderer>().bounds.extents;

    private void Awake()
    {
        health = GetComponent<ActorHealth>();
        body = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>();
        idleFrame = visual.sprite;
        normalDamping = body.linearDamping;
        warningSize = warning.sprite.bounds.size;
        warningTint = warning.color;
        shotsLeft = shots;
        cooldown = firstShotDelay;
        HideWarning();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.TryGetComponent<SlimeController>(out _))
            body.linearVelocity = Vector2.ClampMagnitude(body.linearVelocity * knockbackMultiplier,
                maxKnockbackSpeed * motionScale);
    }

    public void ConfigureWave(int wave, int index, bool elite = false)
    {
        cooldown = firstShotDelay + index * .47f;
        shotsLeft = shots + Mathf.Min(wave / 4, 5);
        shotInterval = Mathf.Max(1.65f, shotInterval - (wave - 1) * .08f);
        bulletSpeed += Mathf.Min((wave - 1) * .09f, 1.4f);
        if (!elite)
            return;

        IsElite = true;
        name = "Elite Shooter";
        transform.localScale *= eliteScale;
        health.AddStartingHealth(eliteBonusHealth);
        shotsLeft = Mathf.CeilToInt(shotsLeft / 4f) * 4;
        Physics2D.SyncTransforms();
    }

    public void ApproachFrom(Vector2 entrance, Vector2 target)
    {
        approaching = true;
        approachTarget = target;
        body.simulated = false;
        transform.position = entrance;
    }

    public void Tick(ActorHealth target, LevelController level)
    {
        var arenaSpeed = level.ArenaSpeed;
        if (!Mathf.Approximately(motionScale, arenaSpeed))
        {
            body.linearVelocity *= arenaSpeed / motionScale;
            body.linearDamping = normalDamping * arenaSpeed;
            motionScale = arenaSpeed;
        }

        var step = Time.fixedDeltaTime * arenaSpeed;
        UpdateShotAnimation(step);
        if (!HasShots)
            return;

        if (approaching)
        {
            var position = Vector2.MoveTowards(transform.position, approachTarget, approachSpeed * step);
            transform.position = position;
            if (position == approachTarget)
            {
                body.position = approachTarget;
                body.simulated = true;
                approaching = false;
            }
            return;
        }

        if (postShotTime > 0f)
        {
            if (IsElite && eliteShotIndex != 1)
                cooldown -= step;
            postShotTime -= step;
            if (postShotTime <= 0f)
                Reposition(target, level.ArenaBounds, level.ProjectileRadius);
            return;
        }

        if (repositionTime > 0f)
        {
            if (IsElite && eliteShotIndex != 1)
                cooldown -= step;
            repositionTime = Mathf.Max(0f, repositionTime - step);
            if ((body.linearVelocity / motionScale - repositionVelocity).sqrMagnitude < .003f)
            {
                body.linearVelocity = repositionVelocity * (motionScale * Mathf.Min(1f, repositionTime / step) *
                    (1f + body.linearDamping * Time.fixedDeltaTime));
            }
            return;
        }

        cooldown -= step;
        var aggressive = IsElite && eliteShotIndex < 2;
        var warningWindow = IsElite && eliteShotIndex == 1 ? Mathf.Min(warningDuration, eliteBurstInterval) : warningDuration;
        if (cooldown > warningWindow)
            return;

        var offset = target.Center - health.Center;
        if (offset.sqrMagnitude < .000001f)
        {
            HideWarning();
            return;
        }
        var direction = offset.normalized;
        if (aimFrame != null && shotAnimationRemaining <= 0f)
        {
            if (Mathf.Abs(direction.x) > .01f)
                visual.flipX = direction.x < 0f;
            visual.sprite = cooldown <= .14f ? aimFrame : idleFrame;
        }
        var muzzle = health.Center + direction * DistanceToEdge(health.HitBounds, direction);
        if (cooldown <= 0)
        {
            lastShotOrigin = muzzle;
            lastShotSide = Vector2.Perpendicular(direction);
            level.Fire(muzzle, direction * bulletSpeed);
            if (shotFrames != null && shotFrames.Length > 0)
            {
                shotAnimationRemaining = shotFrameDuration * shotFrames.Length;
                visual.sprite = shotFrames[0];
            }
            shotsLeft--;
            cooldown = shotInterval;
            HideWarning();
            var stepDelay = postShotDelay;
            if (IsElite)
            {
                var fired = eliteShotIndex;
                eliteShotIndex = (eliteShotIndex + 1) % 4;
                cooldown = fired == 0 ? eliteBurstInterval : fired == 1 ? eliteRecovery : eliteRestingInterval;
                if (fired == 0)
                    stepDelay = eliteQuickStepDelay;
            }
            if (stepDelay > 0f)
                postShotTime = stepDelay;
            else
                Reposition(target, level.ArenaBounds, level.ProjectileRadius);
            return;
        }

        var length = Mathf.Max(0f, Vector2.Dot(target.Center - muzzle, direction) - DistanceToEdge(target.HitBounds, direction));
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        warning.transform.SetPositionAndRotation(muzzle + direction * (length * .5f), Quaternion.Euler(0, 0, angle));
        var scale = transform.lossyScale;
        warning.transform.localScale = new Vector3(length / (warningSize.x * scale.x), .042f / (warningSize.y * scale.y), 1f);
        var charge = 1f - Mathf.Clamp01(cooldown / Mathf.Max(.01f, warningWindow));
        var tint = aggressive ? new Color(1f, .67f, .23f) : warningTint;
        warning.color = new Color(tint.r, tint.g, tint.b, Mathf.Lerp(.25f, .9f, charge));
        warning.enabled = true;
    }

    private void UpdateShotAnimation(float step)
    {
        if (shotAnimationRemaining <= 0f)
            return;

        shotAnimationRemaining = Mathf.Max(0f, shotAnimationRemaining - step);
        var frame = shotFrames.Length - Mathf.CeilToInt(shotAnimationRemaining / shotFrameDuration);
        visual.sprite = frame < shotFrames.Length ? shotFrames[frame] : idleFrame;
    }

    public void StopShotAnimation()
    {
        shotAnimationRemaining = 0f;
        visual.sprite = idleFrame;
    }

    private static float DistanceToEdge(Bounds bounds, Vector2 direction)
    {
        var x = Mathf.Abs(direction.x);
        var y = Mathf.Abs(direction.y);
        return Mathf.Min(x > 0f ? bounds.extents.x / x : float.PositiveInfinity,
            y > 0f ? bounds.extents.y / y : float.PositiveInfinity);
    }

    private void Reposition(ActorHealth target, Bounds bounds, float projectileRadius)
    {
        var offset = health.Center - target.Center;
        var distance = offset.magnitude;
        var minimumDistance = health.Radius + target.Radius + .55f;
        var stepDistance = Mathf.Min(distance, Mathf.Max(minimumDistance, distance - advancePerShot));
        var closer = offset.normalized * stepDistance;
        var radius = health.Radius;
        var extents = health.HitBounds.extents;
        var clearance = Mathf.Abs(lastShotSide.x) * extents.x + Mathf.Abs(lastShotSide.y) * extents.y +
            projectileRadius + .14f;
        stepLeft = !stepLeft;
        var preferredSide = stepLeft ? 1f : -1f;
        var stepAngle = Mathf.Asin(Mathf.Clamp01(clearance / Mathf.Max(stepDistance, .01f))) * Mathf.Rad2Deg;
        var bestDistance = float.PositiveInfinity;
        var travel = Vector2.zero;
        for (var angle = stepAngle; angle <= 180f; angle += 8f)
        {
            for (var side = 0; side < 2; side++)
            {
                var turn = angle * preferredSide * (side == 0 ? 1f : -1f);
                var destination = target.Center + (Vector2)(Quaternion.Euler(0f, 0f, turn) * closer);
                if (destination.x < bounds.min.x + radius || destination.x > bounds.max.x - radius ||
                    destination.y < bounds.min.y + radius || destination.y > bounds.max.y - radius ||
                    Mathf.Abs(Vector2.Dot(destination - lastShotOrigin, lastShotSide)) < clearance)
                    continue;

                var candidate = destination - health.Center;
                if (candidate.sqrMagnitude < bestDistance)
                {
                    travel = candidate;
                    bestDistance = candidate.sqrMagnitude;
                }
            }
        }

        if (travel == Vector2.zero)
            return;

        repositionTime = Mathf.Ceil(travel.magnitude / (repositionSpeed * Time.fixedDeltaTime)) * Time.fixedDeltaTime;
        repositionVelocity = travel / repositionTime;
        body.linearVelocity = repositionVelocity * (motionScale * (1f + body.linearDamping * Time.fixedDeltaTime));
    }

    public void HideWarning()
    {
        warning.enabled = false;
    }
}
