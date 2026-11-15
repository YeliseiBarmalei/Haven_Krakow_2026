using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class ActorHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField] private Color injuredTint = new Color(1f, .52f, .55f);
    [SerializeField, Min(0f)] private float hitFlashDuration;
    [SerializeField] private bool hideOnDeath;
    [SerializeField] private AudioClip damageSound;

    private Collider2D shape;
    private SpriteRenderer visual;
    private Color healthyTint;
    private float hitFlashRemaining;

    public float Current { get; private set; }
    public int Max => maxHealth;
    public Vector2 Center => shape.bounds.center;
    public float Radius => shape.bounds.extents.x;
    public Bounds HitBounds => shape.bounds;
    public event System.Action Changed;

    private void Awake()
    {
        shape = GetComponent<Collider2D>();
        visual = GetComponent<SpriteRenderer>();
        healthyTint = visual.color;
        Current = maxHealth;
    }

    private void Update()
    {
        if (hitFlashRemaining <= 0f)
            return;

        hitFlashRemaining = Mathf.Max(0f, hitFlashRemaining - Time.deltaTime);
        if (hitFlashRemaining == 0f)
            UpdateTint();
    }

    public void UpgradeHealth()
    {
        maxHealth++;
        Current++;
        UpdateTint();
        Changed?.Invoke();
    }

    public void AddStartingHealth(int bonus)
    {
        maxHealth += bonus;
        Current += bonus;
    }

    public void TakeHit(float damage = 1f, Vector2 direction = default)
    {
        if (Current == 0 || damage <= 0)
            return;

        Current = Mathf.Max(0f, Current - damage);
        var defeated = Current == 0 && hideOnDeath;
        var tint = hideOnDeath ? new Color(1f, .55f, .21f) : new Color(1f, .42f, .59f);
        ImpactBurst.Spawn(visual, Center, direction, tint, defeated);
        GameAudio.Play(damageSound, .85f);
        hitFlashRemaining = defeated ? 0f : hitFlashDuration;
        UpdateTint();
        if (defeated)
            gameObject.SetActive(false);
        Changed?.Invoke();
    }

    private void UpdateTint()
    {
        visual.color = hitFlashRemaining > 0f
            ? new Color(1f, .24f, .24f, healthyTint.a)
            : Color.Lerp(healthyTint, injuredTint, 1f - Current / Max);
    }

    public bool Heal()
    {
        if (Current <= 0 || Current >= Max)
            return false;

        Current = Mathf.Min(Max, Current + 1f);
        UpdateTint();
        Changed?.Invoke();
        return true;
    }
}
