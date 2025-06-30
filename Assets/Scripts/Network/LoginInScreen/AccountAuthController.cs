using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using static AuthAPI;
using UnityEngine.SceneManagement;


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
    private Button checkUserBtn;   // name="CheckUserBtn"

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
        var visitorApi = GetComponent<VisitorLoginRequest>();
        // 绑定所有带有 “visitorBtn” USS 类的按钮
        foreach (var btn in root.Query<Button>(className: "visitorbtn").ToList())
        {
            btn.clicked += visitorApi.SendVisitorPlay;
        }

        // 修改密码页面
            var pwd1Field = root.Q<TextField>("NewPwd1");
        var pwd2Field = root.Q<TextField>("NewPwd2");
        var changePwdBtn = root.Q<Button>("ChangePwdBtn");
        changePwdBtn.clicked += () => OnClickChangePwd(pwd1Field, pwd2Field);

        // 验证码


        var verifyCodeFields = root.Query<TextField>(className: "verifycode-input").ToList();
        foreach (var tf in verifyCodeFields)
        {
            // 2. 只允许 6 位
            tf.maxLength = 6;

            // 3. 输入实时过滤：只留数字，多余截掉
            tf.RegisterValueChangedCallback(OnEmailCodeChanged);
        }
        verifyCodeField = root.Q<TextField>(className: "verifycode-input");

        // ── 注册 / 邮箱登录页 ──
        var regEmailField = root.Q<TextField>("RegEmail");
        var regCodeField = root.Q<TextField>("RegCode");          // 验证码框
        var regSendBtn = root.Q<Button>("RegSendCodeBtn");
        var regVerifyBtn = root.Q<Button>("RegVerifyBtn");


        regSendBtn.clicked += () => OnClickSendCode(regEmailField, regSendBtn);
        regVerifyBtn.clicked += () => OnClickVerify(regEmailField, regCodeField);

        // ── 修改密码页 ──
        var rstEmailField = root.Q<TextField>("RstEmail");
        var rstCodeField = root.Q<TextField>("RstCode");
        var rstSendBtn = root.Q<Button>("RstSendCodeBtn");
        var rstVerifyBtn = root.Q<Button>("RstVerifyBtn");

        rstSendBtn.clicked += () => OnClickSendCode(rstEmailField, rstSendBtn);
        rstVerifyBtn.clicked += () => OnClickVerify(rstEmailField, rstCodeField);

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

        checkUserBtn = root.Q<Button>("checkUserBtn");  // 检查用户名按钮
        if (checkUserBtn != null) checkUserBtn.clicked += () => OnClickCheckUsername(regAccField);

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
                ServerResp data = JsonUtility.FromJson<ServerResp>(json);
                PlayerData.I.SetSession(
                    data.uuid,
                    data.user_token,
                    data.cuid,
                    data.character_token,
                    data.server_id,
                    data.server_ip_address,
                    data.server_port
                );
                PlayerData.I.Dump();
                LoadingPanelManager.Instance.Hide();
                Debug.Log("登陆成功");
                Toast("登陆成功！");
                JumpToMainUI(); // 切换到主场景

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
                JumpToMainUI(); // 切换到主场景
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

    private void OnClickCheckUsername(TextField userField)
    {
        string name = userField.value.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Toast("用户名不能为空");
            Focus(userField);
            return;
        }

        if (!IsAccountValid(userField.value.Trim()))
        {
            Toast("账号需≥5位，仅限英文或数字");
            Focus(userField);
            return;
        }

        LoadingPanelManager.Instance.Show();

        api.CheckUsername(
            name,
            ok: _ =>                       // ① code == 0 → 可用
            {
                LoadingPanelManager.Instance.Hide();
                Toast("用户名可以使用！");
            },
            fail: msg =>                   // ② 其它业务码 或 网络错误
            {
                LoadingPanelManager.Instance.Hide();

                // 服务器把业务错误也装在 fail 里，约定 code=1 表示已占用
                if (msg.StartsWith("1"))
                {
                    Toast("已被占用，请换一个");
                }
                else
                {
                    Debug.LogError(msg);   // 记录其它异常
                    Toast($"请求失败：{msg}");
                }
            });
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
            Toast("账号需≥5位<32位，仅限英文或数字");
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
        Regex.IsMatch(acc, @"^[A-Za-z0-9]{5,32}$");

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
    // 之前：private void OnClickSendCode()
    // 现在：
    private void OnClickSendCode(TextField emailField, Button senderBtn)
    {
        string email = emailField.value.Trim();

        if (string.IsNullOrEmpty(email) || !IsEmailValid(email))
        {
            Toast("请输入合法邮箱地址");
            Focus(emailField);
            return;
        }

        LoadingPanelManager.Instance.Show();

        api.GetEmailCode(
            email,
            ok: _ =>
            {
                LoadingPanelManager.Instance.Hide();
                Toast("验证码已发送，请查收邮箱");
                if (cooldownCO == null)
                    cooldownCO = StartCoroutine(ButtonCooldown(senderBtn, 30));
            },
            fail: msg =>
            {
                LoadingPanelManager.Instance.Hide();
                Toast($"发送失败：{msg}");
            });
    }



    private IEnumerator ButtonCooldown(Button btn, int seconds)
    {
        btn.SetEnabled(false);
        btn.style.opacity = 0.8f;

        for (int t = seconds; t > 0; --t)
        {
            btn.text = $"重新获取 ({t}s)";
            yield return new WaitForSeconds(1);
        }

        btn.text = "获取验证码";
        btn.style.opacity = 1f;
        btn.SetEnabled(true);
        cooldownCO = null;
    }

    /* ───── Toast 逻辑 ───── */
    public void Toast(string msg, float duration = 2f)
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
    private void OnClickVerify(TextField emailField, TextField codeField)
    {
        string email = emailField.value.Trim();
        string code = codeField.value.Trim();

        /* 1) 本地校验 */
        if (!IsEmailValid(email))
        {
            Toast("请输入合法邮箱地址");
            Focus(emailField);
            return;
        }

        if (!Regex.IsMatch(code, @"^\d{6}$"))
        {
            Toast("验证码需为 6 位数字");
            Focus(codeField);
            return;
        }

        /* 2) Loading + 请求 */
        LoadingPanelManager.Instance.Show();

        api.EmailLogin(
            email, code,
            ok: _ =>
            {
                LoadingPanelManager.Instance.Hide();

                /* === 根据邮箱框名字判断是哪个流程 === */
                if (emailField.name == "RstEmail")          // 来自找回密码页
                {
                    Toast("验证成功，请设置新密码");
                    // 跳转到修改密码面板
                    if (LoginUIManager.I != null)
                        LoginUIManager.I.ToChangePwPanel();
                }
                else                                        // 普通邮箱登录
                {
                    Toast("登录成功！");
                    JumpToMainUI(); // 切换到主场景
                    // TODO: 如果登录成功要切到主场景，在这里调用
                }

                ClearAllInputs();
            },
            fail: msg =>
            {
                LoadingPanelManager.Instance.Hide();
                Toast($"操作失败：{msg}");
            });
    }
    private void OnClickChangePwd(TextField pwd1, TextField pwd2)
    {
        string p1 = pwd1.value;
        string p2 = pwd2.value;

        /* 1) 本地校验 */
        if (!IsPasswordStrong(p1))
        {
            Toast("密码需≥8位，并包含字母和数字");
            Focus(pwd1);
            return;
        }
        if (!IsPasswordMatch(p1, p2))
        {
            Toast("两次输入的密码不一致");
            Focus(pwd2);
            return;
        }

        /* 2) TODO: 服务器接口 */
        LoadingPanelManager.Instance.Show();
        /*
        api.ChangePassword(
            p1,
            ok: _ => {
                LoadingPanelManager.Instance.Hide();
                Toast("修改成功！");
                ClearAllInputs();
            },
            fail: msg => {
                LoadingPanelManager.Instance.Hide();
                Toast($"修改失败: {msg}");
            });
        */

        // 目前接口未完成，先本地提示
        LoadingPanelManager.Instance.Hide();
        Toast("（示例）本地校验通过，待接入 API");
    }
    private void JumpToMainUI()
    {
        Destroy(gameObject);
        SceneManager.LoadScene("MainUI", LoadSceneMode.Single);
    }

}
