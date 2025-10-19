using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class SettingsListManager : MonoBehaviour
{
    [System.Serializable]
    public class SettingCategory
    {
        public string categoryName;
        public GameObject contentPanel; // The content to show in the right 2/3 area
    }

    [Header("Prefab Reference")]
    [SerializeField] private GameObject settingsItemPrefab;

    [Header("Content Display Area")]
    [SerializeField] private GameObject contentContainer; // The right 2/3 area where category content displays

    [Header("Category Configuration")]
    [SerializeField]
    private SettingCategory[] categories = new SettingCategory[]
    {
        new SettingCategory { categoryName = "Graphics" },
        new SettingCategory { categoryName = "Audio" },
        new SettingCategory { categoryName = "Controls" },
        new SettingCategory { categoryName = "Gameplay" },
        new SettingCategory { categoryName = "Accessibility" }
    };

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = new Color(0.8f, 0.9f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.6f, 0.8f, 1f);

    private List<Button> listItems = new List<Button>();
    private int currentCategoryIndex = 0;
    private bool isInCategoryList = true; // Track if we're navigating the list or the content
    private List<Selectable> currentContentInteractables = new List<Selectable>();

    void Start()
    {
        PopulateList();
        SetupNavigation();
        SelectCategory(0);
    }

    void Update()
    {
        HandleKeyboardInput();
    }

    /// <summary>
    /// Instantiates category list items from the prefab
    /// </summary>
    private void PopulateList()
    {
        if (settingsItemPrefab == null)
        {
            Debug.LogError("Settings Item Prefab is not assigned!");
            return;
        }

        for (int i = 0; i < categories.Length; i++)
        {
            SettingCategory category = categories[i];

            // Instantiate the prefab as a child of this container
            GameObject item = Instantiate(settingsItemPrefab, transform);
            item.name = category.categoryName + "_Button";

            // Get the Button component
            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                listItems.Add(btn);

                // Set the text
                TextMeshProUGUI textComponent = item.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = category.categoryName;
                }

                // Configure button colors
                ColorBlock colors = btn.colors;
                colors.normalColor = normalColor;
                colors.highlightedColor = highlightedColor;
                colors.selectedColor = selectedColor;
                colors.pressedColor = selectedColor;
                btn.colors = colors;

                // Add click listener
                int index = i;
                btn.onClick.AddListener(() => SelectCategory(index));
            }
        }
    }

    /// <summary>
    /// Sets up explicit keyboard navigation between list items
    /// </summary>
    private void SetupNavigation()
    {
        for (int i = 0; i < listItems.Count; i++)
        {
            Navigation nav = listItems[i].navigation;
            nav.mode = Navigation.Mode.Explicit;

            // Only vertical navigation in the list
            if (i > 0)
                nav.selectOnUp = listItems[i - 1];

            if (i < listItems.Count - 1)
                nav.selectOnDown = listItems[i + 1];

            // Disable automatic Tab navigation - we'll handle it manually
            nav.selectOnRight = null;

            listItems[i].navigation = nav;
        }
    }

    /// <summary>
    /// Handles keyboard input following NVDA paradigm
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (isInCategoryList)
        {
            // Arrow key navigation within the category list
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                NavigateUp();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                NavigateDown();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                // Enter/Space selects the category
                SelectCategory(currentCategoryIndex);
            }
            else if (Input.GetKeyDown(KeyCode.Tab) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                // Tab moves to the first interactive element in the content panel
                MoveToContentPanel();
            }
        }
        else
        {
            // We're in the content panel - check for Shift+Tab to go back
            if (Input.GetKeyDown(KeyCode.Tab) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                // Check if we're at the first element in content
                GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
                if (currentSelected != null && currentContentInteractables.Count > 0)
                {
                    Selectable currentSelectable = currentSelected.GetComponent<Selectable>();
                    if (currentSelectable == currentContentInteractables[0])
                    {
                        // We're at the first content element, go back to category list
                        ReturnToCategoryList();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Navigate to the previous category in the list
    /// </summary>
    private void NavigateUp()
    {
        if (listItems.Count == 0) return;

        currentCategoryIndex--;
        if (currentCategoryIndex < 0)
            currentCategoryIndex = listItems.Count - 1;

        EventSystem.current.SetSelectedGameObject(listItems[currentCategoryIndex].gameObject);
        SelectCategory(currentCategoryIndex);
    }

    /// <summary>
    /// Navigate to the next category in the list
    /// </summary>
    private void NavigateDown()
    {
        if (listItems.Count == 0) return;

        currentCategoryIndex++;
        if (currentCategoryIndex >= listItems.Count)
            currentCategoryIndex = 0;

        EventSystem.current.SetSelectedGameObject(listItems[currentCategoryIndex].gameObject);
        SelectCategory(currentCategoryIndex);
    }

    /// <summary>
    /// Displays the content for the selected category in the right panel
    /// </summary>
    private void SelectCategory(int index)
    {
        currentCategoryIndex = index;
        SettingCategory selectedCategory = categories[index];

        // Hide all content panels
        foreach (SettingCategory category in categories)
        {
            if (category.contentPanel != null)
            {
                category.contentPanel.SetActive(false);
            }
        }

        // Show the selected category's content
        if (selectedCategory.contentPanel != null)
        {
            selectedCategory.contentPanel.SetActive(true);
            CacheContentInteractables(selectedCategory.contentPanel);
        }
        else
        {
            currentContentInteractables.Clear();
            Debug.LogWarning($"No content panel assigned for {selectedCategory.categoryName}");
        }

        // Highlight the category button
        EventSystem.current.SetSelectedGameObject(listItems[index].gameObject);
        isInCategoryList = true;
    }

    /// <summary>
    /// Caches all interactive elements in the content panel for tab navigation
    /// </summary>
    private void CacheContentInteractables(GameObject contentPanel)
    {
        currentContentInteractables.Clear();

        // Find all Selectable components (Button, Toggle, Slider, Dropdown, etc.)
        Selectable[] selectables = contentPanel.GetComponentsInChildren<Selectable>(false);

        foreach (Selectable selectable in selectables)
        {
            if (selectable.interactable)
            {
                currentContentInteractables.Add(selectable);
            }
        }
    }

    /// <summary>
    /// Moves focus from category list to the first interactive element in content panel
    /// </summary>
    private void MoveToContentPanel()
    {
        if (currentContentInteractables.Count > 0)
        {
            EventSystem.current.SetSelectedGameObject(currentContentInteractables[0].gameObject);
            isInCategoryList = false;
        }
        else
        {
            Debug.LogWarning("No interactive elements found in the current content panel");
        }
    }

    /// <summary>
    /// Returns focus from content panel back to the category list
    /// </summary>
    private void ReturnToCategoryList()
    {
        EventSystem.current.SetSelectedGameObject(listItems[currentCategoryIndex].gameObject);
        isInCategoryList = true;
    }

    /// <summary>
    /// Public method to reset to the first category
    /// </summary>
    public void ResetSelection()
    {
        SelectCategory(0);
    }
}