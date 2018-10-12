using UnityEngine;

public class EntityRayManager : MonoBehaviour
{
    #region Variables
    [Header("RayManager")]  public  LayerMask           collisionMask;
                            public  PortalController    ptlController;
                            public  PortalController[]  visiblePortals;

    [SerializeField, Space] protected float raySpacing   = 0.2f;   //Maximum distance between rays
    [SerializeField]        protected float skinBuffer   = 0.015f; //Distance the rays are inset into the entity

    [SerializeField, Space] protected float entityHeight = 1.0f; //Entity height in meters -- independent of entity's scale
    [SerializeField]        protected float entityWidth  = 1.0f; //Entity width  in meters -- independent of entity's scale
    [SerializeField]        protected float collisionRotationOffset = 0.0f; //Can be used to compensate for entity's rotation

    protected int          capRayCount;    //Number of rays on the entity's top/bottom sides
    protected float        capRaySpacing;  //Distance between rays on the entity's top/bottom sides
    protected int          sideRayCount;   //Number of rays on the entity's left/right sides
    protected float        sideRaySpacing; //Distance between rays on the entity's left/right sides
    protected originPoints rayOrigins;     //World locations for each corner of the entity

    protected Quaternion collisionRotation;

    [SerializeField] public CollisionInformation cinfo;

    [System.Serializable]
    public struct CollisionInformation
    {
        public bool below, above;               //Is entity touching a wall above/below of itself (ceiling / floor)
        public bool left, right;                //Is entity touching a wall left/right of itself
        public bool edgeLeft, edgeRight;        //Is entity at an edge on its left/right side
        public bool isFalling, throughPlatform; //Is entity in mid-air? -- Does entity fall through "Permeable" platforms?
        public int worldIndex;                  //Loaded world index the entity is in
        public int facingDirection;             //Direction of entity's "forward" -- -1 faces left, 1 faces right

        public void Reset()
        {
            above = below = false;
            left = right = false;
            edgeLeft = edgeRight = false;
        }
    }

    public struct originPoints
    {
        public Vector2 topLeft   , topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    public delegate bool SwappablePortalTests(GameObject _portal, int _index);
    public SwappablePortalTests AdditionalPortalTest;
    public EntityRayManager() { AdditionalPortalTest = (GameObject p, int i) => { return true; }; }

    #endregion //Variables

    public virtual void Start()
    {
        DetermineRaySpacing();
    }

    public virtual void FixedUpdate()
    {
        collisionRotation = Quaternion.Euler(this.transform.rotation.eulerAngles + new Vector3(0, 0, collisionRotationOffset));
    }

    public void SetNearestPortal()
    {
        PortalController tmpClosest = ptlController;
        float tmpDistance = float.MaxValue;

        int index = 0;
        foreach (var p in GameObject.FindGameObjectsWithTag("Portal"))
        {
            if (AdditionalPortalTest(p, index))
            {
                if ((p.transform.position - this.transform.position).magnitude < tmpDistance)
                {
                    PortalController pc = p.GetComponent<PortalController>();
                    if (pc && pc.GetNextIndex(cinfo.worldIndex, false) != cinfo.worldIndex)
                    {
                        tmpDistance = (p.transform.position - this.transform.position).magnitude;
                        tmpClosest = pc;
                    }
                }
            }
            index++;
        }
        ptlController = tmpClosest;
    }

    public void DetermineRaySpacing() //Gets ray counts and spacing based om raySpacing
    {
        float bWidth  = entityWidth  + (-2 * skinBuffer); //Skin compensated width  -- Keeps rays from being 0 length.
        float bHeight = entityHeight + (-2 * skinBuffer); //Skin compensated height -- Keeps rays from being 0 length.

        capRayCount  = Mathf.Clamp(Mathf.CeilToInt(bWidth  / raySpacing), 2, int.MaxValue);
        sideRayCount = Mathf.Clamp(Mathf.CeilToInt(bHeight / raySpacing), 2, int.MaxValue);

        capRaySpacing  = bWidth  / (capRayCount  - 1);
        sideRaySpacing = bHeight / (sideRayCount - 1);

        UpdateRayOrigins();
    }

    public void UpdateRayOrigins() //Gets the world position of the corners of the entity
    {
        float   skinOffset = -2 * skinBuffer;
        Vector2 extents = (new Vector2((entityWidth + skinOffset) / 2, (entityHeight + skinOffset)/2)); //Shrinks the bounds for skin
        Vector2 center  = this.transform.position;

        rayOrigins.topLeft     = center + (Vector2)(collisionRotation * new Vector2(-extents.x,  extents.y)); //Turns relative location into world position
        rayOrigins.topRight    = center + (Vector2)(collisionRotation * new Vector2( extents.x,  extents.y)); //Turns relative location into world position
        rayOrigins.bottomRight = center + (Vector2)(collisionRotation * new Vector2( extents.x, -extents.y)); //Turns relative location into world position
        rayOrigins.bottomLeft  = center + (Vector2)(collisionRotation * new Vector2(-extents.x, -extents.y)); //Turns relative location into world position
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(Vector3.zero), Vector3.one); //Resets gizmo rotation to world
            Gizmos.color = Color.red;                         //Draw red     Top    Left  sphere
            Gizmos.DrawSphere(rayOrigins.topLeft    , 0.05f); //Draw red     Top    Left  sphere
            Gizmos.color = Color.blue;                        //Draw blue    Top    Right sphere
            Gizmos.DrawSphere(rayOrigins.topRight   , 0.05f); //Draw blue    Top    Right sphere
            Gizmos.color = Color.green;                       //Draw green   Bottom Left  sphere
            Gizmos.DrawSphere(rayOrigins.bottomLeft , 0.05f); //Draw green   Bottom Left  sphere
            Gizmos.color = Color.magenta;                     //Draw magenta Bottom Right sphere
            Gizmos.DrawSphere(rayOrigins.bottomRight, 0.05f); //Draw magenta Bottom Right sphere

            Gizmos.color = new Color(0.25f, 1.0f, 0.25f, 1.0f);
            Gizmos.matrix = Matrix4x4.TRS(this.transform.position, collisionRotation, Vector3.one); //Sets gizmo rotation to local
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(entityWidth, entityHeight, 1.0f));
        }
    }

}
