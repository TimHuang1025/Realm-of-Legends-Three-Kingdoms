using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class VisitorLoginRequest : MonoBehaviour
{
    [SerializeField] string testDeviceId = "debug-device-001"; // ← test

    const string kUrl = "http://login.threekingdom.realmoflegend.com:8000/user/VisitorPlay";

    // Inspector ▸ 三点菜单 ▸ POST VisitorPlay
    [ContextMenu("POST VisitorPlay")]
    void PostVisitorPlay()
    {
        StartCoroutine(PostCoroutine());
    }
    public void SendVisitorPlay()
    {
        StartCoroutine(PostCoroutine());
    }
    IEnumerator PostCoroutine()
    {
        // ① 直接拼 JSON 字符串
        string json   = $"{{\"device_id\":\"{testDeviceId}\"}}";
        byte[] raw    = Encoding.UTF8.GetBytes(json);

        // ② UnityWebRequest
        using var req = new UnityWebRequest(kUrl, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler   = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        // ③ 发送
        yield return req.SendWebRequest();

        // ④ 结果
        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogError($"HTTP {req.responseCode}  {req.error}");
        else
            Debug.Log($"Server reply:\n{req.downloadHandler.text}");
    }
}