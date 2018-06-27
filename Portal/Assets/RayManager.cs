using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RayManager : MonoBehaviour
{
    [Header("RayManager")]
    public LayerMask collisionMask;
    public float raySpacing = 0.2f;
    public const float skinBuffer = 0.015f;

    [HideInInspector]public new BoxCollider2D collider;
    [HideInInspector]public float sideRaySpacing;
    [HideInInspector]public float capRaySpacing;
    [HideInInspector]public int sideRayCount;
    [HideInInspector]public int capRayCount;

    public originPoints rayOrigins;
    public struct originPoints{
        public Vector2 topLeft   , topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    protected int layerMaskA = 512;
    protected int layerMaskB = 1024;

    public virtual void Awake()
    {
        collider = GetComponent<BoxCollider2D>();
        portal = GameObject.FindGameObjectWithTag("Portal").GetComponent<tmp_Portal>();
    }

    public virtual void Start()
    {
        DetermineRaySpacing();
    }

    public void DetermineRaySpacing()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(-2 * skinBuffer);

        float bWidth  = bounds.size.x;
        float bHeight = bounds.size.y;

        capRayCount  = Mathf.Clamp(Mathf.CeilToInt(bWidth  / raySpacing), 2, int.MaxValue);
        sideRayCount = Mathf.Clamp(Mathf.CeilToInt(bHeight / raySpacing), 2, int.MaxValue);

        capRaySpacing  = bWidth  / (capRayCount  - 1);
        sideRaySpacing = bHeight / (sideRayCount - 1);

        UpdateRayOrigins();
    }

    public void UpdateRayOrigins()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(-2 * skinBuffer);

        rayOrigins.topLeft     = new Vector2(bounds.min.x, bounds.max.y);
        rayOrigins.topRight    = new Vector2(bounds.max.x, bounds.max.y);
        rayOrigins.bottomLeft  = new Vector2(bounds.min.x, bounds.min.y);
        rayOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
    }

    public tmp_Portal portal;

    public Vector3 GetPortalPassDistance(Vector3 _location){ return GetPortalPassDistance(_location, Vector3.zero); }
    public Vector3 GetPortalPassDistance(Vector3 _location, Vector3 _portalOffset)
    {
        Vector3 playerOffset = _location - (portal.transform.position + _portalOffset);
        Vector3 pointOnPlane = Vector3.ProjectOnPlane(playerOffset, portal.transform.up);
        return pointOnPlane;
    }

}
