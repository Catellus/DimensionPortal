using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalBallController : EntityMotor
{
    private void FixedUpdate()
    {
        Move(new Vector2(5, 0) * Time.fixedDeltaTime);

        if (cinfo.right || cinfo.left || cinfo.above || cinfo.below)
        {
            SetPortal();
        }
    }

    public void SetPortal() //Sets the portal's transform to this.transform
    {
        portal.transform.position = (Vector2)this.transform.position;
        float snappedZ = Mathf.Round(this.transform.rotation.eulerAngles.z / 15) * 15; //Snaps portal Z rotation to multiple of 15
        portal.transform.rotation = Quaternion.Euler(new Vector3(0, 0, snappedZ));
        Destroy(this.gameObject);
    }

    public void SyncSettings(CollisionInformation _cIn, LayerMask _mask, tmp_Portal _portal)
    {
        cinfo.inA = _cIn.inA;
        this.collisionMask = _mask;
        //this.collisionMask &= ~(1 << 30); //Removes portal collision
        portal = _portal;
    }

}
