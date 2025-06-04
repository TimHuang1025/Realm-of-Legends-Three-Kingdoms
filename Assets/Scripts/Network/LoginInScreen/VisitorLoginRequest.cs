using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.WSA;
using static AuthAPI;           // 如果你在 AuthAPI 里有静态常量，可保留
                                // 若没用到可删掉这行

[RequireComponent(typeof(AuthAPI))]
public class VisitorLoginRequest : MonoBehaviour
{
    /*──────── 私有字段 ────────*/
    private AuthAPI api;

    /*──────── 生命周期 ────────*/
    void Awake() => api = GetComponent<AuthAPI>();

    /*──────── 外部触发 ────────*/
    public void SendVisitorPlay()
    {
        string deviceId = SystemInfo.deviceUniqueIdentifier;   // 设备唯一 ID
        LoadingPanelManager.Instance.Show(); // 显示加载面板

        api.VisitorLogin(
            deviceId,
            onSuccess: json =>
            {
                // 1) 解析服务器返回
                var data = JsonUtility.FromJson<ServerResp>(json);
                PlayerData.I.SetSession(
                    data.uuid,
                    data.user_token,
                    data.cuid,
                    data.character_token,
                    data.server_id,
                    data.server_ip_address,
                    data.server_port
                );
                PlayerData.I.Dump();

                // 2) UI 提示
                PopupManager.Show("登录提示", "游客登录成功！");
                Debug.Log("游客登录成功 " + deviceId);

                // 3) 异步加载 MainUI，加载完再销毁自己
                StartCoroutine(LoadMainUIAndCleanup());
            },
            onFail: msg =>
            {
                PopupManager.Show("登录提示", "游客登录失败！\n" + msg);
                Debug.LogError($"游客登录失败：{msg}");
            });
    }

    /*──────── 协程：切场景 + 销毁 ────────*/
    private IEnumerator LoadMainUIAndCleanup()
    {
        // 在切场景过程中先保证自己存活
        DontDestroyOnLoad(gameObject);

        // ① 加载新场景（Single 模式）
        AsyncOperation op = SceneManager.LoadSceneAsync("MainUI", LoadSceneMode.Single);
        while (!op.isDone)
            yield return null;          // 等到 100 %

        // ② 新场景已激活，可安全访问 MainUI 的脚本
        var controller = Object.FindAnyObjectByType<AccountAuthController>();
        if (controller != null)
            controller.Toast("游客登录成功！");

        // ③ 一切收尾后销毁本对象
        Destroy(gameObject);
        OnDestroy();
        void OnDestroy()
        {
            Debug.Log($"[OnDestroy] {gameObject.name} 被销毁", this);
        }
        LoadingPanelManager.Instance.Hide(); 

    }
}
