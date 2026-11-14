using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class ActorHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField] private Color injuredTint = new Color(1f, .52f, .55f);
    [SerializeField] private bool hideOnDeath;
    [SerializeField] private AudioClip damageSound;

    private Collider2D shape;
    private SpriteRenderer visual;
    private Color healthyTint;

    public int Current { get; private set; }
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

    public void UpgradeHealth()
    {
        maxHealth++;
        Current++;
        UpdateTint();
        Changed?.Invoke();
    }

    public void TakeHit(int damage = 1)
    {
        if (Current == 0 || damage <= 0)
            return;

        Current = Mathf.Max(0, Current - damage);
        GameAudio.Play(damageSound, .85f);
        UpdateTint();
        if (Current == 0 && hideOnDeath)
            gameObject.SetActive(false);
        Changed?.Invoke();
    }

    private void UpdateTint()
    {
        visual.color = Color.Lerp(healthyTint, injuredTint, 1f - (float)Current / Max);
    }

    public bool Heal()
    {
        if (Current <= 0 || Current >= Max)
            return false;

        Current++;
        UpdateTint();
        Changed?.Invoke();
        return true;
    }
}
