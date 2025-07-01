/****************************************************************
 * GoogleLoginRequest.cs ― 单脚本搞定 Google 一键登录
 * --------------------------------------------------------------
 * 1. 在 Awake() 自动查找 class="googlebtn" 按钮并绑定
 * 2. 点击 → GoogleAuth 取 id_token
 * 3. 调 AuthAPI.GoogleLogin(idToken, ok/json, fail)
 * 4. 成功后写 PlayerPrefs + PlayerData，再异步进入 MainUI
 ****************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;                       // 反射取 id_token
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Assets.SimpleSignIn.Google.Scripts;      // GoogleAuth / TokenResponse

[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(AuthAPI))]
public sealed class GoogleLoginRequest : MonoBehaviour
{
    /*──────── 依赖引用 ────────*/
    private AuthAPI         api;        // 场景中同物体或其它物体上的 AuthAPI
    private GoogleAuth      google;     // SimpleSignIn SDK
    private List<Button>    btns;       // 所有 Google 按钮

    /*──────── 生命周期 ────────*/
    void Awake()
    {
        //--------------------------------------------------
        // GoogleAuth 初始化（确保干净）
        //--------------------------------------------------
        google = new GoogleAuth();
        google.SignOut(true);

        //--------------------------------------------------
        // UI 绑定：所有 class="googlebtn" 的按钮
        //--------------------------------------------------
        var root = GetComponent<UIDocument>().rootVisualElement;
        btns     = root.Query<Button>(className: "googlebtn").ToList();
        if (btns.Count == 0)
        {
            Debug.LogError("GoogleLoginRequest: UI 中没有任何 class=\"googlebtn\" 的按钮！");
            enabled = false; return;
        }
        foreach (var b in btns) b.clicked += OnBtnClicked;

        //--------------------------------------------------
        // 找 AuthAPI（同 GameObject 或场景中任意一个）
        //--------------------------------------------------
        api = GetComponent<AuthAPI>() ?? FindObjectOfType<AuthAPI>();
        if (api == null)
        {
            Debug.LogError("GoogleLoginRequest: 场景缺少 AuthAPI，无法发送登录请求");
            enabled = false;
        }
    }

    void OnDestroy()
    {
        foreach (var b in btns) if (b != null) b.clicked -= OnBtnClicked;
    }

    /*──────── 点击事件 ────────*/
    private void OnBtnClicked()
    {
        SetInteractable(false);
        LoadingPanelManager.Instance.Show();
        google.GetTokenResponse(OnGoogleToken);
    }

    /*──────── Google SDK 回调 ────────*/
    private void OnGoogleToken(bool success, string error, TokenResponse token)
    {
        if (!success)
        {
            Fail($"Google SDK 失败：{error}");
            return;
        }

        // 兼容 id_token / idToken / IdToken
        string idToken = ExtractIdToken(token);
        if (string.IsNullOrEmpty(idToken))
        {
            Fail("未能解析到 id_token");
            return;
        }

        // 调后台验证
        api.GoogleLogin(
            idToken,
            ok: json =>
            {
                var data = JsonUtility.FromJson<AuthAPI.ServerResp>(json);
                PlayerData.I.SetSession(
                    data.uid, data.user_token,
                    data.cid, data.character_token,
                    data.server_id, data.server_ip_address, data.server_port
                );
                PlayerData.I.Dump();

                PopupManager.Show("登录提示", "Google 登录成功！");
                StartCoroutine(LoadMainUIAndCleanup());
            },
            fail: msg => Fail($"服务器登录失败：{msg}")
        );
    }

    /*──────── 成功 → 进入主场景 ────────*/
    private IEnumerator LoadMainUIAndCleanup()
    {
        DontDestroyOnLoad(gameObject);
        var op = SceneManager.LoadSceneAsync("MainUI", LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        LoadingPanelManager.Instance.Hide();
        Destroy(gameObject);            // 自己用完即销毁
    }

    /*──────── 失败统一处理 ────────*/
    private void Fail(string msg)
    {
        LoadingPanelManager.Instance.Hide();
        PopupManager.Show("登录提示", msg);
        Debug.LogError(msg);
        SetInteractable(true);
    }

    private void SetInteractable(bool state)
    {
        foreach (var b in btns) b.SetEnabled(state);
    }

    /*──────── 反射提取 id_token ────────*/
    private static string ExtractIdToken(TokenResponse tk)
    {
        if (tk == null) return null;
        var t = tk.GetType();
        foreach (var n in new[] { "id_token", "idToken", "IdToken" })
        {
            var f = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(string))
                return f.GetValue(tk) as string;

            var p = t.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(string))
                return p.GetValue(tk) as string;
        }
        return null;
    }
}
