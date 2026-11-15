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
    private static Sprite cloudSprite;
    private static readonly System.Random Random = new System.Random();
    private Shard[] shards;
    private SpriteRenderer ring;
    private SpriteRenderer flash;
    private Shard[] smoke;
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

    public static void SpawnExplosion(SpriteRenderer source, Vector2 center, float scale)
    {
        var burst = new GameObject("Kamikaze Explosion").AddComponent<ImpactBurst>();
        burst.transform.position = center;
        burst.transform.localScale = Vector3.one * scale;
        burst.Initialize(source, Vector2.zero, new Color(1f, .26f, .05f), true, explosion: true);
    }

    private void Initialize(SpriteRenderer source, Vector2 direction, Color tint, bool defeated, bool explosion = false)
    {
        if (shardSprite == null)
            shardSprite = MakeSprite(false);
        if (ringSprite == null)
            ringSprite = MakeSprite(true);

        lifetime = explosion ? 1.05f : defeated ? .55f : .26f;
        radius = defeated ? 1.55f : .73f;
        ring = AddSprite("Shock Ring", ringSprite, source);
        ring.color = Color.Lerp(tint, Color.white, .35f);
        ring.transform.localScale = Vector3.one * .16f;

        var random = Random;
        shards = new Shard[explosion ? 38 : defeated ? 24 : 10];
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

        if (explosion)
            AddExplosionLayers(source);
    }

    private void AddExplosionLayers(SpriteRenderer source)
    {
        if (cloudSprite == null)
            cloudSprite = MakeSprite(false, cloud: true);

        flash = AddSprite("Explosion Flash", cloudSprite, source);
        flash.sortingOrder = ring.sortingOrder + 1;
        flash.color = new Color(1f, .97f, .76f);
        flash.transform.localScale = Vector3.one * .35f;

        smoke = new Shard[9];
        for (var i = 0; i < smoke.Length; i++)
        {
            var angle = (i + (float)Random.NextDouble()) * Mathf.PI * 2f / smoke.Length;
            var outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var puff = AddSprite("Fire and Smoke", cloudSprite, source);
            puff.sortingOrder = ring.sortingOrder - 1;
            puff.color = new Color(1f, .35f, .06f, .9f);
            puff.transform.localScale = Vector3.one * .38f;
            smoke[i] = new Shard
            {
                Renderer = puff,
                Transform = puff.transform,
                Velocity = outward * Mathf.Lerp(.6f, 1.25f, (float)Random.NextDouble()),
                Size = Vector2.one * Mathf.Lerp(.58f, .96f, (float)Random.NextDouble()),
                Spin = Mathf.Lerp(-85f, 85f, (float)Random.NextDouble())
            };
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

        var waveProgress = flash != null ? Mathf.Clamp01(age / .45f) : progress;
        var expansion = 1f - (1f - waveProgress) * (1f - waveProgress);
        ring.transform.localScale = Vector3.one * Mathf.Lerp(.16f, radius * 2f, expansion);
        var ringColor = ring.color;
        ringColor.a = .9f * (1f - expansion);
        ring.color = ringColor;
        var shardExpansion = 1f - (1f - progress) * (1f - progress);
        foreach (var shard in shards)
        {
            shard.Transform.localPosition = shard.Velocity * (lifetime * shardExpansion * .55f);
            shard.Transform.Rotate(0f, 0f, shard.Spin * Time.deltaTime);
            shard.Transform.localScale = shard.Size * Mathf.Lerp(1f, .28f, progress);
            var tint = shard.Tint;
            tint.a = 1f - progress * progress;
            shard.Renderer.color = tint;
        }
        if (flash != null)
            AnimateExplosionLayers(progress);
    }

    private void AnimateExplosionLayers(float progress)
    {
        var flashProgress = Mathf.Clamp01(age / .19f);
        flash.transform.localScale = Vector3.one * Mathf.Lerp(.35f, 1.5f, flashProgress);
        flash.color = new Color(1f, Mathf.Lerp(.97f, .42f, flashProgress), .24f, 1f - flashProgress);

        var fire = new Color(1f, .35f, .06f, .95f);
        var ash = new Color(.22f, .19f, .21f, .7f);
        foreach (var puff in smoke)
        {
            puff.Transform.localPosition = puff.Velocity * Mathf.Sqrt(progress) + Vector2.up * (progress * .45f);
            puff.Transform.localScale = puff.Size * Mathf.Lerp(.4f, 1.55f, Mathf.Sqrt(progress));
            puff.Transform.Rotate(0f, 0f, puff.Spin * Time.deltaTime);
            var color = Color.Lerp(fire, ash, Mathf.Clamp01(progress * 2.7f));
            color.a *= 1f - progress * progress;
            puff.Renderer.color = color;
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

    private static Sprite MakeSprite(bool ring, bool cloud = false)
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
                if (cloud)
                {
                    var angle = Mathf.Atan2(point.y, point.x);
                    var edge = .79f + .08f * Mathf.Cos(angle * 5f) + .06f * Mathf.Sin(angle * 8f);
                    alpha = Mathf.Clamp01((edge - point.magnitude) * 12f);
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * .5f, size, 0, SpriteMeshType.FullRect);
    }
}
