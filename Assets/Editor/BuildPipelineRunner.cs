using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace XiaosuPet.Editor
{
    public static class BuildPipelineRunner
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        public static void BuildWindows()
        {
            ProjectBootstrap.Run();

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputDir = Path.Combine(projectRoot, "Build", "Windows");
            Directory.CreateDirectory(outputDir);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(outputDir, "xiaosu-pet.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log(string.Format(
                "[Xiaosu] Build result={0} size={1} errors={2} warnings={3} output={4}",
                summary.result, summary.totalSize, summary.totalErrors, summary.totalWarnings,
                summary.outputPath));

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[Xiaosu] Build failed. See the log above.");
                EditorApplication.Exit(1);
            }
        }
    }
}
