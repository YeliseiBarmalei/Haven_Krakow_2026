using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class LevelController : MonoBehaviour
{
    [System.Serializable]
    private struct EnemySpawn
    {
        [Tooltip("Position relative to the arena center. Each axis runs from -1 to 1.")]
        public Vector2 position;
        public bool elite;
    }

    [System.Serializable]
    private sealed class Wave
    {
        public EnemySpawn[] shooters;
        [Tooltip("Normalized entry positions along the arena edges.")]
        public Vector2[] kamikazes = System.Array.Empty<Vector2>();
        [Min(0f), Tooltip("Seconds between fireballs. Lower values spawn them more often. Zero disables the event.")]
        public float fireballInterval;
    }

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
    [SerializeField] private Kamikaze kamikazePrefab;
    [SerializeField] private Wave[] waves;
    [SerializeField] private LevelBalance balance;
    [SerializeField] private Material fireballMaterial;
    [SerializeField, Min(.1f)] private float fireballRadius = 1.5f;
    [SerializeField, Min(.6f)] private float fireballAimSpread = 2.8f;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string nextLevelScene;
    [SerializeField] private SlimePiece piecePrefab;
    [SerializeField] private Sprite bandageSprite;
    [SerializeField] private Sprite slimeHealSprite;
    [SerializeField] private AudioClip parrySound;
    [SerializeField] private AudioClip swapSound;
    [SerializeField] private AudioClip shieldParrySound;
    [SerializeField] private AudioClip slowMotionSound;
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private ArenaHud hud;
    [SerializeField] private UpgradeMenu upgradeMenu;
    [SerializeField] private Text waveNotice;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text result;
    [SerializeField] private Text continueLabel;

    private readonly List<Projectile> bullets = new();
    private readonly List<Shooter> shooters = new();
    private readonly List<SlimePiece> thrownPieces = new();
    private Vector2 previousSlimePosition;
    private bool finished;
    private bool won;
    private int wave = 1;
    private float waveDelay = 2f;
    private float pieceTimer;
    private float healingDropTimer = 18f;
    private SlimePiece healingDrop;
    private float fittedAspect;
    private int slimeLevel = 1;
    private int spentPieces;
    private int reflectedDamage = 1;
    private float slowRemaining;
    private float slowRecharge;
    private float swapRecharge;
    private float shieldRemaining;
    private float shieldRecharge;
    private bool shieldConnected;
    private LineRenderer shieldRing;
    private readonly Vector3[] shieldPoints = new Vector3[64];
    private readonly Collider2D[] swapOverlaps = new Collider2D[16];
    private readonly ContactFilter2D swapFilter = new ContactFilter2D { useTriggers = false };
    public float ArenaSpeed => slowRemaining > 0f ? .35f : 1f;
    private float waveNoticeUntil;
    private int parries;
    private int enemiesDefeated;
    private int UpgradeCost => balance.startingUpgradeCost + slimeLevel - 1;
    private string VictoryScene => string.IsNullOrEmpty(nextLevelScene) ? mainMenuScene : nextLevelScene;
    private float fireballTimer;
    private readonly List<FireballHazard> fireballs = new();
    private readonly List<Kamikaze> kamikazes = new();

    public Bounds ArenaBounds => arenaBounds.bounds;
    public float ProjectileRadius => projectilePrefab.GetComponent<CircleCollider2D>().radius * projectilePrefab.transform.localScale.x;

    private void Start()
    {
        FitArena();
        previousSlimePosition = slime.Position;
        CreateShieldRing();
        slime.PieceCollected += UpdateStatus;
        slime.HealthChanged += OnSlimeHealthChanged;
        grandma.Changed += OnGrandmaHealthChanged;
        pieceTimer = balance.pieceInterval;
        Canvas.ForceUpdateCanvases();
        if (tutorialNotice != null)
        {
            tutorialStart = slime.Position;
            SpawnTutorialShooter();
            BeginTutorialStep(TutorialStep.Launch);
            UpdateStatus();
        }
        else
            SpawnWave();
    }

    private void Update()
    {
        if (!Mathf.Approximately(fittedAspect, arenaCamera.aspect))
            FitArena();

        if (waveNotice.enabled && Time.time >= waveNoticeUntil)
            waveNotice.enabled = false;

        if (finished || Time.timeScale <= 0f || FadeTransition.IsBusy)
            return;

        if (tutorialStep != TutorialStep.None)
        {
            UpdateTutorial();
            return;
        }

        if (slime.Pieces - spentPieces >= UpgradeCost)
        {
            OpenUpgradeMenu();
            return;
        }

        UpdateSlowMotion();
        UpdateSwap();
        UpdateShield();
    }

    private void OpenUpgradeMenu()
    {
        GameAudio.Play(levelUpSound, .85f);
        upgradeMenu.Open(slimeLevel + 1, slime.CanUpgradeSize, slime.SizeUpgradePercent, ChooseUpgrade);
    }

    private void UpdateSlowMotion()
    {
        var wasSlow = slowRemaining > 0f;
        slowRemaining = Mathf.Max(0f, slowRemaining - Time.deltaTime);
        slowRecharge = Mathf.Max(0f, slowRecharge - Time.deltaTime);
        if (slowRemaining == 0f && slowRecharge == 0f && Application.isFocused &&
            Keyboard.current?.spaceKey.wasPressedThisFrame == true)
        {
            slowRemaining = balance.slowDuration;
            slowRecharge = balance.slowCooldown;
            GameAudio.PlayTransition(slowMotionSound);
        }

        if (wasSlow != (slowRemaining > 0f))
            GameAudio.SetPlaybackSpeed(ArenaSpeed);

        hud.SetSlowMotion(slowRemaining, slowRecharge, balance.slowDuration, balance.slowCooldown);
        slime.SetSlowMotionAfterimages(slowRemaining > 0f);
    }

    private void OnDisable()
    {
        GameAudio.SetPlaybackSpeed(1f);
    }

    private bool UpdateSwap()
    {
        swapRecharge = Mathf.Max(0f, swapRecharge - Time.deltaTime);
        var target = swapRecharge == 0f ? FindSwapTarget() : null;
        var swapped = target != null && Application.isFocused && Keyboard.current?.eKey.wasPressedThisFrame == true;
        if (swapped)
        {
            var origin = slime.Position;
            slime.SwapTo(target.Position);
            target.TeleportTo(origin);
            previousSlimePosition = slime.Position;
            Physics2D.SyncTransforms();
            swapRecharge = balance.swapCooldown;
            GameAudio.Play(swapSound, balance.swapSoundVolume);
        }

        hud.SetSwap(swapRecharge, balance.swapCooldown, target != null);
        return swapped;
    }

    private void UpdateShield()
    {
        shieldRecharge = Mathf.Max(0f, shieldRecharge - Time.deltaTime);
        if (shieldRemaining > 0f)
        {
            shieldRemaining = Mathf.Max(0f, shieldRemaining - Time.deltaTime);
            if (shieldRemaining == 0f)
                shieldRecharge = shieldConnected ? balance.shieldSuccessCooldown : balance.shieldMissCooldown;
        }
        else if (shieldRecharge == 0f && Application.isFocused &&
            Keyboard.current?.qKey.wasPressedThisFrame == true)
        {
            shieldRemaining = balance.shieldDuration;
            shieldConnected = false;
            var bounds = grandma.HitBounds;
            shieldRing.transform.position = bounds.center;
            var radius = ShieldRadius(bounds);
            for (var i = 0; i < shieldPoints.Length; i++)
            {
                var angle = i * (2f * Mathf.PI / shieldPoints.Length);
                shieldPoints[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            shieldRing.SetPositions(shieldPoints);
        }

        RefreshShield();
    }

    private float ShieldRadius(Bounds bounds) => ((Vector2)bounds.extents).magnitude + balance.shieldPadding;

    private void CreateShieldRing()
    {
        shieldRing = new GameObject("Slime Parry Shield").AddComponent<LineRenderer>();
        shieldRing.transform.SetParent(transform, false);
        shieldRing.useWorldSpace = false;
        shieldRing.loop = true;
        shieldRing.positionCount = shieldPoints.Length;
        shieldRing.sharedMaterial = balance.shieldMaterial != null ? balance.shieldMaterial : grandma.GetComponent<SpriteRenderer>().sharedMaterial;
        shieldRing.sortingOrder = 6;
        shieldRing.enabled = false;
    }

    private void RefreshShield()
    {
        shieldRing.enabled = shieldRemaining > 0f;
        shieldRing.startWidth = shieldRing.endWidth = balance.shieldRingWidth;
        var color = shieldConnected ? balance.shieldHitColor : balance.shieldColor;
        shieldRing.startColor = shieldRing.endColor = color;
        hud.SetParry(shieldRemaining, shieldRecharge, balance.shieldDuration,
            shieldConnected ? balance.shieldSuccessCooldown : balance.shieldMissCooldown);
    }

    private Projectile FindSwapTarget()
    {
        var closest = float.PositiveInfinity;
        Projectile target = null;
        foreach (var bullet in bullets)
            CheckSwapTarget(bullet, ref target, ref closest);
        return target;
    }

    private void CheckSwapTarget(Projectile bullet, ref Projectile target, ref float closest)
    {
        var position = bullet.Position;
        var bounds = ArenaBounds;
        var radius = slime.Radius;
        if (position.x < bounds.min.x + radius || position.x > bounds.max.x - radius ||
            position.y < bounds.min.y + radius || position.y > bounds.max.y - radius)
            return;
        var distance = (position - slime.Position).sqrMagnitude;
        if (distance <= .01f || distance >= closest)
            return;

        var count = Physics2D.OverlapCircle(position, slime.Radius, swapFilter, swapOverlaps);
        for (var i = 0; i < count; i++)
        {
            if (swapOverlaps[i].gameObject != slime.gameObject)
                return;
        }

        closest = distance;
        target = bullet;
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

        if (tutorialStep != TutorialStep.None)
        {
            TickTutorial();
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

        for (var i = kamikazes.Count - 1; i >= 0; i--)
        {
            var runner = kamikazes[i];
            if (runner != null)
                runner.Tick(arenaStep);
            if (runner == null || !runner.IsActive)
                kamikazes.RemoveAt(i);
            if (finished)
                return;
        }

        var underAttack = moreShots || bullets.Count > 0 || kamikazes.Count > 0;
        UpdateFireballs(arenaStep, underAttack);
        if (finished)
            return;

        if (!underAttack && fireballs.Count == 0)
        {
            waveDelay -= arenaStep;
            if (waveDelay <= 0f)
            {
                if (wave == waves.Length)
                {
                    Finish("Level Complete!", true);
                    return;
                }

                NextWave();
            }
        }

        pieceTimer -= arenaStep;
        if (pieceTimer <= 0f)
        {
            pieceTimer = balance.pieceInterval;
            ThrowPiece();
        }

        healingDropTimer -= arenaStep;
        if (healingDropTimer <= 0f)
        {
            healingDropTimer = Random.Range(18f, 27f);
            DropHealingItem();
        }
    }

    private void UpdateFireballs(float step, bool spawn)
    {
        for (var i = fireballs.Count - 1; i >= 0; i--)
        {
            var fireball = fireballs[i];
            if (fireball.Tick(step, slime))
            {
                Destroy(fireball.gameObject);
                fireballs.RemoveAt(i);
            }
            if (finished)
                return;
        }

        var interval = waves[wave - 1].fireballInterval;
        if (!spawn || interval <= 0f)
            return;

        fireballTimer -= step;
        if (fireballTimer > 0f)
            return;

        SpawnFireball(interval);
    }

    private void SpawnFireball(float interval)
    {
        var bounds = ArenaBounds;
        var grandmaBounds = grandma.HitBounds;
        var radius = fireballRadius;
        if (bounds.extents.x < radius || bounds.extents.y < radius)
        {
            fireballTimer = .2f;
            return;
        }
        var playerPosition = slime.Position;
        var min = (Vector2)bounds.min + Vector2.one * radius;
        var max = (Vector2)bounds.max - Vector2.one * radius;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var point = attempt < 16
                ? playerPosition + Random.insideUnitCircle.normalized * Random.Range(.6f, fireballAimSpread)
                : new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
            point = Vector2.Max(min, Vector2.Min(max, point));
            if (grandmaBounds.SqrDistance(new Vector3(point.x, point.y, grandmaBounds.center.z)) <= radius * radius)
                continue;

            var sky = arenaCamera.ViewportToWorldPoint(new Vector3(.5f, 1f, arenaCamera.nearClipPlane)).y;
            fireballs.Add(FireballHazard.Spawn(transform, point, sky, radius, fireballMaterial));
            fireballTimer = interval * Random.Range(.8f, 1.2f);
            return;
        }
        fireballTimer = .2f;
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
        SpawnWave();
    }

    private void SpawnWave()
    {
        var bounds = arenaBounds.bounds;
        var center = (Vector2)bounds.center;
        var extents = (Vector2)bounds.extents - Vector2.one * .55f;
        var lineup = waves[wave - 1].shooters;
        for (var i = 0; i < lineup.Length; i++)
        {
            var position = Vector2.Max(-Vector2.one, Vector2.Min(Vector2.one, lineup[i].position));
            var spawnPoint = center + Vector2.Scale(position, extents);
            var shooter = Instantiate(shooterPrefab, spawnPoint, Quaternion.identity, enemies);
            shooter.ConfigureWave(wave, i, lineup[i].elite);
            SendInShooter(shooter, spawnPoint);
            shooters.Add(shooter);
            hud.TrackEnemy(shooter);
        }
        if (kamikazePrefab != null)
        {
            foreach (var position in waves[wave - 1].kamikazes)
            {
                var offset = Vector2.Max(-Vector2.one, Vector2.Min(Vector2.one, position));
                var runner = Instantiate(kamikazePrefab, center, Quaternion.identity, enemies);
                var entry = center + Vector2.Scale(offset, bounds.extents);
                var margin = runner.GetComponent<SpriteRenderer>().bounds.extents;
                if (Mathf.Abs(offset.x) >= Mathf.Abs(offset.y))
                    entry.x = center.x + Mathf.Sign(offset.x) * (bounds.extents.x + margin.x + .5f);
                else
                    entry.y = center.y + Mathf.Sign(offset.y) * (bounds.extents.y + margin.y + .5f);
                runner.Begin(entry, grandma, this);
                kamikazes.Add(runner);
            }
        }
        var fireballInterval = waves[wave - 1].fireballInterval;
        if (fireballInterval > 0f)
            SpawnFireball(fireballInterval);

        UpdateStatus();
        ShowWaveNotice();
    }

    public void RegisterKamikazeDefeat()
    {
        enemiesDefeated++;
    }

    private void ShowWaveNotice()
    {
        waveNotice.text = waves[wave - 1].fireballInterval > 0f
            ? $"Wave {wave} / {waves.Length} - Fireballs"
            : $"Wave {wave} / {waves.Length}";
        waveNoticeUntil = Time.time + 1.8f;
        waveNotice.enabled = true;
    }

    private void SendInShooter(Shooter shooter, Vector2 target)
    {
        var bounds = arenaBounds.bounds;
        var center = (Vector2)bounds.center;
        var offset = target - center;
        var entrance = target;
        var edgeInset = shooter.WallClearance;
        var visualExtents = shooter.VisualExtents;
        var restricted = ShooterHudArea(visualExtents);
        if (Mathf.Abs(offset.x) / bounds.extents.x > Mathf.Abs(offset.y) / bounds.extents.y)
        {
            entrance.x = center.x + Mathf.Sign(offset.x) * (bounds.extents.x + visualExtents.x + .4f);
            target.x = center.x + Mathf.Sign(offset.x) * (bounds.extents.x - edgeInset);
            if (CrossesHudArea(entrance, target, restricted))
                target.y = entrance.y = Mathf.Max(bounds.min.y + edgeInset, restricted.yMin - .05f);
        }
        else
        {
            entrance.y = center.y + Mathf.Sign(offset.y) * (bounds.extents.y + visualExtents.y + .4f);
            target.y = center.y + Mathf.Sign(offset.y) * (bounds.extents.y - edgeInset);
            if (CrossesHudArea(entrance, target, restricted))
                target.x = entrance.x = Mathf.Min(bounds.max.x - edgeInset, restricted.xMax + .05f);
        }

        shooter.ApproachFrom(entrance, target);
    }

    public Rect ShooterHudArea(Vector2 extents)
    {
        var panel = hud.SlimePanelScreenRect();
        var depth = arenaCamera.WorldToScreenPoint(Vector3.zero).z;
        var min = (Vector2)arenaCamera.ScreenToWorldPoint(new Vector3(panel.xMin, panel.yMin, depth));
        var max = (Vector2)arenaCamera.ScreenToWorldPoint(new Vector3(panel.xMax, panel.yMax, depth));
        return Rect.MinMaxRect(min.x - extents.x, min.y - extents.y - .25f,
            max.x + extents.x, max.y + extents.y);
    }

    public static bool CrossesHudArea(Vector2 start, Vector2 end, Rect area)
    {
        return area.Contains(start) || area.Contains(end) ||
            RectangleEntryTime(start - area.center, end - area.center, area.size * .5f) <= 1f;
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
            var slimeHit = tutorialStep == TutorialStep.Shield ? float.PositiveInfinity :
                EntryTime(start - previousSlimePosition, end - slimePosition, slime.Radius + bullet.Radius);
            var grandmaHit = BoxEntryTime(start - (Vector2)grandmaBounds.center, end - (Vector2)grandmaBounds.center,
                grandmaBounds.extents, bullet.Radius);
            var shieldHit = shieldRemaining > 0f
                ? EntryTime(start - (Vector2)grandmaBounds.center, end - (Vector2)grandmaBounds.center,
                    ShieldRadius(grandmaBounds) + bullet.Radius)
                : float.PositiveInfinity;
            if (shieldHit <= 1f && shieldHit <= slimeHit && shieldHit <= grandmaHit)
            {
                shieldConnected = true;
                ReflectBullet(index, start, end, shieldHit, balance.shieldReturnBoost, shieldParrySound);
                continue;
            }
            if (slimeHit <= 1f && slimeHit <= grandmaHit)
            {
                if (!slime.IsInFlight)
                {
                    RemoveBullet(index);
                    if (tutorialStep == TutorialStep.None)
                        slime.TakeHit(bullet.Velocity);
                    continue;
                }

                ReflectBullet(index, start, end, slimeHit, slimeTravel.magnitude / Time.fixedDeltaTime, parrySound);
                continue;
            }

            if (grandmaHit <= 1f)
            {
                RemoveBullet(index);
                if (tutorialStep == TutorialStep.None)
                    grandma.TakeHit(direction: bullet.Velocity);
                continue;
            }

            if (bounds.Contains(end))
                bullet.AdvanceTo(end);
            else
                RemoveBullet(index);
        }
    }

    private void ReflectBullet(int index, Vector2 start, Vector2 end, float hitTime, float returnBoost, AudioClip sound)
    {
        var bullet = bullets[index];
        var impact = Vector2.Lerp(start, end, hitTime);
        bullet.Deflect(impact, hitTime, returnBoost, ArenaSpeed);
        if (tutorialStep == TutorialStep.None)
            parries++;
        else if (tutorialStep == TutorialStep.Reflect || tutorialStep == TutorialStep.Shield)
            tutorialReflected = true;
        GameAudio.Play(sound, .8f);
        if (HitShooter(impact, bullet.Position, bullet.Radius))
            RemoveBullet(index);
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
            var bounds = health.HitBounds;
            var center = (Vector2)bounds.center;
            var hit = BoxEntryTime(start - center, end - center, bounds.extents, radius);
            if (hit > firstHit)
                continue;
            target = health;
            firstHit = hit;
        }

        if (target == null)
            return false;
        if (tutorialStep == TutorialStep.None || tutorialStep == TutorialStep.Reflect || tutorialStep == TutorialStep.Shield)
            target.TakeHit(reflectedDamage, end - start);
        if (target.Current == 0 && tutorialStep == TutorialStep.None)
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

    private void Finish(string message, bool victory = false)
    {
        finished = true;
        won = victory;
        if (victory && !string.IsNullOrEmpty(nextLevelScene))
            MainMenu.UnlockLevel(nextLevelScene);
        shieldRemaining = 0f;
        RefreshShield();
        slowRemaining = 0f;
        GameAudio.SetPlaybackSpeed(1f);
        hud.SetSlowMotion(0f, slowRecharge, balance.slowDuration, balance.slowCooldown);
        slime.SetSlowMotionAfterimages(false);
        waveNotice.enabled = false;
        slime.CanLaunch = false;
        slime.StopLaunch();
        foreach (var bullet in bullets)
            Destroy(bullet.gameObject);
        bullets.Clear();
        foreach (var shooter in shooters)
        {
            shooter.HideWarning();
            shooter.StopShotAnimation();
        }
        foreach (var fireball in fireballs)
            Destroy(fireball.gameObject);
        foreach (var runner in kamikazes)
            if (runner != null)
                runner.Stop();

        var heading = victory ? message : $"{message} on wave {wave}";
        result.text = $"{heading}\nParries {parries}   |   Enemies defeated {enemiesDefeated}";
        continueLabel.text = victory
            ? (string.IsNullOrEmpty(nextLevelScene) ? "Main Menu" : "Next Level")
            : "Try Again";
        resultPanel.SetActive(true);
    }

    public void Continue()
    {
        if (!finished || FadeTransition.IsBusy || Time.timeScale == 0f)
            return;

        GameAudio.Play(uiClick, .65f);
        FadeTransition.Load(won ? VictoryScene : SceneManager.GetActiveScene().name);
    }

    private void UpdateStatus()
    {
        hud.SetStatus(wave, waves.Length, slime.Health, slime.MaxHealth, grandma.Current, grandma.Max,
            slimeLevel, slime.Pieces - spentPieces, UpgradeCost, reflectedDamage);
    }

    private void ChooseUpgrade(UpgradeMenu.Upgrade upgrade)
    {
        GameAudio.Play(uiClick, .65f);
        spentPieces += UpgradeCost;
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
        if (tutorialStep == TutorialStep.Upgrade)
            CompleteTutorial();
    }
}
