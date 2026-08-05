#if UNITY_EDITOR
using System.IO;
using UnityEditor.Android;
using UnityEngine;

public sealed class SudokuAndroidOrientationPostprocessor : IPostGenerateGradleAndroidProject
{
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
        string customActivityName = "com.arcana.sudoku.SudokuPortraitGameActivity";

        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning("Sudoku Android manifest was not found for orientation patch: " + manifestPath);
            return false;
        }

        WritePortraitActivity(path);
        PatchLaunchStyles(path);

        string manifest = File.ReadAllText(manifestPath);
        string patched = manifest
            .Replace("android:name=\"com.unity3d.player.UnityPlayerGameActivity\"", "android:name=\"" + customActivityName + "\"")
            .Replace("android:screenOrientation=\"reversePortrait\"", "android:screenOrientation=\"portrait\"")
            .Replace("android:resizeableActivity=\"true\"", "android:resizeableActivity=\"false\"")
            .Replace("android:theme=\"@style/BaseUnityGameActivityTheme\"", "android:theme=\"@style/SudokuLaunchTheme\"");

        if (patched != manifest)
        {
            File.WriteAllText(manifestPath, patched);
            Debug.Log("Sudoku Android manifest orientation patched to portrait.");
        }

        return true;
    }

    private static void WritePortraitActivity(string projectPath)
    {
        string javaDirectory = Path.Combine(projectPath, "src/main/java/com/arcana/sudoku");
        Directory.CreateDirectory(javaDirectory);

        string activityPath = Path.Combine(javaDirectory, "SudokuPortraitGameActivity.java");
        File.WriteAllText(activityPath,
@"package com.arcana.sudoku;

import android.content.pm.ActivityInfo;
import android.os.Bundle;
import com.unity3d.player.UnityPlayerGameActivity;

public class SudokuPortraitGameActivity extends UnityPlayerGameActivity {
    private void lockPortrait() {
        setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_PORTRAIT);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        lockPortrait();
        super.onCreate(savedInstanceState);
        lockPortrait();
    }

    @Override
    protected void onStart() {
        lockPortrait();
        super.onStart();
        lockPortrait();
    }

    @Override
    protected void onResume() {
        lockPortrait();
        super.onResume();
        lockPortrait();
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        lockPortrait();
        super.onWindowFocusChanged(hasFocus);
        lockPortrait();
    }
}
");
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
