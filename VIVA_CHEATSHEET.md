# TypeDrive — viva cheat sheet

### 1. What is the main idea of TypeDrive?

It is an arcade typing racer. The car moves forward automatically; the player types each word before its gate and changes lanes to avoid traffic.

### 2. What does `GameManager` control?

It is the single source of truth for run state, campaign/endless mode, score, combo, lives, speed, distance, and pause/game-over transitions.

### 3. How does the game know when a level is complete?

`GameLevelCatalog` gives each level a regular word goal. `GateSpawner` queues those words and then creates a timed storm sentence. `RegisterBossSuccess` changes the game to Level Complete only after the sentence is typed.

### 4. How does typing work?

`TypingController` reads one character at a time from Unity input, compares it with the next expected character, reports progress, handles Backspace, and notifies the gate when the whole word or sentence is complete.

### 5. How are traffic collisions and shields handled?

`LaneHazardSpawner` checks the car’s lane and distance from barriers. It reports a crash to `GameManager`; a shield callback can absorb that crash before lives, speed, combo, or crash reactions are changed.

### 6. How do pause and slow motion work together?

The manager stores a gameplay time scale. Slow motion sets it below one, pause sets Unity’s `Time.timeScale` to zero, and resume restores the stored gameplay scale. Effect timers use unscaled time only while the run is Playing.

### 7. How are score and typing stats calculated?

Words award points based on character count plus a combo bonus; double-score power-up multiplies the award. `TypingStatsTracker` counts correct/attempted characters and active seconds. WPM is correct characters divided into five-character words per minute; accuracy is correct divided by attempted characters.

### 8. How are settings and records saved?

`GameSettings` and `HighScoreManager` use Unity `PlayerPrefs`. They save volume, mute, hint visibility, car choice, best score, and best distance locally on the device.

### 9. How is the game built without imported art or audio?

`TypeDriveSceneBootstrap` creates primitive meshes, materials, UI, particle systems, lights, and generated audio at runtime. The editor builder creates a scene containing that bootstrap object.

### 10. How is the project organized for future changes?

Gameplay managers handle one responsibility each and communicate through events. For example, typing raises progress events, the HUD listens for them, and `PowerUpManager` owns effect timers and crash protection. New levels are data entries in `GameLevelCatalog`.
