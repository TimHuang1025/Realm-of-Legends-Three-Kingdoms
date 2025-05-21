using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class AccountAuthController : MonoBehaviour
{
    private AuthAPI api; //POST服务器API接口
    /* ───── email登录区控件 ───── */
    private TextField loginEmailField;  //class="email-input"
    private TextField verifyCodeField;
    private Button verifyBtn;
    /* ───── 登录区控件 ───── */
    private TextField loginAccField;     // class="account-input"
    private TextField loginPwdField;     // class="pwd-input"
    private Button loginBtn;          // name="LoginBtn"

    /* ───── 注册区控件 ───── */
    private TextField regAccField;       // 可以与 loginAccField 共用
    private TextField regPwdField1;      // class="pwdregister" (第 1 个)
    private TextField regPwdField2;      // class="pwdregister" (第 2 个)
    private Button registerBtn;       // name="RegisterBtn"

    /* ───── 验证码输入框 ───── */
    private TextField emailCodeField;    // name="emailCodeField"
    private List<Button> sendCodeBtn;
    private Coroutine cooldownCO;

    /* ───── ToastPanel ───── */
    private VisualElement toastPanel;
    private Label toastText;
    private Coroutine toastCO;

    /* ───── 生命周期 ───── */
    void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        api = GetComponent<AuthAPI>();
        Button visitorBtn = root.Q<Button>("GuestLogo");
        var visitorapi = GetComponent<VisitorLoginRequest>();
        visitorBtn.clicked += visitorapi.SendVisitorPlay;

        // 验证码
        var verifyBtn = root.Query<Button>(className: "verifybtn").ToList(); //接收验证码按钮
        foreach (var tf in verifyBtn)
            tf.clicked += OnClickVerify;

        sendCodeBtn = root.Query<Button>(className: "getemailcode").ToList(); //接收验证码按钮
        foreach (var tf in sendCodeBtn)
            tf.clicked += OnClickSendCode;

        var verifyCodeFields = root.Query<TextField>(className: "verifycode-input").ToList();
        foreach (var tf in verifyCodeFields)
        {
            // 2. 只允许 6 位
            tf.maxLength = 6;

            // 3. 输入实时过滤：只留数字，多余截掉
            tf.RegisterValueChangedCallback(OnEmailCodeChanged);
        }
        verifyCodeField = root.Q<TextField>(className: "verifycode-input");

        // email 登录
        loginEmailField = root.Q<TextField>(className: "email-input");

        // 登录区
        loginAccField = root.Q<TextField>(className: "account-input");
        loginPwdField = root.Q<TextField>(className: "pwd-input");
        loginBtn = root.Q<Button>("LoginBtn");
        if (loginBtn != null) loginBtn.clicked += OnClickLogin;

        // 注册区
        regAccField = root.Q<TextField>("regAccField");   // ← 直接按 name 查

        regPwdField1 = root.Q<TextField>("regPwdField1");     // 第 1 次密码
        regPwdField2 = root.Q<TextField>("regPwdField2");     // 第 2 次密码

        var regPwds = root.Query<TextField>(className: "pwdregister").ToList();
        if (regPwds.Count >= 2)
        {
            regPwdField1 = regPwds[0];
            regPwdField2 = regPwds[1];
        }
        registerBtn = root.Q<Button>("RegisterBtn");
        if (registerBtn != null) registerBtn.clicked += OnClickRegister;

        // return-btn：清空所有输入
        root.Query<Button>(className: "return-btn")
            .ForEach(b => b.clicked += ClearAllInputs);

        /* ───── Toast 引用 ───── */
        toastPanel = root.Q<VisualElement>("ToastPanel");
        toastText = root.Q<Label>("ToastText");

    }

    /* ───── 登录 ───── */
    private void OnClickLogin()
    {
        if (!CheckAccountAndPwd(loginAccField, loginPwdField)) return;

        LoadingPanelManager.Instance.Show(); //Loading

        Debug.Log($"[Login] {loginAccField.value}/{loginPwdField.value}");
        string acc = loginAccField.value.Trim();
        string pwd = loginPwdField.value;

        api.PasswordLogin(
            acc,
            pwd,
            ok: json =>
            {
                LoadingPanelManager.Instance.Hide();
                Debug.Log("登陆成功");
                Toast("登陆成功！");
            },
            fail: msg =>
            {
                LoadingPanelManager.Instance.Hide();
                Debug.LogError($"登录失败：{msg}");
                Toast("登陆失败！");
            }
        );
        ClearAllInputs();
        // TODO: HTTP 登录请求…
    }

    /* ───── 注册 ───── */
    private void OnClickRegister()
    {
        if (!CheckAccountAndPwd(regAccField, regPwdField1)) return;

        if (!IsPasswordMatch(regPwdField1.value, regPwdField2.value))
        {
            Toast("两次输入的密码不一致");
            Focus(regPwdField2);
            return;
        }
        string acc = regAccField.value.Trim();
        string pwd = regPwdField1.value;

        LoadingPanelManager.Instance.Show();

        api.AccountRegister(
            acc, pwd,
            ok: json =>
            {
                LoadingPanelManager.Instance.Hide();
                Toast("注册成功！");
                Debug.Log($"注册成功: {json}");
            },
            fail: msg =>
            {
                LoadingPanelManager.Instance.Hide();
                if (msg.StartsWith("1"))
                {
                    Toast("用户名已存在，请换一个或者登录");
                    Debug.Log("返还代码1");
                }
                else
                    Toast($"注册失败: {msg}");

                Debug.LogError($"注册失败: {msg}");   // ← 用 Error / Warning，别再写“注册成功”
            });

        Debug.Log($"[Register] {regAccField.value}/{regPwdField1.value}");
        ClearAllInputs();
        // TODO: HTTP 注册请求…
    }

    /* ───── 通用校验 ───── */
    private bool CheckAccountAndPwd(TextField acc, TextField pwd)
    {
        if (acc == null || pwd == null)
        {
            Debug.LogError("账号或密码框未找到，请检查类名");
            return false;
        }

        if (!IsAccountValid(acc.value.Trim()))
        {
            Toast("账号需≥5位，仅限英文或数字");
            Focus(acc);
            return false;
        }

        if (!IsPasswordStrong(pwd.value))
        {
            Toast("密码需≥8位，并包含字母和数字");
            Focus(pwd);
            return false;
        }
        return true;
    }

    /* ───── 静态验证函数 ───── */
    private static bool IsAccountValid(string acc) =>
        Regex.IsMatch(acc, @"^[A-Za-z0-9]{5,}$");

    private static bool IsPasswordStrong(string pwd) =>
        Regex.IsMatch(pwd, @"^(?=.*\d)(?=.*[A-Za-z]).{8,}$");

    private static bool IsPasswordMatch(string a, string b) => a == b;

    private void Focus(TextField tf) =>
        tf?.schedule.Execute(() => tf.Focus()).ExecuteLater(0);

    private void ClearAllInputs()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.Query<TextField>(className: "textinput")
            .ForEach(tf => tf.value = string.Empty);
    }
    private void OnEmailCodeChanged(ChangeEvent<string> evt)
    {
        // 拿到触发本事件的那个输入框
        var field = (TextField)evt.target;

        // 1. 过滤掉非数字
        string cleaned = Regex.Replace(evt.newValue ?? string.Empty, @"\D", "");

        // 2. 超过 6 位就截断
        if (cleaned.Length > 6)
            cleaned = cleaned.Substring(0, 6);

        // 3. 如有变化再异步写回
        if (cleaned != evt.newValue)
        {
            field.schedule.Execute(() =>
            {
                field.SetValueWithoutNotify(cleaned);
                // 把光标放到末尾
                field.SelectRange(cleaned.Length, cleaned.Length);
            });
        }
    }

    /* ───── 邮箱验证码按钮 ───── */
    private void OnClickSendCode()
    {
        // 1) 取得邮箱输入
        string email = loginEmailField.value.Trim();          // ★ 你的邮箱输入框

        // 2) 基础校验：不能为空 & 格式合法
        if (string.IsNullOrEmpty(email) || !IsEmailValid(email))
        {
            Toast("请输入合法邮箱地址");
            Focus(loginEmailField);
            return;
        }

        // 3) 显示 Loading
        LoadingPanelManager.Instance.Show();                  // ★ 如你之前写的管理类

        // 4) 调服务器接口
        api.GetEmailCode(
            email,
            ok: _ =>
            {
                LoadingPanelManager.Instance.Hide();          // 关 Loading
                Toast("验证码已发送，请查收邮箱");

                // 5) 成功后才进入按钮冷却
                if (cooldownCO == null)
                    cooldownCO = StartCoroutine(ButtonCooldown(30));
            },
            fail: msg =>
            {
                LoadingPanelManager.Instance.Hide();          // 关 Loading
                Toast($"发送失败：{msg}");
            });
    }


    private IEnumerator ButtonCooldown(int seconds)
    {
        // 1. 禁用并统一初始样式
        foreach (var b in sendCodeBtn)
        {
            b.SetEnabled(false);
            b.style.opacity = 0.8f;        // 想要的置灰效果
        }

        // 2. 倒计时
        for (int t = seconds; t > 0; --t)
        {
            foreach (var b in sendCodeBtn)
                b.text = $"重新获取 ({t}s)";
            yield return new WaitForSeconds(1);
        }

        // 3. 恢复按钮
        foreach (var b in sendCodeBtn)
        {
            b.text = "获取验证码";
            b.style.opacity = 1f;
            b.SetEnabled(true);
        }

        cooldownCO = null;                 // 允许下一次点击重新进入冷却
    }
    /* ───── Toast 逻辑 ───── */
    private void Toast(string msg, float duration = 2f)
    {
        if (toastCO != null) StopCoroutine(toastCO);

        Debug.Log("开始弹窗");

        toastText.text = msg;
        toastPanel.AddToClassList("show");          // 显示（.show 提供 opacity 过渡）
        toastCO = StartCoroutine(HideLater(duration));
    }

    private IEnumerator HideLater(float t)
    {
        yield return new WaitForSeconds(t);
        toastPanel.RemoveFromClassList("show");     // 隐藏并淡出
        toastCO = null;
    }

    /* ── 邮箱格式检查 ── */
    private static bool IsEmailValid(string mail) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            mail, @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$");
    
    /* ───── 邮箱验证码登录 ───── */
    private void OnClickVerify()
    {
        string email = loginEmailField.value.Trim();       // 你的邮箱输入框
        string code  = verifyCodeField.value.Trim();       // 6 位验证码输入框

        /* 1. 本地校验 */
        if (!IsEmailValid(email))
        {
            Toast("请输入合法邮箱地址");
            Focus(loginEmailField);
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^\d{6}$"))
        {
            Toast("验证码需为 6 位数字");
            Focus(verifyCodeField);
            return;
        }

        /* 2. Loading + 请求 */
        LoadingPanelManager.Instance.Show();

        api.EmailLogin(
            email,
            int.Parse(code),
            ok: _ =>
            {
                LoadingPanelManager.Instance.Hide();
                Toast("登录成功！");
                Debug.Log("邮箱登录成功");
                ClearAllInputs();
            },
            fail: msg =>
            {
                LoadingPanelManager.Instance.Hide();
                Toast($"登录失败：{msg}");
                Debug.LogError($"邮箱登录失败：{msg}");
            });
    }


}
