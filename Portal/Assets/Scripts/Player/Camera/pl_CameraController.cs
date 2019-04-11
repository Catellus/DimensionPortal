using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class pl_CameraController : CameraSettingsEditor
{
#region Variables
    [Space, Header("Controller:")]
    public PlayerController player;                // Player's entity controller
    public float            smoothingTime = 0.25f; // Time it takes for the camera to re-center itself

    private List<PortalController> visiblePortals = new List<PortalController>();
    private Vector2 smoothingVelocity             = Vector2.zero;
    private Vector2 screenTopRight, screenBottomLeft;

#endregion Variables

    public void Start()
    {
        if (!player)
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        if (!base.cam)
            base.cam = this.gameObject.GetComponent<Camera>();

        player.AdditionalPortalTest = DetermineIsPortalVisible;
        player.SetNearestPortal();

        UpdateSettings(player.cinfo.worldIndex);
    }

    private void FixedUpdate()
    {
        this.transform.position = HandleMovement();

        foreach (PortalController pc in visiblePortals)
        {
            if (pc.accessableWorldIndices.Contains(player.cinfo.worldIndex))
            {
                pc.viewQuad.UpdateView(this.transform.position, player.transform.position, player.cinfo.worldIndex);
                pc.viewCamSettingsEditor.FindWorldCameraSettings(pc.GetWorldNameFromNextIndex(player.cinfo.worldIndex, player.reversePortalCycle));
            }
        }
    }

    private Vector3 HandleMovement()
    {
        Vector3 end = Vector2.SmoothDamp(this.transform.position, player.transform.position, ref smoothingVelocity, smoothingTime);
        end.z = player.transform.position.z - 1;
        return end;
    }

    public void UpdateSettings(int _newIndex)
    {
        string name = player.ptlController.GetWorldNameFromIndex(_newIndex);
        base.FindWorldCameraSettings(name);
    }

    private bool DetermineIsPortalVisible(GameObject p, int index)
    {
        if (index == 0)
        {
            visiblePortals.Clear();

            screenTopRight = base.cam.ScreenToWorldPoint(new Vector2(base.cam.pixelWidth, base.cam.pixelHeight));
            screenBottomLeft = base.cam.ScreenToWorldPoint(Vector2.zero);
        }

        PortalController pc = p.GetComponent<PortalController>();
        float halfHeight = pc.portalHalfHeight;


        Vector2 topPos    = p.transform.TransformPoint(new Vector2(0,  halfHeight));
        Vector2 bottomPos = p.transform.TransformPoint(new Vector2(0, -halfHeight));

        if ((topPos.x < screenTopRight.x || bottomPos.x < screenTopRight.x) && (topPos.x > screenBottomLeft.x || bottomPos.x > screenBottomLeft.x)
         && (topPos.y < screenTopRight.y || bottomPos.y < screenTopRight.y) && (topPos.y > screenBottomLeft.y || bottomPos.y > screenBottomLeft.y))
        {
            visiblePortals.Add(p.GetComponent<PortalController>());

            if (!pc.viewCam)
            {
                GameObject go = new GameObject(p.name + " View Camera");

                pc.viewCam = go.AddComponent<Camera>();
                pc.viewCam.orthographic     = true;
                pc.viewCam.nearClipPlane    = base.cam.nearClipPlane;
                pc.viewCam.farClipPlane     = base.cam.farClipPlane;
                pc.viewCam.orthographicSize = base.cam.orthographicSize;

                pc.viewQuad = go.AddComponent<ViewQuadManipulator>();
                pc.viewQuad.ptlController = pc;
                pc.viewQuad.Initialize();

                go.AddComponent<CameraSettingsEditor>().cam = pc.viewCam;
                pc.viewCamSettingsEditor = go.GetComponent<CameraSettingsEditor>();
            }
            else
                pc.viewCam.gameObject.SetActive(true);

            return true;
        }
        else
        {
            if (pc.viewCam)
                pc.viewCam.gameObject.SetActive(false);
            return false;
        }
    }


}
