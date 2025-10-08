using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

// This single script goes on the moving object that you want to track.
public class ProximityController : MonoBehaviour
{
    [Header("Setup")]
    // Drag the destination object from your scene hierarchy into this slot.
    public Transform destinationObject;

    [Header("Wwise Event & RTPC Names")]
    // This MUST exactly match the Play Event targeting your Playlist Container.
    public string playLoopEvent = "Play_Pulse";

    // This MUST exactly match the Game Parameter for distance in Wwise.
    public string proximityRtpcName = "Proximity_Distance";

    void Start()
    {
        // Make sure a destination is assigned in the Inspector before starting.
        if (destinationObject == null)
        {
            Debug.LogError("Destination Object has not been assigned on " + gameObject.name + "!", this);
            this.enabled = false; // Disable this script to prevent errors.
            return;
        }

        // When this object starts, begin playing its looping sound.
        AkUnitySoundEngine.PostEvent(playLoopEvent, this.gameObject);
    }

    void Update()
    {
        // We only run the logic if the destination is set.
        if (destinationObject != null)
        {
            
            // --- Proximity Cue (Beep Rate) ---
            // Continuously calculate the distance from this object to the destination.
            float distance = Vector3.Distance(transform.position, destinationObject.position);
            AkUnitySoundEngine.SetRTPCValue(proximityRtpcName, distance, this.gameObject);
            Debug.Log("Trying to set RTPC '" + proximityRtpcName + "' to distance: " + distance);

            // Send that distance value to the Wwise RTPC to control the speed.
            AkUnitySoundEngine.SetRTPCValue(proximityRtpcName, distance);
        }
    }
}
