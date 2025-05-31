using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CardInventoryUI : MonoBehaviour
{
    /*──────── Inspector 拖入 ────────*/
    [SerializeField] private UpgradePanelController upgradePanelCtrl;   // 升级面板控制脚本
    [SerializeField] private VhSizer vhSizer;                           // 全局/主界面的 VhSizer

    /*──────── 私有字段 ────────*/
    private VisualElement cardsVe;   // 卡片列表容器
    private VisualElement infoVe;    // 详情面板容器

    private Button returnBtn;
    private Button upgradeBtn;
    private Button infobtn;           // 打开 Info 的按钮
    private Button closeInfoBtn;      // 关闭 Info → 返回卡片界面

    /*──────── Awake ────────*/
    void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        /*—— 缓存 Cards / Info 容器 ——*/
        cardsVe = root.Q<VisualElement>("Cards");
        infoVe  = root.Q<VisualElement>("Info");

        if (cardsVe == null)
            Debug.LogError("[CardInventoryUI] 找不到 <VisualElement name=\"Cards\">");
        if (infoVe == null)
            Debug.LogError("[CardInventoryUI] 找不到 <VisualElement name=\"Info\">");

        // 默认显示 Cards，隐藏 Info
        if (infoVe != null)
            infoVe.style.display = DisplayStyle.None;

        /*—— 返回按钮 ——*/
        returnBtn = root.Q<Button>("ReturnBtn");
        if (returnBtn == null)
            Debug.LogError("[CardInventoryUI] 找不到 <Button name=\"ReturnBtn\">");
        else
            returnBtn.clicked += OnClickReturn;

        /*—— 升级按钮 ——*/
        upgradeBtn = root.Q<Button>("InfoUpgradeBtn");
        if (upgradeBtn == null)
            Debug.LogError("[CardInventoryUI] 找不到 <Button name=\"UpgradeButton\">");
        else
            upgradeBtn.clicked += OpenUpgradePanel;

        /*—— 打开 Info 按钮 ——*/
        infobtn = root.Q<Button>("InfoBtn");
        if (infobtn == null)
            Debug.LogError("[CardInventoryUI] 找不到 <Button name=\"InfoBtn\">");
        else
            infobtn.clicked += OpenInfoPanel;

        /*—— 关闭 Info（返回卡片）按钮 ——*/
        closeInfoBtn = root.Q<Button>("ClosePanelForInfo");
        if (closeInfoBtn == null)
            Debug.LogError("[CardInventoryUI] 找不到 <Button name=\"ClosePanelForInfo\">");
        else
            closeInfoBtn.clicked += CloseInfoPanel;
    }

    /*──────── 返回主界面 ────────*/
    void OnClickReturn() => StartCoroutine(ReturnRoutine());

    IEnumerator ReturnRoutine()
    {
        LoadingPanelManager.Instance.Show();
        yield return null;                            // 让 Loading 先渲染
        yield return new WaitForSecondsRealtime(0f);

        // ❗不想销毁缩放脚本可改成 SetActive(false)
        gameObject.SetActive(false);                  // or Destroy(gameObject);

        var op = SceneManager.LoadSceneAsync("MainUI", LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        LoadingPanelManager.Instance.Hide();
    }

    /*──────── 打开升级面板 ────────*/
    void OpenUpgradePanel() => StartCoroutine(OpenUpgradePanelRefresh());

    IEnumerator OpenUpgradePanelRefresh()
    {
        if (upgradePanelCtrl == null)
        {
            Debug.LogError("[CardInventoryUI] upgradePanelCtrl 未赋值！");
            yield break;
        }

        upgradePanelCtrl.Open();          // 激活面板
        yield return null;                // 等 1 帧布局

        if (vhSizer != null)
            vhSizer.Apply();              // 统一刷新一次
    }

    /*──────── 打开 Info 面板 ────────*/
    void OpenInfoPanel()
    {
        if (cardsVe == null || infoVe == null)
        {
            Debug.LogError("[CardInventoryUI] Cards / Info 容器未找到，无法切换面板！");
            return;
        }

        cardsVe.style.display = DisplayStyle.None;   // 隐藏卡片列表
        infoVe.style.display  = DisplayStyle.Flex;   // 显示详情面板

        // Info 面板若需要适配刷新
        if (vhSizer != null)
            vhSizer.Apply();
    }

    /*──────── 关闭 Info 面板（返回卡片列表） ────────*/
    void CloseInfoPanel()
    {
        if (cardsVe == null || infoVe == null) return;

        infoVe.style.display  = DisplayStyle.None;   // 隐藏详情面板
        cardsVe.style.display = DisplayStyle.Flex;   // 显示卡片列表

        if (vhSizer != null)
            vhSizer.Apply();
    }
}
