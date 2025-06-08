using System.Collections.Generic;
using Kamgam.UIToolkitScrollViewPro;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// 阵容编排面板：
/// ① 调 HeroSelectionPanel 选武将
/// ② 拖 splitter 调三兵种比例
/// ③ 读 / 写 LineupDatabase
/// </summary>
public class HeroArrangePanel : MonoBehaviour
{
    /*──────── INSPECTOR ────────*/
    [SerializeField] UIDocument uiDocument;
    [SerializeField] HeroSelectionPanel heroSelectionPanel;
    [Header("数据库")]
    [SerializeField] CardDatabaseStatic cardDB;         // 静态卡牌库
    [SerializeField] LineupDatabase lineupDB;       // 阵容数据
    LineupDatabase runtimeDB;                        // 每次进入面板克隆
    LineupDatabase db => runtimeDB;
    [Header("其它控制器 (可空)")]
    [SerializeField] PlayerBaseController playerBaseController;

    /*──────── 常量 ────────*/
    public float minPercentage = 10f;   // 每兵种最小比例 (%)

    /*──────── UI 节点缓存 ────────*/
    VisualElement infantry, cavalry, archer;
    Label infantryLabel, cavalryLabel, archerLabel;
    VisualElement splitter1, splitter2;
    bool dirty = false;           // 只要动过就置 true
    Button saveBtn;               // 保存按钮

    readonly Dictionary<LineupSlot, Button> slotBtnMap = new();
    readonly Dictionary<LineupSlot, Label> slotNameLblMap = new();
    readonly Dictionary<int, Button> teamBtnMap = new();

    /*──────── 运行时变量 ────────*/
    float totalWidth;
    float infantryWidth, cavalryWidth, archerWidth;

    int activeLineupIndex = 0;   // 当前阵容索引 (0~4)
    Button currentBtn;        // 正在操作的按钮
    LineupSlot pendingSlot;       // 正在写入的槽位

    /*───────────────────────────────────────────*
     *              生命周期
     *───────────────────────────────────────────*/
    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        runtimeDB = Instantiate(lineupDB);
        dirty = false;

        saveBtn = root.Q<Button>("SaveButton");
        if (saveBtn != null)
            saveBtn.clicked += SaveCurrentLineup;

        /*—— 顶部按钮 ——*/
        root.Q<Button>("ResetCurrentCombo")?.RegisterCallback<ClickEvent>(_ => ResetCurrentLineup());
        var returnBtn = root.Q<Button>("ReturnBtn");
        if (returnBtn != null)
            returnBtn.clicked += TryLeavePanel;

        /*—— 四个槽位：Label & Button ——*/
        CacheSlot(LineupSlot.Main, "MainHeroSelectBtn", "MainText");
        CacheSlot(LineupSlot.Sub1, "SubHero1SelectBtn", "Sub1Text");
        CacheSlot(LineupSlot.Sub2, "SubHero2SelectBtn", "Sub2Text");
        CacheSlot(LineupSlot.Strategist, "StrategistSelectBtn", "StrategistText");

        /*—— 三兵种面板 & splitter ——*/
        infantry = root.Q<VisualElement>("Infantry");
        cavalry = root.Q<VisualElement>("Cavalry");
        archer = root.Q<VisualElement>("Archer");
        infantryLabel = root.Q<Label>("InfantryLabel");
        cavalryLabel = root.Q<Label>("CavalryLabel");
        archerLabel = root.Q<Label>("ArcherLabel");
        splitter1 = root.Q<VisualElement>("Splitter1");
        splitter2 = root.Q<VisualElement>("Splitter2");

        /*—— Team1~5 ——*/
        for (int i = 0; i < 5; ++i)
        {
            string name = $"Team{i + 1}";
            teamBtnMap[i] = root.Q<Button>(name);
            int idx = i;
            teamBtnMap[i]?.RegisterCallback<ClickEvent>(_ => SwitchTeam(idx));
        }

        /*—— 几何 & split ——*/
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

        HighlightTeamButton(activeLineupIndex);

