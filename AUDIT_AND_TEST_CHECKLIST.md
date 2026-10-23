# TypeDrive audit and quick test checklist

## Audit findings and fixes

1. **Only three campaign levels existed.** Added two levels and kept each level definition in `GameLevelCatalog`.
2. **Campaign ended as soon as the normal word count was reached.** Removed that early finish; each campaign now queues one final timed sentence gate and completes only when that storm gate is typed.
3. **There was no editor builder in the project.** Added `Assets/Editor/TypeDriveSceneBuilder.cs` with the requested `TypeDrive > Build Complete Game Scene` menu item and Build Settings registration.
4. **The restart path needed to reset all run systems.** The existing `BeginRun`/`OnRunStarted` reset flow is retained and now resets power-ups, stats, car, gates, traffic, and road state. Game Over retries call `RestartRun`.
5. **Pause/resume assumed a normal time scale.** `GameManager` now owns the gameplay time scale, so slow motion survives pause/resume and all effects stop counting while paused. Leaving to the menu restores normal time.
6. **A shielded collision could still spin the car.** `RegisterCrash` returns whether a crash actually applied; hazard and gate reactions only play when the shield did not absorb it.
7. **The score manager marked the whole runtime systems object `DontDestroyOnLoad`.** Removed that call; persistent records are already stored in PlayerPrefs.
8. **UI scripts dereferenced managers and text fields during `OnEnable`/`Update`.** Added null guards and late event subscription to the older inspector-driven UI scripts.
9. **The pause script and bootstrap both listened for Escape.** Removed the second keyboard listener; the bootstrap is the single owner of Escape pause/resume.
10. **Typing input accepted only arrow lane controls.** Added A/D while retaining arrows and added touch lane buttons.
11. **A word-bank entry was literally `boost`, which could look like a stuck BOOST label in the HUD.** Replaced it with `surge`; there is no always-on BOOST text. The exhaust effect only appears during a high combo.
12. **There were no run typing stats, settings, pickups, UI fades, or storm challenges.** Added separate tracker, settings, pickup, effect, stats-view, and fade scripts; the bootstrap wires them through events.
13. **The builder visuals lacked level-specific fog, night-sky details, and local lights.** Added emissive materials, skyline, stars/moon, car headlights, lit street lamps, and fog density that changes with the selected level.
14. **Duplicate GameManager destruction could reset time scale on the surviving run.** `OnDestroy` now only clears the active singleton and time scale when destroying the active manager.

## Two-minute manual checks

Each section is designed to fit in about two minutes in Play Mode. Use the Unity Inspector to speed up a check when needed; remove any temporary Inspector changes before the presentation.

### 1. Startup, builder, pause, and restart

1. Open `Assets/Scenes/TypeDrive.unity`, wait for compilation, then press Play.
2. Confirm the menu appears with no Console errors. Start Level 1 and verify the car moves forward.
3. Press Escape twice; confirm pause then resume. Press Escape, choose **RESTART DRIVE**, and verify the car returns to its starting position with score/lives reset.
4. Stop Play Mode. Confirm the editor menu contains **TypeDrive > Build Complete Game Scene**. Use it only if you want to regenerate the bootstrap scene.

### 2. Five campaign levels and storm gates

1. Start Level 1 and type its ten short words quickly; check gate progress increases and lives stay unchanged.
2. After word ten, confirm the HUD switches to `STORM GATE` with a countdown and a full sentence.
3. Type the sentence before its timer expires; confirm the level-complete panel and fanfare.
4. Start Levels 2–5 from the menu and confirm their names/colors.

### 3. Endless drive

1. Choose **ENDLESS DRIVE** and type several words while the car moves.
2. Hit a traffic barrier or miss a gate. Confirm the combo resets and speed drops, but the run continues and the HUD shows unlimited lives.
3. Continue until a new road segment and roadside props appear; confirm the road keeps extending.

### 4. Power-ups

1. Start a run and drive through the glowing pickups; they appear ahead in a random lane.
2. Confirm the matching HUD badge appears: `[S] SHIELD`, `[SLOW]`, or `[x2]`. Slow motion lasts five seconds; double score lasts eight seconds.
3. Collect a shield, hit a barrier, and confirm it absorbs one hit without spending a life or spinning the car. Confirm the shield badge disappears.
4. Pause with an active timer; confirm the countdown stops, then resumes.

### 5. WPM, accuracy, and best combo

1. Start a run and type a few words, including one deliberate wrong letter.
2. Cause three campaign crashes (or set lives low in the Inspector) to open Game Over.
3. Confirm WPM, accuracy, and best combo count up and settle at final values. Correct words should increase best combo; the deliberate typo should lower accuracy.

### 6. Car selection

1. At the main menu, use the left/right car arrows to cycle Volt, Pulse, and Nova.
2. Confirm the displayed handling/speed difference and car color update.
3. Start a run, return to menu, and confirm the selection remains saved.

### 7. Settings and PlayerPrefs

1. Open **SETTINGS**, drag master and SFX sliders, then click mute and key hints.
2. Confirm mute changes audio, the key-hint labels appear/disappear, and toggle labels update.
3. Return to menu and reopen Settings; confirm values persist. Restart the editor Play Mode once to check PlayerPrefs persistence.

### 8. Animations and procedural sound

1. Type a word and watch its gate pulse/unlock.
2. Build a four-word combo and confirm the exhaust flame appears; break the combo and confirm it stops.
3. Increase speed and watch for speed streaks; pause/resume and confirm UI panels fade.
4. Finish a storm gate and confirm the level fanfare plays. No audio files need to be imported.

### 9. Touch lane controls

1. In Play Mode, click the left/right HUD arrows with a mouse; confirm they move one lane like A/D.
2. On a touch device, tap the same controls and confirm lane changes remain clamped to three lanes.
3. Typing itself still uses the device keyboard; a virtual typing keyboard is optional and is not included.

### 10. Visuals and optional post-processing

1. Start one level, then another. Confirm sky/road colors and fog change between them.
2. Check skyline, moon/stars, emissive road edges, headlights, and street lamps.
3. If Post Processing Stack v2 is installed, inspect the runtime hierarchy for `Optional Post Processing Volume`; if it is absent, the game still runs without the package.

## Verification status

The source was reviewed and edited in the project folder. I attempted a Unity batch run after the changes, but Unity exited with code 199 before compiling because the `LicenseClient-ASUS` licensing channel refused the connection. So the source has not been confirmed by Unity’s compiler or Play Mode in this environment. Run the manual checks in a licensed Unity 2022.3 editor before the live demo.
