/****************************************************************
 * SpinController.cs ― 统一管理 class="spin" 的小转圈
 *   • 默认隐藏（display:none）
 *   • Show(reset)  → 显示 + 可选重置角度 + 开始旋转
 *   • Hide()       → 隐藏 + 停止旋转
 ****************************************************************/
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public sealed class SpinController : MonoBehaviour
{
    public static SpinController Instance { get; private set; }

    [Tooltip("每秒旋转角度；正值顺时针，负值逆时针")]
    public float degreesPerSecond = 360f;

    private List<VisualElement> spins;
    private bool spinning;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        spins    = root.Query<VisualElement>(className:"spin").ToList();

        // 默认隐藏
        foreach (var ve in spins) ve.style.display = DisplayStyle.None;
        spinning = false;
    }

    void Update()
    {
        if (!spinning) return;

        float delta = degreesPerSecond * Time.deltaTime;
        foreach (var ve in spins)
            ve.transform.rotation *= Quaternion.Euler(0, 0, delta);
    }

    /*──────── 公共 API ────────*/
    /// <summary>
    /// 显示转圈；resetRot=true 时把角度归零
    /// </summary>
    public void Show(bool resetRot = true)
    {
        foreach (var ve in spins)
        {
            if (resetRot) ve.transform.rotation = Quaternion.identity;
            ve.style.display = DisplayStyle.Flex;
        }
        spinning = true;
    }

    public void Hide()
    {
        foreach (var ve in spins) ve.style.display = DisplayStyle.None;
        spinning = false;
    }
}
