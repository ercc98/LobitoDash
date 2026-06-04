# Magic Village Dash

## Purpose
A stylized **three-lane endless runner** for mobile, built in Unity 6. The player guides a character
(currently a **wolf**) through a procedurally streamed village, dodging hazards, collecting coins,
fighting/avoiding enemies, and chasing best runs. This is also the real-world **consumer/testbed** for
the `com.erccdev.foundation` package — game systems are built on top of that shared library.

- Repo: `https://github.com/ercc98/MagicVillageDash.git` (MIT licensed)
- Company / Product: **ErccPlay / MagicVillageDash** · `bundleVersion 0.1.0` (pre-release)
- Unity Editor: **6000.2.6f2** (Unity 6) or newer · URP rendering
- Sole author: Ernesto Refugio Cabrera Cerón

## Relationship to ErccDev Foundation
Game code depends on the Foundation package via UPM git URL:
```
"com.erccdev.foundation": "https://github.com/ercc98/Foundation.git"
```
Foundation provides the reusable base (EventBus, SaveService, audio toolkit, swipe/touch input, scene
loader, camera shaker, pause, tutorial, pooling/factories). Game scripts subclass/wire those bases.
When a game feature needs a generic capability, prefer adding it to Foundation and consuming it here.
There is **no** `Assets/Scripts/ErccDev/` folder anymore — that tooling now comes from the package.

## Key dependencies
- `com.unity.addressables` 2.8.0
- `com.unity.cinemachine` 3.1.5
- `com.unity.inputsystem` 1.18.0 (new Input System; `ISwipeInput` abstraction from Foundation)
- `com.unity.localization` 1.5.9
- **Firebase** (analytics) and **Google AdMob** (interstitial ads)

## Layout
- `Assets/Scripts/MagicVillageDash/` — all game-specific code, organized by module:
  - `Runner/` — `LaneRunner` + `ILaneMover` (CharacterController movement, lane snapping, jump/slide/defend).
  - `Character/` — `CharacterControllerBase`, `IMovementController`, `ShadowMover`, and
    `CharacterAnimator/` (`CharacterAnimatorController` with `IMovement/IDeath/IExpression` animator interfaces).
  - `Player/` — `PlayerController`.
  - `World/` — procedural streaming: `ChunkSpawner`, `ChunkFactory`, `ChunkRoot`, `WorldMover`,
    shared `GameSpeedController`, plus `Biomes/` (`BiomeDirector`, `BiomeDefinition`,
    `BiomeTransitionOption`, `ChunkFactoryInstaller`) and `WorldReward3D`.
  - `Enemy/` — `EnemyAI`, `EnemyController`, `EnemySensors`, `EnemySpawnManager`, `EnemyFactory`,
    `EnemyDeathReward`, spawn-permission gating (`IEnemySpawnPermission`).
  - `Collectibles/` — `CoinFactory`, `CoinRailGenerator/Filler`, `CoinCollectible`, collected-coin variants.
  - `Obstacles/` — `ObstacleHazard`, `ObstacleFactory`, rail generators, `IHazardReceiver`.
  - `Score/` — `CoinCounter`, `DistanceTracker`, `RunScoreSystem` (persists high score/distance/coins
    via Foundation's `SaveService`).
  - `Gameplay/` — `RunController`, `IHazardReceiver`.
  - `Input/` — `RunnerSwipeController` + `IRunnerInputController` (glue to Foundation swipe input).
  - `Audio/` — `AudioManager` + `IAudioManager`, `SoundIds`.
  - `Camera/` — `CameraShaker`.
  - `UI/` — HUD texts (`Coin/Distance/Score`), `SimpleGameMenus`, `PauseMenuUI`, `SettingsMenuUI`, `CoinComboUI`.
  - `Tutorial/` — swipe-driven `TutorialManager`, `TutorialOverlayUI`, `TutorialTriggerStep`, context builders.
  - `Pause/` — `PauseApplier`, `PauseServiceBehaviour`.
  - `Settings/` — `GraphicsQualityManager`, `SettingsApplier` + `ISettingsApplier`.
  - `Data/` — `GameDataService`, `RunStatsData`, `AchievementData`.
  - `Achievements/` — `AchievementManager` (on Foundation's `AchievementManagerBase`), `AchievementContextBuilder`,
    `Conditions/` (coins/distance/score reached), `Rewards/` (`CoinReward`, `EventBusReward`); persisted via `GameDataService`.
  - `Collections/` — `CollectionManager` (on Foundation's `CollectionManagerBase`), `CollectionShowcase`,
    `ModelCollectionEntry`; relics (`Collectibles/RelicCollectible`, `RelicFactory`, `RelicRailFiller`) feed discoveries.
  - `Notifications/` — `NotificationManager` + `NotificationToastView` (game toasts on Foundation's
    `NotificationManagerBase`/`NotificationViewBase`), `NotificationTester` (dev-only harness). Achievement/Collection
    toasts come via Foundation's source bridges. Tintable toast art lives in `Assets/Images/Notifications/`.
  - `AdMobScripts/` — `Interstitial`. `FirebaseScripts/` — `FirebaseAnalyticsService`. `Boostrap/` — `LogoSceneController`.
- `Assets/Scenes/` — `IntroScene` (title) → `RunnerScene` (core gameplay).
- `Assets/IgnoreFolder/` — gitignored; holds **licensed third-party asset packs** not in the repo
  (wolf pack, low-poly environment, ambient effects, UI sound packs). The project won't fully open
  without these placed manually (see commit `a35daf4`).

## Conventions (match these when adding code)
- **Interface-driven, SOLID with personality** — mirror the Foundation style: `IThing` interface +
  concrete/base behaviour, factories for spawnable objects (`ChunkFactory`, `CoinFactory`,
  `EnemyFactory`, `ObstacleFactory`), terse helpers, null-guard early returns, concise XML summaries.
  Don't rewrite working code into generic textbook style — preserve the author's voice.
- **Build on Foundation, don't reinvent** — use `SaveService`, EventBus, the audio toolkit, swipe input,
  pause and tutorial bases from the package rather than duplicating them here.
- **Factory + rail/generator pattern** for streamed content (coins, obstacles, chunks, enemies).
- Namespaces follow the folder path under `MagicVillageDash`.

## Gameplay loop
1. Player spawns centered on the lane grid (`IntroScene` → `RunnerScene`).
2. `WorldMover`/`GameSpeedController` advance chunks toward the camera; `ChunkSpawner` recycles new
   chunks ahead, `BiomeDirector` controls biome (e.g. forest/town) transitions.
3. Coins and enemy/world rewards update the HUD and feed `RunScoreSystem`; distance adds points over time.
4. Hitting an `ObstacleHazard` (or enemy) ends the run; `RunScoreSystem` commits new records via `SaveService`.
5. `SimpleGameMenus` / `PauseMenuUI` drive intro ↔ gameplay ↔ game-over ↔ pause states.

## Working in this repo
- A `unity-mcp` MCP server is available for driving the Unity editor directly when connected.
- Current dev happens on feature branches (`ChangingVisuals`, `BiomeImplementation`, `enemyBehavoir`,
  `Localization`, `MakingTutorial`, `mobilePolish`, …); `main` is the integration branch.
- ⚠️ **Security:** history contains a branch named `GoogleAPIKeyLeaked` — a Google API key was likely
  committed at some point. If still live, rotate/revoke it and scrub it from history; never commit keys.
- When adding gameplay tuning knobs, expose them on the relevant controller (`GameSpeedController`,
  `RunScoreSystem`) rather than hardcoding.
