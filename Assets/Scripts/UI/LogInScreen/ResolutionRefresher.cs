using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ResolutionRefresher : MonoBehaviour
{
    const int kMaxRetries = 5;          // 最多重试 5 帧

    /*──────── 生命周期 ─────────*/
    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // 监听之后的场景加载
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 首场景：先等 1 帧再开始重试
        StartCoroutine(InitialRoutine());
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    /*──────── 场景事件 ─────────*/
    void OnSceneLoaded(Scene s, LoadSceneMode m) => StartCoroutine(DeferredRefresh());

    /* 首场景专用：先让 UITK 完成第一次布局 */
    IEnumerator InitialRoutine()
    {
        yield return null;                 // 等一帧
        yield return DeferredRefresh();    // 进入重试协程
    }

    /* 统一的重试协程（后续场景也用它） */
    IEnumerator DeferredRefresh()
    {
        int retryLeft = kMaxRetries;

        // 每帧尝试刷新，直到成功或次数用尽
        while (retryLeft-- > 0)
        {
            yield return null;             // 等下一帧
            if (RefreshAll()) break;       // 成功就退出
        }
    }

    /*──────── 刷新所有 VhSizer ─────────*/
    bool RefreshAll()
    {
        bool allReady = true;

#if UNITY_2022_2_OR_NEWER
        var sizers = Object.FindObjectsByType<VhSizer>(
                        FindObjectsInactive.Include,   // ← 包含 inactive
                        FindObjectsSortMode.None);     // ← 不排序
#else
        var sizers = Object.FindObjectsOfType<VhSizer>(true); // 旧 API
#endif

        foreach (var s in sizers)
        {
            var root = s.GetComponent<UIDocument>()?.rootVisualElement;
            if (root == null || root.layout.height <= 1f)
            {
                allReady = false;
                continue;
            }
            s.Apply();
        }
        return allReady;
    }
}
