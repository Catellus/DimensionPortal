using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class tmp_Portal : MonoBehaviour
{
    [Header("General")]
    public Transform playerLocation;
    public float loadDistance = 2.5f;
    public float switchDistance = 0.85f;
    [Range(0, 1)]public float loadAlpha = 0.5f;
    float plDist = 0;

    [Header("Angles")]
    public float halfHeight = 1;
    public Vector3 topLocation;
    public Vector3 bottomLocation;

    bool sceneLoaded = false;

    private void FixedUpdate()
    {
        plDist = (this.transform.position - playerLocation.position).magnitude;
        LoadOppositeScene();

        
    }

    private void Update()
    {
        FindViewAngles();
    }

    void FindViewAngles()
    {
        topLocation = (this.transform.position + (this.transform.rotation * new Vector3(0, halfHeight, 0)));
        bottomLocation = (this.transform.position - (this.transform.rotation * new Vector3(0, halfHeight, 0)));

        topLocation -= playerLocation.position;
        bottomLocation -= playerLocation.position;

        Debug.DrawLine(playerLocation.position, playerLocation.position + (topLocation * 10), Color.blue);
        Debug.DrawLine(playerLocation.position, playerLocation.position + (bottomLocation * 10), Color.red);
    }

    private void LoadOppositeScene()
    {
        if (SceneManager.sceneCount == 1)
            sceneLoaded = false;
        else if (SceneManager.sceneCount == 2)
            sceneLoaded = true;
        else
            Debug.LogError("Too many or too few scenes! -- SceneCount:" + SceneManager.sceneCount);


        if (plDist < loadDistance && !sceneLoaded)
        {
            SceneManager.LoadSceneAsync("TEST_B", LoadSceneMode.Additive);
        }
        else if (plDist > loadDistance && sceneLoaded)
        {
            SceneManager.UnloadSceneAsync("TEST_B");
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = new Vector4(1, 1, 1, loadAlpha);
        Gizmos.DrawSphere(this.transform.position, loadDistance);

        Gizmos.color = new Vector4(1, 0, 0, loadAlpha);
        Gizmos.DrawSphere(this.transform.position, switchDistance);

    }
}
