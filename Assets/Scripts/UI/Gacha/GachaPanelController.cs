using UnityEngine;
using UnityEngine.UIElements;
using Kamgam.UIToolkitScrollViewPro;
using System;




[RequireComponent(typeof(UIDocument))]
public class GachaPanelController : MonoBehaviour
{
    /*──── 1. Inspector 资源 ────*/
    [Header("UI 文件 / 数据")]
    [SerializeField] private VisualTreeAsset  poolTemplate;
    [SerializeField] private GachaPoolDatabase poolDatabase;
    [SerializeField] private PlayerBaseController playerBaseController;
    Action<ResourceType> bankHandler;  // 监听 PlayerResourceBank onBankChanged

    /*──── 2. 固定节点名 ────*/
    const string kScrollName = "GachaPoolScroll";
    const string kOneRollBtn = "oneroll";
    const string kTenRollBtn = "tenrolls";
    const string kPoolBtn    = "PoolBtn";
    const string freeticketsbtn   = "freeticketsbtn";  // 卡池条目容器
    const string kPoolTitle = "PoolTitle";
    private Button returnBtn;

    /*──── 3. 运行时引用 ────*/
    ScrollViewPro scroll;
    VisualElement listRoot, selectedItem;
    Button oneBtn, tenBtn;
    Label ticketLbl;
    System.Action<int> handler;

    /*──── 4. 生命周期 ────*/
    void OnEnable()
    {

        if (poolTemplate == null || poolDatabase == null)
        {
            Debug.LogWarning("[Gacha] poolTemplate 或 poolDatabase 没拖到 Inspector！");
            return;
        }

        var root = GetComponent<UIDocument>().rootVisualElement;
        Button freeBtn = root.Q<Button>(freeticketsbtn);
        freeBtn.clicked += () =>
        {
            PlayerResourceBank.I.Add(ResourceType.SummonWrit, 5000);      
            Debug.Log("赠送 5000 Ticket");
        };
        bankHandler = t =>
        {
        if (t == ResourceType.SummonWrit && ticketLbl != null)
            ticketLbl.text = PlayerResourceBank.I[ResourceType.SummonWrit].ToString();
        };
        PlayerResourceBank.I.onBankChanged += bankHandler;

        returnBtn = root.Q<Button>("ReturnBtn");
        if (returnBtn != null) returnBtn.clicked += () => playerBaseController?.HideGachaPage();

        /*── Ticket Label ─*/
        ticketLbl = root.Q<Label>("TicketAmount");
        if (ticketLbl != null)
            ticketLbl.text = PlayerResourceBank.I[ResourceType.SummonWrit].ToString();

        handler = v => { if (ticketLbl != null) ticketLbl.text = v.ToString(); };
        GachaTicketManager.I.OnTicketChanged += handler;

        /*── Buttons ─*/
        oneBtn = root.Q<Button>(kOneRollBtn);
        tenBtn = root.Q<Button>(kTenRollBtn);
        if (oneBtn != null) oneBtn.clicked += () => Roll(1);
        if (tenBtn != null) tenBtn.clicked += () => Roll(10);

        /*── ScrollViewPro ─*/
        scroll = root.Q<ScrollViewPro>(kScrollName);
        Debug.Log($"[Gacha] Scroll {(scroll!=null?"√":"NULL")}");
        if (scroll == null)
        {
            scroll = new ScrollViewPro { name = kScrollName };
            root.Add(scroll);
        }
        scroll.mode = ScrollViewMode.Horizontal;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
        scroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        listRoot = scroll.contentContainer;
        listRoot.style.flexDirection = FlexDirection.Row;

        BuildPoolList();
        FocusFirst();
    }

    void OnDisable()
    {
        if (bankHandler != null && PlayerResourceBank.I != null)
            PlayerResourceBank.I.onBankChanged -= bankHandler;
    }

