// Assets/Scripts/Debug/FpsCounterOnGUI.cs
using UnityEngine;

/// <summary>
/// ① 启动时将游戏锁定横屏 & 关闭 VSync
/// ② 根据设备刷新率自动设定 Application.targetFrameRate（120/90/60/30 档）
/// ③ 在屏幕左上角实时显示 FPS 与单帧耗时
/// </summary>
public sealed class FpsCounterOnGUI : MonoBehaviour
{
    //=========== 配置 ===========//
    [Tooltip("是否在 Awake 阶段设置横屏")]
    public bool lockLandscape = true;

    [Tooltip("是否自动根据刷新率调整 targetFrameRate")]
    public bool autoSetFps = true;

    //=========== 内部 ===========//
    float _deltaTime;      // 平滑帧间隔

    #region ───── 初始化 ─────
    void Awake()
    {
        if (lockLandscape)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        // 关闭 VSync（否则 targetFrameRate 不生效）
        QualitySettings.vSyncCount = 0;

        if (autoSetFps)
        {
#if UNITY_2021_3_OR_NEWER
            float hz = Screen.currentResolution.refreshRate;  // 例如 59.94, 90, 119.88
#else
            float hz = Screen.currentResolution.refreshRate;             // 老版本只能取 int
#endif
            int target = hz switch
            {
                >= 118 => 120,
                >= 85  => 90,
                >= 55  => 60,
                _      => 30
            };

            Application.targetFrameRate = target;
            Debug.Log($"[FpsBootstrap] RefreshRate {hz:0.#}Hz  →  targetFrameRate = {target}");
        }
        else
        {
            // 若未自动设置，可在 Inspector 里单独指定
            Application.targetFrameRate = Application.targetFrameRate <= 0 ? 60 : Application.targetFrameRate;
        }
    }
    #endregion

    #region ───── FPS 统计 ─────
    void Update()
    {
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;   // 指数平滑
    }

    void OnGUI()
    {
        int   fps = Mathf.CeilToInt(1f / _deltaTime);
        float ms  = _deltaTime * 1000f;

        GUIStyle style = new()
        {
            alignment = TextAnchor.UpperLeft,
            fontSize  = Mathf.RoundToInt(Screen.height * 0.05f),
            normal    = { textColor = Color.cyan },
            richText  = false
        };

        GUI.Label(new Rect(10, 10, 240, 70), $"{fps} FPS\n{ms:0.0} ms", style);
    }
    #endregion
}
