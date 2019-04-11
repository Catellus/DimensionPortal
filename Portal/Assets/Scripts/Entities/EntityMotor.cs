using UnityEngine;
using ToolBox;

public class EntityMotor : EntityRayManager
{
    #region Variables
    [Space, Header("EntityMotor")]
    public  bool  reversePortalCycle     = false; // Dictates which direction the portal will transport the entity
    private float distanceToPortalCenter = 100;   // Entity's distance to the portal's position
    private int   entitySideOfPortal;             // Portal relative X direction the entity lies

    #endregion


    public override void Start()
    {
        base.Start();
        cinfo.worldIndex = 0;   //Entity is in World0 by default
        cinfo.facingDirection = 1; //Entity faces right by default
    }

    public void Move(Vector2 _ammount, bool _onPlatform = false)
    {
        cinfo.falling = cinfo.below; //Used to store previous tick's "below" for later use
        cinfo.Reset();

        if (ptlController != null)
        {
            distanceToPortalCenter = ((Vector2)this.transform.position - (Vector2)ptlController.transform.position).magnitude;
            entitySideOfPortal     = (int)Mathf.Sign(GetPortalPassDistance(this.transform.position));
            reversePortalCycle     = entitySideOfPortal == -1;
        }

        if (_ammount != Vector2.zero)
            SetNearestPortal();

        if (_ammount.x != 0) //Sets facingDirection
            cinfo.facingDirection = (int)Mathf.Sign(_ammount.x);

        CheckHorizontalCollision(ref _ammount);

        if (_ammount.y != 0)
        {
            CheckVerticalCollision(ref _ammount);

            if (!cinfo.falling && cinfo.below) OnLanded();       //If was not on ground and is now
            if (cinfo.falling && !cinfo.below) OnWalkOffLedge(); //If was on ground and is not now
            cinfo.falling = !cinfo.below;
        }

        this.transform.position += (collisionRotation * _ammount); //Moves the entity

        CheckEntityPassedThroughPortal(); //Uses entitySideOfPortal to check if the entity has swapped worlds

        UpdateRayOrigins(); //Updates Ray Origins to new location
    }


    #region Collision

    //// HORIZONTAL COLLISION \\\\
    public void CheckHorizontalCollision(ref Vector2 _ammount)
    {
        int direction = cinfo.facingDirection;
        int hitDepth = (int)this.transform.position.z;

        float rayLength = (Mathf.Abs(_ammount.x) < skinBuffer) ? rayLength = 2 * skinBuffer : Mathf.Abs(_ammount.x) + skinBuffer;

        Vector2 testRotation = collisionRotation * (Vector2.right * direction);
        Vector2 anchor = (direction == -1) ? rayOrigins.bottomLeft : rayOrigins.bottomRight;

        for (int i = 0; i < sideRayCount; i++)
        {
            Vector2 origin = anchor + (Vector2)(collisionRotation * (Vector2.up * (sideRaySpacing * i)));

            int tmpDepth = hitDepth;
            if (ptlController != null && distanceToPortalCenter <= ptlController.worldSwitchDistance) //Check if origin has passed through Portal
                GetTraceThroughPortal(ref tmpDepth, origin);

            RaycastHit2D hit = Physics2D.Raycast(origin, testRotation, rayLength, collisionMask | detectionMask, tmpDepth, tmpDepth);
            if (DEBUG_showDraws) Debug.DrawRay(origin, testRotation, Color.blue);

            if (hit)
                if (HorizontalHitInteraction(hit, testRotation, direction, rayLength, ref _ammount) == -1)
                    continue;
        }
        //throughPortalCollision = collisionMask; //Resets trace mask
    }