    /*──── 5. 生成池子 ────*/
    void BuildPoolList()
    {
        listRoot.Clear();

        const float poolW = 500f;   // 卡池条目宽
        const float poolH = 420f;   // 卡池条目高
        const float gap   = 32f;    // 条目之间的横向间距

        foreach (var info in poolDatabase.pools)
        {
            var container = poolTemplate.Instantiate();
            container.userData = info;

            var dim = container.Q<VisualElement>("BlackDim");
            if (dim != null)
            {
                dim.style.display = DisplayStyle.None; // 选中前隐藏
                dim.style.opacity = 0.9f;
            }


            /* ★★ 关键尺寸设置 ★★ */
            container.style.width       = poolW;
            container.style.height      = poolH;
            container.style.flexBasis   = poolW;     // 防止被压缩
            container.style.flexShrink  = 0;         // 同上
            container.style.marginRight = gap;       // 间距

            var poolBtn  = container.Q<Button>(kPoolBtn);
            var titleLbl = container.Q<Label>(kPoolTitle);

            if (titleLbl != null) titleLbl.text = info.title;
            if (poolBtn  != null && info.banner != null)
                poolBtn.style.backgroundImage = new StyleBackground(info.banner);

            VisualElement clickTarget = poolBtn != null ? (VisualElement)poolBtn : container;
            clickTarget.RegisterCallback<ClickEvent>(_ => SelectPool(container));

            AttachCountdown(container, info.showCountdown, info.expireSeconds);


            listRoot.Add(container);
        }

        scroll.RefreshAfterHierarchyChange();
    }

    void AttachCountdown(VisualElement container, bool enable, int secondsLeft)
    {
        var label = container.Q<Label>("RemainLbl");
        if (label == null) return;

        // 条件不满足 → 直接隐藏标签
        if (!enable || secondsLeft <= 0)
        {
            label.style.display = DisplayStyle.None;
            return;
        }

        // 计算结束时间
        var endAt = System.DateTime.UtcNow.AddSeconds(secondsLeft);

        void Refresh()
        {
            var left = endAt - System.DateTime.UtcNow;
            if (left <= System.TimeSpan.Zero)
            {
                label.text = "已结束";
                return;                         // 停止调度
            }

            label.text = $"剩余 {left:dd\\天hh\\:mm\\:ss}";
            label.schedule.Execute(Refresh).ExecuteLater(1000);
        }
        Refresh();
    }


    /*──── 6. 选中 ────*/
    void SelectPool(VisualElement item)
    {
        if (selectedItem == item) return;
        selectedItem?.RemoveFromClassList("selected");
        selectedItem = item;
        selectedItem.AddToClassList("selected");
        foreach (var child in listRoot.Children())
        {
            var dim = child.Q<VisualElement>("BlackDim");
            if (dim == null) continue;

            bool isSelected = child == item;
            dim.style.display = isSelected ? DisplayStyle.None : DisplayStyle.Flex;
        }

        if (item.userData is GachaPoolInfo g)
        {
            if (oneBtn != null) oneBtn.text = $"1x ({g.costx1})";
            if (tenBtn != null) tenBtn.text = $"10x ({g.costx10})";
        }
        scroll.ScrollTo(item);
    }

    /*──── 7. 抽卡 ────*/
    void Roll(int count)
    {
        if (selectedItem?.userData is not GachaPoolInfo info) return;

        int cost = (count == 1) ? info.costx1 : info.costx10;
        if (PlayerResourceBank.I[ResourceType.SummonWrit] < cost)
        {
            Debug.Log("Ticket 不够！");
            PopupManager.Show("抽奖失败", $"Ticket 不够！需要 {cost} 张 Ticket。");
            return;
        }
        PlayerResourceBank.I.Spend(ResourceType.SummonWrit, cost);

        // ★ 抽奖
        var results = GachaSystem.Roll(info, count);
        Debug.Log($"Roll {count} 次 → {string.Join(", ", results)}");
        PopupManager.Show("抽奖结果：",$"{string.Join("\n", results)}");

        // TODO：将 results 加到玩家背包 / 弹结果面板
    }

    /*──── 8. 聚焦首池 ────*/
    void FocusFirst()
    {
        if (listRoot.childCount == 0) return;
        var first = listRoot[0];
        (first.Q<Button>(kPoolBtn) ?? first).Focus();
        SelectPool(first);
    }
}
