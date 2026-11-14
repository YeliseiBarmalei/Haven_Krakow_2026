using UnityEngine;

public sealed class ImpactBurst : MonoBehaviour
{
    private struct Shard
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector2 Velocity;
        public Vector2 Size;
        public Color Tint;
        public float Spin;
    }

    private static Sprite shardSprite;
    private static Sprite ringSprite;
    private static readonly System.Random Random = new System.Random();
    private Shard[] shards;
    private SpriteRenderer ring;
    private float lifetime;
    private float age;
    private float radius;

    public static void Spawn(SpriteRenderer source, Vector2 center, Vector2 direction, Color tint,
        bool defeated = false, float scale = 1f)
    {
        var burst = new GameObject(defeated ? "Enemy Defeat Burst" : "Damage Burst").AddComponent<ImpactBurst>();
        burst.transform.position = center;
        burst.transform.localScale = Vector3.one * scale;
        burst.Initialize(source, direction, tint, defeated);
    }

    private void Initialize(SpriteRenderer source, Vector2 direction, Color tint, bool defeated)
    {
        if (shardSprite == null)
            shardSprite = MakeSprite(false);
        if (ringSprite == null)
            ringSprite = MakeSprite(true);

        lifetime = defeated ? .55f : .26f;
        radius = defeated ? 1.55f : .73f;
        ring = AddSprite("Shock Ring", ringSprite, source);
        ring.color = Color.Lerp(tint, Color.white, .35f);
        ring.transform.localScale = Vector3.one * .16f;

        var random = Random;
        shards = new Shard[defeated ? 24 : 10];
        var heading = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        for (var i = 0; i < shards.Length; i++)
        {
            var scatter = direction.sqrMagnitude < .001f || defeated ? 180f : 65f;
            var angle = heading + Mathf.Lerp(-scatter, scatter, (i + (float)random.NextDouble()) / shards.Length);
            var radians = angle * Mathf.Deg2Rad;
            var outward = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            var spark = i % 3 != 0;
            var size = Mathf.Lerp(.055f, defeated ? .19f : .12f, (float)random.NextDouble());
            var renderer = AddSprite("Shard", shardSprite, source);
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var shard = new Shard
            {
                Transform = renderer.transform,
                Renderer = renderer,
                Velocity = outward * Mathf.Lerp(defeated ? 2.1f : 1.7f, defeated ? 5f : 4f, (float)random.NextDouble()),
                Size = new Vector2(size * (spark ? 2.55f : 1.15f), size),
                Tint = Color.Lerp(tint, Color.white, spark ? .52f : .08f),
                Spin = spark ? 0f : Mathf.Lerp(-560f, 560f, (float)random.NextDouble())
            };
            renderer.color = shard.Tint;
            shard.Transform.localScale = shard.Size;
            shards[i] = shard;
        }
    }

    private void LateUpdate()
    {
        age += Time.deltaTime;
        var progress = age / lifetime;
        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        var expansion = 1f - (1f - progress) * (1f - progress);
        ring.transform.localScale = Vector3.one * Mathf.Lerp(.16f, radius * 2f, expansion);
        var ringColor = ring.color;
        ringColor.a = .9f * (1f - expansion);
        ring.color = ringColor;
        foreach (var shard in shards)
        {
            shard.Transform.localPosition = shard.Velocity * (lifetime * expansion * .55f);
            shard.Transform.Rotate(0f, 0f, shard.Spin * Time.deltaTime);
            shard.Transform.localScale = shard.Size * Mathf.Lerp(1f, .28f, progress);
            var tint = shard.Tint;
            tint.a = 1f - progress * progress;
            shard.Renderer.color = tint;
        }
    }

    private SpriteRenderer AddSprite(string label, Sprite sprite, SpriteRenderer source)
    {
        var renderer = new GameObject(label).AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(transform, false);
        renderer.sprite = sprite;
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + 4;
        return renderer;
    }

    private static Sprite MakeSprite(bool ring)
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x + .5f, y + .5f) / size * 2f - Vector2.one;
                var distance = ring ? Mathf.Abs(point.magnitude - .83f) : Mathf.Abs(point.x) + Mathf.Abs(point.y);
                var alpha = Mathf.Clamp01((ring ? .11f - distance : .94f - distance) * 28f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * .5f, size, 0, SpriteMeshType.FullRect);
    }
}