        root.schedule.Execute(() =>
        {
            LoadLineupVisuals();         // 延迟到 Geometry 事件之后执行
        }).ExecuteLater(0);              // 0 = 下一帧

    }

    void CacheSlot(LineupSlot slot, string btnName, string lblName)
    {
        var btn = uiDocument.rootVisualElement.Q<Button>(btnName);
        var lbl = uiDocument.rootVisualElement.Q<Label>(lblName);

        slotBtnMap[slot] = btn;
        slotNameLblMap[slot] = lbl;

        btn.clicked += () =>
        {
            currentBtn = btn;
            pendingSlot = slot;
            heroSelectionPanel.Open(slot, OnHeroChosen);
        };
    }

    /*───────────────────────────────────────────*
     *              阵容切换
     *───────────────────────────────────────────*/
    void SwitchTeam(int idx)
    {
        if (idx == activeLineupIndex) return;
        if (db == null || idx < 0 || idx >= db.lineups.Count) return;

        activeLineupIndex = idx;
        HighlightTeamButton(idx);

        ApplyWidthsFromRatio(db.lineups[idx].ratio);
        LoadLineupVisuals();

        Debug.Log($"切换到阵容 {idx + 1}");
    }

    void HighlightTeamButton(int idx)
    {
        foreach (var kv in teamBtnMap)
        {
            if (kv.Value == null) continue;
            if (kv.Key == idx) kv.Value.AddToClassList("teamselected");
            else kv.Value.RemoveFromClassList("teamselected");
        }
    }

    /*───────────────────────────────────────────*
     *              选武将
     *───────────────────────────────────────────*/
    void OnHeroChosen(CardInfoStatic chosenStatic, PlayerCard _)
    {
        if (db == null || db.lineups.Count == 0) return;

        var info = db.lineups[activeLineupIndex];
        string id = chosenStatic.id;           // ★ 现在存 id

        // 去重：若已在其他槽 → 清掉
        RemoveDuplicate(ref info.mainId, LineupSlot.Main, id);
        RemoveDuplicate(ref info.subId1, LineupSlot.Sub1, id);
        RemoveDuplicate(ref info.subId2, LineupSlot.Sub2, id);
        RemoveDuplicate(ref info.strategistId, LineupSlot.Strategist, id);

        // 写入当前槽
        switch (pendingSlot)
        {
            case LineupSlot.Main: info.mainId = id; break;
            case LineupSlot.Sub1: info.subId1 = id; break;
            case LineupSlot.Sub2: info.subId2 = id; break;
            case LineupSlot.Strategist: info.strategistId = id; break;
        }

        db.lineups[activeLineupIndex] = info;
        dirty = true;                               // ← 标记已修改

        // UI 刷新
        SetHeroVisual(chosenStatic, slotBtnMap[pendingSlot]);
        SetHeroName(chosenStatic, slotNameLblMap[pendingSlot]);

        Debug.Log($"[{pendingSlot}] 设为 {chosenStatic.displayName}");
    }


    void RemoveDuplicate(ref string field, LineupSlot slot, string newId)
    {
        if (field == newId)
        {
            field = string.Empty;
            SetHeroVisual(null, slotBtnMap[slot]);
            SetHeroName(null, slotNameLblMap[slot]);
        }
    }

    /*───────────────────────────────────────────*
     *              Reset & Load
     *───────────────────────────────────────────*/
    void ResetCurrentLineup()
    {
        if (db == null || db.lineups.Count == 0) return;

        var info = db.lineups[activeLineupIndex];
        info.mainId =
        info.subId1 =
        info.subId2 =
        info.strategistId = string.Empty;

        info.ratio = new TroopRatio { infantry = 30, cavalry = 40, archer = 30 };
        db.lineups[activeLineupIndex] = info;
        dirty = true;

        LoadLineupVisuals();
        Debug.Log($"阵容 {activeLineupIndex} 已完全重置");
    }

    void LoadLineupVisuals()
    {
        if (db == null || cardDB == null || db.lineups.Count == 0) return;
        if (activeLineupIndex < 0 || activeLineupIndex >= db.lineups.Count) return;

        var info = db.lineups[activeLineupIndex];

        ApplySlot(LineupSlot.Main, info.mainId);
        ApplySlot(LineupSlot.Sub1, info.subId1);
        ApplySlot(LineupSlot.Sub2, info.subId2);
        ApplySlot(LineupSlot.Strategist, info.strategistId);

        ApplyWidthsFromRatio(info.ratio);
    }
    void SaveCurrentLineup()
    {
#if UNITY_EDITOR
        if (!dirty) { Debug.Log("无改动，不保存"); return; }

        // ① 用浅拷贝同步列表
        lineupDB.lineups.Clear();
        foreach (var l in db.lineups)
            lineupDB.lineups.Add(l);

        UnityEditor.EditorUtility.SetDirty(lineupDB);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        dirty = false;
        Debug.Log("<color=lime>阵容已保存</color>");
#else
        // TODO: 写 JSON / 后端
#endif
    }

    void ApplySlot(LineupSlot slot, string id)
    {
        var card = cardDB.Get(id);               // 根据 id 取静态卡
        SetHeroVisual(card, slotBtnMap[slot]);
        SetHeroName(card, slotNameLblMap[slot]);
    }

    /*───────────────────────────────────────────*
     *              视觉 helpers
     *───────────────────────────────────────────*/
    void SetHeroVisual(CardInfoStatic info, Button btn)
    {
        if (btn == null) return;
        Sprite pic = info?.fullBodySprite ?? info?.iconSprite;
        if (pic != null)
        {
            btn.style.backgroundImage = new StyleBackground(pic);
            btn.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }
        else btn.style.backgroundImage = StyleKeyword.None;
    }

    void SetHeroName(CardInfoStatic info, Label lbl)
    {
        if (lbl == null) return;
        if (info == null)
        {
            lbl.text = "";
            lbl.style.color = Color.gray;
        }
        else
        {
            lbl.text = info.displayName;
            lbl.style.color = Color.white;
        }
    }

    /*───────────────────────────────────────────*
     *              三兵种宽度
     *───────────────────────────────────────────*/
    void SetInitialWidths()
    {
        infantryWidth = totalWidth * 0.3f;
        cavalryWidth = totalWidth * 0.4f;
        archerWidth = totalWidth * 0.3f;
        ApplyWidths();
    }

    void ApplyWidthsFromRatio(TroopRatio r)
    {
        infantryWidth = totalWidth * r.infantry / 100f;
        cavalryWidth = totalWidth * r.cavalry / 100f;
        archerWidth = totalWidth * r.archer / 100f;
        ApplyWidths();
    }

    /// <summary>
