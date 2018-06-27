using UnityEngine;

public class EntityMotor : RayManager
{
    #region Variables
    [Space, Header("EntityMotor")]
    [SerializeField] public CollisionInformation cinfo;
    [System.Serializable] public struct CollisionInformation {
        public bool below, above;
        public bool left, right;
        public bool edgeLeft, edgeRight; //Entity is at an edge on its left/right side
        public bool isFalling, throughPlatform;
        public int facingDirection;
        public bool inA;

        public void Reset()
        {
            above = below = false;
            left = right = false;
        }
    }

    float distanceToPortalCenter;
    int entitySideOfPortal;
    LayerMask throughPortalCollision;

    #endregion

    public override void Start()
    {
        base.Start();
        cinfo.inA = true;
        cinfo.facingDirection = 1; //Start facing right
    }

    public void Move(Vector2 _ammount, bool _onPlatform = false)
    {
        UpdateRayOrigins();

        cinfo.isFalling = cinfo.below;
        cinfo.Reset();

        throughPortalCollision = collisionMask;
        distanceToPortalCenter = (this.transform.position - portal.transform.position).magnitude;

        entitySideOfPortal = (int)Mathf.Sign(GetPortalPassDistance(this.transform.position).x);

        if (_ammount.x != 0)
            cinfo.facingDirection = (int)Mathf.Sign(_ammount.x);

        CheckHorizontalCollision(ref _ammount);

        if (_ammount.y != 0)
        {
            CheckVerticalCollision(ref _ammount);

            if (!cinfo.isFalling && cinfo.below) OnLanded();
            if (cinfo.isFalling && !cinfo.below) OnWalkOffLedge();
            cinfo.isFalling = !cinfo.below;
        }

        this.transform.Translate(_ammount);
        CheckEntityPassedThroughPortal();
    }

    #region Collision

    public void CheckHorizontalCollision(ref Vector2 _ammount)
    {
        float dir = cinfo.facingDirection;
        float rayLength = Mathf.Abs(_ammount.x) + skinBuffer;

        if (Mathf.Abs(_ammount.x) < skinBuffer)
            rayLength = 2 * skinBuffer;

        for (int i = 0; i < sideRayCount; i++)
        {
            Vector2 origin = (dir == -1) ? rayOrigins.bottomLeft : rayOrigins.bottomRight;
            origin += Vector2.up * (sideRaySpacing * i);

            if (distanceToPortalCenter <= portal.switchDistance)
                testThroughPortal(ref throughPortalCollision, origin);

            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * dir, rayLength, throughPortalCollision);

            Debug.DrawRay(origin, Vector2.right * dir, Color.blue);

            if (hit)
            {
                if (hit.transform.gameObject.layer == 30)
                {
                    LayerMask throughMask = 0;
                    throughMask |= 1 << (cinfo.inA ? 10 : 9);
                    RaycastHit2D throughHit = Physics2D.Raycast(hit.point, Vector2.right, rayLength - hit.distance, throughMask);

                    if (throughHit)
                    {
                        _ammount.x = ((hit.distance + throughHit.distance) - skinBuffer) * dir;
                    }
                }
                else
                    _ammount.x = (hit.distance - skinBuffer) * dir;

                cinfo.left = dir == -1;
                cinfo.right = dir == 1;
            }
        }
        throughPortalCollision = collisionMask;
    }
    public LayerMask wallMask;
    public void CheckVerticalCollision(ref Vector2 _ammount)
    {
        int dir = (int)Mathf.Sign(_ammount.y);
        float rayLength = Mathf.Abs(_ammount.y) + skinBuffer;

        if (dir == -1)
        {
            CheckLedges(ref _ammount);
        }

        for (int i = 0; i < capRayCount; i++)
        {
            Vector2 origin = (dir == -1) ? rayOrigins.bottomLeft : rayOrigins.topLeft;
            origin += Vector2.right * (capRaySpacing * i + _ammount.x);

            if (distanceToPortalCenter <= portal.switchDistance)
                testThroughPortal(ref throughPortalCollision, origin);

            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up * dir, rayLength, throughPortalCollision);

            Debug.DrawRay(origin, Vector2.up * dir, Color.red);

            if (hit)
            {
                if (hit.collider.tag == "Permeable")
                {
                    HandlePermeablePlatform();
                }

                _ammount.y = (hit.distance - skinBuffer) * dir;

                cinfo.below = dir == -1;
                cinfo.above = dir == 1;
            }
        }
        throughPortalCollision = collisionMask;
    }

    void testThroughPortal(ref LayerMask _mask, Vector3 _position)
    {
        int testedSide = (int)Mathf.Sign(GetPortalPassDistance(_position).x);

        if (testedSide != entitySideOfPortal)
        {
            _mask &= ~(1 << (cinfo.inA ? 9 : 10)); //If in worldA? remove world A : remove world B  --Does not affect any other collision layers set
            _mask |= 1 << (cinfo.inA ? 10 : 9);    //If in worldA?    add world B :    add world A  --Does not affect any other collision layers set
        }
        else
            _mask = collisionMask;
    }

    private void CheckEntityPassedThroughPortal()
    {
        if (distanceToPortalCenter <= portal.switchDistance)
        {
            if (entitySideOfPortal != (int)Mathf.Sign(GetPortalPassDistance(this.transform.position).x))
            {
                cinfo.inA = !cinfo.inA;
                collisionMask &= ~(1 << (cinfo.inA ? 10 : 9)); //If in worldA? remove world A : remove world B  --Does not affect any other collision layers set
                collisionMask |=   1 << (cinfo.inA ? 9 : 10);    //If in worldA?    add world B :    add world A  --Does not affect any other collision layers set
                EntityPassedThroughPortal();
            }
        }
    }

    protected virtual void EntityPassedThroughPortal() { }



    public void CheckLedges(ref Vector2 _ammount)
    {
    #region Left

        Vector2 leftOrigin = rayOrigins.bottomLeft;
        leftOrigin.x -= (skinBuffer * 2) - _ammount.x;
        RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.down, 1, collisionMask);

        if (leftHit)
            if (!cinfo.edgeLeft) //Just reached left ledge
                OnReachedLedge(false);
        cinfo.edgeLeft = !leftHit;

    #endregion
    #region Right

        Vector2 rightOrigin = rayOrigins.bottomRight;
        rightOrigin.x += (skinBuffer * 2) + _ammount.x;
        RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.down, 1, collisionMask);

        if (rightHit)
            if (!cinfo.edgeRight)
                OnReachedLedge(true);
        cinfo.edgeRight = !rightHit;

    #endregion

        Debug.DrawRay(leftOrigin , Vector2.down, Color.green);
        Debug.DrawRay(rightOrigin, Vector2.down, Color.white);
    }

    #endregion

    #region Virtuals
    protected virtual void OnLanded()                 { }
    protected virtual void OnWalkOffLedge()           { }
    protected virtual void OnReachedLedge(bool _right){ }
    protected virtual void HandlePermeablePlatform()  { }
    #endregion

    private void ResetThroughPlatform() { cinfo.throughPlatform = false; }
}