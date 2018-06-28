using UnityEngine;

public class RayManager : MonoBehaviour
{
    [Header("RayManager")]
    public LayerMask collisionMask;
    public float raySpacing = 0.2f;
    public float skinBuffer = 0.015f;

    public float entityHeight = 1.0f;
    public float entityWidth = 1.0f;

    protected float sideRaySpacing;
    protected float capRaySpacing;
    protected int   sideRayCount;
    protected int   capRayCount;

    public originPoints rayOrigins;
    public struct originPoints{
        public Vector2 topLeft   , topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    public tmp_Portal portal;

    public virtual void Awake()
    {
        if (portal == null)
            portal = GameObject.FindGameObjectWithTag("Portal").GetComponent<tmp_Portal>();
    }

    public virtual void Start()
    {
        DetermineRaySpacing();
    }

    public void DetermineRaySpacing()
    {
        float bWidth  = entityWidth  + (-2 * skinBuffer);
        float bHeight = entityHeight + (-2 * skinBuffer);

        capRayCount  = Mathf.Clamp(Mathf.CeilToInt(bWidth  / raySpacing), 2, int.MaxValue);
        sideRayCount = Mathf.Clamp(Mathf.CeilToInt(bHeight / raySpacing), 2, int.MaxValue);

        capRaySpacing  = bWidth  / (capRayCount  - 1);
        sideRaySpacing = bHeight / (sideRayCount - 1);

        UpdateRayOrigins();
    }

    public void UpdateRayOrigins()
    {
        float tmpSkin = -2 * skinBuffer;
        Vector2 e = (new Vector2((entityWidth + tmpSkin) / 2, (entityHeight + tmpSkin)/2));
        Vector2 c = this.transform.position;

        rayOrigins.topRight    = c + (Vector2)(this.transform.rotation *  new Vector2( e.x,  e.y));
        rayOrigins.topLeft     = c + (Vector2)(this.transform.rotation *  new Vector2(-e.x,  e.y));
        rayOrigins.bottomLeft  = c + (Vector2)(this.transform.rotation *  new Vector2(-e.x, -e.y));
        rayOrigins.bottomRight = c + (Vector2)(this.transform.rotation *  new Vector2( e.x, -e.y));
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(Vector3.zero), Vector3.one);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(rayOrigins.topLeft, 0.05f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(rayOrigins.topRight, 0.05f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(rayOrigins.bottomLeft, 0.05f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(rayOrigins.bottomRight, 0.05f);

        Gizmos.color = new Color(0.25f, 1.0f, 0.25f, 1.0f);
        Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(entityWidth, entityHeight, 1.0f));
    }

}
