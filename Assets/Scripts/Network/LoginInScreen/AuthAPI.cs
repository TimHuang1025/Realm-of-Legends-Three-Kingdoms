using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;
using System.Security.Cryptography;   // ← AES / SHA-256


public class AuthAPI : MonoBehaviour
{
    const string host = "http://login.threekingdom.realmoflegend.com:8000";
    const string AES_KEY = "ROLTKROLTKROLTK1";           // 固定 16 bytes
    [Serializable]
    class ApiResp
    {
        public int code;
        public string message;
    }

    /* ------- 访客登录 ------- */
    public Coroutine VisitorLogin(string deviceId,
                                  Action<string> onSuccess,
                                  Action<string> onFail)
    {
        string url = $"{host}/user/VisitorPlay";
        string body = $"{{\"device_id\":\"{deviceId}\"}}";
        return StartCoroutine(PostJson(url, body, onSuccess, onFail));
    }

    /* ------- 账号密码登录 ------- */
    public Coroutine PasswordLogin(string account, string rawPwd,
                                  Action<string> ok,
                                  Action<string> fail)
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string tsStr = ts.ToString();
        /* ② 第一次 SHA-256：原始密码 → 64 位 hex */
        string sha1 = Sha256Hex(rawPwd);                  // 64 chars

        /* ③ 与 timestamp 拼接后再 SHA-256 */
        string sha2 = Sha256Hex(sha1 + tsStr);            // 最终要发给服务器的字段

        /* ④ 组 JSON，字段名要跟接口一致 */
        string url = $"{host}/user/PasswordLogin";
        string body = $"{{\"username\":\"{account}\"," +
                    $"\"password\":\"{sha2}\"," +
                    $"\"timestamp\":\"{tsStr}\"}}";

        return StartCoroutine(PostJson(url, body, ok, fail));

    }

    /* ------- 账号注册 ------- */
    public Coroutine AccountRegister(string account, string rawPwd,
                                    Action<string> ok, Action<string> fail)
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string tsStr = ts.ToString();
        string pwd1 = Sha256Hex(rawPwd);           // ① SHA-256
        string pwd2 = AesEncrypt(pwd1, ts);        // ② AES-CBC

        Debug.Log("SHA-256 = " + pwd1);
        Debug.Log("AES = " + pwd2);
        string url = $"{host}/user/Register";
        string body = $"{{\"username\":\"{account}\"," +
                  $"\"password\":\"{pwd2}\"," +
                  $"\"timestamp\":\"{tsStr}\"}}";
        Debug.Log("Body = " + body);
        return StartCoroutine(PostJson(url, body, ok, fail));
    }


    /* ------- 公共 POST 帮助函数 ------- */
    IEnumerator PostJson(string url, string json,
                        Action<string> ok, Action<string> fail)
    {
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        //yield return new WaitForSeconds(2f);//测试loading

        yield return req.SendWebRequest();

        /* ① HTTP 层错误 */
        if (req.result != UnityWebRequest.Result.Success)
        {
            fail?.Invoke($"{req.responseCode} {req.error}");
            yield break;
        }

        /* ② 业务层解析 */
        var resp = JsonUtility.FromJson<ApiResp>(req.downloadHandler.text);

        if (resp.code == 0)
            ok?.Invoke(req.downloadHandler.text);       // ← 只有 code==0 才算成功
        else
            fail?.Invoke($"{resp.code} {resp.message}"); // 其余全部视为业务失败
    }

    /* ───── 通用加密工具 ───── */

    static string Sha256Hex(string raw)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();   // 64 chars
    }

    static string AesEncrypt(string plainHex, long tsMillis)
    {
        var key = Encoding.ASCII.GetBytes(AES_KEY);
        var iv = Encoding.ASCII.GetBytes(tsMillis.ToString() + "000");  // 13+3 = 16 bytes
        Debug.Log($"iv string = {Encoding.ASCII.GetString(iv)}");

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var enc = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plainHex);
        var cipher = enc.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(cipher);           // 便于 JSON 传输
    }
    /* ------- 发送邮箱验证码 ------- */
    public Coroutine GetEmailCode(string email,
                                Action<string> ok,
                                Action<string> fail)
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string body = $"{{\"email\":\"{email}\",\"timestamp\":\"{ts}\"}}";
        string url = $"{host}/user/GetEmailCode";
        return StartCoroutine(PostJson(url, body, ok, fail));
    }
    /* ------- 邮箱验证码登录 ------- */
    public Coroutine EmailLogin(string email, int verifycode,
                                Action<string> ok, Action<string> fail)
    {
        string url = $"{host}/user/EmailLogin";
        string body = $"{{\"email\":\"{email}\",\"code\":\"{verifycode}\"}}";
        Debug.Log(body);
        return StartCoroutine(PostJson(url, body, ok, fail));
        
    }

}
