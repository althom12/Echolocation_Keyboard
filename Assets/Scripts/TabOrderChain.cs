using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TabOrderChain : MonoBehaviour
{
    [Header("Define the Tab Path")]
    public List<Selectable> tabPath;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Navigate();
        }
    }

    private void Navigate()
    {
        if (tabPath == null || tabPath.Count == 0)
        {
            Debug.LogWarning("TabOrderChain: Path list is empty!");
            return;
        }

        GameObject currentObj = EventSystem.current.currentSelectedGameObject;

        // 1. DEBUG: Who are we currently on?
        Debug.Log($"Current Selection: {(currentObj != null ? currentObj.name : "None")}");

        if (currentObj == null)
        {
            tabPath[0].Select();
            return;
        }

        Selectable currentSelectable = currentObj.GetComponent<Selectable>();
        int currentIndex = tabPath.IndexOf(currentSelectable);

        // 2. DEBUG: Did we find it in the list?
        if (currentIndex == -1)
        {
            Debug.LogWarning($"Object '{currentObj.name}' is selected, but it is NOT in the Tab Path list. Resetting to start.");
            tabPath[0].Select();
            return;
        }

        // Calculate Next Index
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        int nextIndex;

        if (shiftPressed)
        {
            nextIndex = currentIndex - 1;
            if (nextIndex < 0) nextIndex = tabPath.Count - 1;
        }
        else
        {
            nextIndex = currentIndex + 1;
            if (nextIndex >= tabPath.Count) nextIndex = 0;
        }

        // 3. DEBUG: Where are we trying to go?
        Selectable target = tabPath[nextIndex];
        Debug.Log($"Moving from Index {currentIndex} to {nextIndex} ({target.name})");

        if (target != null && target.interactable && target.gameObject.activeInHierarchy)
        {
            target.Select();
        }
        else
        {
            Debug.LogError($"CANNOT SELECT '{target.name}'! Interactable: {target.interactable}, Active: {target.gameObject.activeInHierarchy}");
        }
    }
}