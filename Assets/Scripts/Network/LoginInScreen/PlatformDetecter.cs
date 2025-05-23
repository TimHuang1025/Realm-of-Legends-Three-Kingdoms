using UnityEngine;

public class PlatformDetector : MonoBehaviour
{
    public static bool IsAndroid { get; private set; }
    public static bool IsiOS     { get; private set; }

    // 在 Inspector 里选 Auto / Android / iOS
    public enum PlatformOverride { Auto, Android, iOS }
    public PlatformOverride overrideMode = PlatformOverride.Auto;

    void Awake()
    {
        RuntimePlatform p = Application.platform;

        // 手动覆盖（仅在 Editor 可改）
        if (overrideMode == PlatformOverride.Android)      p = RuntimePlatform.Android;
        else if (overrideMode == PlatformOverride.iOS)     p = RuntimePlatform.IPhonePlayer;

        IsAndroid = p == RuntimePlatform.Android;
        IsiOS     = p == RuntimePlatform.IPhonePlayer;

        Debug.Log($"[PlatformDetector] {p}");
        DontDestroyOnLoad(gameObject);
    }
}
