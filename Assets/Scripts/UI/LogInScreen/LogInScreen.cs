using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(AudioSource))]
public class LoginUIManager : MonoBehaviour
{
    public static LoginUIManager I;

    /* ====== 可调动画参数 ====== */
    [Header("FX Settings")]
    [SerializeField] private AudioClip clickSfx;        // 首次点击音效
    [SerializeField] private float    fadeDelay = 1f;   // 点击后等待多久开始动画
    [SerializeField] private float    coverFadeTime     = 0.6f;  // 封面淡出时长
    [SerializeField] private float    containerFadeTime = 1.2f;  // 主界面淡入时长
    [Range(0, 1)]     public  float  darkenAlpha = .6f;          // Container 最终叠加不透明度
    [SerializeField]  private Color  darkenColor = Color.black;

    /* ====== 返回栈 ====== */
    private readonly Stack<System.Action> navStack = new();

    /* ====== 节点缓存 ====== */
    private VisualElement root, startScreen, pagesRoot, container;
    private VisualElement emailPage, accountPage, accountPagePanel;
    private VisualElement loginPanel, registerPanel, accountChangePwPanel;
    private VisualElement accChangePw, accEmailVerifyPanel;

    private AudioSource audioSrc;
    private bool hasEntered;

    /* ───────────────── 生命周期 ───────────────── */
    private void OnEnable()
    {
        root     = GetComponent<UIDocument>().rootVisualElement;
        I        = this;
        audioSrc = GetComponent<AudioSource>();

        /* ---- 抓节点 ---- */
        startScreen          = root.Q<VisualElement>("StartScreen");
        pagesRoot            = root.Q<VisualElement>("Page");
        container            = root.Q<VisualElement>("Container");      // 需要变暗的容器
        emailPage            = root.Q<VisualElement>("EmailPageContainer");
        accountPage          = root.Q<VisualElement>("AccountPageContainer");
        accountPagePanel     = root.Q<VisualElement>("AccountPagePanel");
        accountChangePwPanel = root.Q<VisualElement>("AccountChangePwPanel");
        loginPanel           = root.Q<VisualElement>("AccountLoginPanel");
        registerPanel        = root.Q<VisualElement>("RegisterPanel");
        accChangePw          = root.Q<VisualElement>("AccChangePw");
        accEmailVerifyPanel  = root.Q<VisualElement>("AccEmailVerifyPanel");

        /* ---- 顶栏按钮 ---- */
        root.Q<Button>("EmailLogo")?.RegisterCallback<ClickEvent>(_ => ShowEmail());
        root.Query<Button>(className: "accountbtn").ForEach(b => b.RegisterCallback<ClickEvent>(_ => ShowAccount()));

        /* ---- Account 内部按钮 ---- */
        root.Q<Button>("RegisterButton")?.RegisterCallback<ClickEvent>(_ => ShowRegister());
        root.Q<Button>("GoLoginBtn")?.RegisterCallback<ClickEvent>(_ => ShowLogin());
        root.Q<Button>("ChangePasswordButton")?.RegisterCallback<ClickEvent>(_ => ShowAccVerify());

        /* ---- 返回 / 回封面按钮 ---- */
        root.Query<Button>(className: "return-btn").ForEach(b => b.clicked += GoBack);
        root.Query<Button>(className: "returntomain-btn").ForEach(b => b.clicked += ResetToCover);

        /* ---- 初始只显示封面 ---- */
        ResetToCover();
        root.RegisterCallback<PointerDownEvent>(OnFirstClick);
    }

    /* ───────── 首次点击 ───────── */
    private void OnFirstClick(PointerDownEvent _)
    {
        if (hasEntered) return;
        hasEntered = true;

        if (clickSfx) audioSrc.PlayOneShot(clickSfx);
        StartCoroutine(EnterSequence());
    }

    /* 封面 → 主界面 两段动画协程 */
    private IEnumerator EnterSequence()
    {
        /* 1) 等待 delay */
        yield return new WaitForSeconds(fadeDelay);

        /* 2) 封面淡出 */
        float t = 0;
        while (t < coverFadeTime)
        {
            t += Time.deltaTime;
            startScreen.style.opacity = 1 - t / coverFadeTime;
            yield return null;
        }
        startScreen.Hide();
        startScreen.style.opacity = 1;

        /* 3) 主界面淡入 & Container 变暗 */
        pagesRoot.Show();
        pagesRoot.style.opacity = 0;
        container.style.backgroundColor =
            new Color(darkenColor.r, darkenColor.g, darkenColor.b, 0);

        ShowAccount();   // 默认展开 Login

        t = 0;
        while (t < containerFadeTime)
        {
            t += Time.deltaTime;
            float k = t / containerFadeTime;
            pagesRoot.style.opacity = k;
            container.style.backgroundColor =
                new Color(darkenColor.r, darkenColor.g, darkenColor.b, k * darkenAlpha);
            yield return null;
        }
        pagesRoot.style.opacity = 1;
        container.style.backgroundColor =
            new Color(darkenColor.r, darkenColor.g, darkenColor.b, darkenAlpha);
    }

    /* ───────── 封面重置 ───────── */
    private void ResetToCover()
    {
        StopAllCoroutines();
        hasEntered = false;
        navStack.Clear();

        pagesRoot.Hide();
        emailPage.Hide();
        accountPage.Hide();
        HideAccSubPanel();
        pagesRoot.style.opacity = 1;

        container.style.backgroundColor =
            new Color(darkenColor.r, darkenColor.g, darkenColor.b, 0);

        startScreen.Show();
        startScreen.style.opacity = 1;
    }

    /* ───────── 顶栏切换 ───────── */
    private void ShowEmail()
    {
        navStack.Push(ShowLogin);
        pagesRoot.Show();
        emailPage.Show();
        accountPage.Hide();
    }

    private void ShowAccount()
    {
        Debug.Log("ShowAccount called");
        pagesRoot.Show();
        accountPage.Show();
        emailPage.Hide();
        ShowLogin();
    }

    /* ───────── Account 子面板 ───────── */
    private void ShowLogin()
    {
        emailPage.Hide();
        accountPage.Show();
        HideAccSubPanel();
        accountPagePanel.Show();
        loginPanel.Show();
    }
    private void ShowRegister()
    {
        navStack.Push(ShowLogin);
        HideAccSubPanel();
        registerPanel.Show();
    }
    private void ShowAccVerify()
    {
        navStack.Push(ShowLogin);
        HideAccSubPanel();
        accountChangePwPanel.Show();
        accEmailVerifyPanel.Show();
    }
    public void ToChangePwPanel()
    {
        navStack.Push(ShowAccVerify);
        HideAccSubPanel();
        accountChangePwPanel.Show();
        accChangePw.Show();
    }
    private void HideAccSubPanel()
    {
        accountChangePwPanel.Hide();
        registerPanel.Hide();
        loginPanel.Hide();
        accEmailVerifyPanel.Hide();
        accChangePw.Hide();
    }

    /* ───────── 返回 ───────── */
    private void GoBack()
    {
        if (navStack.Count > 0)
            navStack.Pop()?.Invoke();
        else
            ShowLogin();
    }
}

/* ───────── Show / Hide 扩展 ───────── */
static class VisualElementEx
{
    public static void Show(this VisualElement ve) => ve.style.display = DisplayStyle.Flex;
    public static void Hide(this VisualElement ve) => ve.style.display = DisplayStyle.None;
}
