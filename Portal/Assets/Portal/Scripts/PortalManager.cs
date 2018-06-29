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

    [Space, Header("Levels")]
    public string worldA;
    public string worldB;

    private string loadedWorld;
    private string otherWorld;

    private float playerDistance = 0;
    private bool  sceneLoaded    = false;

    private Vector3 topLocation;
    private Vector3 bottomLocation;

    private GameObject cams;

    #endregion //Variables

    public void Awake()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(this.gameObject.tag))
        {
            if (go != this.gameObject)
                Destroy(this.gameObject);
        }

        if (playerLocation == null)
            playerLocation = GameObject.FindGameObjectWithTag("Player").transform;
        if (cams == null)
            cams = GameObject.FindGameObjectWithTag("Cameras");
    }

    private void Start()
    {
        loadedWorld = worldA;
        otherWorld = worldB;
    }

    private void FixedUpdate()
    {
        playerDistance = (playerLocation.position - this.transform.position).magnitude;

        HandleLoadedScenes();
    }

    private void HandleLoadedScenes() //TODO: Make this better.
    {
        if (SceneManager.sceneCount == 1)
            sceneLoaded = false;
        else if (SceneManager.sceneCount == 2)
            sceneLoaded = true;
        else
            Debug.LogError("SceneCount of " + SceneManager.sceneCount + " unexpected");

        if (playerDistance < worldLoadDistance && !sceneLoaded)
        {
            SceneManager.LoadSceneAsync(otherWorld, LoadSceneMode.Additive);
        }
        else if (playerDistance > worldLoadDistance && sceneLoaded)
        {
            SceneManager.UnloadSceneAsync(otherWorld);
        }
    }

    public void SwitchWorlds(bool _inA)
    {
        loadedWorld = _inA ? worldA : worldB;
        otherWorld  = _inA ? worldB : worldA;

        SceneManager.MoveGameObjectToScene(playerLocation.gameObject, SceneManager.GetSceneByName(loadedWorld));
        SceneManager.MoveGameObjectToScene(this.gameObject          , SceneManager.GetSceneByName(loadedWorld));
        SceneManager.MoveGameObjectToScene(cams                     , SceneManager.GetSceneByName(loadedWorld));


    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(this.transform.position, worldLoadDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, worldSwitchDistance);
    }
}
