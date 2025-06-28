// ────────────────────────────────────────────────────────────────────────────────
// 技能详情弹窗（固定 UIDocument 版本，升级写回 PlayerLordCard & 实时环图）
// ────────────────────────────────────────────────────────────────────────────────
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Game;
using Game.Core;   // ISkillMultiplierSource

public class SkillDetailPopupController : MonoBehaviour
{
    /*──────── Inspector ────────*/
    [Header("UI")]
    [SerializeField] UIDocument uiDoc;
    [Tooltip("按 Lv1-4 顺序放入 4 张环图")]
    [SerializeField] Sprite[]  ringSprites;

    [Header("Data")]
    [SerializeField] PlayerLordCard       lord;
    [SerializeField] ActiveSkillDatabase  activeDB;
    [SerializeField] PassiveSkillDatabase passiveDB;

    /*──────── 常量 ────────*/
    const int MIN_LV = 1;
    const int MAX_LV = 4;
    readonly int[] COST = { 1, 2, 4, 6 };   // 升级到 Lv2/3/4/结束

    /*──────── UI 缓存 ────────*/
    VisualElement root, iconImg, ringImg;
    Label nameLbl, typeLbl,
          beforeLvLbl, afterLvLbl,
          beforeTxtLbl, afterTxtLbl,
          ownArtLbl, needArtLbl;
    Button upBtn;

    /*──────── 运行时 ────────*/
    SkillSlot curSlot;
    object    curSkill;
    int       curLv;

    public event Action onUpgrade;   // 外部刷新

    /*──────────────────────── 生命周期 ────────────────────────*/
    void Awake()
    {
        root = uiDoc.rootVisualElement;
        BindNodes();
        root.style.display = DisplayStyle.None;
    }

    /*──────── Bind / Close ────────*/
    void BindNodes()
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

        var close1 = root.Q<Button>("ClosePanel");
        if (close1 != null) close1.clicked += Close;
        var close2 = root.Q<Button>("CloseBtn2");
        if (close2 != null) close2.clicked += Close;
        var mask = root.Q<VisualElement>("BlackSpace");
        if (mask != null) mask.RegisterCallback<ClickEvent>(_ => Close());

        upBtn.clicked += Upgrade;
    }

    public void Open(SkillSlot slot)
    {
        curSlot  = slot;
        curSkill = slot switch
        {
            SkillSlot.Active   => activeDB?.Get(lord.activeSkillId),
            SkillSlot.Passive1 => passiveDB?.Get(lord.passiveOneId),
            _                  => passiveDB?.Get(lord.passiveTwoId)
        };
        if (curSkill == null)
        {
            PopupManager.Show("提示", "技能数据缺失"); return;
        }

        curLv = Mathf.Clamp(lord.GetSkillLevel(slot), MIN_LV, MAX_LV);
        RefreshUI();

        root.BringToFront();
        root.style.display = DisplayStyle.Flex;
    }

    void Close() => root.style.display = DisplayStyle.None;

    /*──────── 刷新 UI ────────*/
    void RefreshUI()
    {
        iconImg.style.backgroundImage = new StyleBackground(Get<Sprite>("iconSprite"));
        nameLbl.text = Get<string>("cnName");
        typeLbl.text = curSlot == SkillSlot.Active ? "主动技能" : "被动技能";

        // 环
        if (ringSprites != null && ringSprites.Length >= 4)
            ringImg.style.backgroundImage =
                new StyleBackground(ringSprites[Mathf.Clamp(curLv - 1, 0, ringSprites.Length - 1)]);

        beforeLvLbl.text = $"等级{curLv}";
        afterLvLbl.text  = curLv >= MAX_LV ? "已满级" : $"等级{curLv + 1}";

        beforeTxtLbl.text = FormatDesc(curLv);
        afterTxtLbl.text  = curLv >= MAX_LV ? "—" : FormatDesc(curLv + 1);

        int  need = curLv >= MAX_LV ? 0 : COST[curLv - 1];
        long own  = PlayerResourceBank.I[ResourceType.ArtOfWar];

        ownArtLbl.text  = own.ToString();
        needArtLbl.text = need.ToString();
        needArtLbl.style.color = own < need ? Color.red : new Color(0.32f, 0.65f, 0.53f);

        upBtn.SetEnabled(curLv < MAX_LV && own >= need);
        upBtn.text = curLv >= MAX_LV ? "满级" : "升级";
    }

    /*──────── 升级 ────────*/
    void Upgrade()
    {
        if (curLv >= MAX_LV) return;

        int need = COST[curLv - 1];
        if (!PlayerResourceBank.I.Spend(ResourceType.ArtOfWar, need))
        {
            PopupManager.Show("提示", "兵法不足"); return;
        }

        curLv++;
        lord.SetSkillLevel(curSlot, curLv);  // ★ 写回 PlayerLordCard
        PlayerPrefs.SetInt($"SKILL_LV_{Get<string>("id")}", curLv); // 备份
        PlayerPrefs.Save();

        onUpgrade?.Invoke();                 // 外层刷新
        PopupManager.Show("提示", "升级成功", 1.2f);
        RefreshUI();
    }

    /*──────── 反射 HELPERS ────────*/
    T Get<T>(string field)
    {
        var info = curSkill.GetType()
                           .GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return info == null ? default : (T)info.GetValue(curSkill);
    }

    string FormatDesc(int lv)
    {
        float baseVal;
        ISkillMultiplierSource src;

        if (curSlot == SkillSlot.Active)
        {
            var act = (ActiveSkillInfo)curSkill;
            baseVal = act.coefficient;
            src     = activeDB;
        }
        else
        {
            var pas = (PassiveSkillInfo)curSkill;
            baseVal = pas.baseValue;
            src     = passiveDB;
        }

        src.LevelMultiplier.TryGetValue(lv, out var lvMul);
        if (lvMul == 0f) lvMul = 1f;           // fallback

        float finalPct = baseVal * lvMul;      // 主公恒 S 阶 ⇒ Tier 倍率 1

        string color(string x) => $"<color=#F4AA3D>{x}</color>";
        return Get<string>("description")
               .Replace("{X}", color($"{finalPct * 100:0.#}%"));
    }
}
