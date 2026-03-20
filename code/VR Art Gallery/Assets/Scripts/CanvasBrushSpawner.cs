using UnityEngine;

public class CanvasBrushSpawner : MonoBehaviour
{
    public GameObject paintbrushPrefab;
    public Transform canvasTransform;
    public Vector3 brushOffset;
    public Vector3 brushRotationEuler;

    private GameObject currentBrush;

    public void SpawnBrush()
    {
        if (currentBrush != null)
            return;

        SpawnBrushAtOffset(Vector3.zero);
    }

    public void RespawnBrushIfMissing()
    {
        if (currentBrush == null)
            SpawnBrushAtOffset(Vector3.zero);
    }

    void SpawnBrushAtOffset(Vector3 additionalOffset)
    {
        if (paintbrushPrefab == null || canvasTransform == null)
            return;

        Vector3 finalOffset = brushOffset + additionalOffset;
        Vector3 brushWorldPos = canvasTransform.TransformPoint(finalOffset);
        Quaternion brushWorldRot = canvasTransform.rotation * Quaternion.Euler(brushRotationEuler);

        currentBrush = Instantiate(paintbrushPrefab, brushWorldPos, brushWorldRot);

        BrushRespawnOnGrab grabState = currentBrush.GetComponent<BrushRespawnOnGrab>();
        if (grabState == null)
            currentBrush.AddComponent<BrushRespawnOnGrab>();
    }

    public GameObject GetCurrentBrush()
    {
        return currentBrush;
    }

    public void ClearBrushReference(GameObject brush)
    {
        if (currentBrush == brush)
            currentBrush = null;
    }
}