using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class SlimePiece : MonoBehaviour
{
    [SerializeField] private Transform landingMarker;
    [SerializeField] private AudioClip pickupSound;

    private bool collected;
    private Vector2 origin;
    private Vector2 destination;
    private float remainingFlight;
    private float flightDuration;
    private bool falling;
    private ActorHealth grandmaToHeal;
    private LevelController arena;
    private float lifetime;
    private const float FlightDuration = .62f;

    public void ThrowTo(Vector2 landing, LevelController level)
    {
        arena = level;
        BeginFlight(landing, FlightDuration);
    }

    public void DropAt(Vector2 landing, Sprite sprite, ActorHealth grandma, LevelController level)
    {
        arena = level;
        lifetime = 12f;
        falling = true;
        grandmaToHeal = grandma;
        GetComponent<SpriteRenderer>().sprite = sprite;
        BeginFlight(landing, 1f);
    }

    private void BeginFlight(Vector2 landing, float duration)
    {
        origin = transform.position;
        destination = landing;
        remainingFlight = flightDuration = duration;
        GetComponent<CircleCollider2D>().enabled = false;
        landingMarker.position = destination;
        landingMarker.gameObject.SetActive(true);
    }

    private void Update()
    {
        var step = Time.deltaTime * (arena != null ? arena.ArenaSpeed : 1f);
        if (falling)
        {
            lifetime -= step;
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (remainingFlight <= 0f)
            return;

        remainingFlight = Mathf.Max(0f, remainingFlight - step);
        var progress = 1f - remainingFlight / flightDuration;
        var arc = falling ? 0f : .45f * Mathf.Sin(progress * Mathf.PI);
        transform.position = Vector2.Lerp(origin, destination, falling ? progress * progress : progress) + Vector2.up * arc;
        landingMarker.position = destination;
        if (remainingFlight == 0f)
        {
            transform.position = destination;
            landingMarker.gameObject.SetActive(false);
            GetComponent<CircleCollider2D>().enabled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.TryGetComponent<SlimeController>(out var slime) || !slime.CanLaunch)
            return;

        if (falling)
        {
            var healed = grandmaToHeal != null ? grandmaToHeal.Heal() : slime.Heal();
            if (!healed)
                return;
        }
        else
        {
            slime.CollectPiece();
        }

        collected = true;
        GameAudio.Play(pickupSound, .65f);
        Destroy(gameObject);
    }
}
