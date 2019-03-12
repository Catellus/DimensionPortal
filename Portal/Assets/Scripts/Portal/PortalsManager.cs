using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;


public class PortalsManager : MonoBehaviour
{

    public List<PortalController> loadedPortals;            // List of portals in every loaded scene

    public List<string> loadedWorlds = new List<string>();  // List of the names of all loaded worlds

    private void OnEnable()     // When this script loads, have "ArrangeSceneOnZ" automatically called when a scene has loaded
    {
        SceneManager.sceneLoaded += ArrangeSceneOnZ;
    }
    private void OnDisable()    // When this script unloads, remove "ArrangeSceneOnZ" from automatically being called when a scene has loaded
    {
        SceneManager.sceneLoaded -= ArrangeSceneOnZ;
    }

    public void Start()
    {
        UpdatePortalsList();
        UpdateLoadedWorlds();
    }

    public void UpdatePortalsList()     // Finds all portals currently loaded, adds its controller to "loadedPortals"
    {
        loadedPortals.Clear();

        foreach (GameObject taggedObject in GameObject.FindGameObjectsWithTag("Portal"))
            if (taggedObject.GetComponent<PortalController>())
                loadedPortals.Add(taggedObject.GetComponent<PortalController>());
    }

    public void UpdateLoadedWorlds()    // Loads all worlds that all currently loaded portals want to access
    {
        List<string> worldLoadRequests = new List<string>();

        worldLoadRequests.Add("PlayerAndPortal");   // Ensures that "PlayerAndPortal" is accounted for and does not get overridden/deleted

        foreach (var p in loadedPortals)
        {
            foreach (string s in p.accessableWorldNames)
            {
                worldLoadRequests.Add(s);       // For each loaded portal, add the names of its requested worlds
            }
        }

        loadedWorlds.Clear();

        foreach (string s in worldLoadRequests)     // Loads each world requested
        {
            if (SceneManager.GetSceneByName(s).name == null)
            {
                SceneManager.LoadSceneAsync(s, LoadSceneMode.Additive);
            }
            loadedWorlds.Add(s);
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)   // Removes loaded worlds not in the load request
        {
            if (!loadedWorlds.Contains(SceneManager.GetSceneAt(i).name))
                SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
        }

        loadedWorlds.Remove("PlayerAndPortal");     // Removed to prevent "PlayerAndPortal" scene from being accessed by portal
        foreach (var p in loadedPortals)            // For each loaded portal: Set the world indices (Z positions) of its requested worlds
        {
            p.accessableWorldIndices.Clear();
            foreach (string s in p.accessableWorldNames)
            {
                p.accessableWorldIndices.Add(loadedWorlds.IndexOf(s));
            }
        }
        loadedWorlds.Insert(0, "PlayerAndPortal");
    }

    void ArrangeSceneOnZ(Scene scene, LoadSceneMode mode)   // Moves the root objects of the scene to its worldIndex
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (scene.name == "PlayerAndPortal" || !loadedWorlds.Contains("PlayerAndPortal"))
                go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, 0);// loadedWorlds.IndexOf(scene.name));
            else
                go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, loadedWorlds.IndexOf(scene.name) - 1);
        }
    }

    public void MoveAllPortalsToIndex(int newIndex)     // When player moves to a new world, move all applicable portals with them
    {
        foreach (var portal in loadedPortals)
        {
            if (portal.accessableWorldIndices.Contains(newIndex))
            {
                portal.transform.position = new Vector3(portal.transform.position.x, portal.transform.position.y, newIndex);
                
            }
        }
    }



}
