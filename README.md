# Satellite Defense

<p align="center">
  <img src="Assets/Images/Publishing/Screenshots/ShowCaseMix.png" width="720" alt="Satellite Defense gameplay">
</p>

<p align="center"><strong>A mobile arcade defense game about protecting a planet from asteroid impacts.</strong></p>

<p align="center">
  <img src="https://img.shields.io/badge/Engine-Unity%206-222c32?logo=unity&logoColor=white" alt="Unity 6">
  <img src="https://img.shields.io/badge/Platform-Android-3DDC84?logo=android&logoColor=white" alt="Android">
  <img src="https://img.shields.io/badge/Genre-Arcade%20Defense-5b8def" alt="Arcade defense">
  <img src="https://img.shields.io/badge/Status-On%20hold-f0ad4e" alt="On hold">
</p>

## About

**Satellite Defense** is a high-score game in a space-science-fiction setting. Control an orbital satellite, destroy incoming asteroids and keep the planet safe for as long as possible.

## Highlights

- **Planetary defense:** Track and destroy asteroid threats before they hit the planet.
- **Power-ups:** Defeated asteroids can drop weapon upgrades and health recovery.
- **High-score loop:** Survive longer, score higher and face increasingly chaotic attacks.
- **Customization:** Shop systems support different satellite, planet, asteroid and background designs.
- **Mobile-ready input:** Touch and joystick controls with local save support.

## Technical details

- **Engine:** Unity `6000.3.25f1`
- **Target platform:** Android
- **Status:** On hold

## Run locally

1. Clone the repository.
2. Open it in Unity Hub with Unity `6000.3.25f1`.
3. Open `Assets/Scenes/MainMenu.unity` and press Play. `Ingame.unity` can also be opened directly for gameplay work.

## Scenes

- `MainMenu`: main menu, shop, privacy link, splash screen and an interactive preview of the selected equipment. Aim and shoot with the satellite while asteroids arrive. The planet cannot lose lives; enemies award no score, coins or power-ups. It contains no pickup controller, score controller, HUD or pause/game-over UI.
- `Ingame`: gameplay, joystick, HUD, pickups, enemies and pause/game-over UI. It contains no main-menu or shop UI and starts a new round when opened directly.

Both scenes are registered in build settings, with `MainMenu` first. Play loads `Ingame`; returning from pause or game over loads `MainMenu`. Replay restarts the round in the gameplay scene. Scene-local cameras, audio and controllers are replaced on transitions; the static save session is loaded once and retained, including pending progression after a failed save. Scene transitions flush progress and reset the paused time scale.

## Code structure

- `GameController` owns the game state and the four equipment catalogs. Pause and shop freeze simulation through `Time.timeScale`; UI animations use unscaled time.
- The scene's `isGameplayScene` flag determines whether `GameController` starts a scored round or presents the interactive menu preview. Both use the active shop loadout. Opening the shop pauses the preview; changing the active selection rebuilds the displayed equipment and enemies. Menu buttons consume their own input, while the background remains available for aiming.
- `IngameEntity` prefabs contain item definitions. `Savegame` owns purchased items, levels, selection, coins and best score. Gameplay never writes progress into prefabs.
- `LoadoutStats` combines the equipped item definitions and saved levels into a read-only set of gameplay values whenever the equipment changes. Attribute defaults, rounding and shared limits live there; weapon-specific shot and enemy spawn interval limits remain with their controllers. `GameController` coordinates the equipment and world objects.
- `IngameEntity` defines purchase prices and upgrade eligibility against an explicit progress view. The shop and save transactions use these same rules; a transaction evaluates its candidate save rather than the live global state.
- `SaveGameController` coordinates local progress through `SaveSession`. Gameplay reads a read-only view; purchases and selections commit to disk before the live state changes. Google Play/cloud-save and achievement placeholders have been removed.
- Weapons own their projectiles. Enemies and power-ups unregister when removed; round transitions clear transient objects. Audio sources are reused with a fixed limit.
- Equipped score multipliers multiply together. Other attributes add together. Fire/spawn rates represent seconds between events; rotation uses degrees per second and probability values use fractions from 0 to 1. Each item may contain an attribute type only once.

