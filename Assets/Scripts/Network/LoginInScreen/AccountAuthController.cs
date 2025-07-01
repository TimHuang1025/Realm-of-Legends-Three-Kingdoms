/****************************************************************
 * AccountAuthController.cs — 登录 / 注册 / 游客 / 邮箱 / 改密
 * UI Toolkit 版（含错误提示优化）
 ****************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static AuthAPI;

[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(AuthAPI))]
public sealed class AccountAuthController : MonoBehaviour
{
    /*──────── 依赖引用 ────────*/
    private AuthAPI             api;
    private VisitorLoginRequest visitorApi;

    /*──────── UI：错误提示 Label ────────*/
    private Label         hinterror;          // 账号
    private Label         hinterrorpwd;       // 密码
    private List<Label>   hinterroremail;     // 邮箱（同类多个）
    private List<Label>   hinterroremailcode; // 邮箱验证码（同类多个）// 成功提示用的绿色
    private static readonly Color okColor = new(0.32f, 0.65f, 0.53f, 1f);
    private string lastCheckedName   = null; // 上一次查询的名字
    private bool? lastNameIsUsable = null;  // 上一次结果：true 可用；false 占用



    /*──────── UI：登录 / 注册 / 邮箱 ────────*/
    private TextField loginAccField, loginPwdField;
    private TextField regAccField,   regPwdField1, regPwdField2;
    private TextField verifyCodeField;
    private Button    loginBtn, registerBtn, checkUserBtn;

    /*──────── UI：邮箱验证码 ────────*/
    private Coroutine cooldownCO;

    /*──────── UI：Toast ────────*/
    private VisualElement toastPanel;
    private Label         toastText;
    private Coroutine     toastCO;

    /*───────────────────────────────────────────
     * Awake — 统一绑定
     *──────────────────────────────────────────*/
    void Awake()
    {
        var root   = GetComponent<UIDocument>().rootVisualElement;
        api        = GetComponent<AuthAPI>();
        visitorApi = GetComponent<VisitorLoginRequest>();

        /*── 错误 Label ──*/
        hinterror       = root.Q<Label>("hinterror");
        hinterrorpwd    = root.Q<Label>("hinterrorpwd");
        hinterroremail  = root.Query<Label>(className:"hinterroremail").ToList();
        hinterroremailcode = root.Query<Label>(className:"hinterroremailcode").ToList();

        /*── 游客按钮 ──*/
        root.Query<Button>(className:"visitorbtn")
            .ForEach(b => b.clicked += visitorApi.SendVisitorPlay);

        /*── 修改密码面板 ──*/
        var pwd1Field = root.Q<TextField>("NewPwd1");
        var pwd2Field = root.Q<TextField>("NewPwd2");
        root.Q<Button>("ChangePwdBtn").clicked += () => OnClickChangePwd(pwd1Field, pwd2Field);

        /*── 验证码框过滤 ──*/
        root.Query<TextField>(className:"verifycode-input").ForEach(tf =>
        {
            tf.maxLength = 6;
            tf.RegisterValueChangedCallback(OnEmailCodeChanged);
        });
        verifyCodeField = root.Q<TextField>(className:"verifycode-input");

        /*── 邮箱验证码绑定 ──*/
        BindEmailVerification(root);

        /*── 登录区 ──*/
        loginAccField = root.Q<TextField>(className:"account-input");
        loginPwdField = root.Q<TextField>(className:"pwd-input");
        loginBtn      = root.Q<Button>("LoginBtn");
        if (loginBtn != null)
            loginBtn.clicked += () =>
            {
                ResetErrorTexts();
                OnClickLogin();
            };

        /*── 注册区 ──*/
        regAccField  = root.Q<TextField>("regAccField");
        regPwdField1 = root.Q<TextField>("regPwdField1");
        regPwdField2 = root.Q<TextField>("regPwdField2");
        registerBtn  = root.Q<Button>("RegisterBtn");
        if (registerBtn != null)
            registerBtn.clicked += () =>
            {
                ResetErrorTexts();
                OnClickRegister();
            };

        checkUserBtn = root.Q<Button>("checkUserBtn");
        if (checkUserBtn != null)
            checkUserBtn.clicked += () =>
            {
                ResetErrorTexts();
                OnClickCheckUsername(regAccField);
            };

        /*── 返回按钮：清空输入 ──*/
        root.Query<Button>(className:"return-btn")
            .ForEach(b => b.clicked += ClearAllInputs);

        /*── Toast ──*/
        toastPanel = root.Q<VisualElement>("ToastPanel");
        toastText  = root.Q<Label>("ToastText");
    }

    /*───────────────────────────────────────────
     * 登录
     *──────────────────────────────────────────*/
    private void OnClickLogin()
    {
        if (!CheckAccountAndPwd(loginAccField, loginPwdField)) return;

        LoadingPanelManager.Instance.Show();

        api.PasswordLogin(
            loginAccField.value.Trim(),
            loginPwdField.value,
            ok: json =>
            {
                SaveSession(json);
                LoadingPanelManager.Instance.Hide();
                Toast("登陆成功！");
                JumpToMainUI();
            },
            fail: msg =>
            {
                LoadingPanelManager.Instance.Hide();
                PopupManager.Show("登录失败", msg);
            });

        ClearAllInputs();
    }

    /*───────────────────────────────────────────
     * 注册
     *──────────────────────────────────────────*/
    private void OnClickRegister()
    {
        if (!CheckAccountAndPwd(regAccField, regPwdField1)) return;

        if (!IsPasswordMatch(regPwdField1.value, regPwdField2.value))
        {
            ShowFieldError(hinterrorpwd, "两次输入的密码不一致", regPwdField2);
            return;
        }

        string nameTrying = regAccField.value.Trim();

        /*––––– 本地拦截：上次已判定为占用，就不再请求 –––––*/
        // ① 改写 if 块
        if (nameTrying == lastCheckedName && lastNameIsUsable == false)
        {
            StartCoroutine(SpinAndTip("已被占用，请换一个或登录", false, regAccField));
            return;
        }

        // ② 共用协程：可同时处理 OK / Error
        IEnumerator SpinAndTip(string msg, bool isOk, TextField focus)
        {
            SpinController.Instance.Show();
            yield return new WaitForSeconds(Random.Range(0.05f, 0.5f));
            SpinController.Instance.Hide();

            if (isOk)
                ShowFieldOk(hinterror, msg, focus);
            else
                ShowFieldError(hinterror, msg, focus);
        }


        /*––––– 正式发注册请求 –––––*/
        registerBtn?.SetEnabled(false);
        SpinController.Instance.Show();

        api.AccountRegister(
            nameTrying,
            regPwdField1.value,
            ok: _ =>
            {
                SpinController.Instance.Hide();
                registerBtn?.SetEnabled(true);

                Toast("注册成功！");
                JumpToMainUI();
            },
            fail: msg =>
            {
                SpinController.Instance.Hide();
                registerBtn?.SetEnabled(true);

                if (msg.StartsWith("1"))
                {
                    // 记录缓存：占用
                    lastCheckedName  = nameTrying;
                    lastNameIsUsable = false;

                    ShowFieldError(hinterror, "用户名已存在，请换一个或登录", regAccField);
                }
                else
                {
                    lastNameIsUsable = null;    // 其它错误不缓存
                    PopupManager.Show("注册失败", msg);
                }
            });
    }

    /*───────────────────────────────────────────
     * 检查用户名
     *──────────────────────────────────────────*/
    private void OnClickCheckUsername(TextField userField)
{
    string name = userField.value.Trim();

    // —— 基本本地校验 ——
    if (string.IsNullOrEmpty(name))
    {
        ShowFieldError(hinterror, "用户名不能为空", userField);
        return;
    }
    if (!IsAccountValid(name))
    {
        ShowFieldError(hinterror, "账号需≥5位，仅限英文或数字", userField);
        return;
    }

    // —— 本地缓存命中：直接提示，不发请求 ——
    if (name == lastCheckedName && lastNameIsUsable.HasValue)
    {
        // 先转圈 0.5 秒，然后再提示
        StartCoroutine(SpinHalfSecondAndTip(lastNameIsUsable.Value, userField));
        return;
    }

    /*───────────────────────────────────────────
    * 协程：转圈 0.5 秒后显示 OK / Error
    *──────────────────────────────────────────*/
    IEnumerator SpinHalfSecondAndTip(bool usable, TextField field)
    {
        SpinController.Instance.Show();
        yield return new WaitForSeconds(Random.Range(0.05f, 0.5f));
        SpinController.Instance.Hide();

        if (usable)
            ShowFieldOk(hinterror, "用户名可以使用！", field);
        else
            ShowFieldError(hinterror, "已被占用，请换一个", field);
    }

    // —— 需要重新访问服务器 ——
    SpinController.Instance.Show();

    api.CheckUsername(
        name,
        ok: _ =>
        {
            SpinController.Instance.Hide();

            // 记录缓存
            lastCheckedName   = name;
            lastNameIsUsable  = true;

            ShowFieldOk(hinterror, "用户名可以使用！", userField);
        },
        fail: msg =>
        {
            SpinController.Instance.Hide();

            // 记录缓存 (只有业务码 1 时才记为占用)
            if (msg.StartsWith("1"))
            {
                lastCheckedName  = name;
                lastNameIsUsable = false;

                ShowFieldError(hinterror, "已被占用，请换一个", userField);
            }
            else
            {
                lastNameIsUsable = null;  // 不缓存未知错误
                PopupManager.Show("请求失败", msg);
            }
        });
}


    /*───────────────────────────────────────────
     * 发送邮箱验证码 & 校验
     *──────────────────────────────────────────*/
    private void BindEmailVerification(VisualElement root)
    {
        var regEmailField = root.Q<TextField>("RegEmail");
        var regCodeField  = root.Q<TextField>("RegCode");
        root.Q<Button>("RegSendCodeBtn").clicked   += () => SendEmailCode(regEmailField, root.Q<Button>("RegSendCodeBtn"));
        root.Q<Button>("RegVerifyBtn").clicked     += () => OnClickVerify(regEmailField, regCodeField);

        var rstEmailField = root.Q<TextField>("RstEmail");
        var rstCodeField  = root.Q<TextField>("RstCode");
        root.Q<Button>("RstSendCodeBtn").clicked   += () => SendEmailCode(rstEmailField, root.Q<Button>("RstSendCodeBtn"));
        root.Q<Button>("RstVerifyBtn").clicked     += () => OnClickVerify(rstEmailField, rstCodeField);
    }

    private void SendEmailCode(TextField emailField, Button senderBtn)
    {
        string email = emailField.value.Trim();
        if (!IsEmailValid(email))
        {
            ShowFieldError(hinterroremail, "请输入合法邮箱地址", emailField);
            return;
        }

        SpinController.Instance.Show();

        api.GetEmailCode(
            email,
            ok: _ =>
            {
                SpinController.Instance.Hide();
                ShowFieldOk(hinterroremail, "验证码已发送，请查收邮箱", emailField);
                if (cooldownCO == null)
                    cooldownCO = StartCoroutine(ButtonCooldown(senderBtn, 30));
            },
            fail: msg =>
            {
                SpinController.Instance.Hide();
                PopupManager.Show("发送失败", msg);
            });
    }

    private void OnClickVerify(TextField emailField, TextField codeField)
    {
        string email = emailField.value.Trim();
        string code  = codeField.value.Trim();

        if (!IsEmailValid(email))
        {
            ShowFieldError(hinterroremail, "请输入合法邮箱地址", emailField);
            return;
        }
        if (!Regex.IsMatch(code, @"^\d{6}$"))
        {
            ShowFieldError(hinterroremailcode, "验证码需为 6 位数字", codeField);
            return;
        }

        SpinController.Instance.Show();

        api.EmailLogin(
            email, code,
            ok: _ =>
            {
                SpinController.Instance.Hide();

                if (emailField.name == "RstEmail")
                {
                    Toast("验证成功，请设置新密码");
                    LoginUIManager.I?.ToChangePwPanel();
                }
                else
                {
                    Toast("登录成功！");
                    JumpToMainUI();
                }
                ClearAllInputs();
            },
            fail: msg =>
            {
                SpinController.Instance.Hide();
                PopupManager.Show("操作失败", msg);
            });
    }

    /*───────────────────────────────────────────
     * 修改密码（示例）
     *──────────────────────────────────────────*/
    private void OnClickChangePwd(TextField pwd1, TextField pwd2)
    {
        string p1 = pwd1.value;
        string p2 = pwd2.value;

        if (!IsPasswordStrong(p1))
        {
            PopupManager.Show("提示", "密码需≥8位，并包含字母和数字");
            Focus(pwd1);
            return;
        }
        if (!IsPasswordMatch(p1, p2))
        {
            PopupManager.Show("提示", "两次输入的密码不一致");
            Focus(pwd2);
            return;
        }

        LoadingPanelManager.Instance.Show();
        LoadingPanelManager.Instance.Hide();
        Toast("（示例）本地校验通过，待接入 API");
    }

    /*───────────────────────────────────────────
     * 共用工具
     *──────────────────────────────────────────*/
    private void SaveSession(string json)
    {
        var d = JsonUtility.FromJson<ServerResp>(json);
        PlayerData.I.SetSession(d.uid, d.user_token, d.cid, d.character_token,
                                d.server_id, d.server_ip_address, d.server_port);
        PlayerData.I.Dump();
    }

    private void ResetErrorTexts()
    {
        hinterror.text     = " ";
        hinterrorpwd.text  = " ";
        foreach (var l in hinterroremail)     l.text = " ";
        foreach (var l in hinterroremailcode) l.text = " ";
    }

