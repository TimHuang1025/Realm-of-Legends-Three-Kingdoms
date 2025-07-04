// Assets/Scripts/GridBuilding/CityGrid.cs
using UnityEngine;
using System.Diagnostics;

[ExecuteAlways]
public class CityGrid : MonoBehaviour
{
    [Header("Grid Setting")]
    public int width = 30;
    public int height = 30;
    public float cell = 1f;
    public Vector3 origin = new Vector3(-15f, 0f, -15f);

    private bool[,] occ;
    public static CityGrid I { get; private set; }

    void Awake()
    {
        if (I != null && I != this)
            UnityEngine.Debug.LogWarning("[CityGrid] 检测到多个实例，已覆盖 I 引用");
        I = this;
        Resize();
        ClearAll(); // 进入 Play 时清空残留
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Resize();
    }
#endif

    void Resize()
    {
        if (width <= 0 || height <= 0) return;
        if (occ == null || occ.GetLength(0) != width || occ.GetLength(1) != height)
        {
            occ = new bool[width, height];
            UnityEngine.Debug.Log($"[CityGrid] Resize: new grid {width}×{height}");
        }
    }

    bool InRange(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

    public bool IsFree(int x, int y) => InRange(x, y) && !occ[x, y];

    public bool IsAreaFree(int x, int y, int size)
    {
        if (!InRange(x, y) || !InRange(x + size - 1, y + size - 1))
            return false;
        for (int dx = 0; dx < size; dx++)
            for (int dy = 0; dy < size; dy++)
                if (occ[x + dx, y + dy])
                    return false;
        return true;
    }

    public void SetArea(int x, int y, int size, bool v)
    {
        // 打印调用堆栈，帮助定位误调用
        var stack = new StackTrace(true);
        UnityEngine.Debug.Log(
            $"[CityGrid] SetArea({x},{y},size={size},occ={v}) called from:\n{stack}"
        );

        if (!InRange(x, y) || !InRange(x + size - 1, y + size - 1))
        {
            UnityEngine.Debug.LogWarning("[CityGrid] SetArea 越界，跳过写入");
            return;
        }

        for (int dx = 0; dx < size; dx++)
            for (int dy = 0; dy < size; dy++)
                occ[x + dx, y + dy] = v;
    }

    public Vector3 CellToWorld(int x, int y) =>
        origin + new Vector3((x + 0.5f) * cell, 0f, (y + 0.5f) * cell);

    public Vector2Int WorldToBL(Vector3 world, int size)
    {
        float localX = (world.x - origin.x) / cell;
        float localZ = (world.z - origin.z) / cell;
        int half = size / 2;
        float extra = (size % 2 == 1) ? 0.5f : 0f;
        float fx = localX - half - extra;
        float fz = localZ - half - extra;
        return new Vector2Int(Mathf.FloorToInt(fx), Mathf.FloorToInt(fz));
    }

    public Vector3 AreaCenter(int blX, int blY, int size) =>
        origin + new Vector3((blX + size * 0.5f) * cell, 0f, (blY + size * 0.5f) * cell);

#if UNITY_EDITOR
    void ClearAll()
    {
        if (occ == null) return;
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                occ[i, j] = false;
        UnityEngine.Debug.Log("[CityGrid] 已清空所有占用");
    }

    void DumpOccupied()
    {
        if (occ == null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[CityGrid] 当前被占用的格子：");
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                if (occ[i, j])
                    sb.AppendLine($"  ({i}, {j})");
        UnityEngine.Debug.Log(sb.ToString());
    }
#endif
}
