#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SudokuProjectSetup
{
    private const string ScenePath = "Assets/Scenes/SudokuDemo.unity";

    static SudokuProjectSetup()
    {
        EditorApplication.delayCall += EnsureDemoScene;
    }

    private static void EnsureDemoScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");

        Scene scene;
        if (File.Exists(ScenePath))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMainCamera();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        if (Object.FindFirstObjectByType<SudokuUIScreen>() == null)
        {
            GameObject screen = new GameObject("SudokuScreen");
            screen.AddComponent<SudokuUIScreen>();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        if (scene.isDirty)
        {
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }

    private static void CreateMainCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;
        camera.orthographic = true;
        camera.orthographicSize = 5f;

        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }
}
#endif
