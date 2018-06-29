using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalManager : MonoBehaviour
{
    #region Variables

    [Header("General"), SerializeField]
    private Transform playerLocation;
    public  float     worldLoadDistance   = 2.5f; //Maximum distance the player must be to load the opposite scene
    public  float     worldSwitchDistance = 0.85f;//Maximum distance the player must be to switch worlds
    public  float     portalHalfHeight    = 1.0f; //Height from the portal's center each end is at

    private float playerDistance = 0;
    private bool  sceneLoaded    = false;

    private Vector3 topLocation;
    private Vector3 bottomLocation;

    #endregion //Variables

    private void FixedUpdate()
    {
        playerDistance = (playerLocation.position - this.transform.position).magnitude;

        if (playerDistance <= worldLoadDistance)
            LoadOppositWorld();
    }

    private void LoadOppositWorld() //TODO: Make this better.
    {
        if (SceneManager.sceneCount == 1)
            sceneLoaded = false;
        else if (SceneManager.sceneCount == 2)
            sceneLoaded = true;
        else
            Debug.LogError("SceneCount of " + SceneManager.sceneCount + " unexpected");


        if (playerDistance < worldLoadDistance && !sceneLoaded)
        {
            SceneManager.LoadSceneAsync("TEST_B", LoadSceneMode.Additive);
        }
        else if (playerDistance > worldLoadDistance && sceneLoaded)
        {
            SceneManager.UnloadSceneAsync("TEST_B");
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(this.transform.position, worldLoadDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, worldSwitchDistance);
    }
}
