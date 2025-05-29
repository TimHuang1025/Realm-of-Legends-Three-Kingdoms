using UnityEngine;
using UnityEngine.WSA;
using static AuthAPI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AuthAPI))]
public class VisitorLoginRequest : MonoBehaviour
{
    //[SerializeField] string testDeviceId = "debug-device-001";

    AuthAPI api;

    void Awake() => api = GetComponent<AuthAPI>();

    public void SendVisitorPlay()
    {
        string deviceId = SystemInfo.deviceUniqueIdentifier;

        api.VisitorLogin(
            deviceId,
            onSuccess: json =>
            {
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
                Debug.Log("游客登录成功 "+ deviceId);
                Destroy(gameObject);
                SceneManager.LoadScene("MainUI", LoadSceneMode.Single);
                var controller = Object.FindAnyObjectByType<AccountAuthController>();
                if (controller != null)
                    controller.Toast("游客登录成功！");
            },
            onFail: msg =>
            {
                Debug.LogError($"游客登录失败：{msg}");
            });
    }
}
