using System.Collections.Generic;
using Kamgam.UIToolkitScrollViewPro;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 阵容编排面板：
/// ① 调 HeroSelectionPanel 选武将
/// ② 拖 splitter 调三兵种比例
/// ③ 读 / 写 LineupDatabase
/// </summary>
public class HeroArrangePanel : MonoBehaviour
{
    /*──────── INSPECTOR ────────*/
    [SerializeField] UIDocument        uiDocument;
    [SerializeField] HeroSelectionPanel heroSelectionPanel;
    [Header("数据库")]
    [SerializeField] CardDatabaseStatic cardDB;         // 静态卡牌库
    [SerializeField] LineupDatabase     lineupDB;       // 阵容数据
    [Header("其它控制器 (可空)")]
    [SerializeField] PlayerBaseController playerBaseController;

    /*──────── 常量 ────────*/
    public float minPercentage = 10f;   // 每兵种最小比例 (%)

    /*──────── UI 节点缓存 ────────*/
    VisualElement infantry, cavalry, archer;
    Label infantryLabel, cavalryLabel, archerLabel;
    VisualElement splitter1, splitter2;

    readonly Dictionary<LineupSlot, Button> slotBtnMap  = new();
    readonly Dictionary<LineupSlot, Label>  slotNameLblMap = new();
    readonly Dictionary<int, Button>        teamBtnMap  = new();

    /*──────── 运行时变量 ────────*/
    float totalWidth;
    float infantryWidth, cavalryWidth, archerWidth;

    int   activeLineupIndex = 0;   // 当前阵容索引 (0~4)
    Button      currentBtn;        // 正在操作的按钮
    LineupSlot  pendingSlot;       // 正在写入的槽位

    /*───────────────────────────────────────────*
     *              生命周期
     *───────────────────────────────────────────*/
    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        /*—— 顶部按钮 ——*/
        root.Q<Button>("ResetCurrentCombo")?.RegisterCallback<ClickEvent>(_ => ResetCurrentLineup());
        root.Q<Button>("ReturnBtn")?.RegisterCallback<ClickEvent>(_ => playerBaseController?.HideArmyControlPage());

        /*—— 四个槽位：Label & Button ——*/
        CacheSlot(LineupSlot.Main,       "MainHeroSelectBtn", "MainText");
        CacheSlot(LineupSlot.Sub1,       "SubHero1SelectBtn", "Sub1Text");
        CacheSlot(LineupSlot.Sub2,       "SubHero2SelectBtn", "Sub2Text");
        CacheSlot(LineupSlot.Strategist, "StrategistSelectBtn", "StrategistText");

