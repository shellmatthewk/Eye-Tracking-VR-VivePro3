// ########################################################################################################################
// ########################################################################################################################
// This programme is used to disable tracking of the HTC Vive Pro Eye.
// The programme is developed by Yu Imaoka and Andri Flury at D-HEST. ETH Zurich.
// 18th of November 2019.
// Software information: SRanipal_SDK_1.1.0.1
// 
// Edited by Matthew Shell at the Kojima Lab 2/14/2025
//
// DisableTracking:     Attached to main camera 
//
// ########################################################################################################################
// ########################################################################################################################



// ************************************************************************************************************************
// Call namespaces.
// ************************************************************************************************************************

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using UnityEngine;
using System;
using System.IO;
using System.Data;
using System.Text;
using UnityEngine.XR;
using Wave.Native;
using Wave.XR;

public class DisableTracking : MonoBehaviour
{
    private GameObject cam;
    
    void Start()
    {
        DisableHeadTracking();
    }
    
    void Update()
    {
        
    }

    void DisableHeadTracking()
    {

        //Disable HMD tracking 

        XRDevice.DisableAutoXRCameraTracking(Camera.main, true); // updated to focus 3
        
        cam = GameObject.Find("Main Camera");                                 //Assigning the game object (Main Camera) to the script.
        cam.transform.position = new Vector3(0, 0f, -2);                               //positions camera with camposition vector.
        cam.transform.rotation = new Quaternion(0, 0, 0, 0);                  //Define starting rotation of the camera.
    }
}
