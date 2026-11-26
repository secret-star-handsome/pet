using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace XiaosuPet.Editor
{
    /// <summary>
    /// One-shot editor bootstrap. Run once in batchmode (and again before every build):
    /// applies PlayerSettings, creates Main.unity with a single PetApp object, and saves.
    /// </summary>
    public static class ProjectBootstrap
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        public static void Run()
        {
            ConfigurePlayer();
            ConfigureQuality();
            ConfigureGraphics();
            CreateScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Xiaosu] Project bootstrap complete.");
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "FUMI";
            PlayerSettings.productName = "晓苏";
            PlayerSettings.bundleVersion = "0.1.0";

            PlayerSettings.defaultScreenWidth = PetConfig.WindowSizeDip;
            PlayerSettings.defaultScreenHeight = PetConfig.WindowSizeDip;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.forceSingleInstance = true;
            PlayerSettings.allowFullscreenSwitch = false;
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(
                BuildTarget.StandaloneWindows64,
                new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.useFlipModelSwapchain = false;

            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Standalone, ManagedStrippingLevel.Low);

            // Personal licenses force the splash; setting false is harmless and helps Pro/Plus.
            PlayerSettings.SplashScreen.show = false;

            // Property exists on some Unity versions only; avoid a hard compile dependency.
            var hdrProperty = typeof(PlayerSettings).GetProperty(
                "useHDRDisplay", BindingFlags.Public | BindingFlags.Static);
            if (hdrProperty != null && hdrProperty.PropertyType == typeof(bool))
            {
                hdrProperty.SetValue(null, false, null);
            }
        }

        private static void ConfigureQuality()
        {
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.vSyncCount = 0;
            }
        }

        private static void ConfigureGraphics()
        {
            // M0 is Built-in Render Pipeline only.
            GraphicsSettings.renderPipelineAsset = null;
        }

        private static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var pet = new GameObject("PetApp");
            pet.AddComponent<PetApp>();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
