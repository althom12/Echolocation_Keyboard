using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MainSettingsNavigation : MonoBehaviour
{
    [SerializeField] private List<Button> settingsButtons;
    [SerializeField] private InputAction upAction;
    [SerializeField] private InputAction downAction;
    [SerializeField] private InputAction tabAction;

    private int currentIndex = 0;
    private EventSystem eventSystem;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        eventSystem = EventSystem.current;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        // Enable input actions
        upAction?.Enable();
        downAction?.Enable();
        tabAction?.Enable();

        // Subscribe to input events
        if (upAction != null)
            upAction.performed += OnUpPressed;
        if (downAction != null)
            downAction.performed += OnDownPressed;
        if (tabAction != null)
            tabAction.performed += OnTabPressed;

        // Select first button when panel opens
        if (settingsButtons.Count > 0)
        {
            currentIndex = 0;
            HighlightButton(currentIndex);
        }
    }

    void OnDisable()
    {
        // Unsubscribe from input events
        if (upAction != null)
            upAction.performed -= OnUpPressed;
        if (downAction != null)
            downAction.performed -= OnDownPressed;
        if (tabAction != null)
            tabAction.performed -= OnTabPressed;

        // Disable input actions
        upAction?.Disable();
        downAction?.Disable();
        tabAction?.Disable();
    }

    private void OnUpPressed(InputAction.CallbackContext context)
    {
        // Only process if this panel is active
        if (!IsPanelActive()) return;

        NavigateUp();
    }

    private void OnDownPressed(InputAction.CallbackContext context)
    {
        // Only process if this panel is active
        if (!IsPanelActive()) return;

        NavigateDown();
    }

    private void OnTabPressed(InputAction.CallbackContext context)
    {
        // Only process if this panel is active
        if (!IsPanelActive()) return;

        ActivateCurrentButton();
    }

    private void NavigateUp()
    {
        if (settingsButtons.Count == 0) return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = settingsButtons.Count - 1;

        HighlightButton(currentIndex);
    }

    private void NavigateDown()
    {
        if (settingsButtons.Count == 0) return;

        currentIndex++;
        if (currentIndex >= settingsButtons.Count)
            currentIndex = 0;

        HighlightButton(currentIndex);
    }

    private void HighlightButton(int index)
    {
        if (index >= 0 && index < settingsButtons.Count)
        {
            eventSystem.SetSelectedGameObject(settingsButtons[index].gameObject);
        }
    }

    private void ActivateCurrentButton()
    {
        if (currentIndex >= 0 && currentIndex < settingsButtons.Count)
        {
            settingsButtons[currentIndex].onClick.Invoke();
        }
    }

    private bool IsPanelActive()
    {
        if (canvasGroup == null) return gameObject.activeSelf;
        return canvasGroup.interactable && canvasGroup.alpha > 0;
    }
}