using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Dropdown))]
public class DropdownArrowNav : MonoBehaviour
{
    private TMP_Dropdown _dropdown;

    void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();
    }

    void Update()
    {
        // Only run if this Dropdown is currently the selected object
        if (EventSystem.current.currentSelectedGameObject == gameObject)
        {
            // Detect Up/Down inputs
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                ChangeValue(1); // Next
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                ChangeValue(-1); // Previous
            }
        }
    }

    private void ChangeValue(int change)
    {
        // Calculate new index
        int newIndex = _dropdown.value + change;

        // Clamp between 0 and the max number of options
        if (newIndex >= 0 && newIndex < _dropdown.options.Count)
        {
            _dropdown.value = newIndex;

            // Optional: Force the dropdown to refresh its visual text immediately
            _dropdown.RefreshShownValue();
        }
    }
}