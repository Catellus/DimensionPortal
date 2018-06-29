using UnityEngine;

public class PortalSphere : EntityMotor
{
    [Header("Portal Sphere")]
    public float movementSpeed = 5.0f; //Speed of the ball's movement

    private void FixedUpdate()
    {
        //Moves the ball on its relative X axis movementSpeed meters per second
        Move(new Vector2(movementSpeed, 0) * Time.fixedDeltaTime);

        if (cinfo.right || cinfo.left || cinfo.above || cinfo.below)
        {
            SetPortal(); //Change portal's position on any collision
        }
    }

    public void SetPortal() //Sets the portal's transform to this.transform
    {
        portal.transform.position = (Vector2)this.transform.position;                  //Moves the portal to its new location
        float snappedZ = Mathf.Round(this.transform.rotation.eulerAngles.z / 15) * 15; //Snaps portal Z rotation to multiple of 15
        portal.transform.rotation = Quaternion.Euler(new Vector3(0, 0, snappedZ));     //Rotates portal to this.rotation
        Destroy(this.gameObject);                                                      //Remove sphere from world
    }

    public void SyncSettings(CollisionInformation _cIn, LayerMask _mask, PortalManager _portal)
    {
        cinfo.inA          = _cIn.inA; //Match player's world -- Used for moving through portal
        this.collisionMask = _mask;    //Match player's world collision

        //Saves from needing to set portal variable in every entityMotor.
        portal = _portal;           //Ensures this ball is moving the correct portal
    }

}
