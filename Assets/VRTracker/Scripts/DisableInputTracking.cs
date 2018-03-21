using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableInputTracking : MonoBehaviour {
    /* VR Tracker
     * This script will disable the tracking from the default tracking system of the headset
     * So that we can use the one from VR Tracker
     */

    // Use this for initialization
    void Start()
    {
        UnityEngine.XR.InputTracking.disablePositionalTracking = true;
    }

    void OnEnable()
    {
        UnityEngine.XR.InputTracking.disablePositionalTracking = true;
    }

}
