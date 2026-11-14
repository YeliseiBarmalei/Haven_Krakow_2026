# Editing the game

Open `MainMenu` to start an endless run or open a scene under `Assets/Scenes/Levels` to test a different opening wave.

- Move the Slime, Grandma, and starting Shooters in the Scene view. Place Shooters under **Enemies** so the level controller includes them.
- Tune movement and player health on the **Slime** prefab. Tune enemy health and firing behavior on the **Shooter** prefab. A scene instance can override these settings for the opening wave.
- The **Level Controller** references the camera, floor, and arena walls. Play Mode fits them to the game window. Later waves spawn shooters around the perimeter. **Piece Interval** controls how often the grandma throws an upgrade.
- The slime's final sprites are in `Assets/Arts`. The Kenney Tiny Dungeon sprites and their CC0 license are in `Assets/PlaceHolders/Sprites/DownloadedAssets`.