    public virtual int HorizontalHitInteraction(RaycastHit2D _hit, Vector2 _rotation, int _direction, float _rayLength, ref Vector2 _ammount)
    {
        bool isCollider = (collisionMask & (1 << _hit.transform.gameObject.layer)) == 1;

        if (!isCollider && _hit.transform.gameObject.layer == 31)
        {
            int hitDepth = ptlController.GetNextIndex(cinfo.worldIndex, reversePortalCycle);
            RaycastHit2D throughHit = Physics2D.Raycast(_hit.point, _rotation, _rayLength - _hit.distance, collisionMask, hitDepth, hitDepth);

            if (throughHit) //Secondary trace collided
            {
                if (_hit.collider.tag == "Permeable")
                    if (HandlePermeablePlatform() == -1) //If the entity collides with a "permeable" platform, handle it in its script
                        return -1;

                _ammount.x  = ((_hit.distance + throughHit.distance) - skinBuffer) * _direction; //Let entity move only the combined distance of the traces
                cinfo.left  = _direction == -1;
                cinfo.right = _direction == 1;
            }
        }
        else if (isCollider)
        {
            if (_hit.collider.tag == "Permeable")
                if (HandlePermeablePlatform() == -1) //If the entity collides with a "permeable" platform, handle it in its script
                    return -1;

            _ammount.x  = (_hit.distance - skinBuffer) * _direction; //Let entity move only the distance of the trace
            cinfo.left  = _direction == -1;
            cinfo.right = _direction == 1;
        }
        return 0;
    }


    //// VERTICAL COLLISION \\\\
    public void CheckVerticalCollision(ref Vector2 _ammount)
    {

        int direction = (int)Mathf.Sign(_ammount.y);
        int hitDepth = (int)this.transform.position.z;

        float rayLength = (Mathf.Abs(_ammount.y) < skinBuffer) ? rayLength = 2 * skinBuffer : Mathf.Abs(_ammount.y) + skinBuffer;

        if (direction == -1)
            CheckLedges(ref _ammount);


        Vector2 testRotation = collisionRotation * (Vector2.up * direction);
        Vector2 anchor = (direction == -1) ? rayOrigins.bottomLeft : rayOrigins.topLeft;

        for (int i = 0; i < capRayCount; i++)
        {
            Vector2 origin = anchor + (Vector2)(collisionRotation * (Vector2.right * (capRaySpacing * i)));
            int tmpDepth = hitDepth;

            if (ptlController != null && distanceToPortalCenter <= ptlController.worldSwitchDistance) //Check if origin has passed through Portal
                GetTraceThroughPortal(ref tmpDepth, origin);

            RaycastHit2D hit = Physics2D.Raycast(origin, testRotation, rayLength, collisionMask | detectionMask, tmpDepth, tmpDepth);
            if (DEBUG_showDraws) Debug.DrawRay(origin, testRotation, Color.red);

            if (hit)
                if (VerticalHitInteraction(hit, testRotation, direction, rayLength, ref _ammount) == -1)
                    continue;
        }
        //throughPortalCollision = collisionMask; //Resets trace mask
    }

    public virtual int VerticalHitInteraction(RaycastHit2D _hit, Vector2 _rotation, int _direction, float _rayLength, ref Vector2 _ammount)
    {
        bool isCollider = (collisionMask & (1 << _hit.transform.gameObject.layer)) == 1;

        if (!isCollider && _hit.transform.gameObject.layer == 31) //If hit portal, trace other world
        {
            int hitDepth = ptlController.GetNextIndex(cinfo.worldIndex, reversePortalCycle); //ptlController.nextCollisionLayer;
            RaycastHit2D throughHit = Physics2D.Raycast(_hit.point, _rotation, _rayLength - _hit.distance, collisionMask, hitDepth, hitDepth);

            if (throughHit)
            {
                _ammount.y = ((_hit.distance + throughHit.distance) - skinBuffer) * _direction; //Let entity move only the combined distance of the traces
                cinfo.below = _direction == -1;
                cinfo.above = _direction == 1;
            }
        }
        else if (isCollider)
        {
            if (_hit.collider.tag == "Permeable")
                if (HandlePermeablePlatform() == -1) //If the entity collides with a "permeable" platform, handle it in its script
                    return -1;

            _ammount.y = (_hit.distance - skinBuffer) * _direction; //Let entity move only the distance of the trace
            cinfo.below = _direction == -1;
            cinfo.above = _direction == 1;
        }
        return 0;
    }


    public void CheckLedges(ref Vector2 _ammount) //Detect if entity has reached a ledge
    {
        #region Left

        Vector2 leftOrigin = rayOrigins.bottomLeft;    //Origin always on entity's left side
        leftOrigin.x -= (skinBuffer * 2) - _ammount.x; //Origin offset skinBuffer away from entity's left side
        RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, collisionRotation * Vector2.down, 1, collisionMask);

