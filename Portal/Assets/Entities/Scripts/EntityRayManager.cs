using UnityEngine;

public class EntityRayManager : MonoBehaviour
{
    #region Variables
    [Header("RayManager")]  public    LayerMask     collisionMask;
                            public    PortalManager portal;

    [SerializeField, Space] protected float raySpacing   = 0.2f;   //Maximum distance between rays
    [SerializeField]        protected float skinBuffer   = 0.015f; //Distance the rays are inset into the entity

    [SerializeField, Space] protected float entityHeight = 1.0f; //Entity height in meters -- independant of entity's scale
    [SerializeField]        protected float entityWidth  = 1.0f; //Entity width  in meters -- independant of entity's scale
    //[SerializeField]        protected float collisionRotationOffset = 0.0f; //Can be used to compensate for entity's rotation

    protected int          capRayCount;    //Number of rays on the entity's top/bottom sides
    protected float        capRaySpacing;  //Distance between rays on the entity's top/bottom sides
    protected int          sideRayCount;   //Number of rays on the entity's left/right sides
    protected float        sideRaySpacing; //Distance between rays on the entity's left/right sides
    protected originPoints rayOrigins;     //World locations for each corner of the entity

    protected Quaternion collisionRotation;

    public struct originPoints
    {
        public Vector2 topLeft   , topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    #endregion //Variables

    public virtual void Awake()
    {
        if (portal == null) //Sets the portal if one is not already set.
            portal = GameObject.FindGameObjectWithTag("Portal").GetComponent<PortalManager>();
    }

    public virtual void Start()
    {
        DetermineRaySpacing();
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

        rayOrigins.topLeft     = center + (Vector2)(this.transform.rotation * new Vector2(-extents.x,  extents.y)); //Turns relative location into world position
        rayOrigins.topRight    = center + (Vector2)(this.transform.rotation * new Vector2( extents.x,  extents.y)); //Turns relative location into world position
        rayOrigins.bottomRight = center + (Vector2)(this.transform.rotation * new Vector2( extents.x, -extents.y)); //Turns relative location into world position
        rayOrigins.bottomLeft  = center + (Vector2)(this.transform.rotation * new Vector2(-extents.x, -extents.y)); //Turns relative location into world position
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(Vector3.zero), Vector3.one); //Resets gizmo rotation to world
        Gizmos.color = Color.red;                        //Draw red     Top    Left  sphere
        Gizmos.DrawSphere(rayOrigins.topLeft, 0.05f);    //Draw red     Top    Left  sphere
        Gizmos.color = Color.blue;                       //Draw blue    Top    Right sphere
        Gizmos.DrawSphere(rayOrigins.topRight, 0.05f);   //Draw blue    Top    Right sphere
        Gizmos.color = Color.green;                      //Draw green   Bottom Left  sphere
        Gizmos.DrawSphere(rayOrigins.bottomLeft, 0.05f); //Draw green   Bottom Left  sphere
        Gizmos.color = Color.magenta;                    //Draw magenta Bottom Right sphere
        Gizmos.DrawSphere(rayOrigins.bottomRight, 0.05f);//Draw magenta Bottom Right sphere

        Gizmos.color = new Color(0.25f, 1.0f, 0.25f, 1.0f);
        Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, Vector3.one); //Sets gizmo rotation to local
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(entityWidth, entityHeight, 1.0f));
    }

}
