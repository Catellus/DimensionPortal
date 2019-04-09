using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartInformation : MonoBehaviour
{
    //public GameObject playerPrefab;
    public PortalsManager portalManager;
    public GameObject portalPrefab;

    [System.Serializable]
    public struct portalSpawnInformation
    {
        public List<string> levelNames;
        public Vector3 startLocation; // Z axis acts as world index.
        public Vector3 startRotation;
    }
    public List<portalSpawnInformation> portalSpawns;

    public Vector3 playerStartLocation;

    void Start()
    {
        //PortalsManager manager = Instantiate(playerPrefab, playerStartLocation, Quaternion.Euler(Vector3.zero)).GetComponent<PortalsManager>();

        int portalIndex = 0;
        foreach (portalSpawnInformation spawn in portalSpawns)
        {
            PortalController portal = Instantiate(portalPrefab, spawn.startLocation, Quaternion.Euler(spawn.startRotation)).GetComponent<PortalController>();
            Debug.LogWarning("SPAWNED a portal");

            string name = "";
            foreach (string world in spawn.levelNames)
            {
                if (name == "")
                    name += "Portal(" + portalIndex + ") : " + world;
                else
                    name += ", " + world;

                portal.accessableWorldNames.Add(world);
            }

            portalIndex++;
            portal.manager = portalManager;
            portal.gameObject.name = name;
        }
        portalManager.UpdatePortalsList();
        //portalManager.UpdateLoadedWorlds();
    }
}
