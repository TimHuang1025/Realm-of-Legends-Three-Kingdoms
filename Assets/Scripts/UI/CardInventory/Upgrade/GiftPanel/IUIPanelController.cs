using UnityEngine;

public interface IUIPanelController
{
    void Open(CardInfoStatic info, PlayerCard dyn);
}

public class PanelHub : MonoBehaviour
{
    [SerializeField] UpgradePanelController upgradePanel;
    [SerializeField] UptierPanelController  uptierPanel;
    [SerializeField] GiftPanelController    giftPanel;

    // 也可以用 Dictionary<string, IUIPanelController> 动态收集

    public void OpenUpgrade(CardInfoStatic info, PlayerCard dyn)
        => OpenPanel(upgradePanel, info, dyn);

    public void OpenUptier(CardInfoStatic info, PlayerCard dyn)
        => OpenPanel(uptierPanel, info, dyn);

    public void OpenGift(CardInfoStatic info, PlayerCard dyn)
        => OpenPanel(giftPanel, info, dyn);

    /*—— 共用私有方法 ——*/
    void OpenPanel(IUIPanelController ctrl,
                   CardInfoStatic info, PlayerCard dyn)
    {
        if (ctrl == null) return;
        ctrl.Open(info, dyn);           // 统一接口
    }
}
