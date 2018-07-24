using UnityEngine;

//TODO: Make a buffer of System.Math.Round(playerLocalPosition.x, 4) on either side of the portal before switching view
//      (Makes the switch smoother by not having a point where it may flash.)

[RequireComponent(typeof(MeshRenderer))]
public class ViewQuadManipulator : MonoBehaviour
{
    #region Variables
    public Material texture;
    public Camera   viewWorldCam;

    private Mesh         quad;
    private MeshFilter   filter;
    private WorldManager portal;
    private Transform    player;

    [Space, Header("Depth Quad")]
    public GameObject depthQuad;
    private MeshFilter depthFilter;
    public Material depthMaterial;

    private Vector3 topLocalPosition, bottomLocalPosition; //Local position of poral's ends
    private Vector3 topFOVSlope, bottomFOVSlope;           //Slope from ends of portal to the player

    private Vector3 playerLocalPosition;
    private Vector3 localTR, localBL, localTL, localBR; //Local positions of Screen corners


    private Vector3[] quadVerts = new Vector3[6];

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

    #endregion //Variables

    private void Awake()
    {
        if (portal == null)
            portal = GameObject.FindGameObjectWithTag("Portal").GetComponent<WorldManager>();
        if (viewWorldCam == null)
            viewWorldCam = GameObject.FindGameObjectWithTag("Cameras").GetComponent<CamerasController>().viewWorldCam;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform; //Gets player's world position

        quad = new Mesh();                                          //Creates new mesh
        filter = this.gameObject.AddComponent<MeshFilter>();        //Creates MeshFilter -- stores mesh.
        this.GetComponent<MeshRenderer>().sharedMaterial = texture; //Sets render texture

        depthFilter = depthQuad.AddComponent<MeshFilter>();
        depthQuad.GetComponent<MeshRenderer>().sharedMaterial = depthMaterial;
    }

    private void LateUpdate()
    {
        Vector3 scTR = viewWorldCam.ScreenToWorldPoint(new Vector3(viewWorldCam.pixelWidth, viewWorldCam.pixelHeight, 0)); //World position of Screen Top    Right Corner
        Vector3 scBL = viewWorldCam.ScreenToWorldPoint(new Vector3(0, 0, 0));                                              //World position of Screen Bottom Left  Corner
        Vector3 scBR = viewWorldCam.ScreenToWorldPoint(new Vector3(viewWorldCam.pixelWidth, 0, 0));                        //World position of Screen Bottom Right Corner
        Vector3 scTL = viewWorldCam.ScreenToWorldPoint(new Vector3(0, viewWorldCam.pixelHeight, 0));                       //World position of Screen Top    Left  Corner

        localTR = this.transform.parent.InverseTransformPoint(scTR); //Local position of Screen Top    Right Corner
        localBL = this.transform.parent.InverseTransformPoint(scBL); //Local position of Screen Bottom Left  Corner
        localBR = this.transform.parent.InverseTransformPoint(scBR); //Local position of Screen Bottom Right Corner
        localTL = this.transform.parent.InverseTransformPoint(scTL); //Local position of Screen Top    Left  Corner

        playerLocalPosition = this.transform.parent.InverseTransformPoint(player.position);

        if (playerLocalPosition.magnitude <= portal.worldLoadDistance) //Only update the view if the player is within range
        {
            MakeQuad();

            quad.vertices = quadVerts;                                                                 //Sets Quad vert positions
            quad.triangles = System.Math.Round(playerLocalPosition.x, 4) < 0 ? ccwIndices : cwIndices; //
            quad.uv = new Vector2[quad.vertices.Length];

            quad.RecalculateNormals(); //unnecessary?
            quad.RecalculateBounds();  //unnecessary?

            filter.sharedMesh = quad;
            depthFilter.sharedMesh = quad;
        }
    }

