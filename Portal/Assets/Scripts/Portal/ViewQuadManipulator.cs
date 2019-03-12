using UnityEngine;
using ToolBox;


public class ViewQuadManipulator : MonoBehaviour
{
    public PortalController portal;     // Portal this quad is "looking through"
           MeshFilter       vFilter;    // 

    Camera        vCam;
    Mesh          vMesh;
    RenderTexture vTexture;

    Transform  viewAnchor;
    int viewIndex;
    public bool reverseCycle;

    int[] cwIndices = {
    0, 1, 2, //FOV A
    0, 2, 3, //FOV B
    2, 5, 3, //FILL A
    2, 4, 5  //FILL B
    };

    //Has inverted normals from cwIndices so the quad is visible at all times
    int[] ccwIndices = {
    0, 2, 1, //FOV A
    0, 3, 2, //FOV B
    2, 3, 5, //FILL A
    2, 5, 4  //FILL B
    };

    Vector3[] viewVerts = new Vector3[6];

    Vector2 cRT, cRB, cLT, cLB;                //Local positions of the screen's corners
    Vector2 portalTopLocal, portalBottomLocal; //Local positions of the top/bottom of the portal
    Vector2 topSlope, bottomSlope;             //Slope from the camera to portal top/bottom

    float pxWidth;      // Window width  in pixels
    float pxHeight;     // Window height in pixels

    public void Initialize(Material _mat)
    {
        vCam = portal.viewCam;
        vTexture = new RenderTexture(vCam.pixelWidth, vCam.pixelHeight, 0);
        vCam.targetTexture = vTexture;

        vMesh = new Mesh();
        vFilter = this.gameObject.AddComponent<MeshFilter>();
        this.gameObject.AddComponent<MeshRenderer>().sharedMaterial = _mat;

        Shader.SetGlobalTexture("_MainTex", vTexture);

        viewAnchor = this.transform;

        pxWidth = vCam.pixelWidth;
        pxHeight = vCam.pixelHeight;
    }

    public void UpdateView(Vector3 _camPosition, int _worldIndex, bool _reverseCycle)
    {
        reverseCycle = _reverseCycle;
        viewIndex = portal.GetNextIndex(_worldIndex, !_reverseCycle);
        this.transform.position = MoveToZ(_camPosition, viewIndex - 1);

        Vector3 wRT = vCam.ScreenToWorldPoint(new Vector2(pxWidth, pxHeight )); // Window Right - Top
        Vector3 wRB = vCam.ScreenToWorldPoint(new Vector2(pxWidth, 0        )); // Window Right - Bottom
        Vector3 wLT = vCam.ScreenToWorldPoint(new Vector2(0      , pxHeight )); // Window Left  - Top
        Vector3 wLB = vCam.ScreenToWorldPoint(new Vector2(0      , 0        )); // Window Left  - Bottom

        cRT = viewAnchor.InverseTransformPoint(wRT);    // World location of corner to relative location
        cRB = viewAnchor.InverseTransformPoint(wRB);    // World location of corner to relative location
        cLT = viewAnchor.InverseTransformPoint(wLT);    // World location of corner to relative location
        cLB = viewAnchor.InverseTransformPoint(wLB);    // World location of corner to relative location

        Vector2 portalTopWorld    = portal.transform.TransformPoint(new Vector2(0,  portal.portalHalfHeight)); // Relative position to world
        Vector2 portalBottomWorld = portal.transform.TransformPoint(new Vector2(0, -portal.portalHalfHeight)); // Relative position to world

        portalTopLocal    = viewAnchor.InverseTransformPoint(portalTopWorld   );
        portalBottomLocal = viewAnchor.InverseTransformPoint(portalBottomWorld);

        topSlope    = portalTopLocal.normalized;
        bottomSlope = portalBottomLocal.normalized;

        //bool useCCW = System.Math.Round(portal.transform.InverseTransformPoint(viewAnchor.position).x, 4) < 0;
        //bool useCCW = MathTools.RoundVector3(portal.transform.InverseTransformPoint(viewAnchor.position), 4).x < 0;
        bool useCCW = !_reverseCycle;

        MakeQuad(useCCW);

        vMesh.vertices = viewVerts;
        vMesh.triangles = useCCW ? ccwIndices : cwIndices;

        vMesh.uv = new Vector2[vMesh.vertices.Length];
        vMesh.RecalculateNormals();
        vMesh.RecalculateBounds();

        vFilter.sharedMesh = vMesh;
    }

