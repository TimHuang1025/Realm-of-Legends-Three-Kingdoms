/******************************************************
 * GoogleLoginRequest.cs
 * 功能：UI Toolkit 版 Google 登录
 *       ① 启动先清旧 token
 *       ② 点击 <Button name="GoogleLogo"> 登录（不缓存 token）
 *       ③ 登录成功后异步切到 MainUI
 *****************************************************/
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Assets.SimpleSignIn.Google.Scripts;

[RequireComponent(typeof(UIDocument))]
public sealed class GoogleLoginRequest : MonoBehaviour
{
    /*──── UXML 元素名称（可改） ────*/
    [SerializeField] private string googleBtnName   = "GoogleLogo";
    [SerializeField] private string outputLabelName = "OutputLabel";

    /*──── 私有字段 ────*/
    private Button     googleBtn;
    private Label      outputLabel;
    private GoogleAuth googleAuth;

    /*──────── 生命周期 ────────*/
    void Awake()
    {
        /* 1) 初始化 GoogleAuth，并清除旧会话 */
        googleAuth = new GoogleAuth();
        googleAuth.SignOut(revokeAccessToken: true);          // 不留任何缓存

        /* 2) 取 UI 控件 */
        var root = GetComponent<UIDocument>().rootVisualElement;
        googleBtn   = root.Q<Button>(googleBtnName);
        outputLabel = root.Q<Label>(outputLabelName);

        if (googleBtn == null)
        {
            Debug.LogError($"未找到 <Button name=\"{googleBtnName}\">，请检查 UXML");
            enabled = false; return;
        }

        googleBtn.clicked += OnGoogleClicked;
    }

    void OnDestroy() => googleBtn.clicked -= OnGoogleClicked;

    /*──────── 点击事件 ────────*/
    private void OnGoogleClicked()
    {
        googleBtn.SetEnabled(false);                 // 防抖
        LoadingPanelManager.Instance.Show();         // Loading

        googleAuth.SignIn(OnSignIn, caching: false); // 不写本地缓存
    }

    /*──────── 登录回调 ────────*/
    private void OnSignIn(bool success, string error, UserInfo info)
    {
        LoadingPanelManager.Instance.Hide();
        googleBtn.SetEnabled(true);

        if (!success)
        {
            PopupManager.Show($"Google 登录失败：{error}");
            return;
        }

        PopupManager.Show($"登录, {info.name}!");
        StartCoroutine(LoadMainUIAndCleanup());      // 跳转主场景
    }

    /*──────── 协程：切场景 + 自毁 ────────*/
    private IEnumerator LoadMainUIAndCleanup()
    {
        DontDestroyOnLoad(gameObject);                                // 场景切换期间保证存活

        AsyncOperation op = SceneManager.LoadSceneAsync("MainUI",     // MainUI 场景
                                                        LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        Destroy(gameObject);                                          // 收尾
    }

    /*──────── UI 输出 ────────*/
    private void Show(string msg)
    {
        Debug.Log(msg);
        if (outputLabel != null) outputLabel.text = msg;
    }
}
