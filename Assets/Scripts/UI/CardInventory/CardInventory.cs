using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CardInventory : MonoBehaviour
{
    public int cardsPerRow = 3;
    public int totalCards = 30;
    public int fixedItemHeight = 200;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var listView = root.Q<ListView>("CardScrollView");

        // Removes visibility of the scroller
        var scrollView = listView.Q<ScrollView>();
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        // Prepare data: each row contains a group of card indices
        List<List<int>> cardRows = new List<List<int>>();
        for (int i = 0; i < totalCards; i += cardsPerRow)
        {
            var row = new List<int>();
            for (int j = 0; j < cardsPerRow && i + j < totalCards; j++)
            {
                row.Add(i + j);
            }
            cardRows.Add(row);
        }
        
        // ListView formatting
        listView.itemsSource = cardRows;
        listView.fixedItemHeight = fixedItemHeight;
        listView.selectionType = SelectionType.None;

        // Add a new row
        listView.makeItem = () =>
        {
            var rowContainer = new VisualElement();
            rowContainer.style.flexDirection = FlexDirection.Row;
            rowContainer.style.justifyContent = Justify.FlexStart;
            rowContainer.style.paddingLeft = 10;
            rowContainer.style.paddingTop = 5;

            // Prevent hover/focus/selected highlight:
            rowContainer.RegisterCallback<MouseEnterEvent>(e =>
            {
                rowContainer.style.backgroundColor = Color.clear;
            });
            rowContainer.RegisterCallback<MouseLeaveEvent>(e =>
            {
                rowContainer.style.backgroundColor = Color.clear;
            });
            rowContainer.RegisterCallback<FocusEvent>(e =>
            {
                rowContainer.style.backgroundColor = Color.clear;
            });
            return rowContainer;
        };

        // Assign a button to each element
        listView.bindItem = (ve, i) =>
        {
            ve.Clear();
            foreach (int cardIndex in cardRows[i])
            {
                var button = new Button(() => Debug.Log($"Card {cardIndex + 1} clicked!"));
                button.text = $"Card {cardIndex + 1}";
                button.AddToClassList("card");
                ve.Add(button);
            }
        };
    }
}