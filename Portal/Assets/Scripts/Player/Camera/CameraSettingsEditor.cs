using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSettingsEditor : MonoBehaviour
{
    public Camera cam;
    public so_CameraSettings currentSettings;

    public void FindWorldCameraSettings(string _worldName)
    {
        so_CameraSettings cs = Resources.Load<so_CameraSettings>("ScriptableCamSettings/" + _worldName + "_CamSettings");
        UpdateCameraSettings(cs);
    }

    public void UpdateCameraSettings(so_CameraSettings _inSettings)
    {
        if (!cam)
            cam = this.gameObject.GetComponent<Camera>();

        cam.backgroundColor = _inSettings.clearColor;
        currentSettings = _inSettings;
    }

}
