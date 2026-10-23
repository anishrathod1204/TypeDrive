using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TypeDrive.Audio;
using TypeDrive.Core;
using TypeDrive.Gameplay;
using TypeDrive.UI;
using TypeDrive.Utility;

namespace TypeDrive.Runtime
{
    /// <summary>Builds a complete playable prototype from built-in Unity primitives.</summary>
    [DefaultExecutionOrder(-10000)]
    public class TypeDriveSceneBootstrap : MonoBehaviour
    {
        static readonly Color Midnight = new Color(0.025f, 0.045f, 0.105f);
        static readonly Color PanelBlue = new Color(0.055f, 0.095f, 0.18f, 0.94f);
        static readonly Color Cyan = new Color(0.12f, 0.91f, 1f);
        static readonly Color Pink = new Color(1f, 0.26f, 0.65f);
        static readonly Color Lime = new Color(0.35f, 1f, 0.63f);
        static readonly Color Amber = new Color(1f, 0.62f, 0.15f);

        GameManager gameManager;
        HighScoreManager highScoreManager;
        GameSettings gameSettings;
        TypingController typingController;
        TypingStatsTracker typingStats;
        PowerUpManager powerUpManager;
        CarController carController;
        GateSpawner gateSpawner;
        LaneHazardSpawner hazardSpawner;
        AudioManager audioManager;
        Text scoreText, speedText, comboText, livesText, wordText, wordCaption, warningText, typoText;
        Text bestScoreText, finalScoreText, newRecordText, pausedStatsText;
        Text finalDistanceText, completionTitleText, completionScoreText, completionDetailsText, completionRecordText;
        Text shieldStatusText, slowStatusText, scoreBoostStatusText;
        Text controlsHintText, menuInstructionsText, carSelectionText, settingsMuteText, settingsHintsText;
        Text finalWpmText, finalAccuracyText, finalComboText;
        Button nextLevelButton;
        Slider volumeSlider, sfxVolumeSlider;
        GameObject menuPanel, hudPanel, pausePanel, gameOverPanel, levelCompletePanel, settingsPanel;
        Material themeSkybox, skylineMaterial, windowGlowMaterial, roadGlowMaterial, laneLineMaterial;
        Vector3 startupCarPosition = new Vector3(0f, 0.15f, 2f);

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.antiAliasing = 4;
            ConfigureLighting();
        }

        void Start()
        {
            EnsureEventSystem();
            CreateManagers();
            CreateEnvironment();
            Camera camera = CreateCamera();
            Transform car = CreateCar();
            CreateRoadSystem(car);
            CreateTypingSystem(car);
            CreateAudio(camera);
            CreatePowerUpSystem(car);
            CreateUI();
            if (gameSettings != null) gameSettings.OnChanged += RefreshSettingsUI;
            if (powerUpManager != null) powerUpManager.OnCollected += HandlePowerUpCollected;
            gameManager.OnRunStarted += ApplyRunTheme;
            ApplyRunTheme();
            SetPanels(GameManager.State.MainMenu);
            RefreshStats();
            RefreshWord(typingController);
            if (bestScoreText != null && highScoreManager != null)
                bestScoreText.text = $"BEST RUN  {highScoreManager.BestScore:N0}";
        }

