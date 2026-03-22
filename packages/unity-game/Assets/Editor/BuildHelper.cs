// =============================================================================
// BuildHelper.cs
// Batchmode ile Android/iOS build almak icin editor scripti.
// Kullanim: Unity -executeMethod RiceFactory.Editor.BuildHelper.BuildAndroid
// =============================================================================

using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RiceFactory.Editor
{
    public static class BuildHelper
    {
        private static readonly string[] SCENES = new[]
        {
            "Assets/Scenes/Boot.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Game.unity"
        };

        [MenuItem("RiceFactory/Build Android APK")]
        public static void BuildAndroid()
        {
            Debug.Log("[BuildHelper] Android APK build basliyor...");

            // Player Settings
            PlayerSettings.companyName = "RiceFactory";
            PlayerSettings.productName = "riceFactory";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Mono scripting backend (hizli build)
            PlayerSettings.SetScriptingBackend(
                UnityEditor.Build.NamedBuildTarget.Android,
                ScriptingImplementation.Mono2x);

            // Ek Android ayarlari
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android, BuildTarget.Android);

            // Shader derleme sorunlarini onlemek icin
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3, UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);

            string buildPath = System.IO.Path.GetFullPath(
                "../../builds/riceFactory.apk");

            var options = new BuildPlayerOptions
            {
                scenes = SCENES,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildHelper] Android build BASARILI! ({report.summary.totalSize / 1024 / 1024}MB)");
                Debug.Log($"[BuildHelper] APK: {buildPath}");
            }
            else
            {
                Debug.LogError($"[BuildHelper] Android build BASARISIZ: {report.summary.totalErrors} hata");
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error)
                            Debug.LogError($"  {msg.content}");
                    }
                }
            }
        }

        [MenuItem("RiceFactory/Build iOS")]
        public static void BuildiOS()
        {
            Debug.Log("[BuildHelper] iOS build basliyor...");

            PlayerSettings.companyName = "RiceFactory";
            PlayerSettings.productName = "riceFactory";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            string buildPath = System.IO.Path.GetFullPath("../../builds/iOS");

            var options = new BuildPlayerOptions
            {
                scenes = SCENES,
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"[BuildHelper] iOS build BASARILI! Xcode: {buildPath}");
            else
                Debug.LogError($"[BuildHelper] iOS build BASARISIZ: {report.summary.totalErrors} hata");
        }
    }
}
