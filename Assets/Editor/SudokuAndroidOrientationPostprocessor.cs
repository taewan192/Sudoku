#if UNITY_EDITOR
using System.IO;
using UnityEditor.Android;
using UnityEngine;

public sealed class SudokuAndroidOrientationPostprocessor : IPostGenerateGradleAndroidProject
{
    private const string AdMobApplicationId = "ca-app-pub-3940256099942544~3347511713";
    private const string AdMobApplicationIdMetadataName = "com.google.android.gms.ads.APPLICATION_ID";

    public int callbackOrder => 100;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        PatchUnityLibraryProject(path);
    }

    private static void PatchUnityLibraryProject(string path)
    {
        if (PatchGradleProject(path))
        {
            return;
        }

        string nestedUnityLibrary = Path.Combine(path, "unityLibrary");
        if (PatchGradleProject(nestedUnityLibrary))
        {
            return;
        }

        DirectoryInfo parent = Directory.GetParent(path);
        if (parent != null)
        {
            PatchGradleProject(Path.Combine(parent.FullName, "unityLibrary"));
        }
    }

    private static bool PatchGradleProject(string path)
    {
        string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");

        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning("Sudoku Android manifest was not found for orientation patch: " + manifestPath);
            return false;
        }

        PatchLaunchStyles(path);

        string manifest = File.ReadAllText(manifestPath);
        string patched = manifest
            .Replace("android:screenOrientation=\"reversePortrait\"", "android:screenOrientation=\"portrait\"")
            .Replace("android:resizeableActivity=\"true\"", "android:resizeableActivity=\"false\"")
            .Replace("android:theme=\"@style/BaseUnityGameActivityTheme\"", "android:theme=\"@style/SudokuLaunchTheme\"");

        patched = EnsureAdMobApplicationId(patched);

        if (patched != manifest)
        {
            File.WriteAllText(manifestPath, patched);
            Debug.Log("Sudoku Android manifest orientation patched to portrait.");
        }

        return true;
    }

    private static string EnsureAdMobApplicationId(string manifest)
    {
        if (manifest.Contains(AdMobApplicationIdMetadataName))
        {
            return manifest;
        }

        int applicationTagStart = manifest.IndexOf("<application", System.StringComparison.Ordinal);
        if (applicationTagStart < 0)
        {
            Debug.LogWarning("Sudoku Android manifest has no application tag for AdMob App ID patch.");
            return manifest;
        }

        int applicationTagEnd = manifest.IndexOf(">", applicationTagStart, System.StringComparison.Ordinal);
        if (applicationTagEnd < 0)
        {
            Debug.LogWarning("Sudoku Android manifest application tag was not closed for AdMob App ID patch.");
            return manifest;
        }

        string metadata =
            "\n    <meta-data android:name=\"" + AdMobApplicationIdMetadataName + "\" android:value=\"" + AdMobApplicationId + "\" />";
        return manifest.Insert(applicationTagEnd + 1, metadata);
    }

    private static void PatchLaunchStyles(string projectPath)
    {
        PatchStyleFile(Path.Combine(projectPath, "src/main/res/values/styles.xml"));
        PatchStyleFile(Path.Combine(projectPath, "src/main/res/values-v31/styles.xml"));
    }

    private static void PatchStyleFile(string stylePath)
    {
        if (!File.Exists(stylePath))
        {
            return;
        }

        string styles = File.ReadAllText(stylePath);
        if (styles.Contains("name=\"SudokuLaunchTheme\""))
        {
            return;
        }

        string launchStyle =
@"  <style name=""SudokuLaunchTheme"" parent=""@style/BaseUnityGameActivityTheme"">
    <item name=""android:windowDisablePreview"">true</item>
    <item name=""android:windowNoTitle"">true</item>
    <item name=""android:windowFullscreen"">true</item>
    <item name=""android:windowActionBar"">false</item>
    <item name=""android:windowBackground"">@android:color/black</item>
  </style>
";

        styles = styles.Replace("</resources>", launchStyle + "</resources>");
        File.WriteAllText(stylePath, styles);
    }
}
#endif
