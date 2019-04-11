using UnityEngine;
using UnityEngine.Rendering;
using ToolBox;


public class ViewQuadManipulator : MonoBehaviour
{
#region Variables
    public  PortalController ptlController; // Portal this quad is "looking through"
    private MeshFilter       vFilter;

    private Camera        vCam;
    private Mesh          vMesh;
    private Renderer      vRenderer;
    private RenderTexture vTexture;

    public  float     viewOffset = 0.01f; // Currently used to put the view quad behind the portal and player, but above the environment
    private int       viewIndex;
    private Vector2   entityOffset;
    private Transform viewAnchor;

    //Has inverted normals from cwIndices so the quad is visible at all times
    private int[] ccwIndices = {
    0, 2, 1, //FOV A
    0, 3, 2, //FOV B
    2, 3, 5, //FILL A
    2, 5, 4  //FILL B
    };
    private int[] cwIndices  = {
    0, 1, 2, //FOV A
    0, 2, 3, //FOV B
    2, 5, 3, //FILL A
    2, 4, 5  //FILL B
    };

    private Vector3[] viewVerts = new Vector3[6];

    private Vector2 cornerRT, cornerRB, cornerLT, cornerLB; //Local positions of the screen's corners
    private Vector2 portalTopLocal, portalBottomLocal;      //Local positions of the top/bottom of the portal
    private Vector2 topSlope      , bottomSlope;            //Slope from the camera to portal top/bottom

    private int vCamPixelHeight, vCamPixelWidth;

#endregion Variables


    public void Initialize()
    {
        vCam               = ptlController.viewCam;
        vTexture           = new RenderTexture(Screen.width, Screen.height, 24);
        vCam.targetTexture = vTexture;

        vMesh      = new Mesh();
        vFilter    = this.gameObject.AddComponent<MeshFilter>();
        vRenderer  = this.gameObject.AddComponent<MeshRenderer>();
        viewAnchor = this.transform;

        vRenderer.materials = new Material[] { new Material (Shader.Find("Unlit/ViewQuad_shader")) };

        vCamPixelWidth = vCam.pixelWidth;
        vCamPixelHeight = vCam.pixelHeight;

        SetUpViewTexture();
    }

