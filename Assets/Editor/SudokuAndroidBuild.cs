#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class SudokuAndroidBuild
{
    private const string IconAssetPath = "Assets/Icons/sudoku_icon_512.png";
    private const string ScenePath = "Assets/Scenes/SudokuDemo.unity";
    private const string DefaultBundleId = "com.arcana.sudoku";
    private const string DefaultProductName = "Sudoku";
    private const string DefaultCompanyName = "fhgkf";

    public static void BuildAab()
    {
        string outputPath = GetArgument("-outputPath");
        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = Path.GetFullPath("../Sudoku.aab");
        }

        ConfigureProject();
        ConfigureAndroidSigning();

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception("Android App Bundle build failed: " + report.summary.result);
        }

        Debug.Log("Sudoku Android App Bundle built: " + outputPath);
    }

    private static void ConfigureProject()
    {
        Directory.CreateDirectory("Assets/Icons");

        TextureImporter importer = AssetImporter.GetAtPath(IconAssetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAssetPath);
        if (icon == null)
        {
            throw new FileNotFoundException("Sudoku icon was not found.", IconAssetPath);
        }

        PlayerSettings.companyName = DefaultCompanyName;
        PlayerSettings.productName = DefaultProductName;
        PlayerSettings.bundleVersion = "1.0.10";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, DefaultBundleId);
        PlayerSettings.Android.bundleVersionCode = 12;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.stripEngineCode = false;
        PlayerSettings.SplashScreen.show = false;

#pragma warning disable 618
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { icon });
#pragma warning restore 618

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.SaveAssets();
    }

    private static void ConfigureAndroidSigning()
    {
        string keystorePath = Environment.GetEnvironmentVariable("SUDOKU_KEYSTORE_PATH");
        string keystorePass = Environment.GetEnvironmentVariable("SUDOKU_KEYSTORE_PASS");
        string keyAlias = Environment.GetEnvironmentVariable("SUDOKU_KEY_ALIAS");
        string keyPass = Environment.GetEnvironmentVariable("SUDOKU_KEY_PASS");

        if (string.IsNullOrEmpty(keystorePath) ||
            string.IsNullOrEmpty(keystorePass) ||
            string.IsNullOrEmpty(keyAlias) ||
            string.IsNullOrEmpty(keyPass))
        {
            PlayerSettings.Android.useCustomKeystore = false;
            Debug.LogWarning("No custom Android keystore environment variables were provided. Unity will use its default signing behavior.");
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasName = keyAlias;
        PlayerSettings.Android.keyaliasPass = keyPass;
    }

    private static string GetArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
#endif
