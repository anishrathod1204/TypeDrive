# TypeDrive

**TypeDrive** is a self-contained 3D typing racer for Unity 2022.3 LTS using the Built-in Render Pipeline. The car drives forward automatically. Type each gate’s word before reaching it, change lanes to avoid traffic, and build a combo to raise your score and speed.

The car, road, skyline, pickups, UI, and sound effects are generated with Unity primitives and code. No downloaded art, audio, or other external assets are required. TextMeshPro and UGUI are package dependencies included by the Unity project.

## Open and run

1. Extract the ZIP to a writable folder.
2. In Unity Hub, choose **Add project from disk** and select the folder containing `Assets`, `Packages`, and `ProjectSettings`.
3. Open with **Unity 2022.3 LTS** (the project version is 2022.3.21f1) and let packages and scripts import.
4. Open `Assets/Scenes/TypeDrive.unity` if Unity does not open it automatically.
5. Wait for import/compilation to finish, check the Console for errors, then press **Play**.
6. Choose a campaign level or **ENDLESS DRIVE**. Enter/Space starts the current mode from the menu.

To rebuild the startup scene from scratch, use **TypeDrive > Build Complete Game Scene** in the Unity menu. The builder creates the bootstrap scene and adds it to Build Settings. It asks Unity to save unsaved scenes first.

## Modes and levels

### Five-stage campaign

Each level has a distinct color theme, speed, word difficulty, traffic spacing, and a final timed storm gate containing one sentence. The campaign starts with three lives. Missing a normal word gate or hitting a barrier costs a life and breaks the combo. Clear the regular word goal, then finish the storm sentence before its timer runs out.

| Level | Theme | Regular word goal | Final challenge |
| --- | --- | ---: | --- |
| 1 — City Sprint | Cyan / city dusk | 10 | Short storm sentence |
| 2 — Midnight Rush | Pink / midnight | 14 | Longer sentence |
| 3 — Neon Grand Prix | Blue / violet | 18 | Hard sentence |
| 4 — Storm Circuit | Teal / rainstorm | 22 | Long sentence under timer |
| 5 — Nova Showdown | Magenta / nova | 26 | Longest sentence under timer |

### Endless drive

The road, scenery, gates, and traffic keep spawning for an unlimited run. Endless mode has no life limit or game-over from crashes. Crashes still break the combo and reduce speed. Word difficulty rises as score increases.

## Controls

| Input | Action |
| --- | --- |
| Type letters and spaces | Complete the active word or storm sentence |
| Backspace | Undo the last correct character |
| A / D or Left / Right arrow | Move one lane |
| Touch arrows | Move one lane on a touch display |
| Escape | Pause or resume |
| Enter / Space | Start from the main menu |
| R | Restart after Game Over |

Typing still uses a physical keyboard; the on-screen lane arrows are the touch fallback.

## Features

- Three selectable cars: **Volt**, **Pulse**, and **Nova**, with different colors, handling, and starting speed. Selection is saved.
- Shield pickup that absorbs one crash, slow motion for five seconds, and double score for eight seconds. Pickups bob and rotate; active effects appear in the HUD.
- WPM, character accuracy, and best combo on the animated Game Over screen.
- Settings for master volume, SFX volume, mute, and key hints. Settings are saved with PlayerPrefs.
- Gate unlock animation, combo-triggered exhaust flame, speed lines, screen/UI fades, and a level-complete fanfare.
- Emissive materials, skyline, stars/moon, point lights, and level-tuned fog. If Post Processing Stack v2 is installed with its resources, the runtime adds a global Bloom volume.
- Procedural engine, typing, crash, pickup, click, and fanfare sounds.

## Code map

- `Assets/Scripts/Runtime/TypeDriveSceneBootstrap.cs` builds the world, UI, audio, and runtime wiring.
- `Assets/Editor/TypeDriveSceneBuilder.cs` implements the one-click scene builder menu.
- `Assets/Scripts/Core/GameManager.cs` owns game state, score, combo, lives, speed, distance, pause, and campaign progress.
- `Assets/Scripts/Core/GameLevelCatalog.cs` defines five stages; `WordBank.cs` supplies words.
- `Assets/Scripts/Gameplay/TypingController.cs`, `GateSpawner.cs`, and `WordGate.cs` implement typing gates and timed storm challenges.
- `Assets/Scripts/Gameplay/PowerUpManager.cs`, `PowerUpSpawner.cs`, and `PowerUpPickup.cs` implement run effects and road pickups.
- `Assets/Scripts/Gameplay/TypingStatsTracker.cs` measures typing results; `CarController.cs` handles the selected car and lane input.
- `Assets/Scripts/Core/GameSettings.cs` stores preferences; `Assets/Scripts/Audio/AudioManager.cs` handles procedural sound.
- `AUDIT_AND_TEST_CHECKLIST.md` lists fixes and short manual checks for each feature.
- `VIVA_CHEATSHEET.md` contains ten short code/project answers for the demo.
- `PROJECT_REPORT.md` explains the design and architecture.

## Demo notes

For a clean classroom demo, select **Level 1 — City Sprint**, type the first word, dodge one barrier with A/D, pause/resume with Escape, then collect a pickup and show its HUD status. For the final challenge, use a later campaign level or demonstrate Endless Drive. Explain the game loop from `GameManager`, `GateSpawner`, `TypingController`, and `WordGate`.

See [AUDIT_AND_TEST_CHECKLIST.md](AUDIT_AND_TEST_CHECKLIST.md) before presenting. The Unity editor compile and Play Mode checklist should be run on the machine where Unity is licensed.
