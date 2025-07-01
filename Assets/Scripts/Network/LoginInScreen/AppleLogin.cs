/******************************************************
 * AppleLoginRequest.cs
 * 功能：UI Toolkit 版 Apple 登录
 *       ① 仅在支持平台显示 Apple 登录 UI
 *       ② 点击任意带 USS 类 "applebtn" 的 <Button> 进行登录
 *       ③ 登录成功后异步切到 MainUI 场景
 * 用法：
 *   <VisualElement class="applecontainer">
 *       <Button class="applebtn" … />
 *       ……
 *   </VisualElement>
 *   可以有任意多个 container / 按钮
 *****************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using AppleAuth.Extensions;

[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(AuthAPI))]
public sealed class AppleLoginRequest : MonoBehaviour
{
    /*──── 私有字段 ────*/
    private IAppleAuthManager   appleAuthManager;
    private List<Button>        appleBtns;
    private List<VisualElement> appleContainers;
    private Label               tokenLabel;
    private AuthAPI             api;                // ← 新增：调用后台接口

    /*──────── 生命周期 ────────*/
    void Awake()
    {
        api = GetComponent<AuthAPI>() ?? FindObjectOfType<AuthAPI>();

#if UNITY_IOS || UNITY_STANDALONE_OSX
        if (AppleAuthManager.IsCurrentPlatformSupported)
            appleAuthManager = new AppleAuthManager(new PayloadDeserializer());
#endif
    }

    void Update() => appleAuthManager?.Update();

    void OnEnable()
    {
        var root         = GetComponent<UIDocument>().rootVisualElement;
        appleContainers  = root.Query<VisualElement>(className: "applecontainer").ToList();
        appleBtns        = root.Query<Button>(className: "applebtn").ToList();
        tokenLabel       = root.Q<Label>("token");

        /* 不支持 Apple 登录的平台：隐藏整块 UI 并停用脚本 */
        if (appleAuthManager == null || api == null)
        {
            foreach (var c in appleContainers) c.style.display = DisplayStyle.None;
            enabled = false;
            return;
        }

        /* 绑定点击事件 */
        foreach (var btn in appleBtns) btn.clicked += OnAppleLoginClicked;
    }

    void OnDestroy()
    {
        if (appleBtns != null)
            foreach (var btn in appleBtns) btn.clicked -= OnAppleLoginClicked;
    }

    /*──────── 点击事件 ────────*/
    private void OnAppleLoginClicked()
    {
        foreach (var btn in appleBtns) btn.SetEnabled(false);
        LoadingPanelManager.Instance?.Show();

        var loginArgs = new AppleAuthLoginArgs(
                            LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

        appleAuthManager.LoginWithAppleId(
            loginArgs,
            credential =>
            {
                LoadingPanelManager.Instance?.Hide();
                foreach (var b in appleBtns) b.SetEnabled(true);

                if (credential is IAppleIDCredential appleIdCredential)
                {
                    string identityTok = System.Text.Encoding.UTF8.GetString(
                                              appleIdCredential.IdentityToken);

                    /* 可选：在 UI 上显示截断后的 token */
                    if (tokenLabel != null)
                    {
                        string shortTok = identityTok.Length > 8
                                          ? identityTok[..8] + "..."
                                          : identityTok;
                        tokenLabel.text = $"UserID:\n{appleIdCredential.User}\n\nToken:\n{shortTok}";
                    }

                    /*―― 调后台验证 ――*/
                    api.AppleLogin(
                        identityTok,
                        ok: json =>
                        {
                            var d = JsonUtility.FromJson<AuthAPI.ServerResp>(json);
                            PlayerData.I.SetSession(
                                d.uid, d.user_token,
                                d.cid, d.character_token,
                                d.server_id, d.server_ip_address, d.server_port
                            );
                            PlayerData.I.Dump();

                            PopupManager.Show("登录提示", "Apple 登录成功！");
                            StartCoroutine(LoadMainUIAndCleanup());
                        },
                        fail: msg =>
                        {
                            PopupManager.Show("登录失败", "服务器返回错误：" + msg);
                        });
                }
            },
            error =>
            {
                LoadingPanelManager.Instance?.Hide();
                foreach (var b in appleBtns) b.SetEnabled(true);
                PopupManager.Show("登录失败", error.ToString());
                Debug.LogError("Apple Login Error: " + error);
            });
    }

    /*──────── 协程：切场景 + 自毁 ────────*/
    private IEnumerator LoadMainUIAndCleanup()
    {
        DontDestroyOnLoad(gameObject);
        var op = SceneManager.LoadSceneAsync("MainUI", LoadSceneMode.Single);
        while (!op.isDone) yield return null;
        Destroy(gameObject);
    }
}
