using UnityEngine;
using UnityEngine.UI;

public sealed partial class LevelController
{
    private enum TutorialStep { None, Launch, Reflect, SlowMotion, Swap, Shield, Collect, Upgrade }

    [SerializeField] private Text tutorialNotice;
    [SerializeField] private string tutorialExitScene = "Level01";
    private TutorialStep tutorialStep;
    private Vector2 tutorialStart;
    private Shooter tutorialShooter;
    private bool tutorialReflected;
    private bool tutorialUsedSlowMotion;
    private bool tutorialShieldReady;
    private bool tutorialSwapped;
    private float tutorialWait;

    private void SpawnTutorialShooter()
    {
        var bounds = ArenaBounds;
        var position = new Vector2(bounds.max.x - 2f, grandma.Center.y);
        tutorialShooter = Instantiate(shooterPrefab, position, Quaternion.identity, enemies);
        shooters.Add(tutorialShooter);
        hud.TrackEnemy(tutorialShooter);
    }

    private void BeginTutorialStep(TutorialStep step)
    {
        ClearTutorialShots();
        tutorialShooter.HideWarning();
        tutorialShooter.StopShotAnimation();
        tutorialStep = step;
        tutorialReflected = false;
        tutorialWait = tutorialShooter.PracticeWarningDuration;
        slime.CanDamageGrandma = false;
        slime.CanLaunch = step != TutorialStep.Upgrade;
        tutorialNotice.enabled = true;
        ShowTutorialPrompt(step switch
        {
            TutorialStep.Launch => "Drag with left mouse and release to launch.",
            TutorialStep.Reflect => "Intercept the bullet aimed at Grandma while sliding.",
            TutorialStep.SlowMotion => "Press Space, then try moving while the bullets slow down.",
            TutorialStep.Swap => "Press E when a bullet is on the arena to swap places with it.",
            TutorialStep.Shield => "Watch the bullet flying toward Grandma.",
            TutorialStep.Collect => $"Collect {UpgradeCost} slime pieces after they land.",
            _ => "Choose your first upgrade."
        });

        if (step == TutorialStep.Collect)
        {
            var bounds = ArenaBounds;
            var distance = Mathf.Max(1.45f, slime.Radius + .65f);
            for (var i = 0; i < UpgradeCost; i++)
            {
                var direction = i % 2 == 0 ? Vector2.up : Vector2.down;
                var point = tutorialStart + direction * distance;
                point.y = Mathf.Clamp(point.y, bounds.min.y + .65f, bounds.max.y - .65f);
                var piece = Instantiate(piecePrefab, grandma.Center, Quaternion.identity);
                piece.ThrowTo(point, this);
            }
        }
    }

    private void ShowTutorialPrompt(string instruction)
    {
        tutorialNotice.text = $"Practice {(int)tutorialStep}/7\n{instruction}";
    }

    private void UpdateTutorial()
    {
        switch (tutorialStep)
        {
            case TutorialStep.Launch when slime.IsInFlight:
                BeginTutorialStep(TutorialStep.Reflect);
                break;
            case TutorialStep.Reflect when tutorialReflected && bullets.Count == 0:
                BeginTutorialStep(TutorialStep.SlowMotion);
                break;
            case TutorialStep.SlowMotion:
                UpdateSlowMotion();
                if (!tutorialUsedSlowMotion && slowRemaining > 0f)
                {
                    tutorialUsedSlowMotion = true;
                    ShowTutorialPrompt("Move with left mouse while time is slow.");
                }
                else if (tutorialUsedSlowMotion && slowRemaining <= 0f)
                {
                    slowRecharge = 0f;
                    hud.SetSlowMotion(0f, 0f, balance.slowDuration, balance.slowCooldown);
                    BeginTutorialStep(TutorialStep.Swap);
                }
                break;
            case TutorialStep.Swap:
                if (!tutorialSwapped && UpdateSwap())
                {
                    tutorialSwapped = true;
                    tutorialWait = .65f;
                    tutorialShooter.HideWarning();
                    ShowTutorialPrompt("You and the bullet switched places.");
                }
                if (tutorialSwapped && (tutorialWait -= Time.deltaTime) <= 0f)
                {
                    swapRecharge = 0f;
                    hud.SetSwap(0f, balance.swapCooldown, false);
                    BeginTutorialStep(TutorialStep.Shield);
                }
                break;
            case TutorialStep.Shield:
                if (tutorialShieldReady || shieldRemaining > 0f)
                    UpdateShield();
                if (tutorialShieldReady && shieldRemaining > 0f)
                {
                    tutorialShieldReady = false;
                    slime.CanLaunch = true;
                    ShowTutorialPrompt("Watch the reflected bullet hit the shooter.");
                }
                if (tutorialReflected && bullets.Count == 0)
                {
                    shieldRemaining = shieldRecharge = 0f;
                    RefreshShield();
                    BeginTutorialStep(TutorialStep.Collect);
                }
                break;
            case TutorialStep.Collect when slime.Pieces >= UpgradeCost:
                BeginTutorialStep(TutorialStep.Upgrade);
                OpenUpgradeMenu();
                break;
        }
    }

    private void TickTutorial()
    {
        if (tutorialShieldReady && shieldRemaining <= 0f)
            return;

        var step = Time.fixedDeltaTime * ArenaSpeed;
        if (tutorialShooter != null && tutorialShooter.isActiveAndEnabled)
            tutorialShooter.TickPracticeAnimation(step);

        if (bullets.Count == 0 && !tutorialReflected &&
            (tutorialStep == TutorialStep.Reflect || tutorialStep == TutorialStep.SlowMotion ||
             tutorialStep == TutorialStep.Shield || (tutorialStep == TutorialStep.Swap && !tutorialSwapped)))
        {
            tutorialWait -= step;
            tutorialShooter.AimPracticeShot(this, grandma, tutorialWait);
            if (tutorialWait <= 0f)
                tutorialWait = .55f + tutorialShooter.PracticeWarningDuration;
        }

        if (tutorialStep == TutorialStep.Shield && !tutorialReflected && shieldRemaining <= 0f && bullets.Count > 0)
        {
            var shot = bullets[0];
            var center = grandma.Center;
            var radius = ShieldRadius(grandma.HitBounds) + shot.Radius;
            if (Vector2.Distance(shot.Position, center) <= radius + .25f)
            {
                shot.TeleportTo(center + (shot.Position - center).normalized * (radius + .005f));
                tutorialShieldReady = true;
                slime.CanLaunch = false;
                slime.StopLaunch();
                ShowTutorialPrompt("Press Q now to reflect the bullet.");
                return;
            }
        }

        MoveBullets();
        previousSlimePosition = slime.Position;
    }

    private void ClearTutorialShots()
    {
        for (var i = bullets.Count - 1; i >= 0; i--)
            RemoveBullet(i);
    }

    private void CompleteTutorial()
    {
        MainMenu.CompleteTutorial();
        finished = true;
        tutorialNotice.enabled = false;
        slime.CanLaunch = false;
        slime.StopLaunch();
        FadeTransition.Load(tutorialExitScene);
    }
}
