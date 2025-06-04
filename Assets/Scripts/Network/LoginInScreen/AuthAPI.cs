using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using System.Security.Cryptography;   // AES / SHA-256 / RSA

public class AuthAPI : MonoBehaviour
{
    /*──────── 常量 ────────*/
    const string host    = "http://login.threekingdom.realmoflegend.com:8000";
    const string AES_KEY = "ROLTKROLTKROLTK1";           // 固定 16 bytes

    /*——— RSA 公钥（两种形式，任选其一） ———*/
    // ① PEM（留作备份）
    const string RSA_PUB_PEM =
@"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEApPOsZmpEoCDxSOiIcoB5
JckxzUh4O/0jJEUt+pIDZSJHfryKvchnlYMO/Kgx5Mb2D55MYnuHKy4StJ6gwxVm
gh92k6P8IiqQuXeptSK+Ze3kmziPHX7g9ycoBTBo73thzIufmWd12W+sG6SZkpyb
kIhRfDN7bJGxc0nApWqNzC14octYSnrqcRWGwj4kMeGkK9ELwye5gxl45iND+cD4
IWjkCgDwhKq9o0PZWYk2hkAMLwbxpKVaaU/rIcW5QxUisOUuWxxtt6m0ua5+AgQR
we5P5ZPHZXCA54OEslgQgxi+VLN8C1cZXvXIjHJNC8jy2EMsuDpxqn02IaJ86t3P
mwIDAQAB
-----END PUBLIC KEY-----";

    // ② 直接拆出的 Modulus 与 Exponent（Base64）
    const string RSA_MOD_B64 =
"pPOsZmpEoCDxSOiIcoB5JckxzUh4O/0jJEUt+pIDZSJHfryKvchnlYMO/Kgx5Mb2" +
"D55MYnuHKy4StJ6gwxVmgh92k6P8IiqQuXeptSK+Ze3kmziPHX7g9ycoBTBo73th" +
"zIufmWd12W+sG6SZkpybkIhRfDN7bJGxc0nApWqNzC14octYSnrqcRWGwj4kMeGk" +
"K9ELwye5gxl45iND+cD4IWjkCgDwhKq9o0PZWYk2hkAMLwbxpKVaaU/rIcW5QxUi" +
"sOUuWxxtt6m0ua5+AgQRwe5P5ZPHZXCA54OEslgQgxi+VLN8C1cZXvXIjHJNC8jy" +
"2EMsuDpxqn02IaJ86t3Pmw==";
    const string RSA_EXP_B64 = "AQAB";        // 65537

    /*──────── 内部结构 ────────*/
    [Serializable]
    class ApiResp
    {
        public int    code;
        public string message;
    }

    public struct ServerResp
    {
        public int    code;
        public string message;

        // 角色
        public string uuid;
        public string user_token;
        public string cuid;
        public string character_token;

        // 服务器信息
        public int    server_id;
        public string server_ip_address;
        public int    server_port;
    }

    /*──────────────────────────────────────────────────────
     *  访客登录
     *──────────────────────────────────────────────────────*/
    public Coroutine VisitorLogin(string deviceId,
                                  Action<string> onSuccess,
                                  Action<string> onFail)
    {
        string url  = $"{host}/user/VisitorPlay";
        string body = $"{{\"device_id\":\"{deviceId}\"}}";
        return StartCoroutine(PostJson(url, body, onSuccess, onFail));
    }

    /*──────────────────────────────────────────────────────
     *  账号密码登录  (两次 SHA-256，保持旧协议)
     *──────────────────────────────────────────────────────*/
    public Coroutine PasswordLogin(string account, string rawPwd,
                                   Action<string> ok, Action<string> fail)
    {
        long   ts    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string tsStr = ts.ToString();

        string sha1 = Sha256Hex(rawPwd);            // 第一次 SHA-256
        string sha2 = Sha256Hex(sha1 + tsStr);      // 第二次 SHA-256

        string url  = $"{host}/user/PasswordLogin";
        string body = $"{{\"username\":\"{account}\"," +
                      $"\"password\":\"{sha2}\"," +
                      $"\"timestamp\":\"{tsStr}\"}}";

        return StartCoroutine(PostJson(url, body, ok, fail));
    }

