using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class CanvasBrushSpawner : MonoBehaviour
{
    public GameObject paintbrushPrefab;
    public Transform canvasTransform;
    public Vector3 brushOffset;
    public Vector3 brushRotationEuler;
    public float brushDespawnDelay = 20f;

    [Header("Respawn Tuning")]
    public float respawnDelay = 2f;
    public Vector3 extraRespawnOffset = new Vector3(0.15f, 0f, 0f);

    bool isRespawnPending = false;

    public void SpawnBrush()
    {
        SpawnBrushAtOffset(Vector3.zero);
    }

    public void SpawnReplacementBrush()
    {
        if (!isRespawnPending)
            StartCoroutine(SpawnReplacementAfterDelay());
    }

    IEnumerator SpawnReplacementAfterDelay()
    {
        isRespawnPending = true;
        yield return new WaitForSeconds(respawnDelay);
        SpawnBrushAtOffset(extraRespawnOffset);
        isRespawnPending = false;
    }

    void SpawnBrushAtOffset(Vector3 additionalOffset)
    {
        if (paintbrushPrefab == null || canvasTransform == null)
            return;

        Vector3 finalOffset = brushOffset + additionalOffset;
        Vector3 brushWorldPos = canvasTransform.TransformPoint(finalOffset);
        Quaternion brushWorldRot = canvasTransform.rotation * Quaternion.Euler(brushRotationEuler);

        GameObject brush = Instantiate(paintbrushPrefab, brushWorldPos, brushWorldRot);

        // Spawn on network so all clients see the brush
        NetworkObject netObj = brush.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError("[CanvasBrushSpawner] paintbrushPrefab is missing a NetworkObject component!");
        }

        BrushRespawnOnGrab respawn = brush.GetComponent<BrushRespawnOnGrab>();
        if (respawn == null)
            respawn = brush.AddComponent<BrushRespawnOnGrab>();

        respawn.spawner = this;
        respawn.despawnDelay = brushDespawnDelay;
    }
}
    