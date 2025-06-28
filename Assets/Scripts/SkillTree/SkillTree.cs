// Assets/SkillTree/FancySkillTree.cs
//--------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;

[RequireComponent(typeof(UIDocument))]
public class FancySkillTree : MonoBehaviour
{
    [Header("拖入 techtree.json")]   public TextAsset       jsonFile;
    [Header("拖入 SkillNode.uxml")] public VisualTreeAsset nodeTemplate;
    [Header("等级圆点：Locked / Unlocked")]
    public Sprite lockedSlotSprite;
    public Sprite unlockedSlotSprite;
    [Header("默认节点图标（可选）")] public Sprite defaultIcon;

    /*──────────────── 数据结构 ────────────────*/
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

    /*──────────────── 运行时字典 ────────────────*/
    private readonly Dictionary<string,int>       levelDict = new();
    private readonly Dictionary<string,SkillData> dataDict  = new();
    private readonly Dictionary<string,SkillView> viewDict  = new();
    private readonly Dictionary<string,Button>    stageBtns = new();

    /*──────────────── UI 引用 ────────────────*/
    private VisualElement root;
    private VisualElement stageOptContainer;
    private Label         stageLabel;
    private List<VisualElement> rows;

    private Label  detailTitle;
    private Label  detailDesc;
    private Button upgradeBtn;

    /*──────────────── JSON ────────────────*/
    private JObject                   treeData;
    private Dictionary<string,string> descTable;

    /*──────────────── 其它 ────────────────*/
    private int playerSpark = 999;
    private string    curKey;
    private SkillView curView;

    /**************** 生命周期 ****************/
    private void OnEnable()
    {
        if (!Validate()) return;

        treeData  = JObject.Parse(jsonFile.text);
        descTable = treeData["description"]?.ToObject<Dictionary<string,string>>() ?? new();

        rows = FindRows();
        if (rows.Count < 5) { Debug.LogError("缺少 5 行 skill-row"); return; }

        BuildStageButtons();
        ShowStage("1");
    }

    /**************** 检查 ****************/
    private bool Validate()
    {
        if (jsonFile == null || nodeTemplate == null || lockedSlotSprite == null || unlockedSlotSprite == null)
        { Debug.LogError("请在 Inspector 拖入 jsonFile, nodeTemplate, 两张 Slot Sprite"); return false; }

        root = GetComponent<UIDocument>().rootVisualElement.Q("TreeRoot");
        stageOptContainer = root.Q<VisualElement>("stage-options");
        stageLabel   = root.Q<Label>("stage-label");
        detailTitle  = root.Q<Label>("skilltitle");
        detailDesc   = root.Q<Label>("skilldescription");
        upgradeBtn   = root.Q<Button>("upgradeBtn");

        return root != null && stageOptContainer!=null && stageLabel!=null && detailTitle!=null && detailDesc!=null && upgradeBtn!=null;
    }

    /**************** 找行容器 ****************/
    private List<VisualElement> FindRows()
    {
        var list = new List<VisualElement>();
        root.Query<VisualElement>().Class("skill-row").ToList(list);
        return list;
    }

    /**************** Stage 按钮 ****************/
    private void BuildStageButtons()
    {
        Button templateBtn = null;
        if (stageOptContainer is Button tpl)
        {
            templateBtn = tpl;
            tpl.style.display = DisplayStyle.None;
            stageOptContainer = tpl.parent;
        }

        foreach (var prop in treeData.Properties().Where(p => int.TryParse(p.Name, out _)))
        {
            string skey = prop.Name;
            Button btn = new Button(() => ShowStage(skey)) { text = $"Stage {skey}" };
            btn.AddToClassList("stage-btn");

            if (templateBtn != null)
            {
                btn.style.width  = templateBtn.style.width;
                btn.style.height = templateBtn.style.height;
            }

            if (stageOptContainer is ScrollView sv)
                sv.contentContainer.Add(btn);
            else
                stageOptContainer.Add(btn);

            stageBtns[skey] = btn;
        }
    }