    /*──────────────────────────────────────────────────────
     *  账号注册  (三层加密)
     *──────────────────────────────────────────────────────*/
    public Coroutine AccountRegister(string account, string rawPwd,
                                     Action<string> ok, Action<string> fail)
    {
        long   ts    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string tsStr = ts.ToString();

        string pwd1 = Sha256Hex(rawPwd);      // ① SHA-256
        string pwd2 = AesEncrypt(pwd1, ts);   // ② AES-CBC  → Base64
        string pwd3 = RsaEncryptBase64(pwd2); // ③ RSA-PKCS1 → Base64
        string url  = $"{host}/user/Register";
        string body = $"{{\"username\":\"{account}\"," +
                      $"\"password\":\"{pwd3}\"," +
                      $"\"timestamp\":\"{tsStr}\"}}";
        Debug.Log("Body = " + body);

        return StartCoroutine(PostJson(url, body, ok, fail));
    }

    /*──────────────────────────────────────────────────────
     *  邮箱验证码
     *──────────────────────────────────────────────────────*/
    public Coroutine GetEmailCode(string email,
                                  Action<string> ok, Action<string> fail)
    {
        long   ts   = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string body = $"{{\"email\":\"{email}\",\"timestamp\":\"{ts}\"}}";
        string url  = $"{host}/user/GetEmailCode";
        return StartCoroutine(PostJson(url, body, ok, fail));
    }

    public Coroutine EmailLogin(string email, string verifycode,
                                Action<string> ok, Action<string> fail)
    {
        string url  = $"{host}/user/EmailLogin";
        string body = $"{{\"email\":\"{email}\",\"code\":\"{verifycode}\"}}";
        return StartCoroutine(PostJson(url, body, ok, fail));
    }

    public Coroutine CheckUsername(string username,
                                   Action<string> ok, Action<string> fail)
    {
        string url  = $"{host}/user/CheckUsername";
        string body = $"{{\"username\":\"{username}\"}}";
        return StartCoroutine(PostJson(url, body, ok, fail));
    }

    /*──────────────────────────────────────────────────────
     *  HTTP POST
     *──────────────────────────────────────────────────────*/
    IEnumerator PostJson(string url, string json,
                         Action<string> ok, Action<string> fail)
    {
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            fail?.Invoke($"{req.responseCode} {req.error}");
            yield break;
        }

        var resp = JsonUtility.FromJson<ApiResp>(req.downloadHandler.text);
        if (resp.code == 0)
            ok?.Invoke(req.downloadHandler.text);
        else
            fail?.Invoke($"{resp.code} {resp.message}");
    }

    /*──────────────────────────────────────────────────────
     *  加密工具
     *──────────────────────────────────────────────────────*/
    static string Sha256Hex(string raw)
    {
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    static string AesEncrypt(string plainHex, long tsMillis)
    {
        byte[] key = Encoding.ASCII.GetBytes(AES_KEY);
        byte[] iv  = Encoding.ASCII.GetBytes(tsMillis.ToString() + "000"); // 16 bytes

        using var aes = Aes.Create();
        aes.Key     = key;
        aes.IV      = iv;
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var enc = aes.CreateEncryptor();
        byte[] bytes  = Encoding.UTF8.GetBytes(plainHex);
        byte[] cipher = enc.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(cipher);
    }

    /*──────────────────────────────────────────────────────
     *  RSA-PKCS#1 加密 (通吃所有 Unity 版本)
     *──────────────────────────────────────────────────────*/
    static string RsaEncryptBase64(string base64Plain)
    {
        byte[] plainBytes = Convert.FromBase64String(base64Plain);

        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus  = Convert.FromBase64String(RSA_MOD_B64),
            Exponent = Convert.FromBase64String(RSA_EXP_B64)
        });

        byte[] cipher = rsa.Encrypt(plainBytes, RSAEncryptionPadding.Pkcs1);
        return Convert.ToBase64String(cipher);
    }
}
