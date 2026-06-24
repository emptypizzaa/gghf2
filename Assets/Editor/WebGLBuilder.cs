using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Batchmode WebGL build entry point for itch.io browser uploads.
/// Invoke with:
/// Unity -batchmode -quit -projectPath /Users/choimarc/gghf2 -executeMethod WebGLBuilder.BuildForItch
/// </summary>
public static class WebGLBuilder
{
    public static void BuildForItch()
    {
        string outputPath = GetArgumentValue("-outputPath");
        if (string.IsNullOrEmpty(outputPath))
            outputPath = Environment.GetEnvironmentVariable("WEBGL_BUILD_PATH");
        if (string.IsNullOrEmpty(outputPath))
            outputPath = Path.GetFullPath("build/outputs/webgl/itch");

        outputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(outputPath);

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 600;
        PlayerSettings.runInBackground = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.WebGL.initialMemorySize = 64;
        PlayerSettings.WebGL.maximumMemorySize = 2048;

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            scenes = new[] { "Assets/Scenes/PaperHeroes_Game.unity" };

        Debug.Log("[WEBGLBUILD] output=" + outputPath);
        Debug.Log("[WEBGLBUILD] scenes=" + string.Join(", ", scenes));
        Debug.Log("[WEBGLBUILD] compression=" + PlayerSettings.WebGL.compressionFormat
            + " dataCaching=" + PlayerSettings.WebGL.dataCaching
            + " memory=" + PlayerSettings.WebGL.initialMemorySize + "/" + PlayerSettings.WebGL.maximumMemorySize);

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        });

        BuildSummary summary = report.summary;
        Debug.Log("[WEBGLBUILD] result=" + summary.result
            + " errors=" + summary.totalErrors
            + " warnings=" + summary.totalWarnings
            + " sizeBytes=" + summary.totalSize
            + " out=" + summary.outputPath
            + " time=" + summary.totalTime);

        EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    private static string GetArgumentValue(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }

        return null;
    }
}
