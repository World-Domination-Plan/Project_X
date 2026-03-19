using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class CanvasBrushSpawner : NetworkBehaviour
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

    // ✅ Added here — fires automatically when canvas is spawned
    void Start()
    {
        Debug.Log($"[BrushSpawner] Start called. IsServer: {NetworkManager.Singleton?.IsServer}");
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            SpawnBrush();
    }

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
        Debug.Log($"[BrushSpawner] SpawnBrushAtOffset called. paintbrushPrefab: {paintbrushPrefab}, canvasTransform: {canvasTransform}");

        if (paintbrushPrefab == null || canvasTransform == null)
        {
            Debug.LogError("[BrushSpawner] STOPPING — paintbrushPrefab or canvasTransform is null!");
            return;
        }

        Vector3 finalOffset = brushOffset + additionalOffset;
        Vector3 brushWorldPos = canvasTransform.TransformPoint(finalOffset);
        Quaternion brushWorldRot = canvasTransform.rotation * Quaternion.Euler(brushRotationEuler);

        GameObject brush = Instantiate(paintbrushPrefab, brushWorldPos, brushWorldRot);

        brush.transform.localScale = paintbrushPrefab.transform.localScale;

        Debug.Log($"[BrushSpawner] Brush instantiated: {brush}");


        BrushRespawnOnGrab respawn = brush.GetComponent<BrushRespawnOnGrab>();
        if (respawn == null)
            respawn = brush.AddComponent<BrushRespawnOnGrab>();

        respawn.spawner = this;
        respawn.despawnDelay = brushDespawnDelay;

        NetworkObject netObj = brush.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
            Debug.Log("[BrushSpawner] Brush spawned on network ✅");
        }
        else
        {
            Debug.LogError("[BrushSpawner] paintbrushPrefab is missing NetworkObject component!");
        }

    }
}