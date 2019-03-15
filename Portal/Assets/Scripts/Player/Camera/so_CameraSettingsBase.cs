using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings")]
public class so_CameraSettingsBase : ScriptableObject   // Used as parent for all cameraSettings
{
    public Color backgroundColor = Color.white;

    public virtual void ApplySettings(Camera _cam)  // Apply each setting to the camera
    {
        _cam.backgroundColor = backgroundColor;
    }
}