/// 根据当前 infantryWidth / cavalryWidth / archerWidth
/// 更新三条宽度 & 百分比标签；
/// 如果比例有改变才写回 db 并置 dirty。
/// </summary>
    void ApplyWidths()
    {
        /*── 1. 视觉刷新 ───────────────────*/
        infantry.style.width = infantryWidth;
        cavalry.style.width  = cavalryWidth;
        archer.style.width   = archerWidth;

        float sum = infantryWidth + cavalryWidth + archerWidth;
        int inf = Mathf.RoundToInt(infantryWidth / sum * 100f);
        int cav = Mathf.RoundToInt(cavalryWidth  / sum * 100f);
        int arc = 100 - inf - cav;                   // 保证和 = 100

        infantryLabel.text = $"步兵: {inf}%";
        cavalryLabel.text  = $"骑兵: {cav}%";
        archerLabel.text   = $"弓兵: {arc}%";

        /*── 2. 写回数据（仅在改变时） ───────────*/
        if (db == null || activeLineupIndex >= db.lineups.Count)
            return;

        var info = db.lineups[activeLineupIndex];

        bool changed = info.ratio.infantry != inf ||
                    info.ratio.cavalry  != cav ||
                    info.ratio.archer   != arc;

        if (changed)
        {
            info.ratio.infantry = inf;
            info.ratio.cavalry  = cav;
            info.ratio.archer   = arc;

            db.lineups[activeLineupIndex] = info;
            dirty = true;                       // ← 只有真正修改时才置脏
        }
    }


    /*───────────────────────────────────────────*
     *              Splitter 拖拽
     *───────────────────────────────────────────*/
    void SetupSplitter(VisualElement splitter,
                       System.Func<float> getLeft,
                       System.Func<float> getRight,
                       System.Action<float, float> setWidths)
    {
        bool dragging = false;
        float startX = 0f;
        float initLeft = 0f;
        float initRight = 0f;
        int pointerId = -1;

        splitter.RegisterCallback<PointerDownEvent>(evt =>
        {
            dragging = true;
            pointerId = evt.pointerId;
            startX = evt.position.x;
            initLeft = getLeft();
            initRight = getRight();
            splitter.CapturePointer(pointerId);
            splitter.style.backgroundColor = Color.red;
        });

        splitter.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!dragging || !splitter.HasPointerCapture(pointerId)) return;
            float delta = evt.position.x - startX;
            float min = totalWidth * minPercentage / 100f;

            float newLeft = Mathf.Clamp(initLeft + delta, min, totalWidth - min);
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
    void TryLeavePanel()
    {
        // 1) 没改动 → 直接离开
        if (!dirty)
        {
            playerBaseController?.HideArmyControlPage();
            return;
        }

        // 2) 有改动 → 弹确认框
        PopupManager.ShowConfirm(
            "当前阵容尚未保存，\n确定离开并丢弃修改吗？",
            onYes: () =>          // 「丢弃并离开」
            {
                dirty = false;    // 丢弃标记
                playerBaseController?.HideArmyControlPage();
            },
            onNo: () =>           // 「保存并离开」
            {
                SaveCurrentLineup();                   // 调用你已有的保存函数
                playerBaseController?.HideArmyControlPage();
            },
            yesText: "丢弃并离开",
            noText:  "保存并离开");
    }



}