﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalController : MonoBehaviour
{
    public PortalsManager manager;

    public List<string> accessableWorldNames;

    public List<int> accessableWorldIndices = new List<int>();

    [Space(10)]
    public float worldLoadDistance   = 15f;
    public float worldSwitchDistance = 0.85f;
    public float portalHalfHeight    = 1f;

    [Space(10)]
    public Camera viewCam;               //Camera that sees into this portal's next world
    public RenderTexture viewTexture;    //Texture the ViewCam renders to
    public ViewQuadManipulator viewQuad; //Edits the view mesh based on player location


    private void Start()
    {
        viewTexture = new RenderTexture(Screen.width, Screen.height, 0);
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

    public void EntityPassPortal(string _entityTag, ref int _worldIndex, bool _reverseCycle)
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(this.transform.position, worldLoadDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, worldSwitchDistance);
    }


} // END PortalController