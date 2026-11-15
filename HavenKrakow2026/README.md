# Editing the game

The kamikaze explosion uses [Explosion-LS100155 by Fg2](https://commons.wikimedia.org/wiki/File:Explosion-LS100155.ogg), a field recording released into the public domain.

See [the game design document](DESIGN.md) for the current rules, level script, and balance values.

Open `MainMenu` and choose **Levels**. Level 1 is available immediately. Winning it unlocks Level 2 in the menu and enables the **Next Level** route from the results screen. Open `Level01` or `Level02` directly to test either level.

Edit the level picker in `Assets/Scenes/LevelSelect.unity` and the practice arena in `Assets/Scenes/Levels/Tutorial.unity`. First-time players enter the practice scene before Level 1. Completing practice loads a fresh Level 1 run.

## Current gameplay

- Drag and release the slime to aim a launch and set its power. Protect Grandma by intercepting enemy shots and collect slime pieces to choose upgrades.
- **Space** slows time, **Q** activates the parry shield, and **E** swaps the slime with the nearest eligible projectile. The projectile keeps its direction and speed.
- Level 1 has three waves with one normal shooter, two normal shooters, and one normal plus one elite shooter. They are 20% larger, have 2 extra HP, and show a skull beside their health bar. After the first shot, an elite starts repositioning early and then lines up a quick second shot. It takes a longer recovery before the two slower shots. Hits and enemy deaths trigger distinct particle effects.

## Editing in Unity

- Move the Slime and Grandma in the Scene view. Set each wave's shooters under **Level Controller → Waves**. Each entry specifies a position relative to the arena center and whether the shooter is elite.
- Tune movement and player health on the **Slime** prefab. Tune enemy health and firing behavior on the **Shooter** prefab.
- The **Level Controller** references the camera, floor, and arena walls. Play Mode fits them to the game window. **Piece Interval** controls how often Grandma throws a piece. **Starting Upgrade Cost** is 2, and each upgrade raises the next cost by 1. A wave's **Fireball Interval** controls its fireball frequency. A value of 0 disables the event for that wave.
- Edit the HUD and upgrade panel under **Canvas** in `Level01`. The volume slider is under **Canvas** in `MainMenu` and inside the pause overlay in `Level01`. Enable inactive panels temporarily to preview them in the editor.
- Both scenes reference `Assets/Audio/MapleLeafRag.ogg`. The music loops and continues between scenes. **Volume** controls music and sound effects together.
- The slime's final sprites are in `Assets/Arts`. The Kenney Tiny Dungeon sprites and their CC0 license are in `Assets/PlaceHolders/Sprites/DownloadedAssets`.
