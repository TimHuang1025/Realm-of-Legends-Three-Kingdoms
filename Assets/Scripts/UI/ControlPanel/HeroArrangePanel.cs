using System.Collections.Generic;
using Kamgam.UIToolkitScrollViewPro;
using UnityEngine;
using UnityEngine.UIElements;

public class HeroArrangePanel : MonoBehaviour
{
    [SerializeField] private HeroSelectionPanel heroSelectionPanel;
    public UIDocument uiDocument;
    LineupDatabase lineupDatabase; // Reference to the lineup database
    public float minPercentage = 10f; // Minimum width percentage per section (e.g., 10%)

    private VisualElement infantry, cavalry, archer;
    private Label infantryLabel, cavalryLabel, archerLabel;
    private VisualElement splitter1, splitter2;
    [SerializeField] private CardDatabase cardDB; 
    [SerializeField] private PlayerBaseController playerBaseController;
    

    private float totalWidth;
    private float infantryWidth, cavalryWidth, archerWidth;
    private ScrollViewPro heroPool; // The scroll view for the hero selection pool
    private Button currentBtn;
    [SerializeField] private LineupDatabase lineupDB;   // 记得 Inspector 里拖 asset
    [SerializeField] private int activeLineupIndex = 0; // 默认写第 1 条阵容，可做 UI 切换
    private Dictionary<int, Button> teamBtnMap = new();

    private LineupSlot pendingSlot;   // 记录这次要写哪个槽位
    private Dictionary<LineupSlot, Button> slotBtnMap = new();
    
    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        Button resetBtn = root.Q<Button>("ResetCurrentCombo");
        resetBtn.clicked += ResetCurrentLineup;

        Button returnBtn = root.Q<Button>("ReturnBtn");
        if (returnBtn != null) returnBtn.clicked += () =>
        {
            playerBaseController?.HideArmyControlPage();
        };

        /* 1. 先缓存四个按钮 */
        Button mainHeroBtn = root.Q<Button>("MainHeroSelectBtn");
        Button subHero1Btn = root.Q<Button>("SubHero1SelectBtn");
        Button subHero2Btn = root.Q<Button>("SubHero2SelectBtn");
        Button strategistBtn = root.Q<Button>("StrategistSelectBtn");

        slotBtnMap[LineupSlot.Main] = mainHeroBtn;
        slotBtnMap[LineupSlot.Sub1] = subHero1Btn;
        slotBtnMap[LineupSlot.Sub2] = subHero2Btn;
        slotBtnMap[LineupSlot.Strategist] = strategistBtn;

        /* 2. 给每个按钮单独绑定点击事件 */
        mainHeroBtn.clicked += () =>
        {
            currentBtn = mainHeroBtn;
            pendingSlot = LineupSlot.Main;
            heroSelectionPanel.Open(LineupSlot.Main, OnHeroChosen);
        };

        subHero1Btn.clicked += () =>
        {
            currentBtn = subHero1Btn;
            pendingSlot = LineupSlot.Sub1;
            heroSelectionPanel.Open(LineupSlot.Sub1, OnHeroChosen);
        };

        subHero2Btn.clicked += () =>
        {
            currentBtn = subHero2Btn;
            pendingSlot = LineupSlot.Sub2;
            heroSelectionPanel.Open(LineupSlot.Sub2, OnHeroChosen);
        };

        strategistBtn.clicked += () =>
        {
            currentBtn = strategistBtn;
            pendingSlot = LineupSlot.Strategist;
            heroSelectionPanel.Open(LineupSlot.Strategist, OnHeroChosen);
        };

        infantry = root.Q<VisualElement>("Infantry");
        cavalry = root.Q<VisualElement>("Cavalry");
        archer = root.Q<VisualElement>("Archer");

        infantryLabel = root.Q<Label>("InfantryLabel");
        cavalryLabel = root.Q<Label>("CavalryLabel");
        archerLabel = root.Q<Label>("ArcherLabel");

        splitter1 = root.Q<VisualElement>("Splitter1");
        splitter2 = root.Q<VisualElement>("Splitter2");

        root.RegisterCallback<GeometryChangedEvent>(evt =>  // Initialize the root container containing the categories and splitters
        {
            totalWidth = root.resolvedStyle.width;
            SetInitialWidths();
        });

