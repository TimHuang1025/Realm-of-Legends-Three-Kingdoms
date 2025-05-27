using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

/// <summary>
/// 专管登录场景 UI 的顶层管理器：
/// 1. 顶栏按钮 → 切外层 Page
/// 2. AccountPage 内部再切 Login / Register 子面板
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LoginUIManager : MonoBehaviour
{
    public static LoginUIManager I;
    readonly Stack<System.Action> navStack = new();   // 返回栈
    /* ---------- 外层 UI ------------- */
    VisualElement selectorBar;                 // #LogInSelectorContainer
    VisualElement pagesRoot;                   // #Page

    /* ---------- Page 容器 ------------ */
    VisualElement emailPage;
    VisualElement accountPage;

    VisualElement accountPagePanel;

    /* ---------- 子面板 (Account) ----- */
    VisualElement loginPanel;                  // #LoginPanel
    VisualElement registerPanel;               // #RegisterPanel
    VisualElement accountChangePwPanel;        // #AccountChangePwPanel
    VisualElement AccChangePw;                 // #AccChangePw
    VisualElement AccEmailVerifyPanel;         // #AccEmailVerifyPanel

    /* ---------- 映射  ----------------- */
    readonly Dictionary<Button, VisualElement> pageOf = new();

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        I = this;

        // Google / Apple 登录按钮 ----------------------
        var appleBtn  = root.Q<VisualElement>("AppleLogInContainer");
        var googleBtn = root.Q<VisualElement>("GoogleLogInContainer");
        if (PlatformDetector.IsAndroid)
        {
            appleBtn.style.display = DisplayStyle.None;
            googleBtn.style.display = DisplayStyle.Flex;
        }

        // 抓核心节点 -------------------------------------------------
        selectorBar = root.Q<VisualElement>("LogInSelectorContainer");
        pagesRoot = root.Q<VisualElement>("Page");

        emailPage = root.Q<VisualElement>("EmailPageContainer");
        accountPage = root.Q<VisualElement>("AccountPageContainer");
        accountPagePanel = root.Q<VisualElement>("AccountPagePanel");
        accountChangePwPanel = root.Q<VisualElement>("AccountChangePwPanel");


        loginPanel = root.Q<VisualElement>("AccountLoginPanel");
        registerPanel = root.Q<VisualElement>("RegisterPanel");

        // 顶栏按钮 -------------------------------------------------
        Button emailBtn = root.Q<Button>("EmailLogo");
        Button accountBtn = root.Q<Button>("AccountLogo");

        AccChangePw = root.Q<VisualElement>("AccChangePw");
        AccEmailVerifyPanel = root.Q<VisualElement>("AccEmailVerifyPanel");

        pageOf[emailBtn] = emailPage;
        pageOf[accountBtn] = accountPage;

        foreach (var kv in pageOf)
        {
            var btn = kv.Key;                       // 复制避免闭包 bug
            btn.clicked += () => ShowPage(btn);
        }

        // Account 内部子面板按钮 -------------------------------
        Button gotoRegisterBtn = root.Q<Button>("RegisterButton");
        Button gotoLoginBtn = root.Q<Button>("GoLoginBtn");
        Button gotoVerifyBtn = root.Q<Button>("ChangePasswordButton");

        if (gotoRegisterBtn != null) gotoRegisterBtn.clicked += ShowRegister;
        if (gotoLoginBtn != null) gotoLoginBtn.clicked += ShowLogin;
        if (gotoVerifyBtn != null) gotoVerifyBtn.clicked += ShowAccVerify;

        // Return 按钮 （class="return-btn"）
        root.Query<Button>(className: "return-btn")
            .ForEach(b => b.clicked += GoBack);

        // 启动：只显示顶栏
        pagesRoot.style.display = DisplayStyle.None;
    }

    /* ============== 顶栏导航 ============== */

    void ShowPage(Button btn)
    {
        navStack.Push(ShowTopBar); // 先压入栈上一页
        // 隐顶栏，显外层 Page 父容器
        selectorBar.style.display = DisplayStyle.None;
        pagesRoot.style.display = DisplayStyle.Flex;

        // 隐全部 Page，再显目标 Page
        foreach (var p in pageOf.Values) p.style.display = DisplayStyle.None;
        pageOf[btn].style.display = DisplayStyle.Flex;

        // 若是 AccountPage，默认展示 LoginPanel
        if (btn.name == "AccountLogo")
            ShowLogin();
    }

    void ShowTopBar()
    {
        HideAccSubPanel();
        selectorBar.style.display = DisplayStyle.Flex;
        pagesRoot.style.display = DisplayStyle.None;
    }

    /* ============== Account 内部子面板 ============== */

    void ShowLogin()
    {
        navStack.Push(ShowTopBar);
        HideAccSubPanel();
        accountPagePanel.style.display = DisplayStyle.Flex;
        loginPanel.style.display = DisplayStyle.Flex;
        registerPanel.style.display = DisplayStyle.None;
    }

    void ShowRegister()
    {
        navStack.Push(ShowLogin);            // 返回注册页上一层 = Login
        HideAccSubPanel();
        loginPanel.style.display = DisplayStyle.None;
        registerPanel.style.display = DisplayStyle.Flex;
    }

    void ShowAccVerify()
    {
        navStack.Push(ShowLogin); 
        HideAccSubPanel();
        registerPanel.style.display = DisplayStyle.None;
        loginPanel.style.display = DisplayStyle.None;
        AccChangePw.style.display = DisplayStyle.None;
        accountChangePwPanel.style.display = DisplayStyle.Flex;
        AccEmailVerifyPanel.style.display = DisplayStyle.Flex;
    }

    void HideAccSubPanel()
    {
        accountChangePwPanel.style.display = DisplayStyle.None;
        registerPanel.style.display = DisplayStyle.None;
        loginPanel.style.display = DisplayStyle.None;
    }

    public void ToChangePwPanel()
    {
        navStack.Push(ShowAccVerify);
        HideAccSubPanel();
        Debug.Log("触发修改密码Panel");
        accountChangePwPanel.style.display = DisplayStyle.Flex;
        AccEmailVerifyPanel.style.display = DisplayStyle.None;
        AccChangePw.style.display = DisplayStyle.Flex;
    }
    void GoBack()
    {
        if (navStack.Count > 0)
        {
            var back = navStack.Pop();
            back?.Invoke();
        }
        else
        {
            // 栈空 ⇒ 回到顶栏
            ShowTopBar();
        }
    }
}