    private void SetUpViewTexture()
    {
        CommandBuffer depthHack = new CommandBuffer();
        depthHack.name          = "Depth hack";

        depthHack.ClearRenderTarget(true, false, Color.black, 0);
        depthHack.DrawRenderer(vRenderer, new Material(Shader.Find("Unlit/DepthQuad_shader")));
        vCam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, depthHack);

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        this.gameObject.GetComponent<MeshRenderer>().GetPropertyBlock(propBlock);
        propBlock.SetTexture("_QuadViewTexture", vTexture);
        this.gameObject.GetComponent<MeshRenderer>().SetPropertyBlock(propBlock);
    }

    public void UpdateView(Vector3 _camPosition, Vector3 _entityPosition, int _worldIndex)
    {
        viewIndex = ptlController.GetNextIndex(_worldIndex, GetEntitySide(_entityPosition));
        this.transform.position = MoveToZ(_camPosition, viewIndex - 1);

        Vector3 worldRT = vCam.ScreenToWorldPoint(new Vector2(vCamPixelWidth, vCamPixelHeight)); // Window Right - Top
        Vector3 worldRB = vCam.ScreenToWorldPoint(new Vector2(vCamPixelWidth, 0              )); // Window Right - Bottom
        Vector3 worldLT = vCam.ScreenToWorldPoint(new Vector2(0             , vCamPixelHeight)); // Window Left  - Top
        Vector3 worldLB = vCam.ScreenToWorldPoint(new Vector2(0              , 0             )); // Window Left  - Bottom

        cornerRT = viewAnchor.InverseTransformPoint(worldRT);    // World location of corner to relative location
        cornerRB = viewAnchor.InverseTransformPoint(worldRB);    // World location of corner to relative location
        cornerLT = viewAnchor.InverseTransformPoint(worldLT);    // World location of corner to relative location
        cornerLB = viewAnchor.InverseTransformPoint(worldLB);    // World location of corner to relative location

        Vector2 portalTopWorld    = ptlController.transform.TransformPoint(new Vector2(0,  ptlController.portalHalfHeight)); // Relative position to world
        Vector2 portalBottomWorld = ptlController.transform.TransformPoint(new Vector2(0, -ptlController.portalHalfHeight)); // Relative position to world

        portalTopLocal    = viewAnchor.InverseTransformPoint(portalTopWorld   );
        portalBottomLocal = viewAnchor.InverseTransformPoint(portalBottomWorld);

        topSlope    = (portalTopWorld    - (Vector2)_entityPosition).normalized;
        bottomSlope = (portalBottomWorld - (Vector2)_entityPosition).normalized;

        MakeQuad(viewAnchor.InverseTransformPoint(_entityPosition));
        vMesh.vertices  = viewVerts;
        vMesh.triangles = (GetEntitySide(_entityPosition)) ? ccwIndices : cwIndices;

        vMesh.uv = new Vector2[vMesh.vertices.Length];
        vMesh.RecalculateNormals();
        vMesh.RecalculateBounds();

        vFilter.sharedMesh = vMesh;
    }

    private bool GetEntitySide(Vector3 _entityPosition)
    {
        Vector3 entityPlanarOffset = Vector3.ProjectOnPlane(_entityPosition - ptlController.transform.position, ptlController.transform.up);
        Vector2 entityRoundedOffset = MathTools.RoundVector3(entityPlanarOffset, 4);

        float a = entityRoundedOffset.x / ptlController.transform.right.x;            //Distance along portal.right vector pointOnPlane sits
        a = (a == 0) ? (entityRoundedOffset.y / ptlController.transform.right.y) : a;

        return (float)System.Math.Round(a, 4) < 0; //Rounds the value to 4 decimal places
    }

    private void MakeQuad(Vector2 _playerPosition)
    {
        int topHitSide = 0;
        int bottomHitSide = 0;

        viewVerts[0] = portalTopLocal;
        viewVerts[1] = portalBottomLocal;
        viewVerts[2] = FindScreenIntersections(_playerPosition, bottomSlope, ref bottomHitSide);
        viewVerts[3] = FindScreenIntersections(_playerPosition, topSlope   , ref topHitSide   );

        if (viewVerts[3].magnitude < portalTopLocal.magnitude)
            viewVerts[3] = portalTopLocal;
        if (viewVerts[2].magnitude < portalBottomLocal.magnitude)
            viewVerts[2] = portalBottomLocal;

        Vector2 v4 = viewVerts[2];
        Vector2 v5 = viewVerts[3];

        GetPositionBasedOnHitSides(topHitSide, bottomHitSide, viewAnchor.transform.TransformPoint(_playerPosition), ref v4, ref v5);

        viewVerts[4] = v4;
        viewVerts[5] = v5;

        float localPositionOfVisibleWorld = (int)viewAnchor.InverseTransformPoint(ptlController.transform.position).z - viewOffset;
        for(int i = 0; i < 6; i++)
        { viewVerts[i] = MoveToZ(viewVerts[i], localPositionOfVisibleWorld); }
    }

    private Vector2 FindScreenIntersections(Vector2 _origin, Vector2 _slope, ref int _sideHit)
    {
        Vector2 result = Vector2.negativeInfinity;

        TestIntersectionOfLineSegments(ref result, ref _sideHit, _origin, _slope.normalized * 100, cornerRT, cornerLT); //Top
        TestIntersectionOfLineSegments(ref result, ref _sideHit, _origin, _slope.normalized * 100, cornerLT, cornerLB); //Left
        TestIntersectionOfLineSegments(ref result, ref _sideHit, _origin, _slope.normalized * 100, cornerLB, cornerRB); //Bottom
        TestIntersectionOfLineSegments(ref result, ref _sideHit, _origin, _slope.normalized * 100, cornerRB, cornerRT); //Right

        return result;
    }

    private void TestIntersectionOfLineSegments(ref Vector2 _result, ref int _usedSides, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3) //Finds intersection of line segments
    {
        if (float.IsNegativeInfinity(_result.x)) //Ensures the _result is only set once -- each _ref passes through four times (once for each side of the screen)
        {
            Vector2 s1, s2;
            s1 = p1 - p0; //Slope of point0 to point1
            s2 = p3 - p2; //Slope of point2 to point3

            float s, t;
            s = (-s1.y * (p0.x - p2.x) + s1.x * (p0.y - p2.y)) / (-s2.x * s1.y + s1.x * s2.y); //Distance along S2 the intersection sits
            t = ( s2.x * (p0.y - p2.y) - s2.y * (p0.x - p2.x)) / (-s2.x * s1.y + s1.x * s2.y); //Distance along S1 the intersection sits

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1) //If intersection sits on line segment p0p1 and p2p3
            {
                _result = p0 + (t * s1); //Sets intersection point

                if      (p2 == cornerRT) //If the line intersects with screen Top
                    _usedSides |= 1 << 0;        //Sets _usedSides to 1
                else if (p2 == cornerLT) //If the line intersects with screen Left
                    _usedSides |= 1 << 1;        //Sets _usedSides to 2
                else if (p2 == cornerLB) //If the line intersects with screen Bottom
                    _usedSides |= 1 << 2;        //Sets _usedSides to 4
                else if (p2 == cornerRB) //If the line intersects with screen RIght
                    _usedSides |= 1 << 3;        //Sets _usedSides to 8

            }
        }
    }

    private void GetPositionBasedOnHitSides(int _topHit, int _bottomHit, Vector3 _entityPosition, ref Vector2 v4, ref Vector2 v5)
    {
        Vector3 entityPlanarOffset = Vector3.ProjectOnPlane(_entityPosition - ptlController.transform.position, ptlController.transform.up);
        Vector2 entityRoundedOffset = MathTools.RoundVector3(entityPlanarOffset, 4);

        int bothHit = _topHit + _bottomHit;
        switch (bothHit)
        {
                                                // 1  == (Impossible) Only one intersection
                                                // 2  == Both Top (Already set to top/bottom intersection position)
            case 3:                             // 3  == One Top One Left
                v4 = v5 = cornerLT;
                break;
                                                // 4  == Both Left (Already set to top/bottom intersection position)
            case 5:                             // 5  == One Top One Bottom
                if (entityRoundedOffset.x < 0)
                { //If player is left of the portal
                    v5 = (_topHit    == 1) ? cornerRT : cornerRB; //Is top    hitting Screen Top?
                    v4 = (_bottomHit == 4) ? cornerRB : cornerRT; //Is bottom hitting Screen Bottom?
                }
                else
                { //If player is right of the portal
                    v5 = (_topHit    == 1) ? cornerLT : cornerLB; //Is top    hitting Screen Top?
                    v4 = (_bottomHit == 4) ? cornerLB : cornerLT; //Is bottom hitting Screen Bottom?
                }
                break;
            case 6:                             // 6  == One Left One Bottom
                v4 = v5 = cornerLB;
                break;
                                                // 7  == (Impossible) Three intersections
                                                // 8  == Both Bottom (Already set to top/bottom intersection position)
            case 9:                             // 9  == One Top One Right
                v4 = v5 = cornerRT;
                break;
            case 10:                            // 10 == One Left One Right
                if (entityRoundedOffset.y < 0)
                { //If player is below the portal
                    v5 = (_topHit    == 2) ? cornerLT : cornerRT; //Is top    hitting Screen Left?
                    v4 = (_bottomHit == 8) ? cornerRT : cornerLT; //Is bottom hitting Screen Right?
                }
                else
                { //If player is above the portal
                    v5 = (_topHit    == 2) ? cornerLB : cornerRB; //Is top    hitting Screen Left?
                    v4 = (_bottomHit == 8) ? cornerRB : cornerLB; //Is bottom hitting Screen Right?
                }
                break;
                                                // 11 == (Impossible) Three intersections
            case 12:                            // 12 == One Right One Bottom
                v4 = v5 = cornerRB;
                break;
                                                // 13 == (Impossible) Three intersections
                                                // 14 == (Impossible) Three intersections
                                                // 15 == (Impossible) Four  intersections
                                                // 16 == Both right (Already set to top/bottom intersection position)
            default:
                if (bothHit != 16 && bothHit != 8 && bothHit != 4 && bothHit != 2) //If not both hitting same side
                    Debug.LogError("BothHit Value not handled! -- " + bothHit);
                break;
        }
    }

    private Vector3 MoveToZ(Vector2 _in, float _z = 1)
    {
        return new Vector3(_in.x, _in.y, _z);
    }
}