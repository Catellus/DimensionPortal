using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalController : MonoBehaviour
{
    public PortalsManager manager;


    public List<string> accessableWorldNames;                     // List of the names         of worlds this portal can get to
    public List<int>    accessableWorldIndices = new List<int>(); // List of the world indices of worlds this portal can get to

    [Space(10)]
    public float worldLoadDistance   = 15f  ; // Distance at which this portal loads/unloads its scene(s)
    public float worldSwitchDistance = 0.85f; // Distance at which an entity can pass through into another scene
    public float portalHalfHeight    = 1f   ;

    [Space(10)]
    public List<Camera>         viewCams;              // All cameras that this portal can see?
    public Camera               viewCam;               // Camera that sees into this portal's next world
    public CameraSettingsEditor viewCamSettingsEditor; // Controls this viewCam's settings
    public RenderTexture        viewTexture;           // Texture the ViewCam renders to
    public ViewQuadManipulator  viewQuad;              // Edits the view mesh based on player location


    private void Start()
    {
        viewTexture = new RenderTexture(Screen.width, Screen.height, 0);    // Creates the texture rendered to by the viewCam
    }

    public string GetWorldNameFromNextIndex(int _curIndex, bool _reverseCycle)
    {
        int nextIndex = GetNextIndex(_curIndex, _reverseCycle);
        return GetWorldNameFromIndex(nextIndex);
    }

    public string GetWorldNameFromIndex(int _index)
    {
        _index = accessableWorldIndices.IndexOf(_index);
        return accessableWorldNames[_index];
    }

    public int GetNextIndex(int _curIndex, bool _reverseCycle) //Make able to do ping pong cycle?
    {
        if (accessableWorldIndices.Contains(_curIndex))
        {
            int inValueIndex = accessableWorldIndices.IndexOf(_curIndex);

            if (!_reverseCycle)
            {
                if (inValueIndex == accessableWorldIndices.ToArray().Length - 1)
                    return accessableWorldIndices[0];
                else
                    return accessableWorldIndices[inValueIndex + 1];
            }
            else
            {
                if (inValueIndex == 0)
                    return accessableWorldIndices[accessableWorldIndices.ToArray().Length - 1];
                else
                    return accessableWorldIndices[inValueIndex - 1];
            }
        }
        else
        {
            Debug.LogWarning("The WorldIndex value " + _curIndex + " is inaccessible to " + this.gameObject.name);
            return _curIndex;
        }
    }

    public void EntityPassedThroughPortal(string _entityTag, ref int _worldIndex, bool _reverseCycle)
    {
        _worldIndex = GetNextIndex((int)_worldIndex, _reverseCycle);
    
        if (_entityTag == "Player")
        {
            manager.MoveAllPortalsToIndex(_worldIndex);
        }
        else
        {
            int prev = GetNextIndex((int)_worldIndex, !_reverseCycle);
            print(_entityTag + " has passed through " + this.gameObject.name + " from " + prev + " to " + _worldIndex);
        }
    }

    public bool DEBUG_ShowGizmos = true;
    private void OnDrawGizmos()
    {
        if (DEBUG_ShowGizmos)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(this.transform.position, worldLoadDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(this.transform.position, worldSwitchDistance);
        }
    }


} // END PortalController