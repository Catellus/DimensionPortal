using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSettingsEditor : MonoBehaviour
{
    public Camera cam;                              // The camera being affected by currentSettings
    public so_CameraSettingsBase currentSettings;   // The settings to be applied to cam (scriptable object)

    public void FindWorldCameraSettings(string _worldName)  // Finds the cameraSettings with the same name as the input _worldName
    {
        so_CameraSettingsBase cs = Resources.Load<so_CameraSettingsBase>("ScriptableCamSettings/" + _worldName + "_CamSettings");
        UpdateCameraSettings(cs);
    }

    public void UpdateCameraSettings(so_CameraSettingsBase _inSettings) // Passes the camera to the settings to apply new settings
    {
        if (!cam)
            cam = this.gameObject.GetComponent<Camera>();

        _inSettings.ApplySettings(cam); // Done in settings to allow different sets to be applied
        currentSettings = _inSettings;
    }

}
