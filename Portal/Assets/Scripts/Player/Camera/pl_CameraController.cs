using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pl_CameraController : CameraSettingsEditor
{
    [Space, Header("Controller:")]
    public PlayerController player;
    public Material         viewMaterial; //Material on the view mesh that displays the portal's next world

    List<PortalController> visiblePortals = new List<PortalController>();

    public float smoothingTime = 0.1f; //Time it takes for the camera to re-center itself
    Vector2 screenTopRight, screenBottomLeft, smoothingVelocity = Vector2.zero;

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
        HandleMovement();

        foreach (PortalController ptl in visiblePortals)
        {
            ptl.viewQuad.UpdateView(this.transform.position, player.cinfo.worldIndex, player.reversePortalCycle);
            ptl.viewCamSettingsEditor.FindWorldCameraSettings(ptl.GetWorldNameFromNextIndex(player.cinfo.worldIndex, player.reversePortalCycle));
        }
    }

    void HandleMovement()
    {
        //float distToPortal = ((player.transform.position - portalController.transform.position).magnitude - portalController.worldSwitchDistance) / 5;
        //Debug.Log(distToPortal);
        //float smoothAmmount = Mathf.Lerp(0, smoothingTime, distToPortal);
        //this.transform.position = SmoothFollowSnapZ(smoothAmmount);
        this.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z - 1);
    }

    Vector3 SmoothFollowSnapZ(float _smoothTime) // Smooths X and Y movement, snaps to Z positions (World switching)
    {
        Vector3 end = Vector2.SmoothDamp(this.transform.position, player.transform.position, ref smoothingVelocity, _smoothTime);
        end.z = player.transform.position.z - 1;
        return end;
    }

    public void UpdateSettings(int _newIndex)
    {
        string name = player.ptlController.GetWorldNameFromIndex(_newIndex);
        base.FindWorldCameraSettings(name);
    }

    bool DetermineIsPortalVisible(GameObject p, int index)
    {
        if (index == 0)
        {
            visiblePortals.Clear();

            screenTopRight   = base.cam.ScreenToWorldPoint(new Vector2(base.cam.pixelWidth, base.cam.pixelHeight));
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
                pc.viewQuad.portal = pc;
                pc.viewQuad.Initialize(viewMaterial);
                //pc.viewQuad.viewAnchor = this.transform;

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

    //private void OnDrawGizmos()
    //{
    //    //Gizmos.color = Color.blue;                  // Show world position of the screen's top    right
    //    //Gizmos.DrawSphere(screenTopRight, 0.25f);   // Show world position of the screen's top    right
    //    //Gizmos.color = Color.cyan;                  // Show world position of the screen's bottom left
    //    //Gizmos.DrawSphere(screenBottomLeft, 0.25f); // Show world position of the screen's bottom left
    //}

}
