using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 배치모드(헤드리스) 안드로이드 빌드 진입점.
/// Unity -batchmode -quit -executeMethod AndroidBuilder.BuildAndroid 로 호출.
/// 브리지/에디터 GUI 없이 APK를 만들기 위해 툴 경로·플레이어 설정을 전부 명시적으로 적용한다.
/// </summary>
public static class AndroidBuilder
{
    // 호출 시 인자로 백엔드/아키텍처를 바꿀 수 있게 환경변수도 지원(없으면 IL2CPP/ARM64 기본).
    public static void BuildAndroid()
    {
        // 1) Android 툴 경로 (이 머신의 검증된 위치)
        AndroidExternalToolsSettings.sdkRootPath = "/Users/choimarc/Library/Android/sdk";
        AndroidExternalToolsSettings.ndkRootPath = "/Users/choimarc/Library/Android/sdk/ndk/27.2.12479018";
        AndroidExternalToolsSettings.jdkRootPath = "/Applications/Unity/Hub/Editor/6000.5.1f1/PlaybackEngines/AndroidPlayer/OpenJDK";

        // 2) 플레이어 설정 (출시용 64비트)
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.gghf.paperheroes");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        // Play 업로드를 위한 최소 SDK는 Unity 기본값 사용. 디버그 키로 자동 서명(사이드로드 가능).

        Debug.Log("[BATCHBUILD] config: backend=" + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)
            + " arch=" + PlayerSettings.Android.targetArchitectures
            + " pkg=" + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)
            + " sdk=" + AndroidExternalToolsSettings.sdkRootPath
            + " ndk=" + AndroidExternalToolsSettings.ndkRootPath
            + " jdk=" + AndroidExternalToolsSettings.jdkRootPath);

        // 3) 빌드
        var opts = new BuildPlayerOptions();
        opts.scenes = new string[] { "Assets/Scenes/PaperHeroes_Game.unity" };
        opts.locationPathName = "/Users/choimarc/gghf2/Builds/PaperHeroes.apk";
        opts.target = BuildTarget.Android;
        opts.targetGroup = BuildTargetGroup.Android;
        opts.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary s = report.summary;
        Debug.Log("[BATCHBUILD] result=" + s.result + " errors=" + s.totalErrors + " warnings=" + s.totalWarnings
            + " sizeBytes=" + s.totalSize + " out=" + s.outputPath + " time=" + s.totalTime);

        if (s.result == BuildResult.Succeeded)
            EditorApplication.Exit(0);
        else
            EditorApplication.Exit(1);
    }
}