        SetupSplitter(splitter1, () => infantryWidth, () => cavalryWidth, (newA, newB) =>  // Sets up a splitter for infantry and cavalry
        {
            infantryWidth = newA;
            cavalryWidth = newB;
        });

        SetupSplitter(splitter2, () => cavalryWidth, () => archerWidth, (newA, newB) =>  // Sets up a splitter for cavalry and archer
        {
            cavalryWidth = newA;
            archerWidth = newB;
        });

        Button team1 = root.Q<Button>("Team1");
        Button team2 = root.Q<Button>("Team2");
        Button team3 = root.Q<Button>("Team3");
        Button team4 = root.Q<Button>("Team4");
        Button team5 = root.Q<Button>("Team5");

        // 建字典: index -> 按钮
        teamBtnMap = new Dictionary<int, Button> {
            {0, team1}, {1, team2}, {2, team3}, {3, team4}, {4, team5}
        };

        // 绑定点击事件
        team1.clicked += () => SwitchTeam(0);
        team2.clicked += () => SwitchTeam(1);
        team3.clicked += () => SwitchTeam(2);
        team4.clicked += () => SwitchTeam(3);
        team5.clicked += () => SwitchTeam(4);

        // 第一次进入时刷新高亮
        HighlightTeamButton(activeLineupIndex);
        LoadLineupVisuals();
    }
    void SwitchTeam(int newIdx)
    {
        if (newIdx == activeLineupIndex) return;
        if (newIdx < 0 || newIdx >= lineupDB.lineups.Count) return;

        activeLineupIndex = newIdx;
        HighlightTeamButton(newIdx);

        // 读取该阵容的兵种比例并应用
        ApplyWidthsFromRatio(lineupDB.lineups[newIdx].ratio);

        // 再刷新四张武将图
        LoadLineupVisuals();
        Debug.Log($"切换到阵容 {newIdx + 1}");
    }

    void HighlightTeamButton(int idx)
    {
        foreach (var kv in teamBtnMap)
        {
            if (kv.Value == null) continue;
            if (kv.Key == idx)
                kv.Value.AddToClassList("teamselected");
            else
                kv.Value.RemoveFromClassList("teamselected");
        }
    }
    void ApplyWidthsFromRatio(TroopRatio ratio)
    {
        // totalWidth 已在 GeometryChangedEvent 中实时更新
        infantryWidth = totalWidth * (ratio.infantry / 100f);
        cavalryWidth  = totalWidth * (ratio.cavalry  / 100f);
        archerWidth   = totalWidth * (ratio.archer   / 100f);

        ApplyWidths();   
    }


    void ResetCurrentLineup()
    {
        if (lineupDB == null || lineupDB.lineups.Count == 0)
        {
            Debug.LogWarning("LineupDB 未设置或为空");
            return;
        }

        /* 1️⃣ 清空武将 + 重置兵种比例 */
        LineupInfo info = lineupDB.lineups[activeLineupIndex];

        info.mainGeneral = info.subGeneral1 = info.subGeneral2 = info.strategist = string.Empty;
        info.ratio = new TroopRatio         // 30 / 40 / 30，对应 SetInitialWidths()
        {
            infantry = 30,
            cavalry  = 40,
            archer   = 30
        };

        lineupDB.lineups[activeLineupIndex] = info;

    #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(lineupDB);   // Ctrl+S 保存
    #endif

        /* 2️⃣ 刷 4 颗按钮外观为默认 */
        SetHeroVisual(null, slotBtnMap[LineupSlot.Main]);
        SetHeroVisual(null, slotBtnMap[LineupSlot.Sub1]);
        SetHeroVisual(null, slotBtnMap[LineupSlot.Sub2]);
        SetHeroVisual(null, slotBtnMap[LineupSlot.Strategist]);

        /* 3️⃣ 重置 UI 宽度 & 百分比显示 */
        SetInitialWidths();     // 这会把 infantryWidth / cavalryWidth / archerWidth
                                // 设回 0.3 / 0.4 / 0.3 并调用 ApplyWidths()

        Debug.Log($"阵容 {activeLineupIndex} 已完全重置（武将 & 兵种）");
    }

    void OnHeroChosen(CardInfo chosen)
    {
        if (lineupDB == null || lineupDB.lineups.Count == 0) return;

        LineupInfo info = lineupDB.lineups[activeLineupIndex];

        // ① 如果别的槽位已经选了这张卡 → 清空
        if (info.mainGeneral   == chosen.cardName && pendingSlot != LineupSlot.Main)
            ClearSlot(LineupSlot.Main,       ref info.mainGeneral);

        if (info.subGeneral1   == chosen.cardName && pendingSlot != LineupSlot.Sub1)
            ClearSlot(LineupSlot.Sub1,       ref info.subGeneral1);

        if (info.subGeneral2   == chosen.cardName && pendingSlot != LineupSlot.Sub2)
            ClearSlot(LineupSlot.Sub2,       ref info.subGeneral2);

        if (info.strategist    == chosen.cardName && pendingSlot != LineupSlot.Strategist)
            ClearSlot(LineupSlot.Strategist, ref info.strategist);

        // ② 把新选择写进当前槽位
        switch (pendingSlot)
        {
            case LineupSlot.Main:       info.mainGeneral   = chosen.cardName; break;
            case LineupSlot.Sub1:       info.subGeneral1   = chosen.cardName; break;
            case LineupSlot.Sub2:       info.subGeneral2   = chosen.cardName; break;
            case LineupSlot.Strategist: info.strategist    = chosen.cardName; break;
        }

        // ③ 写回数据库并保存
        lineupDB.lineups[activeLineupIndex] = info;
    #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(lineupDB);
    #endif

        // ④ 刷按钮外观
        SetHeroVisual(null, slotBtnMap[pendingSlot]);   // 先清目标槽位旧图
        SetHeroVisual(chosen, slotBtnMap[pendingSlot]); // 再贴新图

        Debug.Log($"[{pendingSlot}] 设为 {chosen.cardName}");
    }
    void ClearSlot(LineupSlot slot, ref string fieldRef)
    {
        fieldRef = string.Empty;                            // 清数据库
        SetHeroVisual(null, slotBtnMap[slot]);              // 清按钮背景
    }
    void SetHeroVisual(CardInfo card, Button btn)
    {
        if (btn == null) return;

        if (card != null && (card.fullBodySprite != null || card.iconSprite != null))
        {
            Sprite pic = card.fullBodySprite != null ? card.fullBodySprite : card.iconSprite;
            btn.style.backgroundImage        = new StyleBackground(pic);
            btn.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }
        else
        {
            // card == null 或没有图片 → 还原默认
            btn.style.backgroundImage = StyleKeyword.None;
        }
    }
    void WriteToDatabase(LineupSlot slot, CardInfo card)
    {
        if (lineupDB == null || lineupDB.lineups.Count == 0) {
            Debug.LogWarning("LineupDatabase 未设置或为空");
            return;
        }

        LineupInfo info = lineupDB.lineups[activeLineupIndex];

        switch (slot) {
            case LineupSlot.Main:       info.mainGeneral = card.cardName;   break;
            case LineupSlot.Sub1:       info.subGeneral1 = card.cardName;   break;
            case LineupSlot.Sub2:       info.subGeneral2 = card.cardName;   break;
            case LineupSlot.Strategist: info.strategist  = card.cardName;   break;
        }

        lineupDB.lineups[activeLineupIndex] = info;     // 写回列表

    #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(lineupDB);   // ★ Ctrl+S 立即保存
    #endif

        Debug.Log($"已写入阵容[{activeLineupIndex}]：{slot} = {card.cardName}");
    }
    void LoadLineupVisuals()
    {
        if (lineupDB == null || lineupDB.lineups.Count == 0 || cardDB == null)
        {
            Debug.LogWarning("LineupDB 或 CardDB 未设置 / 为空，无法加载阵容");
            return;
        }

        // 取当前阵容
        if (activeLineupIndex < 0 || activeLineupIndex >= lineupDB.lineups.Count)
        {
            Debug.LogWarning($"activeLineupIndex 超界：{activeLineupIndex}");
            return;
        }

        LineupInfo info = lineupDB.lineups[activeLineupIndex];

        // 主将
        CardInfo mainCard = cardDB.FindByName(info.mainGeneral);
        SetHeroVisual(mainCard, uiDocument.rootVisualElement.Q<Button>("MainHeroSelectBtn"));

        // 副将①
        CardInfo sub1Card = cardDB.FindByName(info.subGeneral1);
        SetHeroVisual(sub1Card, uiDocument.rootVisualElement.Q<Button>("SubHero1SelectBtn"));

        // 副将②
        CardInfo sub2Card = cardDB.FindByName(info.subGeneral2);
        SetHeroVisual(sub2Card, uiDocument.rootVisualElement.Q<Button>("SubHero2SelectBtn"));

        // 军师
        CardInfo stratCard = cardDB.FindByName(info.strategist);
        SetHeroVisual(stratCard, uiDocument.rootVisualElement.Q<Button>("StrategistSelectBtn"));

        ApplyWidthsFromRatio(info.ratio);
    }
    

    // Change this to player choice of army arrangement saved in server
    void SetInitialWidths()
    {
        infantryWidth = totalWidth * 0.3f;
        cavalryWidth = totalWidth * 0.4f;
        archerWidth = totalWidth * 0.3f;
        ApplyWidths();
    }

    // Updates width
    void ApplyWidths()
    {
        infantry.style.width = infantryWidth;
        cavalry.style.width = cavalryWidth;
        archer.style.width = archerWidth;

        float total = infantryWidth + cavalryWidth + archerWidth;
        infantryLabel.text = $"步兵: {Mathf.RoundToInt(infantryWidth / total * 100)}%";
        cavalryLabel.text = $"骑兵: {Mathf.RoundToInt(cavalryWidth / total * 100)}%";
        archerLabel.text = $"弓兵: {Mathf.RoundToInt(archerWidth / total * 100)}%";

        int infPct = Mathf.RoundToInt(infantryWidth / total * 100);
        int cavPct = Mathf.RoundToInt(cavalryWidth  / total * 100);
        int arcPct = Mathf.RoundToInt(archerWidth   / total * 100);

        //  把百分比存回当前阵容
        if (lineupDB != null && activeLineupIndex < lineupDB.lineups.Count)
        {
            var info = lineupDB.lineups[activeLineupIndex];
            info.ratio.infantry = infPct;
            info.ratio.cavalry  = cavPct;
            info.ratio.archer   = arcPct;
            lineupDB.lineups[activeLineupIndex] = info;

        #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(lineupDB);   // 播放模式下 Ctrl+S 可保存
        #endif
        }
    }

    // Sets up a splitter between two of the categories using their widths
    void SetupSplitter(VisualElement splitter,
                       System.Func<float> getLeftWidth,
                       System.Func<float> getRightWidth,
                       System.Action<float, float> setWidths)  // Allows multiple splitters to handle different Action pairs
    {
        bool dragging = false;
        float startX = 0;
        float initialLeft = 0, initialRight = 0;
        int pointerId = -1; // Might have multiple pointers at the same time

        splitter.RegisterCallback<PointerDownEvent>(evt =>  // Locks cursor to the splitter if held down
        {
            dragging = true;
            pointerId = evt.pointerId;
            startX = evt.position.x;
            initialLeft = getLeftWidth();
            initialRight = getRightWidth();
            splitter.CapturePointer(pointerId);

            splitter.style.backgroundColor = Color.red;
        });

        splitter.RegisterCallback<PointerMoveEvent>(evt =>  // Checks how much the cursor moved and updates the positions of the categories
        {
            if (!dragging || !splitter.HasPointerCapture(pointerId)) return;

            float delta = evt.position.x - startX;

            float minWidth = totalWidth * (minPercentage / 100f);

            float newLeft = Mathf.Clamp(initialLeft + delta, minWidth, totalWidth - minWidth);
            float newRight = Mathf.Clamp(initialRight - delta, minWidth, totalWidth - minWidth);

            if (newLeft + newRight <= initialLeft + initialRight)
            {
                setWidths(newLeft, newRight);
                ApplyWidths();
            }
        });

        splitter.RegisterCallback<PointerUpEvent>(evt =>  // Unlocks the cursor to the splitter if not held down
        {
            if (!dragging || evt.pointerId != pointerId) return;
            splitter.ReleasePointer(pointerId);
            dragging = false;

            splitter.style.backgroundColor = Color.white;
        });
    }
}
