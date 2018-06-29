using UnityEngine;
using ToolBox;

public class EntityMotor : EntityRayManager
{
    #region Variables
    [Space, Header("EntityMotor")]
    [SerializeField] public CollisionInformation cinfo;

    [System.Serializable] public struct CollisionInformation {
        public bool below, above;               //Is entity touching a wall above/below of itself (ceiling / floor)
        public bool left, right;                //Is entity touching a wall left/right of itself
        public bool edgeLeft, edgeRight;        //Is entity at an edge on its left/right side
        public bool isFalling, throughPlatform; //Is entity in mid-air? -- Does entity fall through "Permeable" platforms?
        public bool inA;                        //Is entity in WorldA?
        public int facingDirection;             //Direction of entity's "forward" -- -1 faces left, 1 faces right

        public void Reset()
        {
            above    = below     = false;
            left     = right     = false;
            edgeLeft = edgeRight = false;
        }
    }

    private float     distanceToPortalCenter; //Entity's distance to the portal's position
    private int       entitySideOfPortal;     //Direction along the portal's right vector the entity is located (1 or -1)
    private LayerMask throughPortalCollision; //Per-trace collisionMask -- Lets the trace search for objects in opposite world as entity

    #endregion

    public override void Start()
    {
        base.Start();
        cinfo.inA = true;          //Entity is in WorldA by default
        cinfo.facingDirection = 1; //Entity faces right by default
    }

    public void Move(Vector2 _ammount, bool _onPlatform = false)
    {
        cinfo.isFalling = cinfo.below; //Used to store previous tick's "below" for later use
        cinfo.Reset();

        throughPortalCollision = collisionMask; //Resets trace mask
        distanceToPortalCenter = ((Vector2)this.transform.position - (Vector2)portal.transform.position).magnitude;

        //Used later to determine if traces/entity are through the portal
        entitySideOfPortal = (int)Mathf.Sign(GetPortalPassDistance(this.transform.position));

        if (_ammount.x != 0) //Sets facingDirection
            cinfo.facingDirection = (int)Mathf.Sign(_ammount.x);

        CheckHorizontalCollision(ref _ammount);

        if (_ammount.y != 0)
        {
            CheckVerticalCollision(ref _ammount);

            if (!cinfo.isFalling && cinfo.below) OnLanded();       //If was not on ground and is now
            if (cinfo.isFalling && !cinfo.below) OnWalkOffLedge(); //If was on ground and is not now
            cinfo.isFalling = !cinfo.below;
        }

        this.transform.Translate(_ammount); //Moves the entity

        CheckEntityPassedThroughPortal(); //Uses entitySideOfPortal to check if the entity has swapped worlds

        UpdateRayOrigins(); //Updates Ray Origins to new location
    }

    #region Collision

    public void CheckHorizontalCollision(ref Vector2 _ammount)
    {
        float direction = cinfo.facingDirection;
        float rayLength = Mathf.Abs(_ammount.x) + skinBuffer;

        if (Mathf.Abs(_ammount.x) < skinBuffer)
            rayLength = 2 * skinBuffer;

        for (int i = 0; i < sideRayCount; i++) //Iterate and check for collisions
        {
            Vector2 origin = (direction == -1) ? rayOrigins.bottomLeft : rayOrigins.bottomRight; //Start of the trace -- Determine if should check on left/right side
            origin += (Vector2) (this.transform.rotation * (Vector2.up * (sideRaySpacing * i))); //Offset origin on its relative up by its count

            if (distanceToPortalCenter <= portal.worldSwitchDistance) //If the origin is close to the portal, check if it has passed through it
                GetTraceThroughPortal(ref throughPortalCollision, origin);

            RaycastHit2D hit = Physics2D.Raycast(origin, this.transform.rotation * (Vector2.right * direction), rayLength, throughPortalCollision);
            Debug.DrawRay(origin, this.transform.rotation * (Vector2.right * direction), Color.blue);

            if (hit)
            {
                if (hit.transform.gameObject.layer == 30) //If trace hit portal, trace again through portal for opposite world
                {
                    LayerMask throughMask = 0;
                    throughMask |= 1 << (cinfo.inA ? 10 : 9); //Adds opposite world to collision mask (In WorldA? WorldB : WorldA)
                    RaycastHit2D throughHit = Physics2D.Raycast(hit.point, this.transform.rotation * (Vector2.right * direction), rayLength - hit.distance, throughMask);

                    if (throughHit) //Secondary trace collided
                    {
                        _ammount.x = ((hit.distance + throughHit.distance) - skinBuffer) * direction; //Let entity move only the combined distance of the traces
                        cinfo.left  = direction == -1;                                                //Hit left if moving left
                        cinfo.right = direction ==  1;                                                //Hit right if moving right
                    }
                }
                else
                {
                    _ammount.x = (hit.distance - skinBuffer) * direction; //Let entity move only the distance of the trace
                    cinfo.left  = direction == -1;                        //Hit left if moving left
                    cinfo.right = direction ==  1;                        //Hit right if moving right
                }

            }
        }
        throughPortalCollision = collisionMask; //Resets trace mask
    }

