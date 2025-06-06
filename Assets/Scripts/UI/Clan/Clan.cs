using UnityEngine;
using UnityEngine.UIElements;

public class Clan : MonoBehaviour
{
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Shared buttons (now outside containers)
        var filterMemberButton = root.Q<Button>("FilterMember");
        var filterRequestsButton = root.Q<Button>("FilterRequests");
        var filterModerationButton = root.Q<Button>("FilterModeration");

        // Main containers
        var mainMemberContainer = root.Q<VisualElement>("MainMemberContainer");
        var mainRequestsContainer = root.Q<VisualElement>("MainRequestsContainer");
        var mainChangeLogContainer = root.Q<VisualElement>("MainChangeLogContainer");

        void Show(VisualElement show, VisualElement hide1, VisualElement hide2)
        {
            show.style.display = DisplayStyle.Flex;
            hide1.style.display = DisplayStyle.None;
            hide2.style.display = DisplayStyle.None;
        }

        filterMemberButton.clicked += () =>
            Show(mainMemberContainer, mainRequestsContainer, mainChangeLogContainer);
        filterRequestsButton.clicked += () =>
            Show(mainRequestsContainer, mainMemberContainer, mainChangeLogContainer);
        filterModerationButton.clicked += () =>
            Show(mainChangeLogContainer, mainMemberContainer, mainRequestsContainer);
    }

}