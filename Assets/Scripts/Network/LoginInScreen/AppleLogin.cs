/******************************************************
 * AppleLoginRequest.cs
 * 功能：UI Toolkit 版 Apple 登录
 *       ① 仅在支持平台显示 Apple 登录 UI
 *       ② 点击任意带 USS 类 "applebtn" 的 <Button> 进行登录
 *       ③ 登录成功后异步切到 MainUI 场景
 * 用法：
 *   <VisualElement class="applecontainer">      ← 包住整块 Apple 登录 UI
 *       <Button class="applebtn" .../>
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
public sealed class AppleLoginRequest : MonoBehaviour
{
    /*──── 私有字段 ────*/
    private IAppleAuthManager         appleAuthManager;
    private List<Button>              appleBtns;
    private List<VisualElement>       appleContainers;
    private Label                     tokenLabel;

    /*──────── 生命周期 ────────*/
    void Awake()
    {
        if (AppleAuthManager.IsCurrentPlatformSupported &&
            (Application.platform == RuntimePlatform.IPhonePlayer ||
             Application.platform == RuntimePlatform.OSXPlayer  ||
             Application.platform == RuntimePlatform.OSXEditor))
        {
            var deserializer   = new PayloadDeserializer();
            appleAuthManager   = new AppleAuthManager(deserializer);
        }
    }

    void Update() => appleAuthManager?.Update();

    void OnEnable()
    {
        var root            = GetComponent<UIDocument>().rootVisualElement;

        appleContainers     = root.Query<VisualElement>(className: "applecontainer").ToList();
        appleBtns           = root.Query<Button>(className: "applebtn").ToList();
        tokenLabel          = root.Q<Label>("token");

        /* 如果当前平台不支持 Apple 登录，则隐藏所有容器并停用脚本 */
        if (appleAuthManager == null)
        {
            foreach (var c in appleContainers) c.style.display = DisplayStyle.None;
            enabled = false;
            return;
        }

        /* 绑定按钮点击事件 */
        foreach (var btn in appleBtns) btn.clicked += OnAppleLoginClicked;
    }

    void OnDestroy()
    {
        if (appleBtns == null) return;
        foreach (var btn in appleBtns) btn.clicked -= OnAppleLoginClicked;
    }

    /*──────── 点击事件 ────────*/
    private void OnAppleLoginClicked()
    {
        foreach (var btn in appleBtns) btn.SetEnabled(false);   // 防抖
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
                    string userId      = appleIdCredential.User;
                    string identityTok = System.Text.Encoding.UTF8.GetString(
                                              appleIdCredential.IdentityToken);

                    if (tokenLabel != null)
                    {
                        string shortTok = identityTok.Length > 8
                                          ? identityTok[..8] + "..."
                                          : identityTok;
                        tokenLabel.text = $"UserID:\n{userId}\n\nToken:\n{shortTok}";
                    }

                    StartCoroutine(LoadMainUIAndCleanup());
                }
            },
            error =>
            {
                LoadingPanelManager.Instance?.Hide();
                foreach (var b in appleBtns) b.SetEnabled(true);
                PopupManager.Show("登录失败");
                Debug.LogError("Apple Login Error: " + error);
            });
    }

    /*──────── 协程：切场景 + 自毁 ────────*/
    private IEnumerator LoadMainUIAndCleanup()
    {
        DontDestroyOnLoad(gameObject);

        AsyncOperation op = SceneManager.LoadSceneAsync("MainUI", LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        Destroy(gameObject);
    }
}
