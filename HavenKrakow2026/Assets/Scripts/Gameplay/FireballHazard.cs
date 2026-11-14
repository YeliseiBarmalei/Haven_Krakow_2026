using UnityEngine;

public sealed class FireballHazard : MonoBehaviour
{
    private const float FallTime = 1.05f;
    private const float BurnTime = 2.5f;
    private const float BurnInterval = 1f;

    private static Sprite disc;
    private SpriteRenderer ground;
    private SpriteRenderer core;
    private SpriteRenderer trail;
    private Vector2 landing;
    private Vector2 fallOffset;
    private float age;
    private float nextBurn;
    private float radius;

    public static FireballHazard Spawn(Transform parent, Vector2 landing, float skyY, float radius, Material material)
    {
        var hazard = new GameObject("Fireball").AddComponent<FireballHazard>();
        hazard.transform.SetParent(parent, false);
        hazard.transform.position = landing;
        hazard.landing = landing;
        hazard.radius = radius;
        hazard.fallOffset = new Vector2(landing.x > 0f ? -.9f : .9f, skyY - landing.y + .55f);
        hazard.ground = hazard.AddSprite("Impact Zone", material, 3, Vector2.one * (radius * 2f));
        hazard.trail = hazard.AddSprite("Flame Trail", material, 8, new Vector2(.54f, 1.45f));
        hazard.core = hazard.AddSprite("Fireball Core", material, 9, Vector2.one * .55f);
        hazard.trail.color = new Color(1f, .26f, .04f, .76f);
        hazard.core.color = new Color(1f, .83f, .35f);
        hazard.trail.transform.up = hazard.fallOffset.normalized;
        hazard.UpdateFall(0f);
        return hazard;
    }

    public bool Tick(float step, SlimeController slime)
    {
        var previous = age;
        age += step;
        if (age < FallTime)
        {
            UpdateFall(age / FallTime);
            return false;
        }

        if (previous < FallTime)
        {
            core.enabled = false;
            trail.enabled = false;
            nextBurn = FallTime + BurnInterval;
            ImpactBurst.Spawn(ground, landing, Vector2.zero, new Color(1f, .42f, .09f), true, radius / .85f);
            if (Touches(slime))
                slime.TakeHazardDamage(2f);
        }

        var burnAge = age - FallTime;
        if (burnAge >= BurnTime)
            return true;

        ground.color = new Color(1f, .24f, .035f, .66f * (1f - .65f * burnAge / BurnTime));
        if (age >= nextBurn)
        {
            nextBurn += BurnInterval;
            if (Touches(slime))
                slime.TakeHazardDamage(.5f);
        }
        return false;
    }

    private bool Touches(SlimeController slime)
    {
        var reach = radius + slime.Radius;
        return (slime.Position - landing).sqrMagnitude <= reach * reach;
    }

    private void UpdateFall(float progress)
    {
        var offset = fallOffset * (1f - progress);
        core.transform.localPosition = offset;
        trail.transform.localPosition = offset + fallOffset.normalized * .45f;
        var pulse = .5f + .5f * Mathf.Sin(progress * Mathf.PI * 7f);
        ground.color = new Color(1f, .37f, .06f, .6f + .35f * pulse);
    }

    private SpriteRenderer AddSprite(string label, Material material, int order, Vector2 size)
    {
        if (disc == null)
            disc = CreateDisc();

        var renderer = new GameObject(label).AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(transform, false);
        renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        if (material != null)
            renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
        renderer.sprite = disc;
        return renderer;
    }

    private static Sprite CreateDisc()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var distance = (new Vector2(x + .5f, y + .5f) / size * 2f - Vector2.one).magnitude;
            var alpha = distance > 1f ? 0 : distance > .84f ? 255 : 140;
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * .5f,
            size, 0, SpriteMeshType.FullRect);
        texture.Apply(false, true);
        return sprite;
    }
}
