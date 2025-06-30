/******************************************************
 * FancySkillTree.cs – 2025-06-30  TechScroll & Color
 * ----------------------------------------------------
 * 依赖：
 *   · UnityEngine.UIElements
 *   · Newtonsoft.Json.Linq
 *   · TechProgressData        (ScriptableObject)
 *   · PlayerResources         (ScriptableObject，含 long techScroll)
 *   · TechTreeCalculator
 *****************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(UIDocument))]
public class FancySkillTree : MonoBehaviour
{
    /*──────── Inspector 引用 ────────*/
    [Header("拖入 TechProgressData.asset")]
    public TechProgressData progressAsset;
    [SerializeField] private PlayerBaseController playerBaseController;  // 拖引用

    [Header("拖入 techtree.json")]
    public TextAsset jsonFile;

    [Header("拖入 PlayerResources.asset")]
    public PlayerResources playerResources;

    [Header("拖入 SkillNode.uxml")]
    public VisualTreeAsset nodeTemplate;

    [Header("等级圆点：Locked / Unlocked")]
    public Sprite lockedSlotSprite;
    public Sprite unlockedSlotSprite;

    [Header("默认节点图标（可选）")]
    public Sprite defaultIcon;

    /*──────── 颜色常量 ────────*/
    readonly Color okColor  = new(0.32f, 0.65f, 0.53f, 1f);   // 足够
    readonly Color errColor = new(0.94f, 0.15f, 0.15f, 1f);   // 不足

    /*──────── 内部结构 ────────*/
    private class SkillData
    {
        public string type;
        public int maxLvl;
        public List<int> costs;     // 以 TechScroll 为单位
        public List<float> gains;
    }
    private class SkillView
    {
        public VisualElement root;
        public List<VisualElement> lvSlots;
        public SkillData data;
    }

    /*──────── 运行时容器 ────────*/
    private readonly Dictionary<string,int>       levelDict = new();
    private readonly Dictionary<string,SkillData> dataDict  = new();
    private readonly Dictionary<string,SkillView> viewDict  = new();
    private readonly Dictionary<string,Button>    stageBtns = new();
    private readonly Dictionary<string,int>       keyToIdx  = new();

    /*──────── UI 引用 ────────*/
    private VisualElement root;
    private VisualElement stageContainer;
    private Label         stageLabel;
    private List<VisualElement> rows;
    private Label detailTitle, detailDesc;
    private Button upgradeBtn;
    private Label needTitle, skillLvLabel, needLabel;
    
    private Label playermat;  // 显示玩家卷轴数量
    private Button returnBtn;

    /*──────── JSON 数据 ────────*/
    private JObject treeData;
    private Dictionary<string,string> descTable;

    /*──────── 运行时状态 ────────*/
    private string    curKey;
    private SkillView curView;

    /**************** 生命周期 ****************/
    private void OnEnable()
    {
        if (!Validate()) return;

        TechTreeCalculator.JsonFile = jsonFile;
        treeData = JObject.Parse(jsonFile.text);
        descTable = treeData["description"]?.ToObject<Dictionary<string, string>>() ?? new();

        rows = FindRows();
        if (rows.Count < 5) { Debug.LogError("需要 5 行 skill-row"); return; }

        BuildStageButtons();

        string stageKey = progressAsset ? progressAsset.CurrentStage.ToString() : "1";
        ShowStage(stageKey);

        playermat.text = NumberAbbreviator.Format(playerResources.techScroll, 2);
        progressAsset?.RecalculateBonus();
        
        var root = GetComponent<UIDocument>().rootVisualElement;

        // 假设 UXML 里按钮 name="ReturnBtn"
        
    }
    
    private void OnDisable()
    {
        if (returnBtn != null)
            returnBtn.UnregisterCallback<ClickEvent>(_ => playerBaseController.HidePlayerTechTreePage());
    }

    /**************** 校验 & 获取 UI ****************/
    private bool Validate()
    {
        if (jsonFile == null || nodeTemplate == null ||
            lockedSlotSprite == null || unlockedSlotSprite == null ||
            playerResources == null)
        {
            Debug.LogError("缺少引用：jsonFile / nodeTemplate / SlotSprite / PlayerResources");
            return false;
        }

        root = GetComponent<UIDocument>().rootVisualElement.Q("TreeRoot");
        var tplBtn = root.Q<Button>("stage-options");
        if (tplBtn == null) { Debug.LogError("未找到 stage-options"); return false; }

        returnBtn = root.Q<Button>("ReturnBtn");
        if (returnBtn != null)
            returnBtn.RegisterCallback<ClickEvent>(_ => playerBaseController.HidePlayerTechTreePage());
        else
            Debug.LogError("找不到 ReturnBtn");

        stageContainer = tplBtn.parent;
        tplBtn.style.display = DisplayStyle.None;

        stageLabel = root.Q<Label>("stage-label");
        detailTitle = root.Q<Label>("skilltitle");
        detailDesc = root.Q<Label>("skilldescription");
        upgradeBtn = root.Q<Button>("upgradeBtn");
        needLabel = root.Q<Label>("skillupgradeneed");
        needTitle = root.Q<Label>("needtitle");
        skillLvLabel = root.Q<Label>("skilllv");
        playermat = root.Q<Label>("playermat");

        if (needLabel == null) { Debug.LogError("缺少 skillupgradeneed Label"); return false; }
        needLabel.enableRichText = true;

        return true;
    }

    private List<VisualElement> FindRows()
    {
        var list = new List<VisualElement>();
        root.Query<VisualElement>().Class("skill-row").ToList(list);
        return list;
    }

    /**************** Stage 按钮生成 ****************/
    private void BuildStageButtons()
    {
        stageBtns.Clear();
        stageContainer.Clear();

        foreach (var prop in treeData.Properties()
                                     .Where(p => int.TryParse(p.Name, out _))
                                     .OrderBy(p => int.Parse(p.Name)))
        {
            string sKey = prop.Name;

            var btn = new Button(() => ShowStage(sKey))
            { text = $"阶段 {sKey}" };

            btn.AddToClassList("stage-btn");
            stageContainer.Add(btn);
            stageBtns[sKey] = btn;
        }
    }

    /**************** 显示 Stage ****************/
    private void ShowStage(string sKey)
    {
        foreach (var kv in stageBtns)
            kv.Value.EnableInClassList("active", kv.Key == sKey);

        stageLabel.text = $"阶段 {sKey}";
        foreach (var row in rows) row.Clear();

        levelDict.Clear(); viewDict.Clear(); keyToIdx.Clear(); dataDict.Clear();

        if (treeData[sKey] is not JObject stageObj) return;

        /* ---- 按编号升序 ---- */
        var items = stageObj.Properties()
                            .Where(p => int.TryParse(p.Name, out _))
                            .OrderBy(p => int.Parse(p.Name))
                            .Select(p => (key: $"{sKey}-{p.Name}", arr: (JArray)p.Value))
                            .ToList();

        /* ---- 行布局（持久化） ---- */
        List<int> rowLayout = progressAsset?.GetRowLayout(sKey);
        bool needSaveLayout = false;
        if (rowLayout == null || rowLayout.Count != items.Count)
        {
            rowLayout = GenerateRandomLayout(items.Count);
            needSaveLayout = true;
        }

        /* ---- 阶段状态 ---- */
        int  curStageIdx = progressAsset ? progressAsset.CurrentStage : 1;
        bool isPast      = int.Parse(sKey) < curStageIdx;
        bool isCurrent   = int.Parse(sKey) == curStageIdx;
        string progStr   = isCurrent ? progressAsset.Progress : null;

        for (int i = 0; i < items.Count; i++)
        {
            var (key, arr) = items[i];
            int maxLvl = arr[1].Value<int>();
            int lvl = isPast ? maxLvl
                    : isCurrent ? (progStr != null && i < progStr.Length ? progStr[i] - '0' : 0)
                    : 0;

            levelDict[key] = lvl;
            keyToIdx[key]  = i;
        }

        /* ---- 生成节点 ---- */
        for (int i = 0; i < items.Count; i++)
        {
            int rowIdx = rowLayout[i];

            string key = items[i].key;
            int    lvl = levelDict[key];

            bool stageUnlocked = isPast || isCurrent;
            bool dimNode       = !stageUnlocked || lvl == 0;

            var v = CreateNode(key, items[i].arr,
                               rows[rowIdx],
                               dimNode);
            viewDict[key] = v;
        }

        if (needSaveLayout && progressAsset)
            progressAsset.SetRowLayout(sKey, rowLayout);

        detailTitle.text = detailDesc.text = "";
        upgradeBtn.SetEnabled(false);
        needLabel.text = "";
        curKey = null; curView = null;
    }

    /**************** 创建节点 ****************/
    private SkillView CreateNode(string key,
                                 JArray        arr,
                                 VisualElement parentRow,
                                 bool          dim)
    {
        string type  = arr[0].ToString();
        int    max   = arr[1].Value<int>();
        var    costs = arr[2].ToObject<List<int>>();
        var    gains = arr[3].ToObject<List<float>>();

        while (costs.Count < max)  costs.Add(costs.LastOrDefault());
        while (gains.Count < max)  gains.Add(gains.LastOrDefault());

        var data = new SkillData { type = type, maxLvl = max, costs = costs, gains = gains };
        dataDict[key] = data;

        /* ---------- UI ---------- */
        VisualElement c = nodeTemplate.CloneTree();
        parentRow.Add(c);

        var nodeRoot = c.Q<VisualElement>("icon");
        nodeRoot.name = key;

        /* 圆点 */
        var tpl = c.Q<VisualElement>("lvslot");
        var p   = tpl != null ? tpl.parent : c;
        if (tpl != null) p.Remove(tpl);
        p.style.flexDirection = FlexDirection.Row;

        var slots = new List<VisualElement>();
        for (int i = 0; i < max; i++)
        {
            var v = new VisualElement();
            v.AddToClassList("lvslot");
            v.style.backgroundImage = new StyleBackground(lockedSlotSprite);
            p.Add(v);
            slots.Add(v);
        }

        ApplySlotVisual(key, slots);
        nodeRoot.RegisterCallback<ClickEvent>(_ => SelectSkill(key));

        if (defaultIcon)
            nodeRoot.style.backgroundImage = new StyleBackground(defaultIcon);

        nodeRoot.style.opacity = dim ? 0.05f : 1f;

        return new SkillView { root = c, lvSlots = slots, data = data };
    }

    /**************** 选中节点 ****************/
    private void SelectSkill(string key)
    {
        curKey  = key;
        curView = viewDict[key];
        var d   = dataDict[key];
        int lvl = levelDict[key];

        detailTitle.text = Nicify(d.type);
        string tpl = descTable.TryGetValue(d.type, out var t) ? t : "暂无描述";

        int safeIdx = Mathf.Clamp(lvl, 0, d.gains.Count - 1);
        float val   = d.gains[safeIdx];

        string vStr = tpl.Contains("%") ? (val * 100).ToString() : val.ToString();
        detailDesc.text = tpl.Replace("{x}", vStr);

        UpdateNeedLabelAndButton(d, lvl);
    }

    /**************** 更新消耗标签 & 升级按钮 ****************/
    private void UpdateNeedLabelAndButton(SkillData d, int lvl)
    {
        long have   = playerResources.techScroll;
        bool isMax  = lvl >= d.maxLvl;
        long need   = isMax ? 0 : d.costs[lvl];
        bool afford = have >= need;

        /* 1) 技能等级文字 */
        if (lvl == 0)
            skillLvLabel.text = "<color=#999999>未解锁</color>";
        else if (isMax)
            skillLvLabel.text = $"<color=#FFD700>等级 <b>{lvl}</b></color>";
        else
            skillLvLabel.text = $"等级 <b>{lvl}</b>";

        /* 2) 判断阶段是否已解锁 */
        int stageOfNode  = int.Parse(curKey.Split('-')[0]);
        int currentStage = progressAsset ? progressAsset.CurrentStage : 1;
        bool stageUnlocked = stageOfNode == currentStage;

        /* 3) 满级 */
        if (isMax)
        {
            needTitle.text = "<color=#999999>已满级</color>";
            needLabel.Hide();

            upgradeBtn.text = "满级";
            upgradeBtn.SetEnabled(false);
            return;
        }

        /* 4) 未满级：显示需求 */
        needTitle.text = "需要卷轴：";

        string colorHex = ColorUtility.ToHtmlStringRGB(afford ? okColor : errColor);
        needLabel.text  = $"<color=#{colorHex}>{have}</color> / {need:N0}";

        playermat.text = NumberAbbreviator.Format(have, 2);
        needLabel.Show();

        /* 5) 升级按钮状态 */
        if (!stageUnlocked)
        {
            upgradeBtn.text = "阶段未解锁";
            upgradeBtn.SetEnabled(false);
        }
        else if (!afford)
        {
            upgradeBtn.text = "资源不足";
            upgradeBtn.SetEnabled(false);
        }
        else
        {
            upgradeBtn.text = lvl == 0 ? "解锁" : "升级";
            upgradeBtn.SetEnabled(true);

            upgradeBtn.clicked -= UpgradeCurrent;
            upgradeBtn.clicked += UpgradeCurrent;
        }
    }

    //**************** 升级 ****************/
    /**************** 升级 ****************/
    private void UpgradeCurrent()
    {
        if (curKey == null) return;

        string keyBackup = curKey;                // ← 1) 备份

        var d   = dataDict[keyBackup];
        int lvl = levelDict[keyBackup];
        if (lvl >= d.maxLvl) return;

        long need = d.costs[lvl];
        if (playerResources.techScroll < need) return;

        /* 扣资源 */
        playerResources.techScroll -= need;
    #if UNITY_EDITOR
        EditorUtility.SetDirty(playerResources);
    #endif

        /* 升级操作 */
        levelDict[keyBackup] = ++lvl;
        WriteBackProgress(lvl);
        TryUnlockNextStage();

        /* 2) 刷新当前阶段整页 UI */
        string curStageKey = keyBackup.Split('-')[0];   // "6-2" → "6"
        ShowStage(curStageKey);

        /* 3) 重新选中刚才升级的节点，面板立即更新且不会空指针 */
        SelectSkill(keyBackup);
    }




    private void WriteBackProgress(int lvl)
    {
        if (progressAsset && progressAsset.CurrentStage.ToString() == curKey.Split('-')[0])
        {
            int idx = keyToIdx[curKey];
            char[] arr = progressAsset.Progress.ToCharArray();
            if (idx >= arr.Length)
            {
                Array.Resize(ref arr, idx + 1);
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i] == '\0') arr[i] = '0';
            }
            arr[idx] = (char)('0' + Mathf.Clamp(lvl, 0, 9));
            progressAsset.Progress = new string(arr);

            progressAsset.RecalculateBonus();