        /*—— 三兵种面板 & splitter ——*/
        infantry      = root.Q<VisualElement>("Infantry");
        cavalry       = root.Q<VisualElement>("Cavalry");
        archer        = root.Q<VisualElement>("Archer");
        infantryLabel = root.Q<Label>("InfantryLabel");
        cavalryLabel  = root.Q<Label>("CavalryLabel");
        archerLabel   = root.Q<Label>("ArcherLabel");
        splitter1     = root.Q<VisualElement>("Splitter1");
        splitter2     = root.Q<VisualElement>("Splitter2");

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
            SetInitialWidths();
        });

        SetupSplitter(splitter1,
                      () => infantryWidth, () => cavalryWidth,
                      (a, b) => { infantryWidth = a; cavalryWidth = b; });

        SetupSplitter(splitter2,
                      () => cavalryWidth, () => archerWidth,
                      (a, b) => { cavalryWidth = a; archerWidth = b; });

        /*—— 首次刷新 ——*/
        HighlightTeamButton(activeLineupIndex);
        LoadLineupVisuals();
    }

    void CacheSlot(LineupSlot slot, string btnName, string lblName)
    {
        var btn = uiDocument.rootVisualElement.Q<Button>(btnName);
        var lbl = uiDocument.rootVisualElement.Q<Label>(lblName);

        slotBtnMap[slot]   = btn;
        slotNameLblMap[slot] = lbl;

        btn.clicked += () =>
        {
            currentBtn  = btn;
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
        if (lineupDB == null || idx < 0 || idx >= lineupDB.lineups.Count) return;

        activeLineupIndex = idx;
        HighlightTeamButton(idx);

        ApplyWidthsFromRatio(lineupDB.lineups[idx].ratio);
        LoadLineupVisuals();

        Debug.Log($"切换到阵容 {idx + 1}");
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

    /*───────────────────────────────────────────*
     *              选武将
     *───────────────────────────────────────────*/
    void OnHeroChosen(CardInfoStatic chosenStatic, PlayerCard _)
    {
        if (lineupDB == null || lineupDB.lineups.Count == 0) return;

        var info = lineupDB.lineups[activeLineupIndex];
        string name = chosenStatic.displayName;

        // 去重：若已在其他槽 → 清掉
        RemoveDuplicate(ref info.mainGeneral,   LineupSlot.Main,        name);
        RemoveDuplicate(ref info.subGeneral1,   LineupSlot.Sub1,        name);
        RemoveDuplicate(ref info.subGeneral2,   LineupSlot.Sub2,        name);
        RemoveDuplicate(ref info.strategist,    LineupSlot.Strategist,  name);

        // 写入当前槽
        switch (pendingSlot)
        {
            case LineupSlot.Main:       info.mainGeneral = name;   break;
            case LineupSlot.Sub1:       info.subGeneral1 = name;   break;
            case LineupSlot.Sub2:       info.subGeneral2 = name;   break;
            case LineupSlot.Strategist: info.strategist  = name;   break;
        }

        lineupDB.lineups[activeLineupIndex] = info;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(lineupDB);
#endif

        SetHeroVisual(chosenStatic, slotBtnMap[pendingSlot]);
        SetHeroName  (chosenStatic, slotNameLblMap[pendingSlot]);

        Debug.Log($"[{pendingSlot}] 设为 {name}");
    }

    void RemoveDuplicate(ref string field, LineupSlot slot, string newName)
    {
        if (field == newName)
        {
            field = string.Empty;
            SetHeroVisual(null, slotBtnMap[slot]);
            SetHeroName  (null, slotNameLblMap[slot]);
        }
    }

    /*───────────────────────────────────────────*
     *              Reset & Load
     *───────────────────────────────────────────*/
    void ResetCurrentLineup()
    {
        if (lineupDB == null || lineupDB.lineups.Count == 0) return;

        var info = lineupDB.lineups[activeLineupIndex];
        info.mainGeneral =
        info.subGeneral1 =
        info.subGeneral2 =
        info.strategist  = string.Empty;
        info.ratio = new TroopRatio { infantry = 30, cavalry = 40, archer = 30 };
        lineupDB.lineups[activeLineupIndex] = info;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(lineupDB);
#endif
        LoadLineupVisuals();
        Debug.Log($"阵容 {activeLineupIndex} 已完全重置");
    }

    void LoadLineupVisuals()
    {
        if (lineupDB == null || cardDB == null || lineupDB.lineups.Count == 0) return;
        if (activeLineupIndex < 0 || activeLineupIndex >= lineupDB.lineups.Count) return;

        var info = lineupDB.lineups[activeLineupIndex];

        ApplySlot(LineupSlot.Main,       info.mainGeneral);
        ApplySlot(LineupSlot.Sub1,       info.subGeneral1);
        ApplySlot(LineupSlot.Sub2,       info.subGeneral2);
        ApplySlot(LineupSlot.Strategist, info.strategist);

        ApplyWidthsFromRatio(info.ratio);
    }

    void ApplySlot(LineupSlot slot, string name)
    {
        var card = cardDB.Get(name);
        SetHeroVisual(card, slotBtnMap[slot]);
        SetHeroName  (card, slotNameLblMap[slot]);
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
            btn.style.backgroundImage          = new StyleBackground(pic);
            btn.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }
        else btn.style.backgroundImage = StyleKeyword.None;
    }

    void SetHeroName(CardInfoStatic info, Label lbl)
    {
        if (lbl == null) return;
        if (info == null)
        {
            lbl.text        = "";
            lbl.style.color = Color.gray;
        }
        else
        {
            lbl.text        = info.displayName;
            lbl.style.color = Color.white;
        }
    }

    /*───────────────────────────────────────────*
     *              三兵种宽度
     *───────────────────────────────────────────*/
    void SetInitialWidths()
    {
        infantryWidth = totalWidth * 0.3f;
        cavalryWidth  = totalWidth * 0.4f;
        archerWidth   = totalWidth * 0.3f;
        ApplyWidths();
    }

    void ApplyWidthsFromRatio(TroopRatio r)
    {
        infantryWidth = totalWidth * r.infantry / 100f;
        cavalryWidth  = totalWidth * r.cavalry  / 100f;
        archerWidth   = totalWidth * r.archer   / 100f;
        ApplyWidths();
    }

    void ApplyWidths()
    {
        infantry.style.width = infantryWidth;
        cavalry.style.width  = cavalryWidth;
        archer.style.width   = archerWidth;

        float sum = infantryWidth + cavalryWidth + archerWidth;
        int inf = Mathf.RoundToInt(infantryWidth / sum * 100f);
        int cav = Mathf.RoundToInt(cavalryWidth  / sum * 100f);
        int arc = Mathf.RoundToInt(archerWidth   / sum * 100f);

        infantryLabel.text = $"步兵: {inf}%";
        cavalryLabel.text  = $"骑兵: {cav}%";
        archerLabel.text   = $"弓兵: {arc}%";

        if (lineupDB == null || activeLineupIndex >= lineupDB.lineups.Count) return;
        var info = lineupDB.lineups[activeLineupIndex];
        info.ratio.infantry = inf; info.ratio.cavalry = cav; info.ratio.archer = arc;
        lineupDB.lineups[activeLineupIndex] = info;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(lineupDB);
#endif
    }

    /*───────────────────────────────────────────*
     *              Splitter 拖拽
     *───────────────────────────────────────────*/
    void SetupSplitter(VisualElement splitter,
                       System.Func<float> getLeft,
                       System.Func<float> getRight,
                       System.Action<float, float> setWidths)
    {
        bool  dragging    = false;
        float startX      = 0f;
        float initLeft    = 0f;
        float initRight   = 0f;
        int   pointerId   = -1;

        splitter.RegisterCallback<PointerDownEvent>(evt =>
        {
            dragging   = true;
            pointerId  = evt.pointerId;
            startX     = evt.position.x;
            initLeft   = getLeft();
            initRight  = getRight();
            splitter.CapturePointer(pointerId);
            splitter.style.backgroundColor = Color.red;
        });

        splitter.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!dragging || !splitter.HasPointerCapture(pointerId)) return;
            float delta = evt.position.x - startX;
            float min = totalWidth * minPercentage / 100f;

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
}
