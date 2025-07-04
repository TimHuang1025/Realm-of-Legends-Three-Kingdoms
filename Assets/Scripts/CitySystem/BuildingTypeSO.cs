using UnityEngine;

[CreateAssetMenu(menuName = "City/BuildingType")]
public class BuildingTypeSO : ScriptableObject
{
    public string buildingName = "TownHall";
    public GameObject prefab;        // 放真正的模型
    public int size = 1;             // 占几格（先 1×1）
    public Material previewMat;      // 半透明材质
}
