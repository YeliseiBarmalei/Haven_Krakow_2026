using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LevelController : MonoBehaviour
{
    [SerializeField] private SlimeController slime;
    [SerializeField] private ActorHealth grandma;
    [SerializeField] private Transform enemies;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private BoxCollider2D arenaBounds;
    [SerializeField] private Camera arenaCamera;
    [SerializeField, Min(1f)] private float minimumArenaWidth = 15f;
    [SerializeField, Min(1f)] private float minimumArenaHeight = 9.75f;
    [SerializeField] private SpriteRenderer floor;
    [SerializeField] private Transform leftWall, rightWall, topWall, bottomWall;
    [SerializeField] private Shooter shooterPrefab;
    [SerializeField] private SlimePiece piecePrefab;
    [SerializeField] private Sprite bandageSprite;
    [SerializeField] private Sprite slimeHealSprite;
    [SerializeField] private AudioClip parrySound;
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioClip uiClick;
    [SerializeField, Min(1f)] private float pieceInterval = 10f;
    [SerializeField, Min(1)] private int piecesPerLevel = 3;
    [SerializeField] private Text status;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text result;
    [SerializeField] private Text continueLabel;

    private readonly List<Projectile> bullets = new();
    private readonly List<Shooter> shooters = new();
    private readonly List<SlimePiece> thrownPieces = new();
    private Vector2 previousSlimePosition;
    private bool finished;
    private int wave = 1;
    private float waveDelay = 2f;
    private float pieceTimer;
    private float healingDropTimer = 18f;
    private SlimePiece healingDrop;
    private float fittedAspect;
    private int slimeLevel = 1;
    private int spentPieces;
    private int reflectedDamage = 1;
    private UpgradeMenu upgradeMenu;
    private ArenaHud hud;
    private const float SlowDuration = 3f;
    private const float SlowCooldown = 18f;
    private float slowRemaining;
    private float slowRecharge;
    public float ArenaSpeed => slowRemaining > 0f ? .35f : 1f;
    private Text waveNotice;
    private float waveNoticeUntil;
    private int parries;
    private int enemiesDefeated;

    public Bounds ArenaBounds => arenaBounds.bounds;
    public float ProjectileRadius => projectilePrefab.GetComponent<CircleCollider2D>().radius * projectilePrefab.transform.localScale.x;

    private void Start()
    {
        FitArena();
        shooters.AddRange(enemies.GetComponentsInChildren<Shooter>());
        foreach (var shooter in shooters)
            SendInShooter(shooter, shooter.transform.position);
        previousSlimePosition = slime.Position;
        upgradeMenu = UpgradeMenu.Create(status, resultPanel.transform);
        hud = ArenaHud.Create(status, arenaCamera);
        foreach (var shooter in shooters)
            hud.TrackEnemy(shooter);
        CreateWaveNotice();
        slime.PieceCollected += UpdateStatus;
        slime.HealthChanged += OnSlimeHealthChanged;
        grandma.Changed += OnGrandmaHealthChanged;
        pieceTimer = pieceInterval;
        resultPanel.SetActive(false);
        UpdateStatus();
        ShowWaveNotice();
    }

    private void Update()
    {
        if (!Mathf.Approximately(fittedAspect, arenaCamera.aspect))
            FitArena();

        if (!finished && Time.timeScale > 0f && !FadeTransition.IsBusy)
            UpdateSlowMotion();

        if (waveNotice.enabled && Time.time >= waveNoticeUntil)
            waveNotice.enabled = false;

        if (!finished && Time.timeScale > 0f && !FadeTransition.IsBusy &&
            slime.Pieces - spentPieces >= piecesPerLevel)
        {
            GameAudio.Play(levelUpSound, .85f);
            upgradeMenu.Open(slimeLevel + 1, slime.CanUpgradeSize, slime.SizeUpgradePercent, ChooseUpgrade);
        }
    }

    private void UpdateSlowMotion()
    {
        slowRemaining = Mathf.Max(0f, slowRemaining - Time.deltaTime);
        slowRecharge = Mathf.Max(0f, slowRecharge - Time.deltaTime);
        if (slowRemaining == 0f && slowRecharge == 0f && Application.isFocused &&
            Keyboard.current?.spaceKey.wasPressedThisFrame == true)
        {
            slowRemaining = SlowDuration;
            slowRecharge = SlowCooldown;
        }

        hud.SetSlowMotion(slowRemaining, slowRecharge, SlowDuration, SlowCooldown);
    }

    private void FitArena()
    {
        fittedAspect = arenaCamera.aspect;
        const float thickness = .3f;
        arenaCamera.orthographicSize = .5f * Mathf.Max(minimumArenaHeight, minimumArenaWidth / fittedAspect);
        var width = 2f * arenaCamera.orthographicSize * fittedAspect;
        var height = 2f * arenaCamera.orthographicSize;
        floor.size = new Vector2(width, height);
        SetWall(leftWall, new Vector2(-width * .5f + thickness * .5f, 0), new Vector2(thickness, height));
        SetWall(rightWall, new Vector2(width * .5f - thickness * .5f, 0), new Vector2(thickness, height));
        SetWall(topWall, new Vector2(0, height * .5f - thickness * .5f), new Vector2(width, thickness));
        SetWall(bottomWall, new Vector2(0, -height * .5f + thickness * .5f), new Vector2(width, thickness));
        arenaBounds.size = new Vector2(width - 2f * thickness, height - 2f * thickness);
        Physics2D.SyncTransforms();
        slime.KeepInside(arenaBounds.bounds);
        previousSlimePosition = slime.Position;
    }

    private static void SetWall(Transform wall, Vector2 position, Vector2 size)
    {
        var shape = wall.GetComponent<BoxCollider2D>();
        wall.localPosition = position;
        wall.localScale = new Vector3(size.x / shape.size.x, size.y / shape.size.y, 1f);
    }

    private void FixedUpdate()
    {
        if (finished)
            return;

        if (FadeTransition.IsBusy)
        {
            previousSlimePosition = slime.Position;
            return;
        }

        MoveBullets();
        previousSlimePosition = slime.Position;
        if (finished)
            return;

        var moreShots = false;
        var arenaStep = Time.fixedDeltaTime * ArenaSpeed;
        foreach (var shooter in shooters)
        {
            shooter.Tick(grandma, this);
            moreShots |= shooter.HasShots;
        }

        if (!moreShots && bullets.Count == 0)
        {
            waveDelay -= arenaStep;
            if (waveDelay <= 0f)
                NextWave();
        }

        pieceTimer -= arenaStep;
        if (pieceTimer <= 0f)
        {
            pieceTimer = pieceInterval;
            ThrowPiece();
        }

        healingDropTimer -= arenaStep;
        if (healingDropTimer <= 0f)
        {
            healingDropTimer = Random.Range(18f, 27f);
            DropHealingItem();
        }
    }

    private void DropHealingItem()
    {
        if (healingDrop != null || (grandma.Current == grandma.Max && slime.Health == slime.MaxHealth))
            return;

        var bandage = grandma.Current < grandma.Max && (slime.Health == slime.MaxHealth || Random.value < .5f);
        var bounds = arenaBounds.bounds;
        var landing = new Vector2(Random.Range(bounds.min.x + 1.35f, bounds.max.x - 1.35f),
            Random.Range(bounds.min.y + 1.35f, bounds.max.y - 1.35f));
        var start = new Vector2(landing.x, bounds.max.y + 1f);
        healingDrop = Instantiate(piecePrefab, start, Quaternion.identity);
        healingDrop.name = bandage ? "Bandage" : "Healing Slime";
        healingDrop.DropAt(landing, bandage ? bandageSprite : slimeHealSprite, bandage ? grandma : null, this);
    }

    private void NextWave()
    {
        hud.ClearEnemyBars();
        foreach (var shooter in shooters)
            Destroy(shooter.gameObject);
        shooters.Clear();

        wave++;
        waveDelay = 2f;
        var bounds = arenaBounds.bounds;
        var center = (Vector2)bounds.center;
        var count = Mathf.Min(1 + wave / 2, 8);
        var startSide = Random.Range(0, 4);
        for (var i = 0; i < count; i++)
        {
            var side = (startSide + i) % 4;
            var along = Mathf.Lerp(-.65f, .65f, (i + .5f) / count);
            var position = side < 2
                ? new Vector2((side == 0 ? -1 : 1) * (bounds.extents.x - .55f), along * bounds.extents.y)
                : new Vector2(along * bounds.extents.x, (side == 2 ? -1 : 1) * (bounds.extents.y - .55f));
            var spawnPoint = center + position;
            if ((spawnPoint - slime.Position).sqrMagnitude < 2.25f)
                spawnPoint = center - position;
            var shooter = Instantiate(shooterPrefab, spawnPoint, Quaternion.identity, enemies);
            shooter.ConfigureWave(wave, i);
            SendInShooter(shooter, spawnPoint);
            shooters.Add(shooter);
            hud.TrackEnemy(shooter);
        }
        UpdateStatus();
        ShowWaveNotice();
    }

    private void CreateWaveNotice()
    {
        waveNotice = new GameObject("Wave Announcement", typeof(RectTransform)).AddComponent<Text>();
        var rect = waveNotice.rectTransform;
        rect.SetParent(status.transform.parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -92f);
        rect.sizeDelta = new Vector2(300f, 44f);
        waveNotice.font = status.font;
        waveNotice.fontSize = 26;
        waveNotice.fontStyle = FontStyle.Bold;
        waveNotice.alignment = TextAnchor.MiddleCenter;
        waveNotice.color = new Color(.67f, 1f, .88f);
        waveNotice.raycastTarget = false;
        var outline = waveNotice.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(.04f, .08f, .12f, .93f);
    }

    private void ShowWaveNotice()
    {
        waveNotice.text = $"Wave {wave}";
        waveNoticeUntil = Time.time + 1.8f;
        waveNotice.enabled = true;
    }

    private void SendInShooter(Shooter shooter, Vector2 target)
    {
        var bounds = arenaBounds.bounds;
        var center = (Vector2)bounds.center;
        var offset = target - center;
        var entrance = target;
        const float offscreenMargin = 1f;
        const float edgeInset = .55f;
        if (Mathf.Abs(offset.x) / bounds.extents.x > Mathf.Abs(offset.y) / bounds.extents.y)
        {
            entrance.x = center.x + Mathf.Sign(offset.x) * (bounds.extents.x + offscreenMargin);
            target.x = center.x + Mathf.Sign(offset.x) * (bounds.extents.x - edgeInset);
        }
        else
        {
            entrance.y = center.y + Mathf.Sign(offset.y) * (bounds.extents.y + offscreenMargin);
            target.y = center.y + Mathf.Sign(offset.y) * (bounds.extents.y - edgeInset);
        }

        shooter.ApproachFrom(entrance, target);
    }

    private void ThrowPiece()
    {
        thrownPieces.RemoveAll(piece => piece == null || !piece.gameObject.activeSelf);
        if (thrownPieces.Count >= 4)
            return;

        var bounds = arenaBounds.bounds;
        for (var attempt = 0; attempt < 12; attempt++)
        {
            const float margin = 1.25f;
            var landing = new Vector2(Random.Range(bounds.min.x + margin, bounds.max.x - margin),
                Random.Range(bounds.min.y + margin, bounds.max.y - margin));
            if ((landing - grandma.Center).sqrMagnitude < 2.25f ||
                (landing - slime.Position).sqrMagnitude < Mathf.Pow(slime.Radius + .45f, 2))
                continue;

            var piece = Instantiate(piecePrefab, grandma.Center, Quaternion.identity);
            piece.ThrowTo(landing, this);
            thrownPieces.Add(piece);
            break;
        }
    }

    public void Fire(Vector2 muzzle, Vector2 velocity)
    {
        var bullet = Instantiate(projectilePrefab, muzzle + velocity.normalized * ProjectileRadius, Quaternion.identity);
        bullet.Velocity = velocity;
        bullets.Add(bullet);
    }

    private void MoveBullets()
    {
        var slimePosition = slime.Position;
        var slimeTravel = slimePosition - previousSlimePosition;
        var grandmaBounds = grandma.HitBounds;
        var bounds = arenaBounds.bounds;
        for (var index = bullets.Count - 1; index >= 0 && !finished; index--)
        {
            var bullet = bullets[index];
            var start = bullet.Position;
            var end = start + bullet.Velocity * (Time.fixedDeltaTime * ArenaSpeed);
            if (bullet.IsDeflected)
            {
                MoveDeflectedBullet(index, start, bounds);
                continue;
            }

            // Use relative motion so a fast-moving slime cannot pass through a shot between physics steps.
            var slimeHit = EntryTime(start - previousSlimePosition, end - slimePosition, slime.Radius + bullet.Radius);
            var grandmaHit = BoxEntryTime(start - (Vector2)grandmaBounds.center, end - (Vector2)grandmaBounds.center,
                grandmaBounds.extents, bullet.Radius);
            if (slimeHit <= 1f && slimeHit <= grandmaHit)
            {
                if (!slime.IsInFlight)
                {
                    RemoveBullet(index);
                    slime.TakeHit();
                    continue;
                }

                var impact = Vector2.Lerp(start, end, slimeHit);
                bullet.Deflect(impact, slimeHit, slimeTravel.magnitude / Time.fixedDeltaTime, ArenaSpeed);
                parries++;
                GameAudio.Play(parrySound, .8f);
                if (HitShooter(impact, bullet.Position, bullet.Radius))
                    RemoveBullet(index);
                continue;
            }

            if (grandmaHit <= 1f)
            {
                RemoveBullet(index);
                grandma.TakeHit();
                continue;
            }

            bullet.AdvanceTo(end);
        }
    }

    private void MoveDeflectedBullet(int index, Vector2 start, Bounds bounds)
    {
        var bullet = bullets[index];
        var end = start + bullet.Velocity * (Time.fixedDeltaTime * ArenaSpeed);
        if (HitShooter(start, end, bullet.Radius) || !bounds.Contains(end))
            RemoveBullet(index);
        else
            bullet.AdvanceTo(end);
    }

    private bool HitShooter(Vector2 start, Vector2 end, float radius)
    {
        ActorHealth target = null;
        var firstHit = 1f;
        foreach (var shooter in shooters)
        {
            if (!shooter.CanBeHit)
                continue;
            var health = shooter.Health;
            var hit = EntryTime(start - health.Center, end - health.Center, health.Radius + radius);
            if (hit > firstHit)
                continue;
            target = health;
            firstHit = hit;
        }

        if (target == null)
            return false;
        target.TakeHit(reflectedDamage);
        if (target.Current == 0)
            enemiesDefeated++;
        return true;
    }

    private static float BoxEntryTime(Vector2 start, Vector2 end, Vector2 halfSize, float radius)
    {
        // A swept circle against a box covers two expanded rectangles and four rounded corners.
        var hit = Mathf.Min(
            RectangleEntryTime(start, end, halfSize + Vector2.right * radius),
            RectangleEntryTime(start, end, halfSize + Vector2.up * radius));
        for (var x = -1; x <= 1; x += 2)
        {
            for (var y = -1; y <= 1; y += 2)
            {
                var corner = new Vector2(x * halfSize.x, y * halfSize.y);
                hit = Mathf.Min(hit, EntryTime(start - corner, end - corner, radius));
            }
        }
        return hit;
    }

    private static float RectangleEntryTime(Vector2 start, Vector2 end, Vector2 halfSize)
    {
        var travel = end - start;
        var entry = 0f;
        var exit = 1f;
        for (var axis = 0; axis < 2; axis++)
        {
            if (Mathf.Approximately(travel[axis], 0f))
            {
                if (Mathf.Abs(start[axis]) > halfSize[axis])
                    return float.PositiveInfinity;
                continue;
            }

            var near = (-halfSize[axis] - start[axis]) / travel[axis];
            var far = (halfSize[axis] - start[axis]) / travel[axis];
            entry = Mathf.Max(entry, Mathf.Min(near, far));
            exit = Mathf.Min(exit, Mathf.Max(near, far));
            if (entry > exit)
                return float.PositiveInfinity;
        }
        return entry;
    }

    private static float EntryTime(Vector2 start, Vector2 end, float radius)
    {
        var offset = end - start;
        var distance = start.sqrMagnitude - radius * radius;
        if (distance <= 0f)
            return 0f;

        var along = Vector2.Dot(start, offset);
        var speedSquared = offset.sqrMagnitude;
        var discriminant = along * along - speedSquared * distance;
        if (along >= 0f || discriminant < 0f)
            return float.PositiveInfinity;

        return (-along - Mathf.Sqrt(discriminant)) / speedSquared;
    }

    private void RemoveBullet(int index)
    {
        Destroy(bullets[index].gameObject);
        bullets.RemoveAt(index);
    }

    private void OnGrandmaHealthChanged()
    {
        UpdateStatus();
        if (grandma.Current == 0)
            Finish("Grandma was defeated");
    }

    private void OnSlimeHealthChanged()
    {
        UpdateStatus();
        if (slime.Health == 0)
            Finish("The slime was defeated");
    }

    private void Finish(string message)
    {
        finished = true;
        slowRemaining = 0f;
        hud.SetSlowMotion(0f, slowRecharge, SlowDuration, SlowCooldown);
        waveNotice.enabled = false;
        slime.CanLaunch = false;
        slime.StopLaunch();
        foreach (var bullet in bullets)
            Destroy(bullet.gameObject);
        bullets.Clear();
        foreach (var shooter in shooters)
            shooter.HideWarning();

        result.text = $"{message} on wave {wave}\nParries {parries}   |   Enemies defeated {enemiesDefeated}";
        continueLabel.text = "Try Again";
        resultPanel.SetActive(true);
    }

    public void Continue()
    {
        if (!finished || FadeTransition.IsBusy || Time.timeScale == 0f)
            return;

        GameAudio.Play(uiClick, .65f);
        FadeTransition.Load(SceneManager.GetActiveScene().name);
    }

    private void UpdateStatus()
    {
        hud.SetStatus(wave, slime.Health, slime.MaxHealth, grandma.Current, grandma.Max,
            slimeLevel, slime.Pieces - spentPieces, piecesPerLevel, reflectedDamage);
    }

    private void ChooseUpgrade(UpgradeMenu.Upgrade upgrade)
    {
        GameAudio.Play(uiClick, .65f);
        spentPieces += piecesPerLevel;
        slimeLevel++;
        switch (upgrade)
        {
            case UpgradeMenu.Upgrade.GrandmaHealth:
                grandma.UpgradeHealth();
                break;
            case UpgradeMenu.Upgrade.SlimeHealth:
                slime.UpgradeHealth();
                break;
            case UpgradeMenu.Upgrade.ReturnDamage:
                reflectedDamage++;
                break;
            case UpgradeMenu.Upgrade.Size:
                slime.UpgradeSize();
                slime.KeepInside(arenaBounds.bounds);
                previousSlimePosition = slime.Position;
                break;
        }
        UpdateStatus();
    }
}
