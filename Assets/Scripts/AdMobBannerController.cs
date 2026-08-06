using System;
using GoogleMobileAds.Api;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.UI;
#endif

public sealed class AdMobBannerController : MonoBehaviour
{
    private const string TestAndroidBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
    private const int StandardBannerWidth = 320;
    private const int StandardBannerHeight = 50;

    [SerializeField] private bool useTestAds = true;
    [SerializeField] private string androidBannerAdUnitId = TestAndroidBannerAdUnitId;

    private RectTransform slotRect;
    private BannerView bannerView;
    private volatile bool initializationComplete;
    private bool initializationStarted;
    private bool initializationFailed;
    private bool initialized;
    private bool shouldShow;
    private Vector2Int lastPosition = new Vector2Int(int.MinValue, int.MinValue);
    private Vector2Int lastScreenSize = new Vector2Int(int.MinValue, int.MinValue);
#if UNITY_EDITOR
    private RectTransform editorPreviewRect;
#endif

    public void Configure(RectTransform adSlot)
    {
        slotRect = adSlot;
#if UNITY_EDITOR
        EnsureEditorPreview();
        SyncEditorPreview();
#else
        SyncBannerPosition();
#endif
    }

    public void SetVisible(bool visible)
    {
        shouldShow = visible;

#if UNITY_EDITOR
        EnsureEditorPreview();
        if (editorPreviewRect != null)
        {
            editorPreviewRect.gameObject.SetActive(visible);
        }
        return;
#else
        if (shouldShow)
        {
            StartInitialization();
        }

        if (!initialized)
        {
            return;
        }

        if (shouldShow)
        {
            EnsureBanner();
            bannerView?.Show();
            SyncBannerPosition();
            return;
        }

        bannerView?.Hide();
#endif
    }

    private void Awake()
    {
#if UNITY_EDITOR
        initializationComplete = true;
#endif
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR
        if (slotRect != null)
        {
            EnsureEditorPreview();
            SyncEditorPreview();
        }
#else
        if (shouldShow)
        {
            StartInitialization();
        }

        if (!initialized && initializationComplete)
        {
            initialized = true;
            if (shouldShow)
            {
                EnsureBanner();
                bannerView?.Show();
            }
        }

        if (shouldShow && initialized && bannerView != null)
        {
            SyncBannerPosition();
        }
#endif
    }

    private void OnDestroy()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

#if UNITY_EDITOR
        if (editorPreviewRect != null)
        {
            Destroy(editorPreviewRect.gameObject);
            editorPreviewRect = null;
        }
#endif
    }

#if !UNITY_EDITOR
    private void StartInitialization()
    {
        if (initializationStarted || initializationFailed)
        {
            return;
        }

        initializationStarted = true;
        try
        {
            MobileAds.Initialize(delegate
            {
                initializationComplete = true;
            });
        }
        catch (Exception exception)
        {
            initializationFailed = true;
            Debug.LogWarning("AdMob initialization failed. The game will continue without ads. " + exception.Message);
        }
    }

    private void EnsureBanner()
    {
        if (bannerView != null || initializationFailed)
        {
            return;
        }

        try
        {
            Vector2Int position = CalculateBannerPosition();
            bannerView = new BannerView(GetBannerAdUnitId(), AdSize.Banner, position.x, position.y);
            bannerView.LoadAd(new AdRequest());
            lastPosition = position;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
        catch (Exception exception)
        {
            initializationFailed = true;
            bannerView = null;
            Debug.LogWarning("AdMob banner failed. The game will continue without ads. " + exception.Message);
        }
    }
#endif

#if !UNITY_EDITOR
    private void SyncBannerPosition()
    {
        if (bannerView == null || slotRect == null)
        {
            return;
        }

        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        Vector2Int position = CalculateBannerPosition();

        if (position == lastPosition && screenSize == lastScreenSize)
        {
            return;
        }

        bannerView.SetPosition(position.x, position.y);
        lastPosition = position;
        lastScreenSize = screenSize;
    }
#endif

#if !UNITY_EDITOR
    private Vector2Int CalculateBannerPosition()
    {
        if (slotRect == null)
        {
            return new Vector2Int(Mathf.Max(0, (Screen.width - StandardBannerWidth) / 2), 0);
        }

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);
        Canvas canvas = slotRect.GetComponentInParent<Canvas>();
        Camera canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[i]);
            minX = Mathf.Min(minX, screenPoint.x);
            maxX = Mathf.Max(maxX, screenPoint.x);
            minY = Mathf.Min(minY, screenPoint.y);
            maxY = Mathf.Max(maxY, screenPoint.y);
        }

        float slotWidth = Mathf.Max(0f, maxX - minX);
        float slotHeight = Mathf.Max(0f, maxY - minY);
        int x = Mathf.RoundToInt(minX + Mathf.Max(0f, slotWidth - StandardBannerWidth) * 0.5f);
        int y = Mathf.RoundToInt(Screen.height - maxY + Mathf.Max(0f, slotHeight - StandardBannerHeight) * 0.5f);

        x = Mathf.Clamp(x, 0, Mathf.Max(0, Screen.width - StandardBannerWidth));
        y = Mathf.Clamp(y, 0, Mathf.Max(0, Screen.height - StandardBannerHeight));
        return new Vector2Int(x, y);
    }
#endif

#if UNITY_EDITOR
    private void EnsureEditorPreview()
    {
        if (editorPreviewRect != null || slotRect == null)
        {
            return;
        }

        GameObject preview = new GameObject("AdMob Editor Preview", typeof(RectTransform));
        editorPreviewRect = preview.GetComponent<RectTransform>();
        editorPreviewRect.SetParent(slotRect, false);
        editorPreviewRect.anchorMin = new Vector2(0.5f, 0.5f);
        editorPreviewRect.anchorMax = new Vector2(0.5f, 0.5f);
        editorPreviewRect.pivot = new Vector2(0.5f, 0.5f);

        Image background = preview.AddComponent<Image>();
        background.color = new Color(0.96f, 0.98f, 1f, 1f);
        background.raycastTarget = false;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(editorPreviewRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 4f);
        labelRect.offsetMax = new Vector2(-16f, -4f);

        Text label = labelObject.AddComponent<Text>();
        label.text = "AdMob Test Banner";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.12f, 0.17f, 0.22f, 1f);
        label.fontSize = 18;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 10;
        label.resizeTextMaxSize = 18;
        label.raycastTarget = false;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        preview.SetActive(shouldShow);
    }

    private void SyncEditorPreview()
    {
        if (editorPreviewRect == null || slotRect == null)
        {
            return;
        }

        Canvas canvas = slotRect.GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
        float width = Mathf.Min(Mathf.Max(0f, slotRect.rect.width - 28f), StandardBannerWidth / scaleFactor);
        float height = Mathf.Min(Mathf.Max(0f, slotRect.rect.height - 16f), StandardBannerHeight / scaleFactor);

        editorPreviewRect.anchoredPosition = Vector2.zero;
        editorPreviewRect.sizeDelta = new Vector2(width, height);
    }
#endif

    private string GetBannerAdUnitId()
    {
        if (useTestAds || string.IsNullOrWhiteSpace(androidBannerAdUnitId))
        {
            return TestAndroidBannerAdUnitId;
        }

        return androidBannerAdUnitId;
    }
}
