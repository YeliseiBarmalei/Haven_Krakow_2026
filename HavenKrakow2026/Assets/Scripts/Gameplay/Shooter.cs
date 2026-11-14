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
    [SerializeField, Min(.1f)] private float maxKnockbackSpeed = 3f;
    [SerializeField, Min(.1f)] private float approachSpeed = 3.8f;
    [SerializeField, Min(.1f)] private float advancePerShot = .42f;
    [SerializeField, Min(.1f)] private float repositionSpeed = 1.9f;
    [SerializeField, Min(0f)] private float postShotDelay = 1.6f;

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

    public bool HasShots => isActiveAndEnabled && (shotsLeft > 0 || postShotTime > 0f || repositionTime > 0f);
    public bool CanBeHit => isActiveAndEnabled && !approaching;
    public ActorHealth Health => health;

    private void Awake()
    {
        health = GetComponent<ActorHealth>();
        body = GetComponent<Rigidbody2D>();
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
            body.linearVelocity = Vector2.ClampMagnitude(body.linearVelocity, maxKnockbackSpeed * motionScale);
    }

    public void ConfigureWave(int wave, int index)
    {
        cooldown = firstShotDelay + index * .47f;
        shotsLeft = shots + Mathf.Min(wave / 4, 5);
        shotInterval = Mathf.Max(1.65f, shotInterval - (wave - 1) * .08f);
        bulletSpeed += Mathf.Min((wave - 1) * .09f, 1.4f);
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

        if (!HasShots)
            return;

        var step = Time.fixedDeltaTime * arenaSpeed;
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
            postShotTime -= step;
            if (postShotTime <= 0f)
                Reposition(target, level.ArenaBounds, level.ProjectileRadius);
            return;
        }

        if (repositionTime > 0f)
        {
            repositionTime = Mathf.Max(0f, repositionTime - step);
            if ((body.linearVelocity / motionScale - repositionVelocity).sqrMagnitude < .003f)
            {
                body.linearVelocity = repositionVelocity * (motionScale * Mathf.Min(1f, repositionTime / step) *
                    (1f + body.linearDamping * Time.fixedDeltaTime));
            }
            return;
        }

        cooldown -= step;
        if (cooldown > warningDuration)
            return;

        var offset = target.Center - health.Center;
        var direction = offset.normalized;
        var muzzle = health.Center + direction * health.Radius;
        if (cooldown <= 0)
        {
            lastShotOrigin = muzzle;
            lastShotSide = Vector2.Perpendicular(direction);
            level.Fire(muzzle, direction * bulletSpeed);
            shotsLeft--;
            cooldown = shotInterval;
            HideWarning();
            if (postShotDelay > 0f)
                postShotTime = postShotDelay;
            else
                Reposition(target, level.ArenaBounds, level.ProjectileRadius);
            return;
        }

        var length = Mathf.Max(0f, offset.magnitude - health.Radius - target.Radius);
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        warning.transform.SetPositionAndRotation(muzzle + direction * (length * .5f), Quaternion.Euler(0, 0, angle));
        var scale = transform.lossyScale;
        warning.transform.localScale = new Vector3(length / (warningSize.x * scale.x), .042f / (warningSize.y * scale.y), 1f);
        var charge = 1f - Mathf.Clamp01(cooldown / Mathf.Max(.01f, warningDuration));
        warning.color = new Color(warningTint.r, warningTint.g, warningTint.b, Mathf.Lerp(.25f, .9f, charge));
        warning.enabled = true;
    }

    private void Reposition(ActorHealth target, Bounds bounds, float projectileRadius)
    {
        var offset = health.Center - target.Center;
        var distance = offset.magnitude;
        var minimumDistance = health.Radius + target.Radius + .55f;
        var stepDistance = Mathf.Min(distance, Mathf.Max(minimumDistance, distance - advancePerShot));
        var closer = offset.normalized * stepDistance;
        var radius = health.Radius;
        var clearance = radius + projectileRadius + .14f;
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
