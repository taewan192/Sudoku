using System;
using GoogleMobileAds.Api;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.UI;
#endif

public sealed class AdMobBannerController : MonoBehaviour
{
    private const string TestAndroidBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
    private const float BannerRetryDelaySeconds = 8f;

    [SerializeField] private bool useTestAds = true;
    [SerializeField] private string androidBannerAdUnitId = TestAndroidBannerAdUnitId;

    private RectTransform slotRect;
    private BannerView bannerView;
    private volatile bool initializationComplete;
    private bool initializationStarted;
    private bool initializationFailed;
    private bool initialized;
    private bool shouldShow;
    private bool bannerLoaded;
    private bool bannerLoadRequested;
    private float nextBannerLoadTime;
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
            if (bannerLoaded)
            {
                bannerView?.Show();
            }
            return;
        }

        bannerView?.Hide();
#endif
    }

    private void Awake()
    {
#if UNITY_EDITOR
        initializationComplete = true;
#else
        StartInitialization();
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
                if (bannerLoaded)
                {
                    bannerView?.Show();
                }
            }
        }

        if (shouldShow && initialized && bannerView != null)
        {
            if (!bannerLoaded && !bannerLoadRequested && Time.unscaledTime >= nextBannerLoadTime)
            {
                RequestBannerLoad();
            }
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
            bannerView = new BannerView(GetBannerAdUnitId(), AdSize.Banner, AdPosition.Bottom);
            bannerView.OnBannerAdLoaded += HandleBannerLoaded;
            bannerView.OnBannerAdLoadFailed += HandleBannerLoadFailed;
            RequestBannerLoad();
        }
        catch (Exception exception)
        {
            initializationFailed = true;
            bannerView = null;
            Debug.LogWarning("AdMob banner failed. The game will continue without ads. " + exception.Message);
        }
    }

    private void RequestBannerLoad()
    {
        if (bannerView == null || bannerLoadRequested)
        {
            return;
        }

        bannerLoadRequested = true;
        try
        {
            bannerView.LoadAd(new AdRequest());
        }
        catch (Exception exception)
        {
            bannerLoadRequested = false;
            nextBannerLoadTime = Time.unscaledTime + BannerRetryDelaySeconds;
            Debug.LogWarning("AdMob banner load request failed. Retrying soon. " + exception.Message);
        }
    }

    private void HandleBannerLoaded()
    {
        bannerLoaded = true;
        bannerLoadRequested = false;
        nextBannerLoadTime = 0f;
        bannerView?.SetPosition(AdPosition.Bottom);

        if (shouldShow)
        {
            bannerView?.Show();
        }

        Debug.Log("AdMob banner loaded.");
    }

    private void HandleBannerLoadFailed(LoadAdError error)
    {
        bannerLoaded = false;
        bannerLoadRequested = false;
        nextBannerLoadTime = Time.unscaledTime + BannerRetryDelaySeconds;
        Debug.LogWarning("AdMob banner load failed. Retrying soon. " + error);
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
        float width = Mathf.Min(Mathf.Max(0f, slotRect.rect.width - 28f), 320f / scaleFactor);
        float height = Mathf.Min(Mathf.Max(0f, slotRect.rect.height - 16f), 50f / scaleFactor);

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
