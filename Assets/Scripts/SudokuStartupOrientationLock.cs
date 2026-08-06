using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-32000)]
public sealed class SudokuStartupOrientationLock : MonoBehaviour
{
    private static bool hostCreated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void BeforeSplashScreen()
    {
        LockPortraitNow();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeforeSceneLoad()
    {
        LockPortraitNow();
        EnsureHost();
    }

    public static void LockPortraitNow()
    {
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.Portrait;
    }

    private static void EnsureHost()
    {
        if (hostCreated)
        {
            return;
        }

        hostCreated = true;
        GameObject host = new GameObject("SudokuStartupOrientationLock");
        DontDestroyOnLoad(host);
        host.AddComponent<SudokuStartupOrientationLock>();
    }

    private void Awake()
    {
        LockPortraitNow();
    }

    private void OnEnable()
    {
        StartCoroutine(LockDuringStartup());
    }

    private IEnumerator LockDuringStartup()
    {
        for (int i = 0; i < 90; i++)
        {
            LockPortraitNow();
            yield return null;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            LockPortraitNow();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            LockPortraitNow();
        }
    }
}
