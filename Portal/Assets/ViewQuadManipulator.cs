using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: Make a buffer of System.Math.Round(playerLocalPosition.x, 4) on either side of the portal before switching view
//      (Makes the switch smoother by not having a point where it may flash.)

[RequireComponent(typeof(MeshRenderer))]
public class ViewQuadManipulator : MonoBehaviour
{
    public Material texture;

    Transform player;
    tmp_Portal portal;

    Mesh quad;
    MeshFilter filter;

    public Camera cam;

    Vector3 topLocalPosition, bottomLocalPosition;
    Vector3 topFOVSlope, bottomFOVSlope;

    Vector3 playerLocalPosition;
    Vector3 localTR, localBL, localTL, localBR;


    Vector3[] quadVerts = new Vector3[6];

    int[] cwIndices = {
    0, 1, 2, //FOV A
    0, 2, 3, //FOV B
    2, 5, 3, //FILL A
    2, 4, 5  //FILL B
    };

    int[] ccwIndices = {
    0, 2, 1, //FOV A
    0, 3, 2, //FOV B
    2, 3, 5, //FILL A
    2, 5, 4  //FILL B
    };

    private void Awake()
    {
        if (portal == null)
            portal = GameObject.FindGameObjectWithTag("Portal").GetComponent<tmp_Portal>();
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        quad = new Mesh();
        filter = this.gameObject.AddComponent<MeshFilter>();
        this.GetComponent<MeshRenderer>().sharedMaterial = texture;
    }

