/****************************************************
 * RewardAds.cs  （完整覆盖原文件）
 ****************************************************/
using UnityEngine;
using Unity.Services.LevelPlay;

/// <summary>
/// 单例式封装 Unity LevelPlay 奖励视频广告逻辑。<br/>
/// 挂在任意场景中的一个 GameObject 即可。
/// </summary>
public class RewardAds : MonoBehaviour
{
    private string appKey = "2281c929d";    // Android

    /// <summary>本地缓存里是否已有可播放的广告（供 UI 查询）。</summary>
    public bool AdReady { get; private set; }

    /*---------- 运行期静态标记：全局只初始化 / 订阅一次 ----------*/
    private static bool _sdkInitialized;
    private static bool _eventsSubscribed;

    /*------------------------------------------------------------*/
    private void Awake()
    {
        if (!_sdkInitialized)
        {
            IronSource.Agent.init(appKey, IronSourceAdUnits.REWARDED_VIDEO);
            IronSource.Agent.validateIntegration();
            _sdkInitialized = true;
        }

        if (!_eventsSubscribed)
        {
            SubscribeEvents();
            _eventsSubscribed = true;
        }
    }


    /* 事件集中注册 —— 用当前实例的 AdReady 字段记录状态 */
    private void SubscribeEvents()
    {
        var ads = this; // 捕获当前实例，供匿名函数访问

        /* SDK 初始化完成 */
        LevelPlay.OnInitSuccess += _ =>
            Debug.Log("<color=lime>[Ads] SDK Init ✅</color>");

        /* ---------- 奖励视频事件 ---------- */
        IronSourceRewardedVideoEvents.onAdReadyEvent += _ =>
        {
            ads.AdReady = true;
            Debug.Log("<color=yellow>[Ads] onAdReady</color>");
        };

        IronSourceRewardedVideoEvents.onAdUnavailableEvent += () =>
        {
            ads.AdReady = false;
            Debug.Log("<color=grey>[Ads] onAdUnavailable</color>");
        };

        IronSourceRewardedVideoEvents.onAdOpenedEvent  += _ =>
            Debug.Log("<color=lime>[Ads] onAdOpened</color>");

        IronSourceRewardedVideoEvents.onAdClosedEvent  += _ =>
            Debug.Log("<color=lime>[Ads] onAdClosed</color>");

        IronSourceRewardedVideoEvents.onAdShowFailedEvent += (err, _) =>
            Debug.LogError($"[Ads] onAdShowFailed: {err.getDescription()}");

        IronSourceRewardedVideoEvents.onAdRewardedEvent += (placement, _) =>
            Debug.Log($"<color=cyan>[Ads] Reward: {placement.getRewardAmount()} {placement.getRewardName()}</color>");
    }

    private void OnApplicationPause(bool pause) =>
        IronSource.Agent.onApplicationPause(pause);

    /*------------------------------------------------------------
     * 供 UI 按钮调用
     *-----------------------------------------------------------*/
    public void ShowRewardedAd()
    {
        Debug.Log(
            $"[Ads] BtnClick  AdReady={AdReady}  SDK_isReady={IronSource.Agent.isRewardedVideoAvailable()}");

        if (!AdReady)
        {
            Debug.LogWarning("[Ads] 点击过早，广告未缓存完成");
            return;
        }

        IronSource.Agent.showRewardedVideo();
        AdReady = false;                 // 播放完等下一次 onAdReady
    }
}