    void MakeQuad()
    {
        topLocalPosition    = new Vector3(0,  portal.portalHalfHeight, 0);       //Locates the portal's top    position
        bottomLocalPosition = new Vector3(0, -portal.portalHalfHeight, 0);       //Locates the portal's bottom position
        topFOVSlope    = (topLocalPosition    - playerLocalPosition).normalized; //Finds the slope from the player's position to the portal's top    position
        bottomFOVSlope = (bottomLocalPosition - playerLocalPosition).normalized; //Finds the slope from the player's position to the portal's bottom position

        int topHitSide = 0;    //Side of the screen the top    line collides with -- (top = 1)(Left = 2)(bottom = 4)(right = 8)
        int bottomHitSide = 0; //Side of the screen the bottom line collides with -- (top = 1)(Left = 2)(bottom = 4)(right = 8)

        quadVerts[0] = topLocalPosition;
        quadVerts[1] = bottomLocalPosition;
        quadVerts[2] = FindEdgeIntersections(playerLocalPosition, bottomFOVSlope, ref bottomHitSide); //Find screen intersection and use point as vert position
        quadVerts[3] = FindEdgeIntersections(playerLocalPosition, topFOVSlope   , ref topHitSide);    //Find screen intersection and use point as vert position

          //If Top is off screen, set point to Top
        if ((quadVerts[3] - playerLocalPosition).magnitude < (topLocalPosition - playerLocalPosition).magnitude)
            quadVerts[3] = topLocalPosition;
          //If Bottom is off screen, set point to Bottom
        if ((quadVerts[2] - playerLocalPosition).magnitude < (bottomLocalPosition - playerLocalPosition).magnitude)
            quadVerts[2] = bottomLocalPosition;

        int bothHit = topHitSide + bottomHitSide;

        Vector3 v4 = quadVerts[2]; //Sets vert4 to screen bottom intersection
        Vector3 v5 = quadVerts[3]; //Sets vert5 to screen top    intersection

        Vector3 playerPortalOffset = Vector3.ProjectOnPlane(player.position - this.transform.parent.position, this.transform.parent.up);
        switch (bothHit)
        {
                                                // 1  == (Impossible) Only one intersection
                                                // 2  == Both Top (Already set to top/bottom intersection position)
            case 3:                             // 3  == One Top One Left
                v4 = v5 = (Vector2)localTL;
                break;
                                                // 4  == Both Left (Already set to top/bottom intersection position)
            case 5:                             // 5  == One Top One Bottom
                if ( System.Math.Round(playerPortalOffset.x , 4) < 0)
                { //If player is left of the portal
                    v5 = (topHitSide    == 1) ? (Vector2)localTR : (Vector2)localBR; //Is top    hitting Screen Top?
                    v4 = (bottomHitSide == 4) ? (Vector2)localBR : (Vector2)localTR; //Is bottom hitting Screen Bottom?
                }
                else
                { //If player is right of the portal
                    v5 = (topHitSide    == 1) ? (Vector2)localTL : (Vector2)localBL; //Is top    hitting Screen Top?
                    v4 = (bottomHitSide == 4) ? (Vector2)localBL : (Vector2)localTL; //Is bottom hitting Screen Bottom?
                }
                break;
            case 6:                             // 6  == One Left One Bottom
                v4 = v5 = (Vector2)localBL;
                break;
                                                // 7  == (Impossible) Three intersections
                                                // 8  == Both Bottom (Already set to top/bottom intersection position)
            case 9:                             // 9  == One Top One Right
                v4 = v5 = (Vector2)localTR;
                break;
            case 10:                            // 10 == One Left One Right
                if (System.Math.Round(playerPortalOffset.y, 4) < 0)
                { //If player is below the portal
                    v5 = (topHitSide    == 2) ? (Vector2)localTL : (Vector2)localTR; //Is top    hitting Screen Left?
                    v4 = (bottomHitSide == 8) ? (Vector2)localTR : (Vector2)localTL; //Is bottom hitting Screen Right?
                }
                else
                { //If player is above the portal
                    v5 = (topHitSide    == 2) ? (Vector2)localBL : (Vector2)localBR; //Is top    hitting Screen Left?
                    v4 = (bottomHitSide == 8) ? (Vector2)localBR : (Vector2)localBL; //Is bottom hitting Screen Right?
                }
                break;
                                                // 11 == (Impossible) Three intersections
            case 12:                            // 12 == One Right One Bottom
                v4 = v5 = (Vector2)localBR;
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
        quadVerts[4] = v4; //Sets new position of vert4
        quadVerts[5] = v5; //Sets new position of vert5
    }

    Vector3 FindEdgeIntersections(Vector2 origin, Vector2 slope, ref int usedSides)
    {
        Vector3 result = Vector3.negativeInfinity;

        TestIntersectionOfLineSegments(ref result, ref usedSides, origin, slope.normalized * 1000, localTR, localTL); //Top
        TestIntersectionOfLineSegments(ref result, ref usedSides, origin, slope.normalized * 1000, localTL, localBL); //Left
        TestIntersectionOfLineSegments(ref result, ref usedSides, origin, slope.normalized * 1000, localBL, localBR); //Bottom
        TestIntersectionOfLineSegments(ref result, ref usedSides, origin, slope.normalized * 1000, localBR, localTR); //Right

        return result;
    }

    void TestIntersectionOfLineSegments(ref Vector3 _result, ref int _usedSides, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3) //Finds intersection of line segments
    {
        if (float.IsNegativeInfinity(_result.x)) //Ensures the _result is only set once -- each _ref passes thorugh four times (onece for each side of the screen)
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

                if      (p2 == (Vector2)localTR) //If the line intersects with screen Top
                    _usedSides |= 1 << 0;        //Sets _usedSides to 1
                else if (p2 == (Vector2)localTL) //If the line intersects with screen Left
                    _usedSides |= 1 << 1;        //Sets _usedSides to 2
                else if (p2 == (Vector2)localBL) //If the line intersects with screen Bottom
                    _usedSides |= 1 << 2;        //Sets _usedSides to 4
                else if (p2 == (Vector2)localBR) //If the line intersects with screen RIght
                    _usedSides |= 1 << 3;        //Sets _usedSides to 8

            }
        }
    }
}
