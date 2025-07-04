// Assets/Scripts/GridBuilding/BuildingPlacer.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingPlacer : MonoBehaviour
{
    [Header("1. 摄像机 & 射线层")]
    public Camera    cam;
    public LayerMask groundMask;
    [Tooltip("点击已有建筑进入移动模式")]
    public LayerMask buildingMask;

    [Header("2. 可放置预览材质")]
    public Material validMaterial;
    public Material invalidMaterial;

    [Header("3. 可放置的建筑列表（对应 1,2,3…）")]
    public BuildingTypeSO[] buildList;

    [Header("4. 触控取消设置")]
    public bool twoFingerCancel = true;

    [Header("6. 选项过滤")]
    public BuildingTypeSO filterSO;   // 只有这个类型的建筑才弹面板
    [Tooltip("射线投影到地面点到建筑中心的最大水平距离（米）")]
    public float clickRadius = 0.6f;
    [Tooltip("射线最远距离")]
    public float maxRayDist  = 800f;

    // ── 预览相关公有数据 ─────────────────────
    public bool PreviewActive { get; private set; }
    public int  PreviewBLX    { get; private set; }
    public int  PreviewBLY    { get; private set; }
    public int  PreviewSize   { get; private set; }

    // ── 私有状态 ─────────────────────────────
    private CityGrid       grid;
    private BuildingTypeSO previewType;
    private GameObject     preview;
    private Renderer[]     previewRenderers;
    private Material       lineMaterial;

    // 选项面板相关
    private Building       selectedBuilding;

    void Awake()
    {
        grid = CityGrid.I;
        ResetPreviewData();

        

        // 创建线条材质（GL 渲染网格用）
        var shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull",      (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite",    0);
    }

    void Update()
    {
#if UNITY_EDITOR
        // 编辑器热键：1/2/3… 触发新建预览
        for (int i = 0; i < buildList.Length; i++)
            if (Input.GetKeyDown((i + 1).ToString()))
                StartPlacing(i);
#endif

        // 如果当前在预览模式，则走放置流程
        if (PreviewActive)
        {
            HandlePlacement();
            return;
        }

        // 否则优先处理“点击已有建筑弹面板”
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI(Input.mousePosition))
        {
            TryShowOptions(Input.mousePosition);
        }
    }

    /// <summary>
    /// 检测点击已有建筑并弹出选项面板
    /// </summary>
    private void TryShowOptions(Vector2 screenPos)
    {

        var ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, maxRayDist, buildingMask))
            return;

        var b = hit.collider.GetComponentInParent<Building>();
        if (b == null || b.type != filterSO)
            return;

        // 水平距离过滤
        var plane = new Plane(Vector3.up, b.transform.position);
        if (!plane.Raycast(ray, out float enter))
            return;
        var gh = ray.GetPoint(enter);
        if (Vector2.Distance(
                new Vector2(gh.x, gh.z),
                new Vector2(b.transform.position.x, b.transform.position.z)
            ) > clickRadius)
            return;

        // 准备弹面板
        selectedBuilding = b;
    }



    // ── 新建预览 & 放置 逻辑 ─────────────────────

    private void HandlePlacement()
    {
        Vector2 sp; bool place=false, cancel=false;
        if (Input.touchCount>0)
        {
            var t = Input.GetTouch(0);
            sp = t.position;
            if (t.phase==TouchPhase.Ended) place=true;
            if (twoFingerCancel && Input.touchCount>1 &&
                Input.GetTouch(1).phase==TouchPhase.Began)
                cancel = true;
        }
        else
        {
            sp    = Input.mousePosition;
            place = Input.GetMouseButtonDown(0);
            cancel= Input.GetMouseButtonDown(1);
        }

        if (cancel)
        {
            CancelPreview();
            return;
        }

        var ray = cam.ScreenPointToRay(sp);
        if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, groundMask))
            return;

        var worldPos = hit.point;
        int size     = previewType.size;
        var bl       = grid.WorldToBL(worldPos, size);
        bool ok      = grid.IsAreaFree(bl.x, bl.y, size);
        var center   = grid.AreaCenter(bl.x, bl.y, size);

        PreviewBLX  = bl.x;
        PreviewBLY  = bl.y;
        PreviewSize = size;

        preview.transform.position = center;
        UpdatePreviewTint(ok);

        if (place && ok && !IsPointerOverUI(sp))
            ConfirmPlacement(bl.x, bl.y);
    }

    public void StartPlacing(int idx)
    {
        if (idx < 0 || idx >= buildList.Length) return;
        CancelPreview();
        previewType = buildList[idx];
        CreatePreview(previewType);
    }

    public void BeginMove(Building b)
    {
        // 释放旧占格
        int oldSize = b.type.size;
        var oldBL   = grid.WorldToBL(b.transform.position, oldSize);
        grid.SetArea(oldBL.x, oldBL.y, oldSize, false);

        // 缓存类型、删除旧物体
        var type = b.type;
        DestroyImmediate(b.gameObject);
        CancelPreview();

        // 重新预览
        previewType = type;
        CreatePreview(type);
    }

    private void CreatePreview(BuildingTypeSO type)
    {
        if (type == null || type.prefab == null)
        {
            Debug.LogError("[BuildingPlacer] CreatePreview 失败：type 或 prefab 为空");
            ResetPreviewData();
            return;
        }

        preview = Instantiate(type.prefab);
        foreach (var c in preview.GetComponentsInChildren<Building>())
            DestroyImmediate(c);
        foreach (var c in preview.GetComponentsInChildren<Collider>())
            c.enabled = false;

        previewRenderers = preview.GetComponentsInChildren<Renderer>();
        foreach (var r in previewRenderers)
        {
            var m = new Material(r.sharedMaterial);
            m.color = new Color(1f,1f,1f,0.5f);
            r.material = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        PreviewActive = true;
    }

    private void ConfirmPlacement(int bx, int by)
    {
        var pos = grid.AreaCenter(bx, by, previewType.size);
        var go  = Instantiate(previewType.prefab, pos, Quaternion.identity);
        var bld = go.GetComponent<Building>();
        bld.type = previewType;

        grid.SetArea(bx, by, previewType.size, true);
        CancelPreview();
    }

    public void CancelPreview()
    {
        if (preview != null) DestroyImmediate(preview);
        preview = null;
        previewRenderers = null;
        ResetPreviewData();
    }

    private void ResetPreviewData()
    {
        PreviewActive = false;
        PreviewBLX = PreviewBLY = PreviewSize = -1;
    }

    private void UpdatePreviewTint(bool valid)
    {
        if (previewRenderers == null) return;
        Color c = valid
            ? (validMaterial   != null ? validMaterial.color   : new Color(0f,1f,0f,0.5f))
            : (invalidMaterial != null ? invalidMaterial.color : new Color(1f,0f,0f,0.5f));
        foreach (var r in previewRenderers)
            r.material.color = c;
    }

    private bool IsPointerOverUI(Vector2 sp)
    {
        if (EventSystem.current == null) return false;
        return Input.touchCount > 0
            ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }

    //── 运行时渲染网格和预览 ─────────────────────

    void OnRenderObject()
    {
        if (!PreviewActive || lineMaterial == null) return;

        float cell = grid.cell;
        Vector3 o  = grid.origin;
        float y0   = 0.01f;
        float y1   = 0.02f;

        lineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.LINES);

        // 白色网格
        GL.Color(new Color(1f,1f,1f,0.2f));
        for (int x = 0; x <= grid.width; x++)
        {
            GL.Vertex(o + new Vector3(x*cell, y0, 0));
            GL.Vertex(o + new Vector3(x*cell, y0, grid.height*cell));
        }
        for (int y = 0; y <= grid.height; y++)
        {
            GL.Vertex(o + new Vector3(0, y0, y*cell));
            GL.Vertex(o + new Vector3(grid.width*cell, y0, y*cell));
        }

        // 绿/红预览框
        bool ok = grid.IsAreaFree(PreviewBLX, PreviewBLY, PreviewSize);
        GL.Color(ok
            ? new Color(0f,1f,0f,0.6f)
            : new Color(1f,0f,0f,0.6f));
        for (int i = 0; i <= PreviewSize; i++)
        {
            // 竖
            GL.Vertex(o + new Vector3((PreviewBLX+i)*cell, y1, PreviewBLY*cell));
            GL.Vertex(o + new Vector3((PreviewBLX+i)*cell, y1, (PreviewBLY+PreviewSize)*cell));
            // 横
            GL.Vertex(o + new Vector3(PreviewBLX*cell, y1, (PreviewBLY+i)*cell));
            GL.Vertex(o + new Vector3((PreviewBLX+PreviewSize)*cell, y1, (PreviewBLY+i)*cell));
        }

        GL.End();
        GL.PopMatrix();
    }
}