The attribute refactor preserves existing item values, prices, combination rules, rewards and abilities. Serialized attribute IDs and names used by JSON backups and icons remain stable. Renamed interval fields use `FormerlySerializedAs` to preserve existing prefab and scene settings. Runtime verification of this refactor is pending; no tests or builds were run for it.

The attribute editor remains available under **Tools > Attribute > IngameEntity Attribute Editor**. JSON imports are validated before any prefab is modified.

## UI layout

The existing neon frames, colors and menu arrangement are retained. The canvas uses a portrait reference resolution. The HUD remains at the top and toast messages at the bottom. Safe-area positioning is configured manually; there is no automatic safe-area or menu-fitting component.

The shop frame, category tabs, preview and purchase controls have fixed layout bounds. Only the attribute list grows inside a masked vertical scroll view with an automatically hidden scroll indicator. Each reusable attribute row shows its icon, name and value; tapping the icon expands a wrapping explanation within the list. Item changes reset scrolling to the top. Preview images preserve their aspect ratio in fixed, masked boxes.

Geometry, typography and component configuration are authored in the scene and prefabs, rather than configured by initialization code. Each menu owns a fixed coin display; the code updates their values without reparenting them. Life icons wrap within the HUD. Toast messages wrap within a fixed panel. Standard Unity layout components arrange dynamic attribute content; scripts update content, visibility, interaction and scrolling without applying custom size or font changes. This UI update has only been inspected statically; editor, touch and device acceptance are pending, and no tests or builds were run.

## Android release workflow

Unity Android Release Tools are integrated under `scripts/`, with the Unity build helper in `Assets/Editor/UnityAndroidBuild.cs`. The workflow supports signed APK/AAB builds, Google Play uploads and local Drive exports. Project-specific setup, the imported source revision and commands are documented in [scripts/README.md](scripts/README.md).

Local signing secrets and the existing shared Play account are configured. App-specific read and test-release permissions are saved, and API access has been confirmed. The Android target is API 36; unused Unity Analytics, crash reporting and purchasing services are disabled. The current project has no billing or advertising SDK. Store declarations and a new release still need completion; see the current status in [scripts/README.md](scripts/README.md). Tests, builds, exports and uploads require an explicit request under [AGENTS.md](AGENTS.md).

## Local saves

Progress is stored in `Application.persistentDataPath/Saves/savegame.json`, with the previous committed snapshot in `savegame.backup.json`. The versioned JSON envelope includes a SHA-256 checksum to detect file damage; this is not encryption or cheat protection. There is no PlayerPrefs migration.

`FileSaveStore` writes and flushes a temporary file, verifies its contents, then replaces the primary while retaining a valid backup. A corrupt or missing primary recovers from the backup. Temporary files are never loaded as committed progress. Unsupported versions or two unreadable saves block new progression and preserve the files instead of silently starting over. The UI explains recovery and storage errors.

Coin pickups update memory immediately and are saved in batches every five seconds on a background worker. Purchases, upgrades, loadout selection, round completion, pause, background/focus loss, menu return and normal shutdown flush pending changes. Failed purchases leave coins and equipment unchanged; failed reward writes retain pending progress for retry. An abrupt process kill can lose pickups since the last completed save, normally within the five-second autosave interval; disk errors or a suspended writer can extend that window.

## Validation

The existing `ProjectValidation` regression runner and its development-build method still target the original combined `MainScene`. They need adaptation to the separate scenes and scene transitions before their next explicitly authorized use. They were not changed or run for this scene split. The release helper in `UnityAndroidBuild` uses the enabled build-settings scenes, which now include both `MainMenu` and `Ingame`.

The existing storage checks remain in `SaveStorageValidation`. Tests and builds require explicit approval under `AGENTS.md`; pending scene-transition, storage and UI acceptance scenarios are listed in `TESTPLAN.md`. The scene split has only been inspected statically and has not been compiled or exercised in Unity.

Before a release, test touch aiming and pickups on a phone, background/resume, audio and UI layout at the supported aspect ratios. Review gameplay balance as well: corrected damage, score multipliers, coin bonuses and item drops affect progression compared with the old implementation.
