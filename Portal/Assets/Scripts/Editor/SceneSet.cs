using UnityEngine;

[CreateAssetMenu(fileName = "New Scene Set", menuName = "SceneSet")]
public class SceneSet : ScriptableObject
{

    public string WorldA;
    public string WorldB;

    public Vector2 playerLocation;
    public Vector2 portalLocation;
    public float   portalRotation;

}