    public void CheckVerticalCollision(ref Vector2 _ammount)
    {
        int direction = (int)Mathf.Sign(_ammount.y);
        float rayLength = Mathf.Abs(_ammount.y) + skinBuffer;
        if (Mathf.Abs(_ammount.y) < skinBuffer)
            rayLength = 2 * skinBuffer;

        if (direction == -1) //If moving down, check for ledges on entity's left & right sides
            CheckLedges(ref _ammount);

        for (int i = 0; i < capRayCount; i++) //Iterate and check for collisions
        {
            Vector2 origin = (direction == -1) ? rayOrigins.bottomLeft : rayOrigins.topLeft;      //Start of the trace -- Determine if should check on top/bottom
            origin += (Vector2)(this.transform.rotation * (Vector2.right * (capRaySpacing * i))); //Offset origin on its relative right by its count

            if (distanceToPortalCenter <= portal.worldSwitchDistance) //If the origin is close to the portal, check if it has passed through it
                GetTraceThroughPortal(ref throughPortalCollision, origin);

            RaycastHit2D hit = Physics2D.Raycast(origin, this.transform.rotation * (Vector2.up * direction), rayLength, throughPortalCollision);
            Debug.DrawRay(origin, this.transform.rotation * (Vector2.up * direction), Color.red);

            if (hit)
            {
                if (hit.transform.gameObject.layer == 30) //If trace hit portal, trace again through portal for opposite world
                {
                    LayerMask throughMask = 0;
                    throughMask |= 1 << (cinfo.inA ? 10 : 9); //Adds opposite world to collision mask (In WorldA? WorldB : WorldA)
                    RaycastHit2D throughHit = Physics2D.Raycast(hit.point, this.transform.rotation * (Vector2.up * direction), rayLength - hit.distance, throughMask);

                    if (throughHit)
                    {
                        _ammount.y = ((hit.distance + throughHit.distance) - skinBuffer) * direction; //Let entity move only the combined distance of the traces
                        cinfo.below = direction == -1;                                                //Hit below if moving down
                        cinfo.above = direction == 1;                                                 //Hit above if moving up
                    }
                }
                else
                {
                    if (hit.collider.tag == "Permeable") //If the entity collides with a "permeable" platform, handle it in its script
                    {
                        if (HandlePermeablePlatform() == -1)
                            continue; //Ignores permeable platform
                    }

                    _ammount.y = (hit.distance - skinBuffer) * direction; //Let entity move only the distance of the trace
                    cinfo.below = direction == -1;                        //Hit below if moving down
                    cinfo.above = direction == 1;                         //Hit above if moving up
                }
            }
        }
        throughPortalCollision = collisionMask; //Resets trace mask
    }

    void GetTraceThroughPortal(ref LayerMask _mask, Vector3 _position)
    {
        if ((int)Mathf.Sign(GetPortalPassDistance(_position)) != entitySideOfPortal) //If _position is on the opposite side of the portal as the entity at the start of tick
        {
            _mask &= ~(1 << (cinfo.inA ? 9 : 10)); //If in worldA? remove world A : remove world B  --Does not affect any other collision layers set
            _mask |=   1 << (cinfo.inA ? 10 : 9);  //If in worldA?    add world B :    add world A  --Does not affect any other collision layers set
        }
        else
            _mask = collisionMask; //Resets trace mask
    }

