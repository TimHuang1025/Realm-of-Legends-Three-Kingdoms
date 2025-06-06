using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

[RequireComponent(typeof(UIDocument))]
public class CardVideoBinder : MonoBehaviour
{
    [SerializeField] string elementName = "CardArt";
    [SerializeField] Vector2Int rtSize  = new(720,1080);

    RenderTexture rt;
    VideoPlayer   vp;
    VisualElement cardVe;
    UIDocument    uiDoc;

    void Awake()
    {
        // 仅创建 RT 和 VideoPlayer
        rt = new RenderTexture(rtSize.x, rtSize.y, 0);
        vp = gameObject.AddComponent<VideoPlayer>();
        vp.targetTexture   = rt;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.isLooping       = true;
        vp.playOnAwake     = false;

        uiDoc = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        // ⚠️ 此时 UXML 已克隆完成，开始找元素并贴纹理
        cardVe = uiDoc.rootVisualElement.Q<VisualElement>(elementName);
        if (cardVe == null)
        {
            Debug.LogError($"[CardVideoBinder] 找不到元素 {elementName}");
            return;
        }

        cardVe.style.backgroundImage =
            new StyleBackground(Background.FromRenderTexture(rt));
        cardVe.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;

        // 订阅事件
        //CardInventoryUI.OnCardSelected += PlayCardVideo;
    }

    void OnDisable()
    {
        //CardInventoryUI.OnCardSelected -= PlayCardVideo;
    }

    /* ----------------- 切换视频 ----------------- */
    void PlayCardVideo(CardInfo card)
    {
        Debug.Log($"播放视频：{card.cardName}");

        if (card.videoClip == null) { vp.Stop(); return; }

        vp.Stop();                       // 保证替换前已停止
        vp.clip = card.videoClip;
        vp.Prepare();                    // 预解码，防黑屏
        vp.prepareCompleted += _ => vp.Play();
    }
}
