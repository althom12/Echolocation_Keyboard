using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleClickPlayer : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Assign the Wwise Event to play on click")]
    public AK.Wwise.Event soundToPlay;

    private CustomInputActions _input;

    private void Awake()
    {
        _input = new CustomInputActions();
    }

    private void OnEnable()
    {
        _input.Enable();
        // Subscribe to the specific action you requested
        _input.Player.LeftClick.performed += OnLeftClickPerformed;
    }

    private void OnDisable()
    {
        _input.Disable();
        // Unsubscribe to prevent memory leaks
        _input.Player.LeftClick.performed -= OnLeftClickPerformed;
    }

    private void OnLeftClickPerformed(InputAction.CallbackContext context)
    {
        if (soundToPlay != null && soundToPlay.IsValid())
        {
            soundToPlay.Post(gameObject);
        }
    }
}