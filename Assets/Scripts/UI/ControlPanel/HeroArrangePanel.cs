// Assets/Scripts/Game/UI/HeroArrangePanel.cs
using System.IO;
using System.Collections.Generic;
using Kamgam.UIToolkitScrollViewPro;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 阵容编排面板：<br/>
/// ① 调 HeroSelectionPanel 选武将<br/>
/// ② 拖 splitter 调三兵种比例<br/>
/// ③ 读 / 写 LineupDatabase（Editor）或 JSON（移动端）<br/>
/// </summary>
public class HeroArrangePanel : MonoBehaviour
{
    /*──────── INSPECTOR ────────*/
    [SerializeField] UIDocument uiDocument;
    [SerializeField] HeroSelectionPanel heroSelectionPanel;

    [Header("数据库")]
    [SerializeField] CardDatabaseStatic cardDB;   // 静态卡牌库
    [SerializeField] LineupDatabase lineupDB;     // 仅供 Editor 保存
    LineupDatabase runtimeDB;                     // 运行时克隆 / 反序列化
    LineupDatabase db => runtimeDB;

    [Header("其它控制器 (可空)")]
    [SerializeField] PlayerBaseController playerBaseController;

    /*──────── 常量 ────────*/
    public float minPercentage = 10f;   // 每兵种最小比例 (%)

    /*──────── UI 节点缓存 ────────*/
    VisualElement infantry, cavalry, archer;
    Label infantryLabel, cavalryLabel, archerLabel;
    VisualElement splitter1, splitter2;
    Button saveBtn;

    readonly Dictionary<LineupSlot, Button> slotBtnMap   = new();
    readonly Dictionary<LineupSlot, Label>  slotNameLblMap = new();
    readonly Dictionary<int, Button>        teamBtnMap   = new();

    /*──────── 运行时变量 ────────*/
    bool dirty = false;                 // 只要动过就置 true
    float totalWidth;
    float infantryWidth, cavalryWidth, archerWidth;
    int activeLineupIndex = 0;          // 当前阵容索引 (0~4)
    Button currentBtn;                  // 正在操作的按钮
    LineupSlot pendingSlot;             // 正在写入的槽位

    /*───────────────────────────────────────────*
     *              生命周期
     *───────────────────────────────────────────*/
    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        /*──── ① 先尝试从 JSON 读取（移动端持久化）────*/
        runtimeDB = LoadFromJson() ?? Instantiate(lineupDB);
        dirty = false;

        /*──── ② 顶部按钮 ────*/
        saveBtn = root.Q<Button>("SaveButton");
        if (saveBtn != null) saveBtn.clicked += SaveCurrentLineup;

        root.Q<Button>("ResetCurrentCombo")?.RegisterCallback<ClickEvent>(_ => ResetCurrentLineup());
        root.Q<Button>("ReturnBtn")?.RegisterCallback<ClickEvent>(_ => TryLeavePanel());

        /*──── ③ 四个槽位缓存 ────*/
        CacheSlot(LineupSlot.Main,       "MainHeroSelectBtn", "MainText");
        CacheSlot(LineupSlot.Sub1,       "SubHero1SelectBtn", "Sub1Text");
        CacheSlot(LineupSlot.Sub2,       "SubHero2SelectBtn", "Sub2Text");
        CacheSlot(LineupSlot.Strategist, "StrategistSelectBtn", "StrategistText");

        /*──── ④ 三兵种面板 & splitter ────*/
        infantry      = root.Q<VisualElement>("Infantry");
        cavalry       = root.Q<VisualElement>("Cavalry");
        archer        = root.Q<VisualElement>("Archer");
        infantryLabel = root.Q<Label>("InfantryLabel");
        cavalryLabel  = root.Q<Label>("CavalryLabel");
        archerLabel   = root.Q<Label>("ArcherLabel");
        splitter1     = root.Q<VisualElement>("Splitter1");
        splitter2     = root.Q<VisualElement>("Splitter2");

        /*──── ⑤ Team1~5 切换按钮 ────*/
        for (int i = 0; i < 5; ++i)
        {
            string name = $"Team{i + 1}";
            teamBtnMap[i] = root.Q<Button>(name);
            int idx = i;
            teamBtnMap[i]?.RegisterCallback<ClickEvent>(_ => SwitchTeam(idx));
        }
        HighlightTeamButton(activeLineupIndex);