    void MakeQuad(bool _useCCW)
    {
        int topHitSide = 0;
        int bottomHitSide = 0;

        viewVerts[0] = portalTopLocal;
        viewVerts[1] = portalBottomLocal;
        viewVerts[2] = FindScreenIntersections(Vector2.zero, bottomSlope, ref bottomHitSide);
        viewVerts[3] = FindScreenIntersections(Vector2.zero, topSlope   , ref topHitSide   );

        if (viewVerts[3].magnitude < portalTopLocal.magnitude)
            viewVerts[3] = portalTopLocal;
        if (viewVerts[2].magnitude < portalBottomLocal.magnitude)
            viewVerts[2] = portalBottomLocal;

        Vector2 v4 = viewVerts[2];
        Vector2 v5 = viewVerts[3];

        GetPositionBasedOnHitSides(topHitSide, bottomHitSide, _useCCW, ref v4, ref v5);

        viewVerts[4] = v4;
        viewVerts[5] = v5;

        float localPositionOfVisibleWorld = (int)viewAnchor.InverseTransformPoint(portal.transform.position).z - 0.45f;
        for(int i = 0; i < 6; i++)
        { viewVerts[i] = MoveToZ(viewVerts[i], localPositionOfVisibleWorld); }
    }

    Vector2 FindScreenIntersections(Vector2 _origin, Vector2 _slope, ref int _sideHit)
    {
        Vector2 result = Vector2.negativeInfinity;

        TestIntersectionOfLineSegments(ref result, ref _sideHit, _origin, _slope.normalized * 100, cRT, cLT); //Top
        TestIntersectionOfLineSegments(ref result, ref _sideHit, _origin, _slope.normalized * 100, cLT, cLB); //Left
        TestIntersectionOfLineSegments(ref result, ref _sideHit, _origin, _slope.normalized * 100, cLB, cRB); //Bottom
        TestIntersectionOfLineSegments(ref result, ref _sideHit, _origin, _slope.normalized * 100, cRB, cRT); //Right

        return result;
    }

    void TestIntersectionOfLineSegments(ref Vector2 _result, ref int _usedSides, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3) //Finds intersection of line segments
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

                if      (p2 == cRT) //If the line intersects with screen Top
                    _usedSides |= 1 << 0;        //Sets _usedSides to 1
                else if (p2 == cLT) //If the line intersects with screen Left
                    _usedSides |= 1 << 1;        //Sets _usedSides to 2
                else if (p2 == cLB) //If the line intersects with screen Bottom
                    _usedSides |= 1 << 2;        //Sets _usedSides to 4
                else if (p2 == cRB) //If the line intersects with screen RIght
                    _usedSides |= 1 << 3;        //Sets _usedSides to 8

            }
        }
    }

    void GetPositionBasedOnHitSides(int _topHit, int _bottomHit, bool _inverse, ref Vector2 v4, ref Vector2 v5)
    {
        int bothHit = _topHit + _bottomHit;

        switch (bothHit)
        {
                                                // 1  == (Impossible) Only one intersection
                                                // 2  == Both Top (Already set to top/bottom intersection position)
            case 3:                             // 3  == One Top One Left
                v4 = v5 = cLT;
                break;
                                                // 4  == Both Left (Already set to top/bottom intersection position)
            case 5:                             // 5  == One Top One Bottom
                if (_inverse)
                { //If player is left of the portal
                    v5 = (_topHit    == 1) ? cRT : cRB; //Is top    hitting Screen Top?
                    v4 = (_bottomHit == 4) ? cRB : cRT; //Is bottom hitting Screen Bottom?
                }
                else
                { //If player is right of the portal
                    v5 = (_topHit    == 1) ? cLT : cLB; //Is top    hitting Screen Top?
                    v4 = (_bottomHit == 4) ? cLB : cLT; //Is bottom hitting Screen Bottom?
                }
                break;
            case 6:                             // 6  == One Left One Bottom
                v4 = v5 = cLB;
                break;
                                                // 7  == (Impossible) Three intersections
                                                // 8  == Both Bottom (Already set to top/bottom intersection position)
            case 9:                             // 9  == One Top One Right
                v4 = v5 = cRT;
                break;
            case 10:                            // 10 == One Left One Right
                if (_inverse)
                { //If player is below the portal
                    v5 = (_topHit    == 2) ? cLT : cRT; //Is top    hitting Screen Left?
                    v4 = (_bottomHit == 8) ? cRT : cLT; //Is bottom hitting Screen Right?
                }
                else
                { //If player is above the portal
                    v5 = (_topHit    == 2) ? cLB : cRB; //Is top    hitting Screen Left?
                    v4 = (_bottomHit == 8) ? cRB : cLB; //Is bottom hitting Screen Right?
                }
                break;
                                                // 11 == (Impossible) Three intersections
            case 12:                            // 12 == One Right One Bottom
                v4 = v5 = cRB;
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

    Vector3 MoveToZ(Vector2 _in, float _z = 1)
    {
        return new Vector3(_in.x, _in.y, _z);
    }
}