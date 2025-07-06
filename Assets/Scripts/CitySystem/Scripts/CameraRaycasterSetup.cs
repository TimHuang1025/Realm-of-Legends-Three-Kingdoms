using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 自动配置相机的射线检测器
/// 将此脚本添加到主相机上
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraRaycasterSetup : MonoBehaviour
{
    void Start()
    {
        Camera cam = GetComponent<Camera>();
        
        // 确保相机有PhysicsRaycaster（用于3D物体检测）
        if (!cam.TryGetComponent(out PhysicsRaycaster pr))
        {
            pr = cam.gameObject.AddComponent<PhysicsRaycaster>();
        }
        
        // ★ 排除UI层，避免与GraphicRaycaster冲突
        pr.eventMask = ~(1 << LayerMask.NameToLayer("UI"));
        
        // 确保相机能看到UI层
        cam.cullingMask |= 1 << LayerMask.NameToLayer("UI");
        
        Debug.Log($"[CameraSetup] PhysicsRaycaster configured, excluding UI layer");
        Debug.Log($"[CameraSetup] Camera culling mask includes UI: {(cam.cullingMask & (1 << LayerMask.NameToLayer("UI"))) != 0}");
    }
}