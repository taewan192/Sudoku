using GoogleMobileAds.Api;
using UnityEngine;

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
    private bool initialized;
    private bool shouldShow;
    private Vector2Int lastPosition = new Vector2Int(int.MinValue, int.MinValue);
    private Vector2Int lastScreenSize = new Vector2Int(int.MinValue, int.MinValue);

    public void Configure(RectTransform adSlot)
    {
        slotRect = adSlot;
        SyncBannerPosition();
    }

    public void SetVisible(bool visible)
    {
        shouldShow = visible;

        if (!initialized)
        {
            return;
        }

        if (shouldShow)
        {
            EnsureBanner();
            bannerView.Show();
            SyncBannerPosition();
            return;
        }

        bannerView?.Hide();
    }

    private void Awake()
    {
        MobileAds.Initialize(delegate
        {
            initializationComplete = true;
        });
    }

    private void LateUpdate()
    {
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
    }

    private void OnDestroy()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
    }

    private void EnsureBanner()
    {
        if (bannerView != null)
        {
            return;
        }

        Vector2Int position = CalculateBannerPosition();
        bannerView = new BannerView(GetBannerAdUnitId(), AdSize.Banner, position.x, position.y);
        bannerView.LoadAd(new AdRequest());
        lastPosition = position;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

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

    private Vector2Int CalculateBannerPosition()
    {
        if (slotRect == null)
        {
            return new Vector2Int(Mathf.Max(0, (Screen.width - StandardBannerWidth) / 2), 0);
        }

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
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

    private string GetBannerAdUnitId()
    {
        if (useTestAds || string.IsNullOrWhiteSpace(androidBannerAdUnitId))
        {
            return TestAndroidBannerAdUnitId;
        }

        return androidBannerAdUnitId;
    }
}