        /*──── ⑥ Geometry 回调 & Splitter 拖拽 ────*/
        root.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            totalWidth = root.resolvedStyle.width;
            ApplyWidthsFromRatio(db.lineups[activeLineupIndex].ratio);
        });

        SetupSplitter(splitter1,
            () => infantryWidth, () => cavalryWidth,
            (a, b) => { infantryWidth = a; cavalryWidth = b; });

        SetupSplitter(splitter2,
            () => cavalryWidth, () => archerWidth,
            (a, b) => { cavalryWidth = a; archerWidth = b; });

        /*──── ⑦ 延迟一帧加载视觉 ────*/
        root.schedule.Execute(LoadLineupVisuals).ExecuteLater(0);
    }

    /*───────────────────────────────────────────*
     *              阵容切换 / 选武将
     *───────────────────────────────────────────*/
    void SwitchTeam(int idx)
    {
        if (idx == activeLineupIndex || idx < 0 || idx >= db.lineups.Count) return;
        activeLineupIndex = idx;
        HighlightTeamButton(idx);
        ApplyWidthsFromRatio(db.lineups[idx].ratio);
        LoadLineupVisuals();
    }
    void HighlightTeamButton(int idx)
    {
        foreach (var kv in teamBtnMap)
        {
            if (kv.Value == null) continue;
            if (kv.Key == idx) kv.Value.AddToClassList("teamselected");
            else               kv.Value.RemoveFromClassList("teamselected");
        }
    }

    void CacheSlot(LineupSlot slot, string btnName, string lblName)
    {
        var btn = uiDocument.rootVisualElement.Q<Button>(btnName);
        var lbl = uiDocument.rootVisualElement.Q<Label>(lblName);
        slotBtnMap[slot]     = btn;
        slotNameLblMap[slot] = lbl;

        btn.clicked += () =>
        {
            currentBtn  = btn;
            pendingSlot = slot;
            heroSelectionPanel.Open(slot, OnHeroChosen);
        };
    }

    void OnHeroChosen(CardInfoStatic chosenStatic, PlayerCard _)
    {
        if (db == null) return;
        var info = db.lineups[activeLineupIndex];
        string id = chosenStatic.id;

        // 去重
        RemoveDuplicate(ref info.mainId,       LineupSlot.Main,       id);
        RemoveDuplicate(ref info.subId1,       LineupSlot.Sub1,       id);
        RemoveDuplicate(ref info.subId2,       LineupSlot.Sub2,       id);
        RemoveDuplicate(ref info.strategistId, LineupSlot.Strategist, id);

        // 写入
        switch (pendingSlot)
        {
            case LineupSlot.Main:       info.mainId       = id; break;
            case LineupSlot.Sub1:       info.subId1       = id; break;
            case LineupSlot.Sub2:       info.subId2       = id; break;
            case LineupSlot.Strategist: info.strategistId = id; break;
        }
        db.lineups[activeLineupIndex] = info;
        dirty = true;

        SetHeroVisual(chosenStatic,   slotBtnMap[pendingSlot]);
        SetHeroName  (chosenStatic,   slotNameLblMap[pendingSlot]);
    }
    void RemoveDuplicate(ref string field, LineupSlot slot, string newId)
    {
        if (field == newId)
        {
            field = string.Empty;
            SetHeroVisual(null, slotBtnMap[slot]);
            SetHeroName(null,   slotNameLblMap[slot]);
        }
    }

    /*───────────────────────────────────────────*
     *              保存 / 读取
     *───────────────────────────────────────────*/
    void SaveCurrentLineup()
    {
#if UNITY_EDITOR
        if (!dirty) { Debug.Log("无改动，不保存"); return; }

        lineupDB.lineups.Clear();
        lineupDB.lineups.AddRange(db.lineups);

        UnityEditor.EditorUtility.SetDirty(lineupDB);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        dirty = false;
        Debug.Log("<color=lime>阵容已保存 (Editor)</color>");
#else
        try
        {
            string json = JsonUtility.ToJson(db, false);
            File.WriteAllText(GetJsonPath(), json);
            dirty = false;
            Debug.Log("<color=lime>阵容已保存 (JSON)</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"保存阵容失败: {ex.Message}");
        }
#endif
    }
    LineupDatabase LoadFromJson()
    {
        string path = GetJsonPath();
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            var clone   = ScriptableObject.CreateInstance<LineupDatabase>();
            JsonUtility.FromJsonOverwrite(json, clone);
            return clone;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"读取阵容 JSON 失败: {ex.Message}");
            return null;
        }
    }
    string GetJsonPath() =>
        Path.Combine(Application.persistentDataPath, "lineups.json");

    /*───────────────────────────────────────────*
     *              Reset / Visual
     *───────────────────────────────────────────*/
    void ResetCurrentLineup()
    {
        var info = db.lineups[activeLineupIndex];
        info.mainId = info.subId1 = info.subId2 = info.strategistId = string.Empty;
        info.ratio  = new TroopRatio { infantry = 30, cavalry = 40, archer = 30 };
        db.lineups[activeLineupIndex] = info;
        dirty = true;
        LoadLineupVisuals();
    }

    void LoadLineupVisuals()
    {
        if (activeLineupIndex >= db.lineups.Count) return;
        var info = db.lineups[activeLineupIndex];
        ApplySlot(LineupSlot.Main,       info.mainId);
        ApplySlot(LineupSlot.Sub1,       info.subId1);
        ApplySlot(LineupSlot.Sub2,       info.subId2);
        ApplySlot(LineupSlot.Strategist, info.strategistId);
        ApplyWidthsFromRatio(info.ratio);
    }
    void ApplySlot(LineupSlot slot, string id)
    {
        var card = string.IsNullOrEmpty(id) ? null : cardDB.Get(id);
        SetHeroVisual(card, slotBtnMap[slot]);
        SetHeroName(card,   slotNameLblMap[slot]);
    }

    /*──────── Hero Visual helpers ────────*/
    void SetHeroVisual(CardInfoStatic info, Button btn)
    {
        if (btn == null) return;
        var pic = info?.fullBodySprite ?? info?.iconSprite;
        if (pic != null)
        {
            btn.style.backgroundImage          = new StyleBackground(pic);
            btn.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }
        else btn.style.backgroundImage = StyleKeyword.None;
    }
    void SetHeroName(CardInfoStatic info, Label lbl)
    {
        if (lbl == null) return;
        lbl.text       = info != null ? info.displayName : "";
        lbl.style.color = info != null ? Color.white     : Color.gray;
    }

    /*───────────────────────────────────────────*
     *              三兵种比例 & Splitter
     *───────────────────────────────────────────*/
    void ApplyWidthsFromRatio(TroopRatio r)
    {
        infantryWidth = totalWidth * r.infantry / 100f;
        cavalryWidth  = totalWidth * r.cavalry  / 100f;
        archerWidth   = totalWidth * r.archer   / 100f;
        ApplyWidths();
    }
    void ApplyWidths()
    {
        /* 1) 视觉 */
        infantry.style.width = infantryWidth;
        cavalry.style.width  = cavalryWidth;
        archer.style.width   = archerWidth;

        /* 2) 计算整数比例并更新标签 */
        float sum = infantryWidth + cavalryWidth + archerWidth;
        int inf = Mathf.RoundToInt(infantryWidth / sum * 100f);
        int cav = Mathf.RoundToInt(cavalryWidth  / sum * 100f);
        int arc = 100 - inf - cav;

        infantryLabel.text = $"步兵: {inf}%";
        cavalryLabel.text  = $"骑兵: {cav}%";
        archerLabel.text   = $"弓兵: {arc}%";

        /* 3) 写回数据 */
        var info = db.lineups[activeLineupIndex];
        if (info.ratio.infantry != inf ||
            info.ratio.cavalry  != cav ||
            info.ratio.archer   != arc)
        {
            info.ratio.infantry = inf;
            info.ratio.cavalry  = cav;
            info.ratio.archer   = arc;
            db.lineups[activeLineupIndex] = info;
            dirty = true;
        }
    }

    void SetupSplitter(VisualElement splitter,
                       System.Func<float> getLeft,
                       System.Func<float> getRight,
                       System.Action<float, float> setWidths)
    {
        bool   dragging = false;
        float  startX   = 0f, initLeft = 0f, initRight = 0f;
        int    pointerId = -1;

        splitter.RegisterCallback<PointerDownEvent>(evt =>
        {
            dragging  = true;
            pointerId = evt.pointerId;
            startX    = evt.position.x;
            initLeft  = getLeft();
            initRight = getRight();
            splitter.CapturePointer(pointerId);
            splitter.style.backgroundColor = Color.red;
        });

        splitter.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!dragging || evt.pointerId != pointerId) return;
            float delta = evt.position.x - startX;
            float min   = totalWidth * minPercentage / 100f;

            float newLeft  = Mathf.Clamp(initLeft  + delta, min, totalWidth - min);
            float newRight = Mathf.Clamp(initRight - delta, min, totalWidth - min);

            setWidths(newLeft, newRight);
            ApplyWidths();
        });

        splitter.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!dragging || evt.pointerId != pointerId) return;
            splitter.ReleasePointer(pointerId);
            dragging = false;
            splitter.style.backgroundColor = Color.white;
        });
    }

    /*───────────────────────────────────────────*
     *              离开面板确认
     *───────────────────────────────────────────*/
    void TryLeavePanel()
    {
        if (!dirty) { playerBaseController?.HideArmyControlPage(); return; }

        PopupManager.ShowConfirm(
            "当前阵容尚未保存，\n确定离开并丢弃修改吗？",
            onYes: () => { dirty = false; playerBaseController?.HideArmyControlPage(); },
            onNo:  () => { SaveCurrentLineup(); playerBaseController?.HideArmyControlPage(); },
            yesText: "丢弃并离开",
            noText:  "保存并离开");
    }
}