// 成功提示用的绿色

/*── 单 Label : Error ──*/
private void ShowFieldError(Label lbl, string msg, TextField focus = null)
{
    lbl.text = msg;
    lbl.style.color = Color.red;   // ❗ 若已在 USS 设红色，可删此行
}

/*── 单 Label : OK ──*/
private void ShowFieldOk(Label lbl, string msg, TextField focus = null)
{
    lbl.text = msg;
    lbl.style.color = okColor;
}

/*── 多 Label : Error ──*/
private void ShowFieldError(IEnumerable<Label> lbls, string msg, TextField focus = null)
{
    foreach (var l in lbls)
    {
        l.text = msg;
        l.style.color = Color.red;
    }
}

/*── 多 Label : OK ──*/
private void ShowFieldOk(IEnumerable<Label> lbls, string msg, TextField focus = null)
{
    foreach (var l in lbls)
    {
        l.text = msg;
        l.style.color = okColor;
    }
}


    private bool CheckAccountAndPwd(TextField acc, TextField pwd)
    {
        if (!IsAccountValid(acc.value.Trim()))
        {
            ShowFieldError(hinterror, "账号需≥5位<32位，仅限英文或数字", acc);
            return false;
        }
        if (!IsPasswordStrong(pwd.value))
        {
            ShowFieldError(hinterrorpwd, "密码需≥8位，并包含字母和数字", pwd);
            return false;
        }
        return true;
    }

    private static bool IsAccountValid(string acc) =>
        Regex.IsMatch(acc, @"^[A-Za-z0-9]{5,32}$");

    private static bool IsPasswordStrong(string pwd) =>
        Regex.IsMatch(pwd, @"^(?=.*\d)(?=.*[A-Za-z]).{8,}$");

    private static bool IsPasswordMatch(string a, string b) => a == b;

    private void Focus(TextField tf) =>
        tf?.schedule.Execute(() => tf.Focus()).ExecuteLater(0);

    private void ClearAllInputs()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.Query<TextField>(className:"textinput")
            .ForEach(tf => tf.value = string.Empty);
        ResetErrorTexts();
    }

    private void OnEmailCodeChanged(ChangeEvent<string> evt)
    {
        var field = (TextField)evt.target;
        string pure = Regex.Replace(evt.newValue ?? string.Empty, @"\D", "");
        if (pure.Length > 6) pure = pure[..6];

        if (pure != evt.newValue)
        {
            field.schedule.Execute(() =>
            {
                field.SetValueWithoutNotify(pure);
                field.SelectRange(pure.Length, pure.Length);
            });
        }
    }

    private IEnumerator ButtonCooldown(Button btn, int sec)
    {
        btn.SetEnabled(false);
        btn.style.opacity = 0.8f;
        for (int t = sec; t > 0; --t)
        {
            btn.text = $"重新获取 ({t}s)";
            yield return new WaitForSeconds(1);
        }
        btn.text = "获取验证码";
        btn.style.opacity = 1f;
        btn.SetEnabled(true);
        cooldownCO = null;
    }

    public void Toast(string msg, float dur = 2f)
    {
        if (toastCO != null) StopCoroutine(toastCO);
        toastText.text = msg;
        toastPanel.AddToClassList("show");
        toastCO = StartCoroutine(HideLater(dur));
    }

    private IEnumerator HideLater(float t)
    {
        yield return new WaitForSeconds(t);
        toastPanel.RemoveFromClassList("show");
        toastCO = null;
    }

    private static bool IsEmailValid(string mail) =>
        Regex.IsMatch(mail, @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$");

    /*──────── 场景切换 ────────*/
    private void JumpToMainUI()
    {
        Destroy(gameObject);
        SceneManager.LoadScene("MainUI", LoadSceneMode.Single);
    }
}
