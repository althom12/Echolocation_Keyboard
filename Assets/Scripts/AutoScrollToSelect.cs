using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScrollRect))]
public class AutoScrollToSelected : MonoBehaviour
{
    public float scrollSpeed = 10f; // Adjust for smoothness (or use high value for instant snap)

    private ScrollRect m_ScrollRect;
    private RectTransform m_ContentRect;
    private RectTransform m_SelectedRect;

    void Awake()
    {
        m_ScrollRect = GetComponent<ScrollRect>();
        m_ContentRect = m_ScrollRect.content;
    }

    void Update()
    {
        // 1. Get the currently selected object
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null) return;

        // 2. Check if the selected object is inside THIS scroll view
        if (selected.transform.parent != m_ContentRect.transform) return;

        m_SelectedRect = selected.GetComponent<RectTransform>();

        // 3. Calculate boundaries to see if we need to scroll
        UpdateScrollPosition();
    }

    void UpdateScrollPosition()
    {
        // Get the world corners of the Viewport (the visible window)
        Vector3[] viewportCorners = new Vector3[4];
        m_ScrollRect.viewport.GetWorldCorners(viewportCorners);

        // Get the world corners of the Selected Button
        Vector3[] selectedCorners = new Vector3[4];
        m_SelectedRect.GetWorldCorners(selectedCorners);

        // Check if the Button is BELOW the Viewport (Bottom Y check)
        // [0] = Bottom Left corner
        if (selectedCorners[0].y < viewportCorners[0].y)
        {
            float diff = viewportCorners[0].y - selectedCorners[0].y;
            m_ScrollRect.content.anchoredPosition += Vector2.up * diff;
        }
        // Check if the Button is ABOVE the Viewport (Top Y check)
        // [1] = Top Left corner
        else if (selectedCorners[1].y > viewportCorners[1].y)
        {
            float diff = selectedCorners[1].y - viewportCorners[1].y;
            m_ScrollRect.content.anchoredPosition += Vector2.up * diff;
        }
    }
}