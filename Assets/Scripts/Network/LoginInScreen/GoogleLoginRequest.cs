/******************************************************
 * GoogleLoginRequest.cs
 * 功能：UI Toolkit 版 Google 登录
 *       ① 启动先清旧 token
 *       ② 点击任意带 USS 类 "googlebtn" 的 <Button> 获取 Token
 *       ③ Token 获取成功后异步切到 MainUI
 *****************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Linq;                       // 需要 ToList()
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Assets.SimpleSignIn.Google.Scripts; // 引入 GoogleAuth / TokenResponse

[RequireComponent(typeof(UIDocument))]
public sealed class GoogleLoginRequest : MonoBehaviour
{
    /*──── 私有字段 ────*/
    private List<Button> googleBtns;     // 保存所有按钮
    private GoogleAuth   googleAuth;

    /*──────── 生命周期 ────────*/
    void Awake()
    {
        /* 1) 初始化 GoogleAuth，并清除旧会话 */
        googleAuth = new GoogleAuth();
        googleAuth.SignOut(revokeAccessToken: true);          // 不留任何缓存

        /* 2) 取 UI 控件并绑定点击事件 */
        var root      = GetComponent<UIDocument>().rootVisualElement;
        googleBtns    = root.Query<Button>(className: "googlebtn").ToList();

        if (googleBtns.Count == 0)
        {
            Debug.LogError("未找到任何带 USS 类 \"googlebtn\" 的 <Button>，请检查 UXML/USS");
            enabled = false; return;
        }

        foreach (var btn in googleBtns)
        {
            btn.clicked += OnGoogleClicked;
        }
    }

    void OnDestroy()
    {
        if (googleBtns == null) return;
        foreach (var btn in googleBtns)
        {
            btn.clicked -= OnGoogleClicked;
        }
    }

    /*──────── 点击事件 ────────*/
    private void OnGoogleClicked()
    {
        // 防抖：一次性禁用所有 google 按钮
        foreach (var btn in googleBtns) btn.SetEnabled(false);

        LoadingPanelManager.Instance.Show();                   // Loading

        // 直接获取 Token；如无缓存会自动弹出授权
        googleAuth.GetTokenResponse(OnGetTokenResponse);
    }

    /*──────── Token 回调 ────────*/
    private void OnGetTokenResponse(bool success, string error, TokenResponse token)
    {
        LoadingPanelManager.Instance.Hide();
        foreach (var btn in googleBtns) btn.SetEnabled(true);  // 重新启用按钮

        if (!success)
        {
            PopupManager.Show($"登录失败：{error}");
            return;
        }

        PopupManager.Show("登录成功！");

        StartCoroutine(LoadMainUIAndCleanup());                // 跳转主场景
    }

    /*──────── 协程：切场景 + 自毁 ────────*/
    private IEnumerator LoadMainUIAndCleanup()
    {
        DontDestroyOnLoad(gameObject);                         // 场景切换期间保证存活

        AsyncOperation op = SceneManager.LoadSceneAsync("MainUI", LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        Destroy(gameObject);                                   // 收尾
    }
}