        void Update()
        {
            if (gameManager == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (gameManager.CurrentState == GameManager.State.Playing)
                    gameManager.SetState(GameManager.State.Paused);
                else if (gameManager.CurrentState == GameManager.State.Paused)
                    gameManager.SetState(GameManager.State.Playing);
            }

            if (gameManager.CurrentState == GameManager.State.MainMenu && (settingsPanel == null || !settingsPanel.activeSelf) &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
                gameManager.StartRun();

            if (gameManager.CurrentState == GameManager.State.GameOver && Input.GetKeyDown(KeyCode.R))
                gameManager.RestartRun();

            if (gameManager.CurrentState == GameManager.State.Playing)
                RefreshLiveReadouts();
        }

        void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged -= SetPanels;
                gameManager.OnRunStarted -= ApplyRunTheme;
                gameManager.OnScoreChanged -= RefreshStats;
                gameManager.OnLivesChanged -= RefreshStats;
            }
            if (gameSettings != null) gameSettings.OnChanged -= RefreshSettingsUI;
            if (powerUpManager != null) powerUpManager.OnCollected -= HandlePowerUpCollected;
            if (typingController != null)
            {
                typingController.OnProgressChanged -= RefreshWord;
                typingController.OnTypo -= ShowTypo;
            }
        }

        void EnsureEventSystem()
        {
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            eventSystem.enabled = true;

            BaseInputModule inputModule = eventSystem.GetComponent<BaseInputModule>();
            if (inputModule == null)
                inputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            inputModule.enabled = true;
        }

        void CreateManagers()
        {
            GameObject systems = new GameObject("Game Systems");
            gameManager = systems.AddComponent<GameManager>();
            highScoreManager = systems.AddComponent<HighScoreManager>();
            gameSettings = systems.AddComponent<GameSettings>();
            powerUpManager = systems.AddComponent<PowerUpManager>();
        }

        void ConfigureLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.37f, 0.53f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.055f, 0.075f, 0.17f);
            RenderSettings.fogDensity = 0.0035f;

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                themeSkybox = new Material(skyShader);
                themeSkybox.SetColor("_SkyTint", new Color(0.44f, 0.42f, 0.7f));
                themeSkybox.SetColor("_GroundColor", new Color(0.09f, 0.1f, 0.19f));
                themeSkybox.SetFloat("_AtmosphereThickness", 1.15f);
                RenderSettings.skybox = themeSkybox;
            }

            GameObject sun = new GameObject("Soft Sunset Light");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.78f, 0.66f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(34f, -28f, 0f);
        }

        void CreateEnvironment()
        {
            Material ground = MakeMaterial(new Color(0.035f, 0.055f, 0.12f), 0.08f, 0.25f);
            MakePrimitive(PrimitiveType.Cube, "Endless dark terrain", transform,
                new Vector3(0f, -0.32f, 12000f), new Vector3(160f, 0.6f, 30000f), ground);

            skylineMaterial = MakeMaterial(new Color(0.065f, 0.09f, 0.2f), 0.12f, 0.35f);
            windowGlowMaterial = MakeMaterial(new Color(0.14f, 0.44f, 0.75f), 0.2f, 0.3f, true);
            for (int i = 0; i < 36; i++)
            {
                float z = 45f + i * 24f;
                float height = UnityEngine.Random.Range(9f, 24f);
                float width = UnityEngine.Random.Range(5f, 10f);
                float depth = UnityEngine.Random.Range(6f, 13f);
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * UnityEngine.Random.Range(20f, 36f);
                    GameObject building = MakePrimitive(PrimitiveType.Cube, "Distant skyline block", transform,
                        new Vector3(x, height * 0.5f - 0.3f, z + UnityEngine.Random.Range(-8f, 8f)),
                        new Vector3(width, height, depth), skylineMaterial);
                    if (i % 3 == 0)
                        MakePrimitive(PrimitiveType.Cube, "Neon rooftop", building.transform,
                            new Vector3(0f, 0.48f, -0.05f), new Vector3(0.82f, 0.025f, 0.78f), windowGlowMaterial);
                }
            }
        }

        Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 61f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 400f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
            follow.offset = new Vector3(0f, 5.2f, -9.5f);
            follow.baseFOV = 61f;
            follow.maxFOVBonus = 9f;
            cameraObject.AddComponent<ScreenShake>();
            CreateSkyDetails(cameraObject.transform);
            ParticleSystem speedLines = cameraObject.gameObject.AddComponent<ParticleSystem>();
            var lineMain = speedLines.main;
            lineMain.loop = true;
            lineMain.playOnAwake = true;
            lineMain.startLifetime = 0.48f;
            lineMain.startSpeed = -45f;
            lineMain.startSize3D = true;
            lineMain.startSizeX = 0.025f;
            lineMain.startSizeY = 0.025f;
            lineMain.startSizeZ = 1.2f;
            lineMain.startColor = new Color(0.2f, 0.92f, 1f, 0.32f);
            lineMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            var lineEmission = speedLines.emission;
            lineEmission.rateOverTime = 0f;
            var lineShape = speedLines.shape;
            lineShape.shapeType = ParticleSystemShapeType.Box;
            lineShape.scale = new Vector3(18f, 6f, 3f);
            ParticleSystemRenderer lineRenderer = speedLines.GetComponent<ParticleSystemRenderer>();
            Shader particleShader = Shader.Find("Particles/Standard Unlit");
            if (particleShader != null)
            {
                lineRenderer.material = new Material(particleShader);
                lineRenderer.material.color = new Color(0.2f, 0.92f, 1f, 0.32f);
            }
            cameraObject.gameObject.AddComponent<SpeedLinesEffect>();
            OptionalPostProcessing.TryAttach(camera);
            return camera;
        }

        void CreateSkyDetails(Transform cameraTransform)
        {
            Material starMaterial = MakeMaterial(new Color(0.5f, 0.83f, 1f), 0f, 0f, true);
            for (int i = 0; i < 64; i++)
            {
                float x = UnityEngine.Random.Range(-75f, 75f);
                float y = UnityEngine.Random.Range(12f, 54f);
                float z = UnityEngine.Random.Range(150f, 280f);
                float size = UnityEngine.Random.Range(0.08f, 0.22f);
                MakePrimitive(PrimitiveType.Sphere, "Sky star", cameraTransform,
                    new Vector3(x, y, z), Vector3.one * size, starMaterial);
            }
            Material moonMaterial = MakeMaterial(new Color(0.74f, 0.84f, 1f), 0f, 0.1f, true);
            MakePrimitive(PrimitiveType.Sphere, "Moon", cameraTransform,
                new Vector3(52f, 37f, 210f), new Vector3(10f, 10f, 1.2f), moonMaterial);
        }
        Transform CreateCar()
        {
            GameObject car = new GameObject("Player Car");
            car.transform.position = startupCarPosition;
            Rigidbody body = car.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            BoxCollider collider = car.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.48f, 0f);
            collider.size = new Vector3(1.7f, 1f, 3.25f);

            Material bodyMat = MakeMaterial(new Color(0.04f, 0.7f, 0.82f), 0.55f, 0.55f);
            Material glassMat = MakeMaterial(new Color(0.16f, 0.23f, 0.43f), 0.7f, 0.3f);
            Material tireMat = MakeMaterial(new Color(0.018f, 0.025f, 0.04f), 0f, 0.18f);
            Material lightMat = MakeMaterial(new Color(0.7f, 0.95f, 1f), 0.2f, 0.15f, true);
            Material tailMat = MakeMaterial(Pink, 0.15f, 0.1f, true);
            bodyMat.EnableKeyword("_EMISSION");
            bodyMat.SetColor("_EmissionColor", new Color(0.04f, 0.7f, 0.82f) * 0.4f);

            MakePrimitive(PrimitiveType.Cube, "Sculpted lower body", car.transform,
                new Vector3(0f, 0.26f, 0f), new Vector3(1.7f, 0.42f, 3.3f), bodyMat);
            MakePrimitive(PrimitiveType.Cube, "Canopy", car.transform,
                new Vector3(0f, 0.61f, -0.12f), new Vector3(1.12f, 0.48f, 1.53f), glassMat);
            MakePrimitive(PrimitiveType.Cube, "Front splitter", car.transform,
                new Vector3(0f, 0.12f, 1.57f), new Vector3(1.9f, 0.12f, 0.34f), tireMat);

            for (int side = -1; side <= 1; side += 2)
            {
                MakePrimitive(PrimitiveType.Cube, "Headlight", car.transform,
                    new Vector3(side * 0.57f, 0.35f, 1.68f), new Vector3(0.33f, 0.09f, 0.12f), lightMat);
                MakePrimitive(PrimitiveType.Cube, "Tail light", car.transform,
                    new Vector3(side * 0.58f, 0.34f, -1.68f), new Vector3(0.34f, 0.1f, 0.1f), tailMat);
                foreach (float z in new[] { -1.05f, 1.05f })
                {
                    GameObject wheel = MakePrimitive(PrimitiveType.Cylinder, "Low-profile wheel", car.transform,
                        new Vector3(side * 0.84f, 0.15f, z), new Vector3(0.3f, 0.3f, 0.18f), tireMat);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }

            GameObject exhaust = new GameObject("Nitro exhaust");
            exhaust.transform.SetParent(car.transform, false);
            exhaust.transform.localPosition = new Vector3(0f, 0.16f, -1.74f);
            ParticleSystem particles = exhaust.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 0.32f;
            main.startSpeed = 2.4f;
            main.startSize = 0.22f;
            main.startColor = Cyan;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = particles.emission;
            emission.rateOverTime = 42f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.13f;
            exhaust.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            NitroEffect nitro = car.AddComponent<NitroEffect>();
            nitro.nitroParticles = particles;
            nitro.comboThreshold = 4;
            ParticleSystemRenderer nitroRenderer = particles.GetComponent<ParticleSystemRenderer>();
            Shader flameShader = Shader.Find("Particles/Standard Unlit");
            if (flameShader != null)
            {
                nitroRenderer.material = new Material(flameShader);
                nitroRenderer.material.color = new Color(1f, 0.35f, 0.08f);
            }

            carController = car.AddComponent<CarController>();
            carController.ApplyPreset();
            CreateHeadlights(car.transform);
            CameraFollow follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : FindObjectOfType<CameraFollow>();
            if (follow != null) follow.target = car.transform;
            return car.transform;
        }

        void CreateHeadlights(Transform car)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject lamp = new GameObject("Headlight glow");
                lamp.transform.SetParent(car, false);
                lamp.transform.localPosition = new Vector3(side * 0.58f, 0.4f, 1.58f);
                Light point = lamp.AddComponent<Light>();
                point.type = LightType.Point;
                point.color = Cyan;
                point.intensity = 1.2f;
                point.range = 8f;
                point.shadows = LightShadows.None;
            }
        }

        void CreateRoadSystem(Transform car)
        {
            Material road = MakeMaterial(new Color(0.055f, 0.075f, 0.13f), 0.16f, 0.35f);
            roadGlowMaterial = MakeMaterial(Cyan, 0.15f, 0.1f, true);
            laneLineMaterial = MakeMaterial(new Color(0.3f, 0.38f, 0.55f), 0.05f, 0.1f);

            GameObject roadPrefab = new GameObject("Road segment template");
            {
                float z = 15f;
                MakePrimitive(PrimitiveType.Cube, "Road deck", roadPrefab.transform,
                    new Vector3(0f, -0.17f, z), new Vector3(12f, 0.34f, 30f), road);
                for (int side = -1; side <= 1; side += 2)
                    MakePrimitive(PrimitiveType.Cube, "Luminous road edge", roadPrefab.transform,
                        new Vector3(side * 5.82f, 0.015f, z), new Vector3(0.075f, 0.035f, 30f), roadGlowMaterial);
                for (int lane = -1; lane <= 1; lane += 2)
                    for (int dash = 0; dash < 5; dash++)
                        MakePrimitive(PrimitiveType.Cube, "Dashed lane marker", roadPrefab.transform,
                            new Vector3(lane * 1.9f, 0.014f, z - 12f + dash * 6f),
                            new Vector3(0.045f, 0.025f, 2.1f), laneLineMaterial);
            }
            roadPrefab.SetActive(false);

            GameObject roadManagerObject = new GameObject("Road Manager");
            RoadManager roadManager = roadManagerObject.AddComponent<RoadManager>();
            roadManager.car = car;
            roadManager.roadSegmentPrefab = roadPrefab;
            roadManager.segmentLength = 30f;
            roadManager.segmentsAhead = 7;

            GameObject[] props = { BuildTreePrefab(), BuildLampPrefab(), BuildPropBuildingPrefab() };
            GameObject propsObject = new GameObject("Roadside Scenery");
            PropSpawner propSpawner = propsObject.AddComponent<PropSpawner>();
            propSpawner.car = car;
            propSpawner.propPrefabs = props;
            propSpawner.sideOffset = 8.5f;
            propSpawner.spacing = 16f;
            propSpawner.propsAhead = 14;
        }

        GameObject BuildTreePrefab()
        {
            GameObject root = new GameObject("Neon tree template");
            Material trunk = MakeMaterial(new Color(0.12f, 0.2f, 0.31f), 0.1f, 0.2f);
            Material leaves = MakeMaterial(new Color(0.08f, 0.66f, 0.61f), 0.1f, 0.2f, true);
            MakePrimitive(PrimitiveType.Cylinder, "Trunk", root.transform,
                new Vector3(0f, 0.9f, 0f), new Vector3(0.2f, 0.9f, 0.2f), trunk);
            MakePrimitive(PrimitiveType.Sphere, "Crown", root.transform,
                new Vector3(0f, 2.2f, 0f), new Vector3(1.15f, 1.8f, 1.15f), leaves);
            root.SetActive(false);
            return root;
        }

        GameObject BuildLampPrefab()
        {
            GameObject root = new GameObject("Neon streetlight template");
            Material pole = MakeMaterial(new Color(0.18f, 0.22f, 0.35f), 0.3f, 0.4f);
            Material glow = MakeMaterial(new Color(1f, 0.36f, 0.67f), 0.1f, 0.1f, true);
            MakePrimitive(PrimitiveType.Cylinder, "Pole", root.transform,
                new Vector3(0f, 2f, 0f), new Vector3(0.1f, 2f, 0.1f), pole);
            MakePrimitive(PrimitiveType.Cube, "Arm", root.transform,
                new Vector3(-0.48f, 3.92f, 0f), new Vector3(1f, 0.08f, 0.08f), pole);
            MakePrimitive(PrimitiveType.Sphere, "Lamp", root.transform,
                new Vector3(-0.92f, 3.86f, 0f), new Vector3(0.35f, 0.18f, 0.35f), glow);
            GameObject pointObject = new GameObject("Warm lamp light");
            pointObject.transform.SetParent(root.transform, false);
            pointObject.transform.localPosition = new Vector3(-0.92f, 3.75f, 0f);
            Light point = pointObject.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = new Color(1f, 0.4f, 0.75f);
            point.intensity = 0.45f;
            point.range = 7f;
            point.shadows = LightShadows.None;
            root.SetActive(false);
            return root;
        }

        GameObject BuildPropBuildingPrefab()
        {
            GameObject root = new GameObject("Roadside building template");
            Material walls = MakeMaterial(new Color(0.095f, 0.13f, 0.26f), 0.15f, 0.3f);
            Material windows = MakeMaterial(new Color(0.1f, 0.67f, 0.9f), 0.1f, 0.1f, true);
            MakePrimitive(PrimitiveType.Cube, "Building", root.transform,
                new Vector3(0f, 2f, 0f), new Vector3(2.1f, 4f, 1.6f), walls);
            for (int row = 0; row < 3; row++)
                for (int column = -1; column <= 1; column++)
                    MakePrimitive(PrimitiveType.Cube, "Window", root.transform,
                        new Vector3(column * 0.52f, 0.9f + row * 1.1f, -0.81f),
                        new Vector3(0.28f, 0.36f, 0.035f), windows);
            root.SetActive(false);
            return root;
        }
        void CreateTypingSystem(Transform car)
        {
            GameObject gatePrefab = BuildGatePrefab();
            GameObject gateManagerObject = new GameObject("Word Gate Spawner");
            gateSpawner = gateManagerObject.AddComponent<GateSpawner>();
            gateSpawner.car = car;
            gateSpawner.gatePrefab = gatePrefab;
            gateSpawner.gateSpacing = 24f;
            gateSpawner.gatesAhead = 5;

            GameObject typingObject = new GameObject("Typing Input");
            typingController = typingObject.AddComponent<TypingController>();
            typingController.Bind(gateSpawner);
            typingStats = typingObject.AddComponent<TypingStatsTracker>();
            typingStats.Bind(typingController);

            GameObject hazardPrefab = BuildHazardPrefab();
            GameObject hazardsObject = new GameObject("Traffic Hazard Spawner");
            hazardSpawner = hazardsObject.AddComponent<LaneHazardSpawner>();
            hazardSpawner.car = car;
            hazardSpawner.hazardPrefab = hazardPrefab;
            hazardSpawner.spacing = 40f;
            hazardSpawner.firstHazardZ = 44f;
            hazardSpawner.hazardsAhead = 6;
        }

        GameObject BuildGatePrefab()
        {
            GameObject root = new GameObject("Word gate template");
            Material frame = MakeMaterial(new Color(0.07f, 0.68f, 0.82f), 0.2f, 0.25f, true);
            MakePrimitive(PrimitiveType.Cube, "Left gate post", root.transform,
                new Vector3(-5.45f, 1.52f, 0f), new Vector3(0.28f, 3.05f, 0.35f), frame);
            MakePrimitive(PrimitiveType.Cube, "Right gate post", root.transform,
                new Vector3(5.45f, 1.52f, 0f), new Vector3(0.28f, 3.05f, 0.35f), frame);
            MakePrimitive(PrimitiveType.Cube, "Gate header", root.transform,
                new Vector3(0f, 3.05f, 0f), new Vector3(11.15f, 0.32f, 0.48f), frame);
            // Keep the road clear: the active word is shown in the screen-space HUD,
            // so a world-space word sign would duplicate it and block the view.

            GameObject trigger = new GameObject("Deadline trigger");
            trigger.transform.SetParent(root.transform, false);
            WordGate wordGate = root.AddComponent<WordGate>();
            wordGate.triggerPoint = trigger.transform;
            root.AddComponent<GateUnlockAnimation>();
            root.SetActive(false);
            return root;
        }

        GameObject BuildHazardPrefab()
        {
            GameObject root = new GameObject("Traffic barrier template");
            Material hazard = MakeMaterial(new Color(1f, 0.29f, 0.18f), 0.1f, 0.1f, true);
            Material stripe = MakeMaterial(new Color(1f, 0.76f, 0.18f), 0.1f, 0.1f, true);
            MakePrimitive(PrimitiveType.Cube, "Barrier core", root.transform,
                new Vector3(0f, 0.72f, 0f), new Vector3(1.8f, 1.4f, 0.86f), hazard);
            for (int side = -1; side <= 1; side += 2)
                MakePrimitive(PrimitiveType.Cube, "Warning stripe", root.transform,
                    new Vector3(side * 0.48f, 0.74f, -0.445f), new Vector3(0.16f, 1.2f, 0.04f), stripe);
            root.SetActive(false);
            return root;
        }

        void CreateAudio(Camera camera)
        {
            GameObject audioObject = new GameObject("Procedural Audio");
            AudioSource engine = audioObject.AddComponent<AudioSource>();
            engine.spatialBlend = 0f;
            engine.playOnAwake = false;
            AudioSource sfx = audioObject.AddComponent<AudioSource>();
            sfx.spatialBlend = 0f;
            sfx.playOnAwake = false;
            audioManager = audioObject.AddComponent<AudioManager>();
            audioManager.Configure(engine, sfx, typingController,
                CreateEngineLoop(), CreateTone("Keystroke", 0.035f, 900f, 0.48f),
                CreateSweep("Word complete", 0.32f, 480f, 980f),
                CreateTone("Typing error", 0.13f, 165f, 0.7f),
                CreateCrashClip(), CreateTone("UI click", 0.045f, 620f, 0.4f));
            audioManager.powerUpClip = CreateSweep("Power up pickup", 0.26f, 420f, 1150f);
            audioManager.levelCompleteClip = CreateFanfare();
            if (gameSettings != null) audioManager.SetEffectsVolume(gameSettings.SfxVolume);

            ScreenShake shake = camera.GetComponent<ScreenShake>();
            GameObject feedbackObject = new GameObject("Crash Feedback");
            CrashFeedback feedback = feedbackObject.AddComponent<CrashFeedback>();
            feedback.screenShake = shake;
            feedback.audioManager = audioManager;
        }

        AudioClip CreateTone(string name, float duration, float frequency, float amplitude)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Clamp01(1f - t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * amplitude;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateSweep(string name, float duration, float startFrequency, float endFrequency)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float p = t / duration;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, p);
                float envelope = Mathf.Sin(Mathf.PI * p);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.34f;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateCrashClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.42f;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];
            System.Random random = new System.Random(7);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Pow(1f - t / duration, 2f);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * 72f * t) * 0.6f + noise * 0.35f) * envelope * 0.55f;
            }
            AudioClip clip = AudioClip.Create("Soft arcade crash", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateEngineLoop()
        {
            const int sampleRate = 22050;
            const int count = 22050;
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float low = Mathf.Sin(2f * Mathf.PI * 54f * t);
                float overtone = Mathf.Sin(2f * Mathf.PI * 108f * t) * 0.34f;
                samples[i] = (low + overtone) * 0.18f;
            }
            AudioClip clip = AudioClip.Create("Engine hum loop", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateFanfare()
        {
            const int sampleRate = 22050;
            const float duration = 1.05f;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                int note = Mathf.Clamp(Mathf.FloorToInt(t / 0.24f), 0, notes.Length - 1);
                float local = t - note * 0.24f;
                float envelope = Mathf.Clamp01(1f - local / 0.31f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * notes[note] * t) * envelope * 0.36f;
            }
            AudioClip clip = AudioClip.Create("Level complete fanfare", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void CreatePowerUpSystem(Transform car)
        {
            GameObject prefab = new GameObject("Power-up pickup template");
            Material pickupMaterial = MakeMaterial(new Color(0.22f, 0.95f, 1f), 0.1f, 0.2f, true);
            MakePrimitive(PrimitiveType.Sphere, "Glowing pickup core", prefab.transform,
                Vector3.zero, Vector3.one * 0.8f, pickupMaterial);
            GameObject ring = MakePrimitive(PrimitiveType.Cylinder, "Pickup halo", prefab.transform,
                Vector3.zero, new Vector3(1.2f, 0.05f, 1.2f), pickupMaterial);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            prefab.AddComponent<PowerUpPickup>();
            prefab.SetActive(false);

            GameObject spawnerObject = new GameObject("Power-up Spawner");
            PowerUpSpawner spawner = spawnerObject.AddComponent<PowerUpSpawner>();
            spawner.car = car;
            spawner.manager = powerUpManager;
            spawner.pickupPrefab = prefab;
        }
        void ApplyRunTheme()
        {
            if (gameManager == null) return;

            GameLevelDefinition theme = gameManager.IsEndless
                ? GameLevelCatalog.Get(0)
                : gameManager.SelectedLevel;

            RenderSettings.fogColor = theme.FogColor;
            int themeIndex = gameManager.IsEndless ? 0 : gameManager.SelectedLevelIndex;
            RenderSettings.fogDensity = 0.0032f + themeIndex * 0.00042f;
            RenderSettings.ambientLight = theme.AmbientColor;
            if (themeSkybox != null)
            {
                themeSkybox.SetColor("_SkyTint", theme.SkyTint);
                themeSkybox.SetColor("_GroundColor", theme.FogColor * 0.75f);
                RenderSettings.skybox = themeSkybox;
            }

            SetThemeMaterial(skylineMaterial, theme.SkylineColor, false);
            SetThemeMaterial(windowGlowMaterial, theme.WindowGlow, true);
            SetThemeMaterial(roadGlowMaterial, theme.RoadGlow, true);
            SetThemeMaterial(laneLineMaterial, theme.LaneMarking, false);
        }

        void SetThemeMaterial(Material material, Color color, bool glow)
        {
            if (material == null) return;
            material.color = color;
            if (glow && material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 1.35f);
        }

        void CreateUI()
        {
            GameObject canvasObject = new GameObject("TypeDrive UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            menuPanel = CreatePanel("Main Menu", canvas.transform, new Color(0.02f, 0.035f, 0.09f, 0.92f));
            CreateLabel(menuPanel.transform, "Title", "TYPE<color=#22E8FF>DRIVE</color>", 74, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.81f), new Vector2(0.5f, 0.81f), Vector2.zero, new Vector2(900f, 105f));
            CreateLabel(menuPanel.transform, "Tagline", "TYPE FAST.  DRIVE FASTER.", 25, new Color(0.67f, 0.79f, 0.96f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.73f), new Vector2(0.5f, 0.73f), Vector2.zero, new Vector2(720f, 48f));
            CreateLabel(menuPanel.transform, "Intro", "A neon highway typing challenge", 18, new Color(0.43f, 0.61f, 0.79f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.675f), new Vector2(0.5f, 0.675f), Vector2.zero, new Vector2(720f, 38f));
            CreateLabel(menuPanel.transform, "Drive Modes", "CHOOSE YOUR DRIVE", 14, new Color(0.49f, 0.68f, 0.83f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.645f), new Vector2(0.5f, 0.645f), Vector2.zero, new Vector2(500f, 28f));

            CreateButton(menuPanel.transform, "LEVEL 1  •  CITY SPRINT", new Vector2(-200f, 27f), new Vector2(370f, 54f), Cyan,
                () => gameManager.StartLevel(0));
            CreateButton(menuPanel.transform, "LEVEL 2  •  MIDNIGHT RUSH", new Vector2(-200f, -35f), new Vector2(370f, 54f), new Color(1f, 0.53f, 0.78f),
                () => gameManager.StartLevel(1));
            CreateButton(menuPanel.transform, "LEVEL 3  •  NEON GRAND PRIX", new Vector2(200f, 27f), new Vector2(370f, 54f), new Color(0.58f, 0.61f, 1f),
                () => gameManager.StartLevel(2));
            CreateButton(menuPanel.transform, "LEVEL 4  •  STORM CIRCUIT", new Vector2(200f, -35f), new Vector2(370f, 54f), new Color(0.35f, 0.95f, 0.87f),
                () => gameManager.StartLevel(3));
            CreateButton(menuPanel.transform, "LEVEL 5  •  NOVA SHOWDOWN", new Vector2(0f, -98f), new Vector2(370f, 54f), new Color(1f, 0.47f, 0.65f),
                () => gameManager.StartLevel(4));
            CreateButton(menuPanel.transform, "ENDLESS DRIVE  •  UNLIMITED", new Vector2(0f, -159f), new Vector2(370f, 54f), Lime,
                () => gameManager.StartEndless());
            CreateButton(menuPanel.transform, "SETTINGS", new Vector2(0f, -224f), new Vector2(230f, 48f), new Color(0.45f, 0.58f, 0.76f),
                OpenSettings);

            CreateLabel(menuPanel.transform, "Car Selection Title", "SELECT YOUR CAR", 15, new Color(0.49f, 0.68f, 0.83f),
                TextAnchor.MiddleCenter, new Vector2(0.85f, 0.59f), new Vector2(0.85f, 0.59f), Vector2.zero, new Vector2(230f, 30f));
            CreateButton(menuPanel.transform, "◀", new Vector2(450f, 0f), new Vector2(50f, 48f), new Color(0.24f, 0.42f, 0.62f),
                () => ChangeCar(-1));
            carSelectionText = CreateLabel(menuPanel.transform, "Selected Car", "VOLT", 20, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.85f, 0.5f), new Vector2(0.85f, 0.5f), Vector2.zero, new Vector2(165f, 46f));
            CreateButton(menuPanel.transform, "▶", new Vector2(670f, 0f), new Vector2(50f, 48f), new Color(0.24f, 0.42f, 0.62f),
                () => ChangeCar(1));

            bestScoreText = CreateLabel(menuPanel.transform, "Best Score", "BEST SCORE  0", 17, Lime,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.205f), new Vector2(0.5f, 0.205f), Vector2.zero, new Vector2(500f, 34f));
            menuInstructionsText = CreateLabel(menuPanel.transform, "Instructions",
                "TYPE EACH WORD BEFORE ITS GATE  •  BUILD A STREAK TO GAIN SPEED\n" +
                 "USE  A / D  OR  ← / →  TO DODGE  •  BACKSPACE CORRECTS  •  ESC PAUSES",
                15, new Color(0.78f, 0.86f, 0.98f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.11f), new Vector2(0.5f, 0.11f), Vector2.zero, new Vector2(1150f, 66f));
            CreateLabel(menuPanel.transform, "Footer", "TYPE DRIVE  •  CAMPAIGN + ENDLESS ARCADE", 13, new Color(0.37f, 0.49f, 0.68f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.035f), new Vector2(0.5f, 0.035f), Vector2.zero, new Vector2(650f, 24f));

            hudPanel = new GameObject("HUD", typeof(RectTransform));
            hudPanel.transform.SetParent(canvas.transform, false);
            hudPanel.AddComponent<CanvasGroup>();
            hudPanel.AddComponent<UIFadeTransition>();
            hudPanel.GetComponent<CanvasGroup>().alpha = 0f;
            scoreText = CreateLabel(hudPanel.transform, "Score", "SCORE  0", 25, Color.white,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -54f), new Vector2(340f, 78f));
            speedText = CreateLabel(hudPanel.transform, "Speed", "08 m/s", 24, Cyan,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(260f, 78f));
            comboText = CreateLabel(hudPanel.transform, "Combo", "STREAK  x0", 21, Lime,
                TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-360f, -54f), new Vector2(210f, 78f));
            livesText = CreateLabel(hudPanel.transform, "Lives", "LIVES  ♥  ♥  ♥", 19, Pink,
                TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -54f), new Vector2(205f, 78f));

            GameObject topBar = CreateImage(hudPanel.transform, "HUD top bar", PanelBlue,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(1600f, 112f));
            topBar.transform.SetAsFirstSibling();
            GameObject accent = CreateImage(hudPanel.transform, "HUD accent", Cyan,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(1600f, 3f));
            accent.transform.SetSiblingIndex(1);

            GameObject wordCard = CreateImage(hudPanel.transform, "Word card", new Color(0.04f, 0.075f, 0.15f, 0.92f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(790f, 186f));
            wordCaption = CreateLabel(wordCard.transform, "Objective", "CAMPAIGN OBJECTIVE", 16, new Color(0.49f, 0.68f, 0.83f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.79f), Vector2.zero, new Vector2(740f, 38f));
            wordText = CreateLabel(wordCard.transform, "Current Word", "READY", 58, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(740f, 80f));
            warningText = CreateLabel(hudPanel.transform, "Traffic Warning", "", 22, Amber,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -115f), new Vector2(800f, 55f));
            typoText = CreateLabel(hudPanel.transform, "Input Feedback", "CHECK THAT LETTER", 15, Pink,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(400f, 28f));
            typoText.gameObject.SetActive(false);
            controlsHintText = CreateLabel(hudPanel.transform, "Controls", "A / D OR  ← / →  CHANGE LANE          ESC  PAUSE", 13,
                new Color(0.58f, 0.68f, 0.82f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(620f, 24f));
            shieldStatusText = CreateLabel(hudPanel.transform, "Shield Icon", "[S] SHIELD", 14, Cyan,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-240f, -122f), new Vector2(190f, 30f));
            slowStatusText = CreateLabel(hudPanel.transform, "Slow Motion Icon", "[SLOW] 5.0s", 14, new Color(0.69f, 0.62f, 1f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(190f, 30f));
            scoreBoostStatusText = CreateLabel(hudPanel.transform, "Double Score Icon", "[x2] SCORE", 14, Amber,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(240f, -122f), new Vector2(190f, 30f));
            shieldStatusText.gameObject.SetActive(false);
            slowStatusText.gameObject.SetActive(false);
            scoreBoostStatusText.gameObject.SetActive(false);

            CreateTouchButton(hudPanel.transform, "◀", new Vector2(76f, 258f), () => carController?.RequestLaneChange(-1));
            CreateTouchButton(hudPanel.transform, "▶", new Vector2(-76f, 258f), () => carController?.RequestLaneChange(1), true);

            pausePanel = CreatePanel("Pause Menu", canvas.transform, new Color(0.015f, 0.025f, 0.07f, 0.88f));
            CreateLabel(pausePanel.transform, "Paused Title", "RUN PAUSED", 54, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(700f, 90f));
            pausedStatsText = CreateLabel(pausePanel.transform, "Pause Stats", "", 21, new Color(0.64f, 0.79f, 0.92f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(700f, 44f));
            CreateButton(pausePanel.transform, "RESUME", new Vector2(0f, -22f), new Vector2(260f, 58f), Cyan,
                () => gameManager.SetState(GameManager.State.Playing));
            CreateButton(pausePanel.transform, "RESTART DRIVE", new Vector2(0f, -98f), new Vector2(260f, 54f), Pink,
                () => gameManager.StartRun());
            CreateButton(pausePanel.transform, "MAIN MENU", new Vector2(0f, -168f), new Vector2(260f, 50f), new Color(0.27f, 0.36f, 0.54f),
                () => gameManager.SetState(GameManager.State.MainMenu));

            gameOverPanel = CreatePanel("Game Over", canvas.transform, new Color(0.025f, 0.025f, 0.09f, 0.94f));
            CreateLabel(gameOverPanel.transform, "Game Over Title", "DRIVE OVER", 53, Pink,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(760f, 90f));
            finalScoreText = CreateLabel(gameOverPanel.transform, "Final Score", "SCORE  0", 34, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(650f, 60f));
            finalDistanceText = CreateLabel(gameOverPanel.transform, "Final Distance", "DISTANCE  0 m", 22, Cyan,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.59f), new Vector2(0.5f, 0.59f), Vector2.zero, new Vector2(650f, 42f));
            finalWpmText = CreateLabel(gameOverPanel.transform, "Final WPM", "0 WPM", 19, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.53f), new Vector2(0.5f, 0.53f), Vector2.zero, new Vector2(300f, 35f));
            finalAccuracyText = CreateLabel(gameOverPanel.transform, "Final Accuracy", "100% ACCURACY", 19, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(300f, 35f));
            finalComboText = CreateLabel(gameOverPanel.transform, "Best Combo", "BEST COMBO  x0", 19, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(330f, 35f));
            newRecordText = CreateLabel(gameOverPanel.transform, "Record Label", "NEW PERSONAL BEST", 16, Lime,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(650f, 30f));
            AnimatedRunStats animatedStats = gameOverPanel.AddComponent<AnimatedRunStats>();
            animatedStats.wpmText = finalWpmText;
            animatedStats.accuracyText = finalAccuracyText;
            animatedStats.bestComboText = finalComboText;
            CreateButton(gameOverPanel.transform, "TRY AGAIN", new Vector2(0f, -175f), new Vector2(270f, 56f), Cyan,
                () => gameManager.RestartRun());
            CreateButton(gameOverPanel.transform, "MAIN MENU", new Vector2(0f, -250f), new Vector2(270f, 52f), new Color(0.26f, 0.36f, 0.54f),
                () => gameManager.SetState(GameManager.State.MainMenu));
            CreateLabel(gameOverPanel.transform, "Retry Hint", "CLICK TRY AGAIN  •  OR PRESS R", 14,
                new Color(0.58f, 0.68f, 0.82f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), Vector2.zero, new Vector2(500f, 30f));

            levelCompletePanel = CreatePanel("Level Complete", canvas.transform, new Color(0.025f, 0.04f, 0.12f, 0.94f));
            completionTitleText = CreateLabel(levelCompletePanel.transform, "Completion Title", "LEVEL COMPLETE", 50, Lime,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(760f, 85f));
            completionScoreText = CreateLabel(levelCompletePanel.transform, "Completion Score", "SCORE  0", 34, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.63f), new Vector2(0.5f, 0.63f), Vector2.zero, new Vector2(650f, 58f));
            completionDetailsText = CreateLabel(levelCompletePanel.transform, "Completion Details", "", 20, Cyan,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(800f, 40f));
            completionRecordText = CreateLabel(levelCompletePanel.transform, "Completion Record", "NEW PERSONAL BEST", 17, Lime,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(650f, 34f));
            nextLevelButton = CreateButton(levelCompletePanel.transform, "NEXT LEVEL", new Vector2(0f, -75f), new Vector2(290f, 56f), Cyan,
                () =>
                {
                    int nextIndex = gameManager.SelectedLevelIndex + 1;
                    gameManager.StartLevel(nextIndex < GameLevelCatalog.Count ? nextIndex : 0);
                });
            CreateButton(levelCompletePanel.transform, "ENDLESS DRIVE", new Vector2(0f, -145f), new Vector2(290f, 52f), Lime,
                () => gameManager.StartEndless());
            CreateButton(levelCompletePanel.transform, "MAIN MENU", new Vector2(0f, -215f), new Vector2(290f, 50f), new Color(0.26f, 0.36f, 0.54f),
                () => gameManager.SetState(GameManager.State.MainMenu));

            settingsPanel = CreatePanel("Settings Menu", canvas.transform, new Color(0.015f, 0.025f, 0.07f, 0.97f));
            CreateLabel(settingsPanel.transform, "Settings Title", "GAME SETTINGS", 46, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.69f), new Vector2(0.5f, 0.69f), Vector2.zero, new Vector2(700f, 75f));
            CreateLabel(settingsPanel.transform, "Volume Label", "MASTER VOLUME", 18, new Color(0.67f, 0.79f, 0.96f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f), Vector2.zero, new Vector2(500f, 36f));
            volumeSlider = CreateSlider(settingsPanel.transform, new Vector2(0f, 45f), new Vector2(420f, 30f));
            volumeSlider.value = gameSettings != null ? gameSettings.MasterVolume : 0.8f;
            volumeSlider.onValueChanged.AddListener(value => gameSettings?.SetMasterVolume(value));
            CreateLabel(settingsPanel.transform, "SFX Volume Label", "SFX VOLUME", 18, new Color(0.67f, 0.79f, 0.96f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.49f), new Vector2(0.5f, 0.49f), Vector2.zero, new Vector2(500f, 36f));
            sfxVolumeSlider = CreateSlider(settingsPanel.transform, new Vector2(0f, -45f), new Vector2(420f, 30f));
            sfxVolumeSlider.value = gameSettings != null ? gameSettings.SfxVolume : 0.85f;
            sfxVolumeSlider.onValueChanged.AddListener(value => gameSettings?.SetSfxVolume(value));
            Button muteButton = CreateButton(settingsPanel.transform, "MUTE", new Vector2(-145f, -120f), new Vector2(230f, 54f), new Color(0.25f, 0.43f, 0.63f),
                () => gameSettings?.ToggleMuted());
            settingsMuteText = muteButton.GetComponentInChildren<Text>();
            Button hintButton = CreateButton(settingsPanel.transform, "KEY HINTS", new Vector2(145f, -120f), new Vector2(230f, 54f), new Color(0.25f, 0.43f, 0.63f),
                () => gameSettings?.SetShowHints(!(gameSettings != null && gameSettings.ShowHints)));
            settingsHintsText = hintButton.GetComponentInChildren<Text>();
            CreateButton(settingsPanel.transform, "BACK TO MENU", new Vector2(0f, -205f), new Vector2(270f, 58f), Cyan,
                CloseSettings);
            settingsPanel.GetComponent<Image>().raycastTarget = true;
            settingsPanel.SetActive(false);
            RefreshSettingsUI();
            RefreshCarSelection();

            gameManager.OnStateChanged += SetPanels;
            gameManager.OnScoreChanged += RefreshStats;
            gameManager.OnLivesChanged += RefreshStats;
            typingController.OnProgressChanged += RefreshWord;
            typingController.OnTypo += ShowTypo;
            hudPanel.SetActive(false);
        }

        GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = CreateImage(parent, name, color,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            CanvasGroup group = panel.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            panel.AddComponent<UIFadeTransition>();
            panel.SetActive(false);
            return panel;
        }

        GameObject CreateImage(Transform parent, string name, Color color,
                               Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return obj;
        }

        Text CreateLabel(Transform parent, string name, string value, int size, Color color,
                         TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax,
                         Vector2 position, Vector2 dimensions)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            Text text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        Button CreateButton(Transform parent, string title, Vector2 position, Vector2 size,
                          Color color, Action action)
        {
            GameObject buttonObject = new GameObject(title + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
            colors.pressedColor = new Color(0.72f, 0.82f, 0.95f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                if (audioManager != null) audioManager.PlayUIClick();
                if (action != null) action.Invoke();
            });

            Text label = CreateLabel(buttonObject.transform, "Label", title, 19, Midnight,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.fontStyle = FontStyle.Bold;
            return button;
        }

        Slider CreateSlider(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject root = new GameObject("Volume Slider", typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = size;

            GameObject background = CreateImage(root.transform, "Track", new Color(0.12f, 0.19f, 0.31f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.offsetMin = new Vector2(0f, 7f);
            bgRect.offsetMax = new Vector2(0f, -7f);
            background.GetComponent<Image>().raycastTarget = true;

            GameObject fill = CreateImage(root.transform, "Fill", Cyan,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handle = CreateImage(root.transform, "Handle", Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(26f, 34f));
            Image handleImage = handle.GetComponent<Image>();
            handleImage.raycastTarget = true;
            Slider slider = root.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRect;
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.transition = Selectable.Transition.ColorTint;
            return slider;
        }

        void CreateTouchButton(Transform parent, string title, Vector2 position, Action action, bool right = false)
        {
            GameObject obj = new GameObject("Touch " + title, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(right ? 1f : 0f, 0f);
            rect.pivot = new Vector2(right ? 1f : 0f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(88f, 70f);
            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.12f, 0.91f, 1f, 0.68f);
            Button button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action?.Invoke());
            Text label = CreateLabel(obj.transform, "Label", title, 26, Midnight,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        void ChangeCar(int direction)
        {
            CarController.SelectPreset(CarController.SelectedPresetIndex + direction);
            if (carController != null) carController.ApplyPreset();
            RefreshCarSelection();
        }

        void RefreshCarSelection()
        {
            if (carSelectionText == null) return;
            CarController.CarPreset preset = CarController.SelectedPreset;
            carSelectionText.text = $"{preset.displayName}\nHND {preset.handling:0.00}  SPD +{preset.speedBonus:0.0}";
            carSelectionText.fontSize = 14;
            carSelectionText.color = preset.bodyColor;
        }

        void OpenSettings()
        {
            if (settingsPanel == null) return;
            settingsPanel.GetComponent<UIFadeTransition>()?.SetVisible(true);
            RefreshSettingsUI();
        }

        void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.GetComponent<UIFadeTransition>()?.SetVisible(false);
        }

        void RefreshSettingsUI()
        {
            if (gameSettings != null)
            {
                if (settingsMuteText != null) settingsMuteText.text = gameSettings.IsMuted ? "UNMUTE" : "MUTE";
                if (settingsHintsText != null) settingsHintsText.text = gameSettings.ShowHints ? "HINTS: ON" : "HINTS: OFF";
                if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(gameSettings.MasterVolume);
                if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(gameSettings.SfxVolume);
                if (audioManager != null) audioManager.SetEffectsVolume(gameSettings.SfxVolume);
                if (controlsHintText != null) controlsHintText.gameObject.SetActive(gameSettings.ShowHints);
                if (menuInstructionsText != null) menuInstructionsText.gameObject.SetActive(gameSettings.ShowHints);
            }
        }

        void HandlePowerUpCollected(PowerUpPickup.Type type)
        {
            if (audioManager != null) audioManager.PlayPowerUp();
        }

        void SetPanels(GameManager.State state)
        {
            if (menuPanel == null) return;
            ShowPanel(menuPanel, state == GameManager.State.MainMenu);
            ShowPanel(hudPanel, state == GameManager.State.Playing);
            ShowPanel(pausePanel, state == GameManager.State.Paused);
            ShowPanel(gameOverPanel, state == GameManager.State.GameOver);
            ShowPanel(levelCompletePanel, state == GameManager.State.LevelComplete);
            if (state != GameManager.State.MainMenu) ShowPanel(settingsPanel, false);

            if (state == GameManager.State.Paused && pausedStatsText != null)
                pausedStatsText.text = string.Format("{0}  •  SCORE {1:N0}  •  DISTANCE {2:0} m",
                    gameManager.IsEndless ? "ENDLESS DRIVE" : gameManager.SelectedLevel.DisplayName,
                    gameManager.score, gameManager.distanceTravelled);
            if (state == GameManager.State.GameOver)
            {
                if (finalScoreText != null) finalScoreText.text = string.Format("SCORE  {0:N0}", gameManager.score);
                if (finalDistanceText != null) finalDistanceText.text = string.Format("DISTANCE  {0:0} m", gameManager.distanceTravelled);
                if (newRecordText != null) newRecordText.gameObject.SetActive(highScoreManager != null && highScoreManager.LastRunWasNewBest);
                AnimatedRunStats statView = gameOverPanel.GetComponent<AnimatedRunStats>();
                if (statView != null && typingStats != null)
                    statView.Show(typingStats.WordsPerMinute, typingStats.AccuracyPercent, typingStats.BestCombo);
            }
            if (state == GameManager.State.LevelComplete)
            {
                bool finalLevel = gameManager.SelectedLevelIndex >= GameLevelCatalog.Count - 1;
                completionTitleText.text = finalLevel ? "CAMPAIGN COMPLETE" : "LEVEL COMPLETE";
                completionScoreText.text = string.Format("SCORE  {0:N0}", gameManager.score);
                completionDetailsText.text = string.Format("{0}  •  {1}/{2} GATES  •  {3:0} m",
                    gameManager.SelectedLevel.DisplayName, gameManager.WordsCompleted,
                    gameManager.WordGoal, gameManager.distanceTravelled);
                if (completionRecordText != null) completionRecordText.gameObject.SetActive(highScoreManager != null && highScoreManager.LastRunWasNewBest);
                Text nextLabel = nextLevelButton.GetComponentInChildren<Text>();
                if (nextLabel != null) nextLabel.text = finalLevel ? "REPLAY CAMPAIGN" : "NEXT LEVEL";
                if (audioManager != null) audioManager.PlayLevelComplete();
            }
            if (state == GameManager.State.MainMenu)
            {
                if (bestScoreText != null && highScoreManager != null)
                    bestScoreText.text = string.Format("BEST SCORE  {0:N0}", highScoreManager.BestScore);
                RefreshCarSelection();
            }
        }

        void ShowPanel(GameObject panel, bool visible)
        {
            if (panel == null) return;
            UIFadeTransition fade = panel.GetComponent<UIFadeTransition>();
            if (fade != null) fade.SetVisible(visible);
            else panel.SetActive(visible);
        }
        void RefreshStats()
        {
            if (gameManager == null || scoreText == null) return;
            scoreText.text = $"SCORE  {gameManager.score:N0}";
            speedText.text = $"{gameManager.currentSpeed:0} m/s";
            comboText.text = gameManager.combo >= 2 ? $"STREAK  x{gameManager.combo}" : "STREAK  x0";
            livesText.text = gameManager.IsEndless
                ? "LIVES  ∞"
                : string.Format("LIVES  {0}", string.Join("  ", MakeHearts(gameManager.lives)));
            RefreshRunObjective();
            RefreshPowerUpReadout();
        }

        string[] MakeHearts(int count)
        {
            string[] hearts = new string[Mathf.Max(0, count)];
            for (int i = 0; i < hearts.Length; i++) hearts[i] = "♥";
            return hearts;
        }

        void RefreshLiveReadouts()
        {
            if (speedText != null) speedText.text = $"{gameManager.currentSpeed:0} m/s";
            if (warningText != null)
                warningText.text = hazardSpawner != null ? hazardSpawner.WarningMessage : "";
            RefreshRunObjective();
            RefreshPowerUpReadout();
        }

        void RefreshRunObjective()
        {
            if (wordCaption == null || gameManager == null) return;
            if (gameManager.IsEndless)
                wordCaption.text = string.Format("ENDLESS DRIVE  •  {0} GATES  •  {1:0.0} KM",
                    gameManager.WordsCompleted, gameManager.distanceTravelled / 1000f);
            else if (gateSpawner != null && gateSpawner.CurrentGate != null && gateSpawner.CurrentGate.IsBoss)
            {
                wordCaption.text = $"{gateSpawner.GetBossTimerText()}  •  TYPE THE FULL SENTENCE";
            }
            else
                wordCaption.text = string.Format("{0}  •  GATES {1}/{2}",
                    gameManager.SelectedLevel.DisplayName, gameManager.WordsCompleted, gameManager.WordGoal);
        }
        void RefreshWord(TypingController controller)
        {
            if (wordText == null || controller == null) return;
            wordText.fontSize = 58;
            if (string.IsNullOrEmpty(controller.FullWord))
            {
                wordText.text = "READY";
                return;
            }

            int split = Mathf.Clamp(controller.TypedCount, 0, controller.FullWord.Length);
            string done = controller.FullWord.Substring(0, split).ToUpperInvariant();
            string left = controller.FullWord.Substring(split).ToUpperInvariant();
            wordText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(Lime)}>{done}</color>" +
                            $"<color=#{ColorUtility.ToHtmlStringRGB(Color.white)}>{left}</color>";
            wordText.fontSize = controller.FullWord.Length > 32 ? 25 : controller.FullWord.Length > 22 ? 32 : 58;
        }

        void RefreshPowerUpReadout()
        {
            if (powerUpManager == null) return;
            if (shieldStatusText != null) shieldStatusText.gameObject.SetActive(powerUpManager.HasShield);
            if (slowStatusText != null)
            {
                bool active = powerUpManager.SlowMotionRemaining > 0f;
                slowStatusText.gameObject.SetActive(active);
                if (active) slowStatusText.text = $"[SLOW] {powerUpManager.SlowMotionRemaining:0.0}s";
            }
            if (scoreBoostStatusText != null)
            {
                bool active = powerUpManager.DoubleScoreRemaining > 0f;
                scoreBoostStatusText.gameObject.SetActive(active);
                if (active) scoreBoostStatusText.text = $"[x2] {powerUpManager.DoubleScoreRemaining:0.0}s";
            }
        }

        void ShowTypo()
        {
            if (typoText == null) return;
            typoText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideTypo));
            Invoke(nameof(HideTypo), 0.42f);
        }

        void HideTypo()
        {
            if (typoText != null) typoText.gameObject.SetActive(false);
        }

        static Material MakeMaterial(Color color, float metallic, float smoothness, bool emission = false)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.35f);
            }
            return material;
        }

        static GameObject MakePrimitive(PrimitiveType type, string name, Transform parent,
                                        Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = localScale;

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            return obj;
        }
    }
}



