using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSettingsEditor : MonoBehaviour
{
    public Camera cam;
    public PortalController portalController;

    public so_CameraSettings currentSettings;

    private void Start()
    {
        cam = this.gameObject.GetComponent<Camera>();
    }

    public void FindWorldCameraSettings(int _inIndex)
    {
        bool reverse = portalController.viewCam.gameObject.GetComponent<ViewQuadManipulator>().reverseCycle;
        int viewIndex = portalController.GetNextIndex(_inIndex, !reverse);
        int worldIndex= portalController.accessableWorldIndices.IndexOf(viewIndex);
        string name = portalController.accessableWorldNames[worldIndex];

        FindWorldCameraSettings(name);

    }

    public void FindWorldCameraSettings(string _worldName)
    {
        so_CameraSettings cs = Resources.Load<so_CameraSettings>("ScriptableCamSettings/" + _worldName + "_CamSettings");
        UpdateCameraSettings(cs);
    }

    public void UpdateCameraSettings(so_CameraSettings _inSettings)
    {
        if (!cam)
            cam = this.gameObject.GetComponent<Camera>();

        currentSettings = _inSettings;
        cam.backgroundColor = currentSettings.clearColor;
    }

}