    private void CheckEntityPassedThroughPortal() //Checks if the enity has moved from one side of the portal to the other
    {
        if (distanceToPortalCenter <= portal.worldSwitchDistance)
        {
            if (entitySideOfPortal != (int)Mathf.Sign(GetPortalPassDistance(this.transform.position))) //Side of portal at start of tick //Side of portal at end of tick
            {
                cinfo.inA = !cinfo.inA;                        //Switch worlds
                collisionMask &= ~(1 << (cinfo.inA ? 10 : 9)); //If in worldA? remove world A : remove world B  -- Does not affect any other collision layers set
                collisionMask |=   1 << (cinfo.inA ? 9 : 10);  //If in worldA?    add world B :    add world A  -- Does not affect any other collision layers set
                EntityPassedThroughPortal();                   //Virtual call for additional functionality in extended scripts
            }
        }
    }

    public float GetPortalPassDistance(Vector2 _location) //Gets the distance along the portal's right vector _location resides
    {
        //MathTools.RoundVector -- Rounds x, y, and Z to specified decimal place
        Vector2 entityOffset = MathTools.RoundVector3(_location - (Vector2)portal.transform.position, 5);                     //Makes "entityOffset" a relative position
        Vector2 pointOnPlane = (Vector2)MathTools.RoundVector3(Vector3.ProjectOnPlane(entityOffset, portal.transform.up), 5); //Gets the nearest point to entityOffset along the portal's right vector

        float a = pointOnPlane.x / portal.transform.right.x;            //Distance along portal.right vector pointOnPlane sits
        a = (a == 0) ? (pointOnPlane.y / portal.transform.right.y) : a; //If X- axis returned 0, find the Y-axis value.

        return (float)System.Math.Round(a, 4); //Rounds the value to 4 decimal places
    }

    public void CheckLedges(ref Vector2 _ammount) //Detect if entity has reached a ledge
    {
    #region Left

        Vector2 leftOrigin = rayOrigins.bottomLeft;    //Origin always on entity's left side
        leftOrigin.x -= (skinBuffer * 2) - _ammount.x; //Origin offset skinBuffer away from entity's left side
        RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.down, 1, collisionMask);

        if (!leftHit)                  //If trace found no ground
            if (!cinfo.edgeLeft)       //If entity was not at ledge last tick
                OnReachedLedge(false); //Additional functionality for extended scripts (is ledge at right?)
        cinfo.edgeLeft = !leftHit;

    #endregion
    #region Right

        Vector2 rightOrigin = rayOrigins.bottomRight;   //Origin always on entity's right side
        rightOrigin.x += (skinBuffer * 2) + _ammount.x; //Origin offset skinBuffer away from entity's right side
        RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.down, 1, collisionMask);

        if (!rightHit)                //If trace found no ground
            if (!cinfo.edgeRight)     //If entity was not at ledge last tick
                OnReachedLedge(true); //Additional functionality for extended scripts (is ledge at right?)
        cinfo.edgeRight = !rightHit;

    #endregion

        Debug.DrawRay(leftOrigin , this.transform.rotation * (Vector2.down), Color.green);
        Debug.DrawRay(rightOrigin, this.transform.rotation * (Vector2.down), Color.white);
    }

    #endregion

    #region Virtuals
    protected virtual void OnLanded()                  { }           //Additional functionality used in extended scripts -- Entity stops falling and is grounded
    protected virtual void OnWalkOffLedge()            { }           //Additional functionality used in extended scripts -- Entity is no longer grounded
    protected virtual void OnReachedLedge(bool _right) { }           //Additional functionality used in extended scripts -- Entity has reached a ledge
    protected virtual int  HandlePermeablePlatform()   { return 0; } //Additional functionality used in extended scripts -- Entity is colliding with permeable platform
    protected virtual void EntityPassedThroughPortal() { }           //Additional functionality used in extended scripts -- Entity has switched dimensions
    #endregion

    private void ResetThroughPlatform() { cinfo.throughPlatform = false; } //Makes entity no longer pass through permeable platforms

}