    /**************** 显示 Stage ****************/
    private void ShowStage(string sKey)
    {
        foreach(var kv in stageBtns) kv.Value.EnableInClassList("active",kv.Key==sKey);
        stageLabel.text=$"Stage {sKey}";
        foreach(var row in rows) row.Clear();
        viewDict.Clear();

        JObject stageObj = treeData[sKey] as JObject;
        if(stageObj==null) return;

        var items = stageObj.Properties().Where(p=>int.TryParse(p.Name,out _))
                    .Select(p=>(key:$"{sKey}-{p.Name}", arr:(JArray)p.Value))
                    .OrderBy(_=>Guid.NewGuid()).ToList();

        int[] load=new int[rows.Count]; int idx=0; var rnd=new System.Random();
        for(int r=0;r<rows.Count&&idx<items.Count;r++) add(rows[r],items[idx++],load,r);
        while(idx<items.Count){ int r=rnd.Next(rows.Count); if(load[r]<4) add(rows[r],items[idx++],load,r); }

        detailTitle.text=""; detailDesc.text=""; upgradeBtn.SetEnabled(false); curKey=null; curView=null;

        void add(VisualElement row,(string key,JArray arr) item,int[] l,int r){
             var v=CreateNode(item.key,item.arr,row);
             viewDict[item.key]=v; l[r]++;
        }
    }

    /**************** 创建节点 ****************/
    private SkillView CreateNode(string key,JArray arr,VisualElement parentRow)
    {
        string type=arr[0].ToString(); int max=arr[1].Value<int>();
        var costs=arr[2].ToObject<List<int>>(); var gains=arr[3].ToObject<List<float>>();
        var data=new SkillData{type=type,maxLvl=max,costs=costs,gains=gains};
        dataDict[key]=data; if(!levelDict.ContainsKey(key)) levelDict[key]=0;

        VisualElement c=nodeTemplate.CloneTree(); c.style.marginRight=56; parentRow.Add(c);
        var nodeRoot=c.Q<VisualElement>("icon"); nodeRoot.name=key; nodeRoot.AddToClassList("skill-node");
        //c.Q<Label>("skill-label").text = Nicify(type);

        /* ─ lvslot 圆点 ─ */
        var tplSlot=c.Q<VisualElement>("lvslot"); var slotParent=tplSlot.parent; slotParent.Remove(tplSlot);
        var lvSlots=new List<VisualElement>();
        for(int i=0;i<max;i++){ var s=new VisualElement(); s.AddToClassList("lvslot"); slotParent.Add(s); lvSlots.Add(s); }

        /* 图标 */
        Sprite sp=defaultIcon; if(sp!=null){ nodeRoot.style.backgroundImage=new StyleBackground(sp); nodeRoot.style.unityBackgroundScaleMode=ScaleMode.ScaleToFit;}

        RefreshSlotVisual(key,nodeRoot,lvSlots);
        nodeRoot.RegisterCallback<ClickEvent>(_=>SelectSkill(key));

        return new SkillView{root=c,lvSlots=lvSlots,data=data};
    }

    /**************** 选中 / 升级 ****************/
    private void SelectSkill(string key)
    {
        curKey=key; curView=viewDict[key]; var data=dataDict[key]; int lvl=levelDict[key];
        detailTitle.text=Nicify(data.type);
        string tpl=descTable.TryGetValue(data.type,out var t)?t:"暂无描述";
        string vStr=lvl>=data.maxLvl?"MAX": tpl.Contains("%")?(data.gains[lvl]*100).ToString():data.gains[lvl].ToString();
        detailDesc.text=tpl.Replace("{x}",vStr);
        upgradeBtn.SetEnabled(lvl<data.maxLvl);
        upgradeBtn.clicked-=OnUpgrade; upgradeBtn.clicked+=OnUpgrade;
    }
    private void OnUpgrade()
    {
        if(curKey==null) return; var d=dataDict[curKey]; int lvl=levelDict[curKey];
        if(lvl>=d.maxLvl||playerSpark<d.costs[lvl]) return;
        playerSpark-=d.costs[lvl]; levelDict[curKey]=++lvl;
        RefreshSlotVisual(curKey,curView.root.Q("icon"),curView.lvSlots);
        SelectSkill(curKey);
    }

    /**************** 刷新视觉 ****************/
    private void RefreshSlotVisual(string key,VisualElement nodeRoot,List<VisualElement> lvSlots)
    {
        var data=dataDict[key]; int lvl=levelDict[key];
        nodeRoot.EnableInClassList("locked",false); nodeRoot.EnableInClassList("available",false); nodeRoot.EnableInClassList("learned",false);

        for(int i=0;i<lvSlots.Count;i++)
            lvSlots[i].style.backgroundImage=new StyleBackground(i<lvl?unlockedSlotSprite:lockedSlotSprite);

        if(lvl>=data.maxLvl) nodeRoot.AddToClassList("learned");
        else if(playerSpark>=data.costs[lvl]) nodeRoot.AddToClassList("available");
        else nodeRoot.AddToClassList("locked");
    }

    /**************** 工具 ****************/
    private static string Nicify(string raw)=>string.Join(" ",raw.Split('_').Select(s=>CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s)));
}
