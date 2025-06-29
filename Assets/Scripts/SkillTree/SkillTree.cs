/******************************************************
 * FancySkillTree.cs  (ScrollViewPro 兼容 + 完整功能)
 * ----------------------------------------------------
 * 1. Stage 按钮生成到 <Button name="stage-options">.parent
 * 2. 升级按钮文字：解锁 / 升级 / 满级 / 未解锁
 * 3. 过去阶段始终显示满级；未来阶段锁定
 * 4. 当前阶段全满 → 自动解锁下一阶段
 *****************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(UIDocument))]
public class FancySkillTree : MonoBehaviour
{
    /*──────── 外部引用 ────────*/
    [Header("拖入 TechProgressData.asset")]
    public TechProgressData progressAsset;

    [Header("拖入 techtree.json")]
    public TextAsset jsonFile;

    [Header("拖入 SkillNode.uxml")]
    public VisualTreeAsset nodeTemplate;

    [Header("等级圆点：Locked / Unlocked")]
    public Sprite lockedSlotSprite;
    public Sprite unlockedSlotSprite;

    [Header("默认节点图标（可选）")]
    public Sprite defaultIcon;

    /*──────── 内部结构 ────────*/
    private class SkillData
    {
        public string type;
        public int maxLvl;
        public List<int> costs;
        public List<float> gains;
    }
    private class SkillView
    {
        public VisualElement root;
        public List<VisualElement> lvSlots;
        public SkillData data;
    }

    /*──────── 运行时字典 ────────*/
    private readonly Dictionary<string,int>       levelDict = new();
    private readonly Dictionary<string,SkillData> dataDict  = new();
    private readonly Dictionary<string,SkillView> viewDict  = new();
    private readonly Dictionary<string,Button>    stageBtns = new();
    private readonly Dictionary<string,int>       keyToIdx  = new();   // itemKey → 编号-1

    /*──────── UI 引用 ────────*/
    private VisualElement root;
    private VisualElement stageContainer;       // ScrollViewPro contentContainer
    private Label         stageLabel;
    private List<VisualElement> rows;
    private Label  detailTitle;
    private Label  detailDesc;
    private Button upgradeBtn;

    /*──────── JSON 数据 ────────*/
    private JObject                   treeData;
    private Dictionary<string,string> descTable;

    /*──────── 运行时变量 ────────*/
    private int playerSpark = 999;   // 示例资源
    private string    curKey;
    private SkillView curView;

    /**************** 生命周期 ****************/
    private void OnEnable()
    {
        if (!Validate()) return;

        TechTreeCalculator.JsonFile = jsonFile;
        treeData  = JObject.Parse(jsonFile.text);
        descTable = treeData["description"]?.ToObject<Dictionary<string,string>>() ?? new();

        rows = FindRows();
        if (rows.Count < 5) { Debug.LogError("缺少 5 行 skill-row"); return; }

        BuildStageButtons();
        string stageKey = progressAsset ? progressAsset.CurrentStage.ToString() : "1";
        ShowStage(stageKey);

        progressAsset?.RecalculateBonus();
    }

    /**************** 校验 & 获取 UI ****************/
    private bool Validate()
    {
        if (jsonFile == null || nodeTemplate == null ||
            lockedSlotSprite == null || unlockedSlotSprite == null)
        {
            Debug.LogError("缺少引用：jsonFile / nodeTemplate / SlotSprite");
            return false;
        }

        root         = GetComponent<UIDocument>().rootVisualElement.Q("TreeRoot");
        var tplBtn   = root.Q<Button>("stage-options");          // 模板按钮
        if (tplBtn == null) { Debug.LogError("未找到 stage-options"); return false; }

        stageContainer = tplBtn.parent;                          // 真正容器
        tplBtn.style.display = DisplayStyle.None;                // 隐藏模板

        stageLabel   = root.Q<Label>("stage-label");
        detailTitle  = root.Q<Label>("skilltitle");
        detailDesc   = root.Q<Label>("skilldescription");
        upgradeBtn   = root.Q<Button>("upgradeBtn");

        return root != null && stageContainer != null &&
               stageLabel != null && detailTitle != null &&
               detailDesc != null && upgradeBtn != null;
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
            string skey = prop.Name;
            var btn = new Button(() => ShowStage(skey)) { text = $"阶级 {skey}" };
            btn.AddToClassList("stage-btn");
            stageContainer.Add(btn);
            stageBtns[skey] = btn;
        }
    }

    /**************** 显示 Stage ****************/
    private void ShowStage(string sKey)
    {
        foreach (var kv in stageBtns)
            kv.Value.EnableInClassList("active", kv.Key == sKey);

        stageLabel.text = $"Stage {sKey}";
        foreach (var row in rows) row.Clear();

        levelDict.Clear(); viewDict.Clear(); keyToIdx.Clear(); dataDict.Clear();

        JObject stageObj = treeData[sKey] as JObject;
        if (stageObj == null) return;

        /* 小项编号升序 */
        var items = stageObj.Properties()
                            .Where(p => int.TryParse(p.Name, out _))
                            .OrderBy(p => int.Parse(p.Name))
                            .Select(p => (key:$"{sKey}-{p.Name}", arr:(JArray)p.Value))
                            .ToList();

        /* 行布局（持久化） */
        List<int> rowLayout = progressAsset?.GetRowLayout(sKey);
        bool needSaveLayout = false;
        if (rowLayout == null || rowLayout.Count != items.Count)
        {
            rowLayout = GenerateRandomLayout(items.Count);
            needSaveLayout = true;
        }

        /* 当前/过去/未来 阶段判断 */
        int curStageIdx = progressAsset ? progressAsset.CurrentStage : 1;
        bool isPast    = int.Parse(sKey) < curStageIdx;
        bool isCurrent = int.Parse(sKey) == curStageIdx;

        string progStr = isCurrent ? progressAsset.Progress : null;

        for (int i = 0; i < items.Count; i++)
        {
            var (key, arr) = items[i];
            int maxLvl = arr[1].Value<int>();
            int lvl;

            if (isPast)                 lvl = maxLvl;                              // 已完成阶段
            else if (isCurrent)         lvl = (i < progStr.Length) ? progStr[i]-'0' : 0;
            else                        lvl = 0;                                   // 未来阶段

            levelDict[key] = lvl;
            keyToIdx[key]  = i;
        }

        /* 生成节点 */
        for (int i = 0; i < items.Count; i++)
        {
            int rowIdx = rowLayout[i];
            var v = CreateNode(items[i].key, items[i].arr, rows[rowIdx]);
            viewDict[items[i].key] = v;
        }

        if (needSaveLayout && progressAsset)
            progressAsset.SetRowLayout(sKey, rowLayout);

        detailTitle.text = detailDesc.text = "";
        upgradeBtn.SetEnabled(false);
        curKey = null; curView = null;
    }

    private List<int> GenerateRandomLayout(int count)
    {
        var layout = new List<int>(new int[count]);
        int[] load = new int[rows.Count];
        var rnd = new System.Random();
        for (int i = 0; i < count; i++)
        {
            int r;
            do { r = rnd.Next(rows.Count); } while (load[r] >= 4);
            layout[i] = r; load[r]++;
        }
        return layout;
    }

    /**************** 创建节点 ****************/
    private SkillView CreateNode(string key, JArray arr, VisualElement parentRow)
    {
        string type = arr[0].ToString();
        int    max  = arr[1].Value<int>();
        var    costs = arr[2].ToObject<List<int>>();
        var    gains = arr[3].ToObject<List<float>>();

        var data = new SkillData { type=type, maxLvl=max, costs=costs, gains=gains };
        dataDict[key] = data;

        VisualElement c = nodeTemplate.CloneTree();
        parentRow.Add(c);

        var nodeRoot = c.Q<VisualElement>("icon");
        nodeRoot.name = key;

        /* lvslot 列表 */
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

        return new SkillView { root=c, lvSlots=slots, data=data };
    }

    /**************** 选中节点 ****************/
    private void SelectSkill(string key)
    {
        curKey = key; curView = viewDict[key];
        var d   = dataDict[key];
        int lvl = levelDict[key];

        detailTitle.text = Nicify(d.type);
        string tpl = descTable.TryGetValue(d.type, out var t) ? t : "暂无描述";
        string v = lvl >= d.maxLvl ? "MAX"
                 : tpl.Contains("%") ? (d.gains[lvl]*100).ToString() : d.gains[lvl].ToString();
        detailDesc.text = tpl.Replace("{x}", v);

        int stageIdx = int.Parse(key.Split('-')[0]);

        if (stageIdx > progressAsset.CurrentStage)
        {
            upgradeBtn.text = "未解锁";
            upgradeBtn.SetEnabled(false);
            return;
        }

        UpdateUpgradeButton(lvl, d.maxLvl);
    }

    private void UpdateUpgradeButton(int lvl, int maxLvl)
    {
        if (lvl >= maxLvl)
        {
            upgradeBtn.text = "满级";
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

    /**************** 升级 ****************/
    private void UpgradeCurrent()
    {
        if (curKey == null) return;
        var d   = dataDict[curKey];
        int lvl = levelDict[curKey];
        if (lvl >= d.maxLvl || playerSpark < d.costs[lvl]) return;

        playerSpark -= d.costs[lvl];
        levelDict[curKey] = ++lvl;

        ApplySlotVisual(curKey, curView.lvSlots);
        SelectSkill(curKey);

        /* 写回 Progress */
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
            arr[idx] = (char)('0' + lvl);
            progressAsset.Progress = new string(arr);

            progressAsset.RecalculateBonus();
#if UNITY_EDITOR
            EditorUtility.SetDirty(progressAsset);
#endif
        }

        TryUnlockNextStage();
    }

    /**************** 自动解锁下一阶段 ****************/
    private void TryUnlockNextStage()
    {
        string curKey = progressAsset.CurrentStage.ToString();
        if (!(treeData[curKey] is JObject stageObj)) return;

        string prog = progressAsset.Progress;
        int idx = 0;

        foreach (var prop in stageObj.Properties()
                                     .Where(p => int.TryParse(p.Name, out _))
                                     .OrderBy(p => int.Parse(p.Name)))
        {
            int maxLvl = ((JArray)prop.Value)[1].Value<int>();
            int curLvl = (idx < prog.Length) ? prog[idx]-'0' : 0;
            if (curLvl < maxLvl) return;       // 尚未满级
            idx++;
        }

        /* 解锁下一阶段 */
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
        ShowStage(nextKey);
    }

    /**************** 刷新圆点 ****************/
    private void ApplySlotVisual(string key, List<VisualElement> slots)
    {
        int lvl = levelDict[key];
        for (int i = 0; i < slots.Count; i++)
            slots[i].style.backgroundImage =
                new StyleBackground(i < lvl ? unlockedSlotSprite : lockedSlotSprite);
    }

    /**************** 工具 ****************/
    private static string Nicify(string raw) =>
        string.Join(" ", raw.Split('_').Select(s => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s)));
}
