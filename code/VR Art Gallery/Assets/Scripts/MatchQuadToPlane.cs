using UnityEngine;

public class MatchQuadToPlane : MonoBehaviour
{
    public MeshRenderer sourcePlaneRenderer;

    void Start()
    {
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var size = sourcePlaneRenderer.bounds.size; // (10, ~0, 10)

        transform.localScale = new Vector3(size.x, size.z, 1f);

        transform.position = sourcePlaneRenderer.bounds.center;
    }
}