#if UNITY_EDITOR
            EditorUtility.SetDirty(progressAsset);
#endif
        }
    }

    /**************** 行布局随机生成 *****************/
    private List<int> GenerateRandomLayout(int count)
    {
        var layout = new List<int>(new int[count]);
        int[] load = new int[rows.Count];
        var rnd = new System.Random();

        for (int i = 0; i < count; i++)
        {
            int r;
            do { r = rnd.Next(rows.Count); } while (load[r] >= 4);
            layout[i] = r;
            load[r]++;
        }
        return layout;
    }

    /**************** 自动解锁下一阶段 ****************/
    private void TryUnlockNextStage()
    {
        string curStageKey = progressAsset.CurrentStage.ToString();
        if (treeData[curStageKey] is not JObject stageObj) return;

        string prog = progressAsset.Progress;
        int idx = 0;
        foreach (var prop in stageObj.Properties()
                                     .Where(p => int.TryParse(p.Name, out _))
                                     .OrderBy(p => int.Parse(p.Name)))
        {
            int maxLvl = ((JArray)prop.Value)[1].Value<int>();
            int curLvl = (idx < prog.Length) ? prog[idx] - '0' : 0;
            if (curLvl < maxLvl) return;
            idx++;
        }

        int nextStage = progressAsset.CurrentStage + 1;
        string nextKey = nextStage.ToString();
        if (!treeData.Properties().Any(p => p.Name == nextKey)) return;

        int itemCnt = treeData[nextKey]
                        .Where(t => int.TryParse(((JProperty)t).Name, out _))
                        .Count();

        progressAsset.CurrentStage = nextStage;
        progressAsset.Progress     = new string('0', itemCnt);
        progressAsset.RecalculateBonus();
#if UNITY_EDITOR
        EditorUtility.SetDirty(progressAsset);
#endif

        if (stageLabel.text.EndsWith($"阶段 {nextKey}"))
            ShowStage(nextKey);
    }

    /**************** 圆点刷新 & 节点亮度 ****************/
    /**************** 圆点刷新 & 节点亮度 ****************/
    /**************** 圆点刷新 & 节点亮度 ****************/
    private void ApplySlotVisual(string key, List<VisualElement> slots)
    {
        int lvl = levelDict[key];

        /* ---- 圆点贴图 ---- */
        for (int i = 0; i < slots.Count; i++)
            slots[i].style.backgroundImage =
                new StyleBackground(i < lvl ? unlockedSlotSprite : lockedSlotSprite);

        /* ---- 亮度判定 ---- */
        int stageIdx  = int.Parse(key.Split('-')[0]);
        int curStage  = progressAsset ? progressAsset.CurrentStage : 1;
        bool unlocked = stageIdx < curStage || (stageIdx == curStage && lvl > 0);

        float opacity = unlocked ? 1f : 0.05f;

        if (viewDict.TryGetValue(key, out var sv))
        {
            sv.root.style.opacity = opacity;                  // 整个节点
            var icon = sv.root.Q<VisualElement>("icon");
            if (icon != null) icon.style.opacity = opacity;   // 单独 icon（保险）
        }
    }



    /**************** 小工具 ****************/
    private static string Nicify(string raw) =>
        string.Join(" ", raw.Split('_')
                            .Select(s => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s)));
}
