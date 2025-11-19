using UnityEngine;
using AK.Wwise;

/// <summary>
/// Defines a single audio landmark in the environment.
/// Contains ONLY landmark data - no scene references (those go in LandmarkUIBinding).
/// 
/// USAGE:
/// 1. Create new LandmarkDefinition asset (Right-click ? Create ? Audio ? Landmark Definition)
/// 2. Set landmark name, loop event, volume RTPC, and default volume
/// 3. Use in LandmarkUIBinding to connect to spatial emitter and UI elements
/// 
/// EXAMPLES:
/// - Clock (looping ticking sound)
/// - HVAC (looping ventilation hum)
/// - Water Fountain (looping water sound)
/// - Bird Sounds (looping ambient chirping)
/// </summary>
[CreateAssetMenu(fileName = "Landmark_NewLandmark", menuName = "Audio/Landmark Definition")]
public class LandmarkDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name (e.g., 'Clock', 'HVAC', 'Water Fountain')")]
    public string landmarkName;

    [Header("Spatial Audio")]
    [Tooltip("The Wwise event that loops for this landmark")]
    public AK.Wwise.Event loopEvent;

    [Header("Volume Control")]
    [Tooltip("RTPC for controlling volume (used on both spatial emitter and UI slider)")]
    public AK.Wwise.RTPC volumeRTPC;

    [Tooltip("Default volume value (0-100)")]
    [Range(0f, 100f)]
    public float defaultVolume = 50f;
}