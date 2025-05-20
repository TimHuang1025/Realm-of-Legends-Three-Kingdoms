using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class AuthAPI : MonoBehaviour
{
    const string host = "http://login.threekingdom.realmoflegend.com:8000";

    /* ------- 访客登录 ------- */
    public Coroutine VisitorLogin(string deviceId,
                                  Action<string> onSuccess,
                                  Action<string> onFail)
    {
        string url  = $"{host}/user/VisitorPlay";
        string body = $"{{\"device_id\":\"{deviceId}\"}}";
        return StartCoroutine(PostJson(url, body, onSuccess, onFail));
    }

    /* ------- 账号密码登录 ------- */
    public Coroutine AccountLogin(string account, string password,
                                  Action<string> onSuccess,
                                  Action<string> onFail)
    {
        string ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        string url  = $"{host}/user/PasswordLogin";
        string body = $"{{\"account\":\"{account}\",\"password\":\"{password}\"}}";
        return StartCoroutine(PostJson(url, body, onSuccess, onFail));
    }

    /* ------- 公共 POST 帮助函数 ------- */
    IEnumerator PostJson(string url, string json,
                         Action<string> ok, Action<string> fail)
    {
        using var req = new UnityWebRequest(url, "POST");
        byte[] raw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler   = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            fail?.Invoke($"{req.responseCode} {req.error}");
        else
            ok?.Invoke(req.downloadHandler.text);
    }
}