    private void LateUpdate()
    {
        Vector3 scBL = cam.ScreenToWorldPoint(new Vector3(0, 0, 0));
        Vector3 scTR = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, 0));
        Vector3 scBR = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, 0, 0));
        Vector3 scTL = cam.ScreenToWorldPoint(new Vector3(0, cam.pixelHeight, 0));

        localTR = this.transform.parent.InverseTransformPoint(scTR);
        localBL = this.transform.parent.InverseTransformPoint(scBL);
        localBR = this.transform.parent.InverseTransformPoint(scBR);
        localTL = this.transform.parent.InverseTransformPoint(scTL);

        playerLocalPosition = this.transform.parent.InverseTransformPoint(player.position);

        MakeQuad();

        quad.vertices = quadVerts;
        quad.triangles = System.Math.Round(playerLocalPosition.x, 4) < 0 ? ccwIndices : cwIndices;
        quad.uv = new Vector2[quad.vertices.Length];

        quad.RecalculateNormals(); //Probably unnecessary?
        quad.RecalculateBounds();  //Probably unnecessary?

        filter.sharedMesh = quad;
    }

    void MakeQuad()
    {
        topLocalPosition = new Vector3(0, portal.halfHeight, 0);
        bottomLocalPosition = new Vector3(0, -portal.halfHeight, 0);
        topFOVSlope = (topLocalPosition - playerLocalPosition).normalized;
        bottomFOVSlope = (bottomLocalPosition - playerLocalPosition).normalized;

        int topHitSide = 0;
        int bottomHitSide = 0;

//TODO: Add optimization -- Don't render the quad at all if it is off screen (intersections.magintude < (top-or-bottom) - playerlocation)

        quadVerts[0] = topLocalPosition;
        quadVerts[1] = bottomLocalPosition;
        quadVerts[2] = FindEdgeIntersections(playerLocalPosition, bottomFOVSlope, ref bottomHitSide);
        quadVerts[3] = FindEdgeIntersections(playerLocalPosition, topFOVSlope   , ref topHitSide);

          //If Top is off screen, set point to Top
        if ((quadVerts[3] - playerLocalPosition).magnitude < (topLocalPosition - playerLocalPosition).magnitude)
            quadVerts[3] = topLocalPosition;
          //If Bottom is off screen, set point to Bottom
        if ((quadVerts[2] - playerLocalPosition).magnitude < (bottomLocalPosition - playerLocalPosition).magnitude)
            quadVerts[2] = bottomLocalPosition;

        int bothHit = topHitSide + bottomHitSide;

        Vector3 v4 = quadVerts[2];
        Vector3 v5 = quadVerts[3];

        Vector3 playerPortalOffset = Vector3.ProjectOnPlane(player.position - this.transform.parent.position, this.transform.parent.up);
        switch (bothHit)
        {
                                                // 1  == (Impossible) Only one intersection
                                                // 2  == Both Top (Set to top/bottom intersection position)
            case 3:                             // 3  == One Top One Left
                v4 = v5 = (Vector2)localTL;
                break;
                                                // 4  == Both Left (Set to top/bottom intersection position)
            case 5:                             // 5  == One Top One Bottom
                if ( System.Math.Round(playerPortalOffset.x , 4) < 0)
                {
                    v5 = (topHitSide == 1)    ? (Vector2)localTR : (Vector2)localBR;
                    v4 = (bottomHitSide == 4) ? (Vector2)localBR : (Vector2)localTR;
                }
                else
                {
                    v5 = (topHitSide == 1)    ? (Vector2)localTL : (Vector2)localBL;
                    v4 = (bottomHitSide == 4) ? (Vector2)localBL : (Vector2)localTL;
                }
                break;
            case 6:                             // 6  == One Left One Bottom
                v4 = v5 = (Vector2)localBL;
                break;
                                                // 7  == (Impossible) Three intersections
                                                // 8  == Both Bottom (Set to top/bottom intersection position)
            case 9:                             // 9  == One Top One Right
                v4 = v5 = (Vector2)localTR;
                break;
            case 10:                            // 10 == One Left One Right
                if (System.Math.Round(playerPortalOffset.y, 4) < 0)
                {
                    v5 = (topHitSide == 2)    ? (Vector2)localTL : (Vector2)localTR;
                    v4 = (bottomHitSide == 8) ? (Vector2)localTR : (Vector2)localTL;
                }
                else
                {
                    v5 = (topHitSide == 2)    ? (Vector2)localBL : (Vector2)localBR;
                    v4 = (bottomHitSide == 8) ? (Vector2)localBR : (Vector2)localBL;
                }
                break;
                                                // 11 == (Impossible) Three intersections
            case 12:                            // 12 == One Right One Bottom
                v4 = v5 = (Vector2)localBR;
                break;
                                                // 13 == (Impossible) Three intersections
                                                // 14 == (Impossible) Three intersections
                                                // 15 == (Impossible) Four  intersections
                                                // 16 == Both right (Set to top/bottom intersection position)
            default:
                if (bothHit != 16 && bothHit != 8 && bothHit != 4 && bothHit != 2) //if not both hitting one side
                    Debug.LogError("BothHit Value not handled! -- " + bothHit);
                break;
        }
        quadVerts[4] = v4;
        quadVerts[5] = v5;
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

    void TestIntersectionOfLineSegments(ref Vector3 _result, ref int _usedSides, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3) //Between line segments
    {
        if (float.IsNegativeInfinity(_result.x))
        {
            Vector2 s1, s2;
            s1 = p1 - p0;
            s2 = p3 - p2;

            float s, t;
            s = (-s1.y * (p0.x - p2.x) + s1.x * (p0.y - p2.y)) / (-s2.x * s1.y + s1.x * s2.y);
            t = (s2.x * (p0.y - p2.y) - s2.y * (p0.x - p2.x)) / (-s2.x * s1.y + s1.x * s2.y);

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                _result = p0 + (t * s1);

                if (p2 == (Vector2)localTR)
                    _usedSides |= 1 << 0;
                else if (p2 == (Vector2)localTL)
                    _usedSides |= 1 << 1;
                else if (p2 == (Vector2)localBL)
                    _usedSides |= 1 << 2;
                else if (p2 == (Vector2)localBR)
                    _usedSides |= 1 << 3;

            }
        }
    }

    /*
     * 
     * Draws screen box used for portal view edge detection
     * 
        if (slope == (Vector2)topSlope)
        {
            Debug.DrawLine(this.transform.TransformPoint(localTL), this.transform.TransformPoint(localTR), Color.white);
            Debug.DrawLine(this.transform.TransformPoint(localTL), this.transform.TransformPoint(localBL), Color.green);
            Debug.DrawLine(this.transform.TransformPoint(localBL), this.transform.TransformPoint(localBR), Color.blue);
            Debug.DrawLine(this.transform.TransformPoint(localTR), this.transform.TransformPoint(localBR), Color.red);
        }

    */

    //Vector2 GetIntersection(Vector2 fovStart, Vector2 fovSlope, Vector2 edgeStart, Vector2 edgeSlope) //Between vectors
    //{
    //    edgeStart = playerLocalPosition;
    //    edgeSlope = topSlope;

    //    var n = Vector3.Cross(fovSlope, edgeSlope);
    //    var u = Vector3.Cross(n, fovStart - edgeStart) / Vector3.Dot(n, n);

    //    var AA = fovStart - fovSlope * Vector3.Dot(edgeSlope, u);
    //    //var BB = edgeStart - edgeSlope * Vector3.Dot(fovSlope, u);

    //    return AA;
    //}

}