        if (!leftHit)                  //If trace found no ground
            if (!cinfo.edgeLeft)       //If entity was not at ledge last tick
                OnReachedLedge(false); //Additional functionality for extended scripts (is ledge at right?)
        cinfo.edgeLeft = !leftHit;

        #endregion
        #region Right

        Vector2 rightOrigin = rayOrigins.bottomRight;   //Origin always on entity's right side
        rightOrigin.x += (skinBuffer * 2) + _ammount.x; //Origin offset skinBuffer away from entity's right side
        RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, collisionRotation * Vector2.down, 1, collisionMask);

        if (!rightHit)                //If trace found no ground
            if (!cinfo.edgeRight)     //If entity was not at ledge last tick
                OnReachedLedge(true); //Additional functionality for extended scripts (is ledge at right?)
        cinfo.edgeRight = !rightHit;

        #endregion

        if (DEBUG_showDraws) Debug.DrawRay(leftOrigin, collisionRotation * (Vector2.down), Color.green);
        if (DEBUG_showDraws) Debug.DrawRay(rightOrigin, collisionRotation * (Vector2.down), Color.white);
    }



    //========================\\
    //   PORTAL INTERACTION   \\
    //========================\\

    public float GetPortalPassDistance(Vector2 _location) //Gets the distance along the portal's right vector _location resides
    {
        //MathTools.RoundVector -- Rounds x, y, and Z to specified decimal place
        Vector2 entityOffset = (Vector2)MathTools.RoundVector3(_location - (Vector2)ptlController.transform.position, 4);            //Makes "entityOffset" a relative position
        Vector2 pointOnPlane = (Vector2)MathTools.RoundVector3(Vector3.ProjectOnPlane(entityOffset, ptlController.transform.up), 4); //Gets the nearest point to entityOffset along the portal's right vector

        float a = pointOnPlane.x / ptlController.transform.right.x;            //Distance along portal.right vector pointOnPlane sits
        a = (a == 0) ? (pointOnPlane.y / ptlController.transform.right.y) : a; //If X- axis returned 0, find the Y-axis value.

        return (float)System.Math.Round(a, 4); //Rounds the value to 4 decimal places
    }

    void GetTraceThroughPortal(ref int _depth, Vector3 _position)
    {
        if ((int)Mathf.Sign(GetPortalPassDistance(_position)) != entitySideOfPortal) //If _position is on the opposite side of the portal as the entity at the start of tick
            _depth = ptlController.GetNextIndex(cinfo.worldIndex, reversePortalCycle);
    }

    private void CheckEntityPassedThroughPortal() //Checks if the entity has moved from one side of the portal to the other
    {
        if (ptlController != null && distanceToPortalCenter <= ptlController.worldSwitchDistance)
            if (entitySideOfPortal != (int)Mathf.Sign(GetPortalPassDistance(this.transform.position))) //Side of portal at start of tick != Side of portal at end of tick
                EntityPassedThroughPortal(); //Virtual call for additional functionality in extended scripts
    }

    protected virtual void EntityPassedThroughPortal()
    {
        reversePortalCycle = !reversePortalCycle; // Here to prevent scene flashing when switching worlds (I have no idea why this works)
        ptlController.EntityPassedThroughPortal(this.gameObject.tag, ref cinfo.worldIndex, !reversePortalCycle);
        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, cinfo.worldIndex);
    }

    protected override void GetNewNearestPortalSide()
    {
        entitySideOfPortal = (int)Mathf.Sign(GetPortalPassDistance(this.transform.position));
        reversePortalCycle = entitySideOfPortal == -1;
    }

    #endregion

    #region Virtuals
    protected virtual void OnLanded() { }                         // Entity stops falling and is grounded
    protected virtual void OnWalkOffLedge() { }                   // Entity is no longer grounded
    protected virtual void OnReachedLedge(bool _atRight) { }      // Entity has reached a ledge
    protected virtual int HandlePermeablePlatform() { return 0; } // Entity is colliding with permeable platform


    #endregion

    private void ResetThroughPlatform() { cinfo.throughPlatform = false; } //Makes entity no longer pass through permeable platforms

}