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

    /* ───── 生命周期 ───── */
    void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        api = GetComponent<AuthAPI>();
        Button visitorBtn = root.Q<Button>("GuestLogo");
        var visitorapi = GetComponent<VisitorLoginRequest>();
        visitorBtn.clicked += visitorapi.SendVisitorPlay;

        sendCodeBtn = root.Query<Button>(className: "getemailcode").ToList(); //接收验证码按钮
        foreach (var tf in sendCodeBtn)
            tf.clicked += OnClickSendCode;

        var emailCodeField = root.Query<TextField>(className: "verifycode").ToList();
        foreach (var tf in emailCodeField)
        {
            // 2. 只允许 6 位
            tf.maxLength = 6;

            // 3. 输入实时过滤：只留数字，多余截掉
            tf.RegisterValueChangedCallback(OnEmailCodeChanged);
        }

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

    }

    /* ───── 登录 ───── */
    private void OnClickLogin()
    {
        if (!CheckAccountAndPwd(loginAccField, loginPwdField)) return;

        Debug.Log($"[Login] {loginAccField.value}/{loginPwdField.value}");
        string acc = loginAccField.value.Trim();
        string pwd = loginPwdField.value;

        api.AccountLogin(
            acc,
            pwd,
            onSuccess: json =>
            {
                Debug.Log("登陆成功");
            },
            onFail: msg =>
            {
                Debug.LogError($"登录失败：{msg}");
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
            Toast("账号需≥3位，仅限英文或数字");
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
        Regex.IsMatch(acc, @"^[A-Za-z0-9]{3,}$");

    private static bool IsPasswordStrong(string pwd) =>
        Regex.IsMatch(pwd, @"^(?=.*\d)(?=.*[A-Za-z]).{8,}$");

    private static bool IsPasswordMatch(string a, string b) => a == b;

    /* ───── 工具 ───── */
    private void Toast(string msg) => Debug.LogWarning(msg);

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
        // 1. 过滤掉非数字
        string cleaned = Regex.Replace(evt.newValue, @"\D", "");

        // 2. 超过 6 位就截断
        if (cleaned.Length > 6)
            cleaned = cleaned.Substring(0, 6);

        // 3. 如果实际有变化，再异步写回
        if (cleaned != evt.newValue)
        {
            // 推迟到当前事件循环结束后再写，避免内部索引错位
            emailCodeField.schedule.Execute(() =>
            {
                emailCodeField.SetValueWithoutNotify(cleaned);

                // 可选：把光标放到末尾，用户体验更自然
                emailCodeField.SelectRange(cleaned.Length, cleaned.Length);
            });
        }
    }

    private void OnClickSendCode()
    {
        //发送验证码
        if (cooldownCO == null)
            cooldownCO = StartCoroutine(ButtonCooldown(8));
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
}
