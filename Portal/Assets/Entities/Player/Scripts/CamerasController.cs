using UnityEngine;
using UnityEngine.Rendering;

public class CamerasController : MonoBehaviour
{
    #region Variables
    [Header("Player")]
    public Transform player;

    [Space, Header("Cameras")]
    public  Camera playerWorldCam, viewWorldCam; //Cameras for the world the player is in and the world seen through the portal
    private RenderTexture m_viewRenderTarget;    //Render location used for the viewQuad

    //Camera cull colors
    Color blueish = new Color(0.19215686274f, 0.30196078431f, 0.47450980392f, 1.0f); //WorldA clear color
    Color redish  = new Color(0.47450980392f, 0.19215686274f, 0.21176470588f, 1.0f); //WorldB clear color

    private CommandBuffer m_depthBuffer;
    public Renderer quad;

    bool inA = true;

    #endregion //Variables

    public void Awake()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(this.gameObject.tag))
        {
            if (go != this.gameObject)
                Destroy(this.gameObject);
        }

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        m_depthBuffer = new CommandBuffer();
        m_depthBuffer.ClearRenderTarget(true, true, redish, 0);
        m_depthBuffer.name = "View quad depth buffer";
        m_depthBuffer.DrawRenderer(quad, new Material(Shader.Find("Unlit/sh_Depth")));

        viewWorldCam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, m_depthBuffer);
    }

    private void Start()
    {
        //Create and set render target for the view quad
        m_viewRenderTarget = new RenderTexture(Screen.width, Screen.height, 24); //Creates a renderTarget based on screen size
        viewWorldCam.targetTexture = m_viewRenderTarget;                         //Sets render target
        Shader.SetGlobalTexture("_MainTex", m_viewRenderTarget);                 //Sets texture on viewQuad shader as renderTarget.
    }

    private void LateUpdate()
    {
        //Set camera X & Y to player's
        this.transform.position = player.position + new Vector3(0, 0, -10);
    }

    private void UpdateDepthCullColor()
    {
        m_depthBuffer.ClearRenderTarget(false, true, inA ? redish : blueish, 0);
    }

    public void ChangeDimension(bool _NowInA)
    {
        inA = _NowInA;
        viewWorldCam.cullingMask &= ~(1 << (inA ? 9 : 10));   //Remove layer player is in     (WorldA : WorldB)
        viewWorldCam.cullingMask |=   1 << (inA ? 10 : 9);    //Add    layer player is not in (WorldB : WorldA)
        viewWorldCam.backgroundColor = inA ? redish : blueish;

        playerWorldCam.cullingMask &= ~(1 << (inA ? 10 : 9)); //Remove layer player is not in (WorldB : WorldA)
        playerWorldCam.cullingMask |=   1 << (inA ? 9 : 10);  //Add    layer player is in     (WorldA : WorldB)
        playerWorldCam.backgroundColor = inA ? blueish : redish;

        UpdateDepthCullColor();
    }

}
