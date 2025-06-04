using UnityEngine;
using UnityEngine.UIElements;

public class ControlPanel : MonoBehaviour
{
    public UIDocument uiDocument;
    public float minPercentage = 10f; // Minimum width percentage per section (e.g., 10%)

    private VisualElement infantry, cavalry, archer;
    private Label infantryLabel, cavalryLabel, archerLabel;
    private VisualElement splitter1, splitter2;

    private float totalWidth;
    private float infantryWidth, cavalryWidth, archerWidth;

    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

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
