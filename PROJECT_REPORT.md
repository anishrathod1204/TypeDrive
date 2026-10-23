# TypeDrive — final-year game project report

## Project overview

TypeDrive is a single-player 3D typing racer built for Unity 2022.3 LTS with the Built-in Render Pipeline. It combines three-lane driving with a timed typing challenge: the car moves forward by itself, the player types the active word before its gate, and dodges traffic with lane controls.

The project is designed to be easy to run and explain. The startup scene contains one bootstrap object; when Play Mode starts, that script generates the car, road, skyline, gates, hazards, menus, HUD, lights, materials, and procedural audio. There are no imported art or sound assets.

## Objectives

1. Make typing the main action in a simple arcade driving loop.
2. Give immediate feedback for correct characters, errors, completed words, crashes, and power-ups.
3. Scale challenge through five campaign stages and a timed sentence at the end of every stage.
4. Keep an unlimited practice mode and player-selectable cars.
5. Use small event-driven systems that are easy to demonstrate and extend.

## Modes and progression

### Campaign

The campaign has five themed stages. Each has a word goal, speed, vocabulary tier, gate spacing, traffic density, sky/road colors, and fog. Every stage ends with a “storm gate”: one sentence that must be typed before its countdown expires. The player has three lives; a missed gate or traffic collision loses one life, breaks the combo, and reduces speed. A shield can absorb one crash.

| Stage | Regular word goal | Challenge |
| --- | ---: | --- |
| City Sprint | 10 | Introductory timed sentence |
| Midnight Rush | 14 | Faster pace and longer sentence |
| Neon Grand Prix | 18 | Hard vocabulary and tighter timing |
| Storm Circuit | 22 | Long sentence under a timer |
| Nova Showdown | 26 | Longest final sentence |

### Endless drive

Endless Drive continuously recycles the road and roadside props. It has no life limit and crashes cannot end the run. A crash still resets the combo and lowers speed. Word difficulty increases at score thresholds.

## Gameplay systems

- **Typing:** Characters are checked left to right. Backspace removes the last correct character; mistakes do not advance the word. Boss sentences include spaces.
- **Driving:** The car auto-drives in one of three lanes. A/D, arrow keys, and touch lane buttons move one lane at a time.
- **Scoring:** Each word awards points for its length and a capped combo bonus. Successful words increase speed. Double score multiplies points earned while active.
- **Power-ups:** Shield absorbs a collision; slow motion lasts five seconds; double score lasts eight seconds. Pickups animate with spin and bob, and active effects are shown in the HUD.
- **Typing report:** WPM uses correctly typed characters, five characters per word, and active run time. Accuracy compares correct and attempted characters. Best combo is tracked for the run.
- **Settings:** Master volume, sound effects volume, mute, and key hints are saved with PlayerPrefs. Car selection is also saved.

## Architecture

`TypeDriveSceneBootstrap` builds the scene and connects the systems. `GameManager` owns run state and publishes state, score, life, and run-start events. Gameplay systems report changes rather than directly controlling the UI. The HUD subscribes to typing and manager events. `PowerUpManager` owns effect timers and crash protection; it composes correctly with pause by keeping the active gameplay time scale separately from the pause scale.

The core run sequence is:

1. The player selects a car and campaign stage or Endless Drive.
2. `GameManager` resets run data and invokes `OnRunStarted`.
3. Road, car, traffic, gates, power-ups, and typing systems reset from that event.
4. `GateSpawner` picks words from `WordBank`; `TypingController` checks characters and updates the active gate.
5. Word completion awards score and combo. After a campaign word goal, the spawner queues a sentence gate.
6. `WordGate` enforces the deadline; a crash is routed through `GameManager`, where the shield gets first chance to absorb it.
7. The final sentence opens Level Complete. Game Over shows the final run’s typing stats and saved-record status.

### Main scripts

- `Assets/Scripts/Core/GameManager.cs`: run state, score, lives, speed, distance, pause, and mode.
- `Assets/Scripts/Core/GameLevelCatalog.cs`: five level definitions and visual/pacing data.
- `Assets/Scripts/Core/WordBank.cs`: random words grouped by difficulty.
- `Assets/Scripts/Core/GameSettings.cs`: saved audio and key-hint preferences.
- `Assets/Scripts/Gameplay/TypingController.cs`: character input and word progress.
- `Assets/Scripts/Gameplay/GateSpawner.cs` and `WordGate.cs`: word queues, deadlines, and storm gates.
- `Assets/Scripts/Gameplay/LaneHazardSpawner.cs`: traffic generation, warnings, collision detection.
- `Assets/Scripts/Gameplay/PowerUpManager.cs`, `PowerUpSpawner.cs`, and `PowerUpPickup.cs`: pickups, timers, and crash protection.
- `Assets/Scripts/Gameplay/TypingStatsTracker.cs`: WPM, accuracy, and best combo.
- `Assets/Scripts/Gameplay/CarController.cs`: lane movement, saved car selection, and crash response.
- `Assets/Scripts/Runtime/TypeDriveSceneBootstrap.cs`: generated world, UI, effects, and system wiring.
- `Assets/Editor/TypeDriveSceneBuilder.cs`: one-click scene creation and Build Settings registration.

## Visual/audio design

The highway uses built-in primitive meshes and emissive Standard materials. The procedural city has a skyline, stars, a moon, lit street lamps, and headlights on the player car. Each level changes the sky tint, road glow, skyline/window colors, ambient light, and fog density. Particles create the exhaust flame and speed lines. Audio clips are synthesized from waveforms at runtime. If Post Processing Stack v2 and its resources are installed, a global Bloom volume is created through reflection; the project has no hard dependency on that package.

## Build and classroom demonstration

1. Open the project in Unity 2022.3 LTS and wait for packages/scripts to import.
2. Open `Assets/Scenes/TypeDrive.unity` and press Play. To regenerate the startup scene, use **TypeDrive > Build Complete Game Scene**.
3. Start City Sprint, type a word, and dodge one barrier with A/D.
4. Pause/resume with Escape, collect a power-up, and show its HUD indicator.
5. Complete the normal gate goal and storm sentence to show the completion panel/fanfare.
6. Show the Game Over stats and restart flow, then demonstrate Endless Drive.
7. Explain the separation between `GameManager`, `TypingController`, `GateSpawner`, and `PowerUpManager`.

See `AUDIT_AND_TEST_CHECKLIST.md` for the issue/fix list and quick manual checks, and `VIVA_CHEATSHEET.md` for ten short teacher questions and answers.
