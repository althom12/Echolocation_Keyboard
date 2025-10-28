using System;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using static ak.wwise.core;
using static Cinemachine.CinemachineOrbitalTransposer;
using static Unity.Burst.Intrinsics.X86;
using static UnityEngine.Rendering.DebugUI.Table;

/// <summary>
/// Attached to each main menu button.
/// Plays different audio depending on whether we're returning from a subwindow.
/// </summary>
public class WwiseMainMenuButton : MonoBehaviour, ISelectHandler
{
    [Header("Audio Channel")]
    public AudioEventChannelSO audioChannel; // Drag your 'UIAudioChannel' asset here

    [Header("Selection Audio")]
    public AK.Wwise.Event selectionEvent; // Drag 'Event_UI_Select_MainMenu' here ? CHANGED

    [Header("Switches")]
    public AK.Wwise.Switch normalSwitch; // e.g., "MainMenu_Obstacles_Normal"
    public AK.Wwise.Switch returnContextSwitch; // e.g., "MainMenu_Obstacles_WithContext"

    /// <summary>
    /// Called by Unity's EventSystem when this button is selected.
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        Debug.Log($"[WwiseMainMenuButton] OnSelect called on {gameObject.name}");

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null || audioChannel == null)
        {
            Debug.LogError($"[WwiseMainMenuButton] Missing references! AudioManager: {(audioManager != null)}, Channel: {(audioChannel != null)}");
            return;
        }

        // Check if we're returning from a subwindow
        bool isReturning = audioManager.IsReturningToMainMenu();
        Debug.Log($"[WwiseMainMenuButton] IsReturning: {isReturning}");

        // Choose the appropriate switch based on context
        AK.Wwise.Switch switchToSend = isReturning ? returnContextSwitch : normalSwitch;

        // Create the packet
        AudioEventChannelSO.WwiseEventPacket packet = new AudioEventChannelSO.WwiseEventPacket
        {
            WwiseEvent = selectionEvent,
            WwiseSwitch = switchToSend,
            Emitter = this.gameObject
        };

        Debug.Log($"[WwiseMainMenuButton] Raising audio event through channel");
        // Raise the event through the audio channel
        audioChannel.RaiseEvent(packet);

        // IMPORTANT: Clear the return flag after using it
        if (isReturning)
        {
            audioManager.SetReturningToMainMenu(false);
        }
    }
}