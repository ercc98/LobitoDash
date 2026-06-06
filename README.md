# Magic Village Dash

Magic Village Dash is a stylized three-lane endless runner built with Unity. Guide your character through a colorful village, dodge hazards, collect coins, and push for new personal bests while the environment streams endlessly toward the camera.

This repository contains the Unity project and supporting assets required to open, run, and extend the game in the Unity Editor.

## Project Highlights

- **Responsive lane running** – `LaneRunner` combines CharacterController movement with lane snapping, jump, and slide actions for responsive touch-based play.
- **Procedural world streaming** – The world system continuously spawns and recycles chunks while advancing them using a shared `GameSpeedController` so gameplay feels seamless.
- **Collectible economy** – Coin prefabs spawn on rails, snap to the runner, and update the run score via the `CoinCounter` and `RunScoreSystem` components.
- **Persistent best runs** – `RunScoreSystem` saves high scores, farthest distance, and best coin haul using the shared `SaveService` utility so players can chase long-term goals.
- **Collections & achievements** – Relic pickups and achievement unlocks discover entries in a shared `CollectionCatalog`; ownership persists via `GameDataService`.
- **Den meta-progression** – A cozy `DenScene` where earned structures are placed into a forest clearing. Earn it in a race → place it in the den; placements persist between sessions. *(In progress.)*
- **Modular audio** – `AudioManager` provides category-aware playback helpers built on the reusable Foundation audio toolkit.

## Repository Structure

```
Assets/
  Animations/          Animation clips, controllers, and timelines.
  Images/, Models/,    Art assets for environments, props, and UI.
  Prefabs/             Player, world chunks, collectibles, and UI prefabs.
  Scenes/              `IntroScene` (title), `RunnerScene` (core gameplay), `DenScene` (meta-progression den).
  Scripts/
    MagicVillageDash/  Game-specific scripts (runner motor, world, collections, den, UI, etc.),
                       organized by module under the MagicVillageDash namespace.
Packages/              Unity package manifest and lock file.
ProjectSettings/       Project-wide Unity configuration.
```

> Shared, reusable systems (EventBus, `SaveService`, audio toolkit, swipe input, scene loader,
> pause/tutorial bases, pooling/factories) live in the **`com.erccdev.foundation`** package, pulled in
> via UPM git URL — there is no longer an `Assets/Scripts/ErccDev/` folder. Game scripts subclass and
> wire those Foundation bases.

## Requirements

- **Unity Editor**: 6000.2.6f2 (Unity 6) or newer.
- **Unity Input System** package enabled (already referenced in `Packages/manifest.json`).
- Tested with URP for rendering (configured via the Universal RP package).

## Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/<your-org>/MagicVillageDash.git
   ```
2. **Open in Unity Hub**
   - Add the cloned folder and select the Unity 6000.2.6f2 editor (or newer 6.x release).
   - The exact editor version is stored in `ProjectSettings/ProjectVersion.txt` if you need to install it first.
3. **Install dependencies**
   - Unity will resolve packages listed in `Packages/manifest.json` automatically, including the `com.erccdev.foundation` package (pulled from its git URL). No manual imports are required for that shared tooling.
4. **Open a scene**
   - Start with `Assets/Scenes/IntroScene.unity` for the full title-to-run flow, or jump directly into `RunnerScene.unity` for immediate gameplay iteration.
5. **Enter Play Mode** and test the game.

> **Note:** The project uses the new Unity Input System. If you see binding errors on first open, let Unity reimport packages, then reopen the scenes.

## Controls

The project uses the Unity Input System together with the reusable `ISwipeInput` abstraction from the Foundation package.

| Action | Keyboard / Gamepad (in editor) | Touch / Mouse |
| ------ | ------------------------------ | ------------- |
| Move left/right | Arrow keys / A-D / gamepad d-pad or stick | Swipe left/right |
| Jump | Space / South button | Swipe up or tap (if `tapTriggersJump` enabled) |
| Slide | Left Ctrl / East button | Swipe down |

> **Tip:** Assign a Foundation swipe-input source to the `RunnerSwipeController` component to translate input events into runner commands.

## Gameplay Loop

1. Player spawns centered on the lane grid.
2. `WorldMover` components translate environment chunks toward the camera while spawn controllers recycle new chunks ahead.
3. Coins collected update the HUD and contribute to the score; distance traveled adds points over time.
4. Colliding with `ObstacleHazard` prefabs ends the run; `RunScoreSystem.CommitIfBest()` persists new records.
5. UI menus (`SimpleGameMenus`) transition between intro, gameplay, and game-over states.

## Building a Player

1. Open `RunnerScene` and ensure the `IntroScene` is listed first in **File → Build Settings**.
2. Confirm your target platform (PC, Mac, Linux, Android, or iOS) and click **Switch Platform** if needed.
3. Press **Build** or **Build And Run**, then choose an output folder.

## Extending the Project

- **Add new obstacles**: Create prefabs under `Assets/Prefabs/Obstacles`, implement the `ObstacleHazard` interface, and register them with the `ObstacleFactory` for runtime spawning.
- **Customize scoring**: Tweak `pointsPerMeter` and `pointsPerCoin` on `RunScoreSystem` or add new score sources via its public API.
- **Audio & VFX**: Use the audio manager helpers (`Play`, `PlayLoop`, `StopLoop`) to trigger sounds from gameplay scripts, and extend URP materials for additional polish.

## Development Tips

- Keep scenes lightweight: use prefab variants for obstacles and environment chunks so changes propagate consistently.
- When iterating on gameplay tuning, adjust `GameSpeedController` parameters and the runner acceleration curve to balance difficulty.
- The `SaveService` writes player records to persistent data; clear it between test sessions if you need a clean slate.

## Licensing

This project is licensed under the MIT License. See [LICENSE](LICENSE) for the full text. Art assets and third-party packages remain subject to their respective licenses; verify terms before redistributing them outside this repository.

---

Magic Village Dash is a great starting point for building mobile-friendly endless runners with modular systems you can adapt to new themes and mechanics. Have fun experimenting!
