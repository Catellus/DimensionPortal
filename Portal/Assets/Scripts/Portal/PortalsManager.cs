using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;


public class PortalsManager : MonoBehaviour
{

    public List<PortalController> loadedPortals;

    public List<string> loadedWorlds = new List<string>();

    private void OnEnable()
    {
        SceneManager.sceneLoaded += ArrangeScene;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= ArrangeScene;
    }

    public void Start()
    {
        UpdatePortalsList();
        UpdateLoadedWorlds();
    }

    public void UpdatePortalsList()
    {
        loadedPortals.Clear();

        foreach (GameObject taggedObject in GameObject.FindGameObjectsWithTag("Portal"))
            if (taggedObject.GetComponent<PortalController>())
                loadedPortals.Add(taggedObject.GetComponent<PortalController>());
    }

    public void UpdateLoadedWorlds()
    {
        List<string> worldLoadRequests = new List<string>();

        worldLoadRequests.Add("PlayerAndPortal");

        foreach (var p in loadedPortals)
        {
            foreach (string s in p.accessableWorldNames)
            {
                worldLoadRequests.Add(s);
            }
        }

        loadedWorlds.Clear();

        foreach (string s in worldLoadRequests)
        {
            if (SceneManager.GetSceneByName(s).name == null)
            {
                SceneManager.LoadSceneAsync(s, LoadSceneMode.Additive);
            }
            loadedWorlds.Add(s);
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (!loadedWorlds.Contains(SceneManager.GetSceneAt(i).name))
                SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
        }

        loadedWorlds.Remove("PlayerAndPortal");
        foreach (var p in loadedPortals)
        {
            p.accessableWorldIndices.Clear();
            foreach (string s in p.accessableWorldNames)
            {
                p.accessableWorldIndices.Add(loadedWorlds.IndexOf(s));
            }
        }
        loadedWorlds.Insert(0, "PlayerAndPortal");
    }

    void ArrangeScene(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (scene.name == "PlayerAndPortal" || !loadedWorlds.Contains("PlayerAndPortal"))
                go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, 0);// loadedWorlds.IndexOf(scene.name));
            else
                go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, loadedWorlds.IndexOf(scene.name) - 1);
        }
    }

    public void MoveAllPortalsToIndex(int newIndex)
    {
        foreach (var portal in loadedPortals)
        {
            if (portal.accessableWorldIndices.Contains(newIndex))
                portal.transform.position = new Vector3(portal.transform.position.x, portal.transform.position.y, newIndex);
        }
    }



}
