// ────────────────────────────────────────────────────────────────────────────────
// Assets/Scripts/UI/PlayerCard/SkillDetailPopupController.cs
// 固定 UIDocument 版本 + 升级事件回调（带 Ring 更新逻辑）
// ────────────────────────────────────────────────────────────────────────────────
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Game;


public class SkillDetailPopupController : MonoBehaviour
{
    /*──────── Inspector ────────*/
    [Header("UI")]
    [SerializeField] UIDocument uiDoc;          // 预放在场景
    [Tooltip("按 Lv0-4 顺序放入 5 张环图")]
    [SerializeField] Sprite[]   ringSprites;    // Lv0~4 环

    [Header("Data")]
    [SerializeField] PlayerLordCard       lord;
    [SerializeField] ActiveSkillDatabase  activeDB;
    [SerializeField] PassiveSkillDatabase passiveDB;

    /*──────── 运行缓存 ────────*/
    VisualElement root, iconImg, ringImg;
    Label nameLbl, typeLbl,
          beforeLvLbl, afterLvLbl,
          beforeTxtLbl, afterTxtLbl,
          ownArtLbl, needArtLbl;
    Button upBtn;

    readonly int[] COST = { 1, 2, 4, 6 };       // Lv0-3 升级消耗
    SkillSlot curSlot;
    object    curSkill;
    int       curLv;

    /* 事件：升级完毕通知外层页面刷新 */
    public event Action onUpgrade;

    /*──────────────────────── 生命周期 ────────────────────────*/
    void Awake()
    {
        root = uiDoc.rootVisualElement;
        CacheNodes();
        root.style.display = DisplayStyle.None;         // 默认隐藏
    }

    /*──────────────── 抓节点 ─────────────────*/
    void CacheNodes()
    {
        iconImg      = root.Q<VisualElement>("MainSkillImage");
        ringImg      = root.Q<VisualElement>("MainSkillRing");
        nameLbl      = root.Q<Label>("SkillNameLabel");
        typeLbl      = root.Q<Label>("SkillTypeLabel");
        beforeLvLbl  = root.Q<Label>("BeforeSkillLv");
        afterLvLbl   = root.Q<Label>("AfterSkillLv");
        beforeTxtLbl = root.Q<Label>("BeforeSkillText");
        afterTxtLbl  = root.Q<Label>("AfterSkillText");
        ownArtLbl    = root.Q<Label>("PlayerMaterial1Num");
        needArtLbl   = root.Q<Label>("UpgradeMaterial1Num");
        upBtn        = root.Q<Button>("UpgradeBtn");

        // 关闭
       var close1 = root.Q<Button>("ClosePanel");
        if (close1 != null) close1.clicked += Close;
        var close2 = root.Q<Button>("CloseBtn2");
        if (close2 != null) close2.clicked += Close;
        var mask = root.Q<VisualElement>("BlackSpace");
        if (mask != null) mask.RegisterCallback<ClickEvent>(_ => Close());

        upBtn.clicked += Upgrade;
    }

    /*──────── 外部呼叫 ────────*/
    public void Open(SkillSlot slot)
    {
        curSlot  = slot;
        curSkill = slot switch
        {
            SkillSlot.Active   => activeDB?.Get(lord.activeSkillId),
            SkillSlot.Passive1 => passiveDB?.Get(lord.passiveOneId),
            _                  => passiveDB?.Get(lord.passiveTwoId)
        };
        if (curSkill == null) { PopupManager.Show("提示", "技能数据缺失"); return; }

        curLv = Mathf.Clamp(PlayerPrefs.GetInt($"SKILL_LV_{Get<string>("id")}", 0), 0, 4);
        RefreshUI();
        root.BringToFront();
        root.style.display = DisplayStyle.Flex;
    }

    void Close() => root.style.display = DisplayStyle.None;

    /*──────────────── 刷新 UI ─────────────────*/
    void RefreshUI()
    {
        /*── 图标 & 名称 ──*/
        iconImg.style.backgroundImage = new StyleBackground(Get<Sprite>("iconSprite"));
        nameLbl.text = Get<string>("cnName");
        typeLbl.text = curSlot == SkillSlot.Active ? "主动技能" : "被动技能";

        /*── 技能环：根据等级替换贴图 ──*/
        if (ringSprites != null && ringSprites.Length >= 5)
            ringImg.style.backgroundImage =
                new StyleBackground(ringSprites[Mathf.Clamp(curLv, 0, ringSprites.Length - 1)]);

        /*── 等级部分 ──*/
        int maxLv = 4;
        beforeLvLbl.text = $"等级{curLv}";
        afterLvLbl.text  = curLv >= maxLv ? "已满级" : $"等级{curLv + 1}";

        /*── 说明文字 ──*/
        beforeTxtLbl.text = FormatDesc(curLv);
        afterTxtLbl.text  = curLv >= maxLv ? "—" : FormatDesc(curLv + 1);

        /*── 兵法消耗 & 按钮状态 ──*/
        int  need = curLv >= COST.Length ? 0 : COST[curLv];
        long own  = PlayerResourceBank.I[ResourceType.ArtOfWar];
        ownArtLbl.text  = own.ToString();
        needArtLbl.text = need.ToString();
        needArtLbl.style.color = own < need ? Color.red : new Color(0.32f, 0.65f, 0.53f);

        upBtn.SetEnabled(curLv < maxLv && own >= need);
        upBtn.text = curLv >= maxLv ? "满级" : "升级";
    }

    /*──────────────── 升级 ─────────────────*/
    void Upgrade()
    {
        int need = COST[curLv];
        if (!PlayerResourceBank.I.Spend(ResourceType.ArtOfWar, need))
        {
            PopupManager.Show("提示", "兵法不足"); return;
        }
        curLv++;
        PlayerPrefs.SetInt($"SKILL_LV_{Get<string>("id")}", curLv);
        PlayerPrefs.Save();

        onUpgrade?.Invoke();                     // 通知外层刷新

        PopupManager.Show("提示", "升级成功", 1.2f);
        RefreshUI();                             // 立刻刷新弹窗自身
    }

    /*──────────────── 反射工具 ─────────────────*/
    T Get<T>(string field)
    {
        var info = curSkill.GetType()
                           .GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return info == null ? default : (T)info.GetValue(curSkill);
    }

    string FormatDesc(int lv)
    {
        float baseVal = Get<float>("baseValue");
        float perLv   = Get<float>("valuePerLv");
        string col(string x) => $"<color=#F4A43D>{x}</color>";
        return Get<string>("description").Replace("{X}", col((baseVal + lv * perLv).ToString("0.#")));
    }
}
