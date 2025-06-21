/****************************************************
 * UIBindRewardAd.cs   （完整覆盖）
 ****************************************************/
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class UIBindRewardAd : MonoBehaviour
{
    [Header("可选：后台有多个 Placement 时改名")]
    [SerializeField] private string placementName = "DefaultRewardedVideo";

    [SerializeField] private UIDocument uiDocument;      // 可留空
    private Button btnReward;

    /*───────────────────────────── 生命周期 ─────────────────────────────*/
    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        // ★这里改到 OnEnable 后，rootVisualElement 已经准备好了
        btnReward = uiDocument.rootVisualElement.Q<Button>("AdsRewardBtn");
        if (btnReward == null)
        {
            Debug.LogError("[UIBindRewardAd] UXML 里找不到 <Button name=\"AdsRewardBtn\">");
            return;
        }

        btnReward.clicked += OnBtnClicked;

        IronSourceRewardedVideoEvents.onAdAvailableEvent   += OnAdAvailable;
        IronSourceRewardedVideoEvents.onAdUnavailableEvent += OnAdUnavailable;

        // 同步按钮可用状态
        btnReward.SetEnabled(IronSource.Agent.isRewardedVideoAvailable());

        Debug.Log("[UIBindRewardAd] Reward 按钮绑定完成");
    }

    private void OnDisable()
    {
        if (btnReward != null)
            btnReward.clicked -= OnBtnClicked;

        IronSourceRewardedVideoEvents.onAdAvailableEvent   -= OnAdAvailable;
        IronSourceRewardedVideoEvents.onAdUnavailableEvent -= OnAdUnavailable;
    }

    /*───────────────────────────── UI 回调 ─────────────────────────────*/
    private void OnBtnClicked()
    {
        Debug.Log("<color=yellow>[RewardAd] 点击按钮 — OnBtnClicked()</color>");

        if (IronSource.Agent.isRewardedVideoAvailable())
        {
            IronSource.Agent.showRewardedVideo(placementName);
            Debug.Log("<color=cyan>[RewardAd] 已调用 showRewardedVideo()</color>");
        }
        else
        {
            Debug.LogWarning("[RewardAd] 广告还没准备好");
        }
    }

    /*────────────────────────── IronSource 回调 ─────────────────────────*/
    private void OnAdAvailable(IronSourceAdInfo _)
        => btnReward?.schedule.Execute(() => btnReward.SetEnabled(true));

    private void OnAdUnavailable()
        => btnReward?.schedule.Execute(() => btnReward.SetEnabled(false));
}
