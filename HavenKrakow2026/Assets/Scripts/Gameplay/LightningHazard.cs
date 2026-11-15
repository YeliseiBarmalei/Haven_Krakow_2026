using UnityEngine;

public sealed class LightningHazard : MonoBehaviour
{
    private const float WarningDuration = 1.05f;
    private const float FlashDuration = .14f;
    private const float Damage = 2f;

    private static readonly Color GlowColor = new Color(.32f, .63f, 1f);
    private static readonly Color WarningColor = new Color(.18f, .79f, .97f);
    private LineRenderer warning;
    private SpriteRenderer warningIcon;
    private LineRenderer glow;
    private LineRenderer bolt;
    private float age;
    private float radius;
    public bool HitSlime { get; private set; }

    public static LightningHazard Spawn(Transform parent, Vector2 landing, float skyY, float radius, Material material,
        Sprite warningSprite)
    {
        var strike = new GameObject("Lightning Strike").AddComponent<LightningHazard>();
        strike.transform.SetParent(parent, false);
        strike.transform.position = landing;
        strike.radius = radius;

        var circle = new Vector3[48];
        for (var i = 0; i < circle.Length; i++)
        {
            var angle = i * Mathf.PI * 2f / circle.Length;
            circle[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        strike.warning = strike.AddLine("Strike Warning", material, 3, .055f, WarningColor, circle);
        strike.warning.loop = true;
        if (warningSprite != null)
        {
            strike.warningIcon = new GameObject("Lightning Warning Icon").AddComponent<SpriteRenderer>();
            strike.warningIcon.transform.SetParent(strike.transform, false);
            strike.warningIcon.sprite = warningSprite;
            strike.warningIcon.sortingOrder = 9;
            var size = warningSprite.bounds.size;
            strike.warningIcon.transform.localScale = Vector3.one * (radius * .8f / Mathf.Max(size.x, size.y));
        }

        var path = new Vector3[10];
        var height = Mathf.Max(1f, skyY - landing.y + .25f);
        for (var i = 1; i < path.Length; i++)
        {
            var fraction = (float)i / (path.Length - 1);
            var side = i % 2 == 0 ? -1f : 1f;
            path[i] = new Vector3(side * Random.Range(.08f, .42f), height * fraction);
        }
        strike.glow = strike.AddLine("Lightning Glow", material, 9, .25f, GlowColor, path);
        strike.bolt = strike.AddLine("Lightning Bolt", material, 10, .095f, Color.white, path);
        strike.glow.enabled = strike.bolt.enabled = false;
        return strike;
    }

    public bool Tick(float step, SlimeController slime, AudioClip strikeSound, float strikeVolume, bool dealDamage = true)
    {
        var previous = age;
        age += step;
        if (age < WarningDuration)
        {
            var pulse = .5f + .5f * Mathf.Sin(age / WarningDuration * Mathf.PI * 7f);
            SetOpacity(warning, WarningColor, .55f + .45f * pulse);
            return false;
        }

        if (previous < WarningDuration)
        {
            warning.enabled = false;
            if (warningIcon != null)
                warningIcon.enabled = false;
            glow.enabled = bolt.enabled = true;
            GameAudio.Play(strikeSound, strikeVolume);
            var reach = radius + slime.Radius;
            HitSlime = (slime.Position - (Vector2)transform.position).sqrMagnitude <= reach * reach;
            if (HitSlime && dealDamage)
                slime.TakeHazardDamage(Damage);
        }

        var opacity = 1f - Mathf.Clamp01((age - WarningDuration) / FlashDuration);
        SetOpacity(bolt, Color.white, opacity);
        SetOpacity(glow, GlowColor, opacity * .7f);
        return opacity <= 0f;
    }

    private LineRenderer AddLine(string label, Material material, int order, float width, Color color, Vector3[] points)
    {
        var line = new GameObject(label).AddComponent<LineRenderer>();
        line.transform.SetParent(transform, false);
        line.useWorldSpace = false;
        line.startWidth = line.endWidth = width;
        if (material != null)
            line.sharedMaterial = material;
        line.sortingOrder = order;
        line.positionCount = points.Length;
        line.SetPositions(points);
        SetOpacity(line, color, 1f);
        return line;
    }

    private static void SetOpacity(LineRenderer line, Color color, float opacity)
    {
        color.a = opacity;
        line.startColor = line.endColor = color;
    }
}
