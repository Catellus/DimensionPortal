using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamerasController : MonoBehaviour
{

    public PlayerController player;
    public Material viewMaterial; //Material on the view mesh that displays the portal's next world

    Camera mainCam;               //Camera that is always active (Does not see into other worlds)

    public float smoothingTime = 0.1f; //Time it takes for the camera to re-center itself
    Vector2 smoothingVelocity = Vector2.zero;

    List<GameObject> visiblePortals = new List<GameObject>();

    Vector2 screenTopRight, screenBottomLeft;

    private void Start()
    {
        mainCam = this.gameObject.GetComponent<Camera>();

        if (!player)
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

        player.AdditionalPortalTest = DetermineIsPortalVisible;
        player.SetNearestPortal();
    }

    private void FixedUpdate()
    {
        //float distToPortal = ((player.transform.position - player.ptlController.transform.position).magnitude - player.ptlController.worldSwitchDistance) / 3;
        //float smoothAmmount = Mathf.Lerp(0, smoothingTime, distToPortal);
        //this.transform.position = SmoothFollowSnapZ(smoothAmmount);
        this.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z - 1);

        foreach (GameObject g in visiblePortals)
            g.GetComponent<PortalController>().viewQuad.UpdateView(this.transform.position, player.cinfo.worldIndex, player.reversePortalCycle);
    }

    //private void LateUpdate()
    //{
    //    foreach (GameObject g in visiblePortals)
    //        g.GetComponent<PortalController>().viewQuad.UpdateView(this.transform.position, player.reversePortalCycle);
    //}

    bool DetermineIsPortalVisible(GameObject p, int index)
    {
        if (index == 0)
        {
            visiblePortals.Clear();

            screenTopRight = mainCam.ScreenToWorldPoint(new Vector2(mainCam.pixelWidth, mainCam.pixelHeight));
            screenBottomLeft = mainCam.ScreenToWorldPoint(Vector2.zero);
        }

        PortalController pc = p.GetComponent<PortalController>();
        float halfHeight = pc.portalHalfHeight;

        Vector2 topPos = p.transform.TransformPoint(new Vector2(0, halfHeight));
        Vector2 bottomPos = p.transform.TransformPoint(new Vector2(0, -halfHeight));

        if ((topPos.x < screenTopRight.x || bottomPos.x < screenTopRight.x) && (topPos.x > screenBottomLeft.x || bottomPos.x > screenBottomLeft.x)
         && (topPos.y < screenTopRight.y || bottomPos.y < screenTopRight.y) && (topPos.y > screenBottomLeft.y || bottomPos.y > screenBottomLeft.y))
        {
            visiblePortals.Add(p);

            if (!pc.viewCam)
            {
                GameObject go = new GameObject(p.name + " View Camera");

                pc.viewCam = go.AddComponent<Camera>();
                pc.viewCam.orthographic  = true;
                pc.viewCam.nearClipPlane = 0.5f;
                pc.viewCam.farClipPlane  = 1.5f;

                pc.viewQuad = go.AddComponent<ViewQuadManipulator>();
                pc.viewQuad.portal = pc;
                pc.viewQuad.Initialize(viewMaterial);
                //pc.viewQuad.viewAnchor = this.transform;
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

    Vector3 SmoothFollowSnapZ(float _smoothTime) // Smooths X and Y movement, snaps to Z positions (World switching)
    {
        //Vector3 end = Vector2.SmoothDamp(this.transform.position, player.transform.position, ref smoothingVelocity, _smoothTime);
        Vector3 end = this.transform.position;
        end.z = player.transform.position.z - 1;
        return end;
    }



    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.blue;                  // Show world position of the screen's top    right
        //Gizmos.DrawSphere(screenTopRight, 0.25f);   // Show world position of the screen's top    right
        //Gizmos.color = Color.cyan;                  // Show world position of the screen's bottom left
        //Gizmos.DrawSphere(screenBottomLeft, 0.25f); // Show world position of the screen's bottom left
    }

}
