using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CardInventoryUI : MonoBehaviour
{
    /*──────── Inspector 拖入 ────────*/
    [SerializeField] UpgradePanelController upgradePanelCtrl;   // 升级面板控制脚本
    [SerializeField] VhSizer                vhSizer;            // 全局/主界面的 VhSizer

    /*──────── 私有字段 ────────*/
    Button returnBtn;
    Button upgradeBtn;

    void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        /*—— 返回按钮 ——*/
        returnBtn = root.Q<Button>("ReturnBtn");
        if (returnBtn == null)
            Debug.LogError("[CardInventoryUI] 找不到 <Button name=\"ReturnBtn\">");
        else
            returnBtn.clicked += OnClickReturn;

        /*—— 升级按钮 ——*/
        upgradeBtn = root.Q<Button>("UpgradeButton");
        if (upgradeBtn == null)
            Debug.LogError("[CardInventoryUI] 找不到 <Button name=\"UpgradeButton\">");
        else
            upgradeBtn.clicked += OpenUpgradePanel;
    }

    /*──────── 返回主界面 ────────*/
    void OnClickReturn() => StartCoroutine(ReturnRoutine());

    IEnumerator ReturnRoutine()
    {
        LoadingPanelManager.Instance.Show();
        yield return null;                            // 让 Loading 先渲染
        yield return new WaitForSecondsRealtime(1f);

        // ❗如果不想把缩放脚本也销毁，改成 SetActive(false)
        gameObject.SetActive(false);                 // or Destroy(gameObject);

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
}
