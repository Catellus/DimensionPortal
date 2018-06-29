using UnityEngine;

public class CamerasController : MonoBehaviour
{
    #region Variables
    [Header("Player")]
    public Transform player;

    [Space, Header("Cameras")]
    public  Camera playerWorldCam, viewWorldCam; //Cameras for the world the player is in and the world seen through the portal
    private RenderTexture m_viewRenderTarget;    //Render location used for the viewQuad

    //Camera cull colors
    Color blueish = new Color(0.47450980392f, 0.19215686274f, 0.21176470588f, 1.0f); //WorldA clear color
    Color redish  = new Color(0.19215686274f, 0.30196078431f, 0.47450980392f, 1.0f); //WorldB clear color

    #endregion //Variables

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

    public void ChangeDimension(bool _NowInA)
    {
        viewWorldCam.cullingMask &= ~(1 << (_NowInA ? 9 : 10));   //Remove layer player is in     (WorldA : WorldB)
        viewWorldCam.cullingMask |=   1 << (_NowInA ? 10 : 9);    //Add    layer player is not in (WorldB : WorldA)
        viewWorldCam.backgroundColor = _NowInA ? blueish : redish;

        playerWorldCam.cullingMask &= ~(1 << (_NowInA ? 10 : 9)); //Remove layer player is not in (WorldB : WorldA)
        playerWorldCam.cullingMask |=   1 << (_NowInA ? 9 : 10);  //Add    layer player is in     (WorldA : WorldB)
        playerWorldCam.backgroundColor = _NowInA ? redish : blueish;
    }

}
