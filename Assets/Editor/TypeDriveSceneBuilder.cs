using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TypeDrive.Runtime;

namespace TypeDrive.Editor
{
    /// <summary>One-click, built-in-asset scene builder for the complete Type Drive game.</summary>
    public static class TypeDriveSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/TypeDrive.unity";

        [MenuItem("TypeDrive/Build Complete Game Scene")]
        public static void BuildCompleteGameScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string directory = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrap = new GameObject("TypeDrive Startup");
            bootstrap.AddComponent<TypeDriveSceneBootstrap>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            bool alreadyIncluded = false;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == ScenePath)
                {
                    scenes[i] = new EditorBuildSettingsScene(ScenePath, true);
                    alreadyIncluded = true;
                }
            }
            if (!alreadyIncluded)
            {
                var updated = new EditorBuildSettingsScene[scenes.Length + 1];
                updated[0] = new EditorBuildSettingsScene(ScenePath, true);
                for (int i = 0; i < scenes.Length; i++) updated[i + 1] = scenes[i];
                scenes = updated;
            }
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
            Debug.Log("Type Drive scene built and added to Build Settings: " + ScenePath);
        }
    }
}
