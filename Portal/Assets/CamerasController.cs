using UnityEngine;

public class CamerasController : MonoBehaviour
{
    public Transform player;

    [Space]
    public Camera CamA, CamB, CamC;

    RenderTexture rt;

    private void Start()
    {
        rt = new RenderTexture(Screen.width, Screen.height, 24);
        CamB.targetTexture = rt;
        Shader.SetGlobalTexture("_MainTex", rt);
    }

    private void LateUpdate()
    {
        this.transform.position = player.position + new Vector3(0, 0, -10);
        //CamA.backgroundColor = player.GetComponent<EntityMotor>().inA ? Color.blue : Color.black;
    }

    Color redish = new Color(0.19215686274f, 0.30196078431f, 0.47450980392f, 1.0f);
    Color blueish = new Color(0.47450980392f, 0.19215686274f, 0.21176470588f, 1.0f);


    public void ChangeDimension(bool _NowInA)
    {
        CamB.cullingMask &= ~(1 << (_NowInA ? 9 : 10)); //If in worldA? remove world A : remove world B  --Does not affect any other collision layers set
        CamB.cullingMask |=   1 << (_NowInA ? 10 : 9);    //If in worldA?    add world B :    add world A  --Does not affect any other collision layers set
        CamB.backgroundColor = _NowInA ? blueish : redish;


        CamA.cullingMask &= ~(1 << (_NowInA ? 10 : 9)); //If in worldA? remove world A : remove world B  --Does not affect any other collision layers set
        CamA.cullingMask |=   1 << (_NowInA ? 9 : 10);    //If in worldA?    add world B :    add world A  --Does not affect any other collision layers set
        CamA.backgroundColor = _NowInA ? redish : blueish;
    }

}
