using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldManager : MonoBehaviour
{
    #region Variables

    [Header("General"), SerializeField]
    private Transform playerLocation;
    public  float     worldLoadDistance   = 2.5f; //Maximum distance the player must be to load the opposite scene
    public  float     worldSwitchDistance = 0.85f;//Maximum distance the player must be to switch worlds
    public  float     portalHalfHeight    = 1.0f; //Height from the portal's center each end is at

    [Space, Header("Levels")]
    public string worldA; //Level Name -- Player's starting world
    public string worldB; //Level Name -- Starting view world

    private string loadedWorld; //Stores the name of the player's current world
    private string otherWorld;  //Stores the name of the current view world

    private float playerDistance = 0;     //Used for loading/unloading OppositeWorld
    private bool  sceneLoaded    = false;

    private GameObject cams; //Player's camera system -- Stored to easily move it into the opposite world

    #endregion //Variables

    public void Awake()
    {
        //Objects are sorted by spawn order -- If this object already exists, this duplicate is destroyed.
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(this.gameObject.tag)) //Find all objects with this object's tag
        {
            if (go != this.gameObject)    //If another object has the same tag, destroy this object
                Destroy(this.gameObject);
        }

        if (playerLocation == null)
            playerLocation = GameObject.FindGameObjectWithTag("Player").transform; //Store reference for player's location
        if (cams == null)
            cams = GameObject.FindGameObjectWithTag("Cameras");                    //Store reference for player's cameraSystem
    }

    private void Start()
    {
        loadedWorld = worldA; //Set starting world as world player is in
        otherWorld  = worldB; //Set View world as world player is not in
    }

    private void FixedUpdate()
    {
        playerDistance = (playerLocation.position - this.transform.position).magnitude;
        HandleLoadedScenes();
    }

    private void HandleLoadedScenes()
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

        SceneManager.MoveGameObjectToScene(playerLocation.gameObject, SceneManager.GetSceneByName(loadedWorld)); //Moves the Player    to the new world
        SceneManager.MoveGameObjectToScene(this.gameObject          , SceneManager.GetSceneByName(loadedWorld)); //Moves the Portal    to the new world
        SceneManager.MoveGameObjectToScene(cams                     , SceneManager.GetSceneByName(loadedWorld)); //Moves CameraSystems to the new world
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(this.transform.position, worldLoadDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, worldSwitchDistance);
    }
}
