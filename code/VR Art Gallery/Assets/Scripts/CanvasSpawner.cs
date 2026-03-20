using Unity.Netcode;
using UnityEngine;

public class CanvasSpawner : NetworkBehaviour
{
    [Header("Prefab")]
    public GameObject canvasPrefab;
    public GameObject paintbrushPrefab;

    [Header("VR Camera / Head Transform (CenterEye)")]
    public Transform playerHead;

    [Header("Spawn Tuning")]
    public float spawnDistance = 1.5f;
    public float heightOffset = -0.15f;
    public bool faceUser = true;

    [Header("Brush Spawn")]
    public Vector3 brushOffset = new Vector3(0.35f, -0.2f, 0f);
    public Vector3 brushRotationEuler = Vector3.zero;

    [Header("Anti-overlap (optional)")]
    public float checkRadius = 0.25f;
    public float pushStep = 0.3f;
    public int maxPushTries = 8;
    public LayerMask overlapMask = ~0;

    [Header("Test image (optional override)")]
    public Texture2D testTexture;

    public void Create()
    {
        if (!canvasPrefab)
        {
            Debug.LogError("[CanvasSpawner] canvasPrefab not assigned.");
            return;
        }
        if (!playerHead)
        {
            Debug.LogError("[CanvasSpawner] playerHead not assigned.");
            return;
        }

        // Calculate position & rotation locally, then send to server
        Vector3 forwardFlat = Vector3.ProjectOnPlane(playerHead.forward, Vector3.up).normalized;
        if (forwardFlat.sqrMagnitude < 0.001f)
            forwardFlat = playerHead.forward;

        Vector3 pos = playerHead.position + forwardFlat * spawnDistance + Vector3.up * heightOffset;

        for (int i = 0; i < maxPushTries; i++)
        {
            if (!Physics.CheckSphere(pos, checkRadius, overlapMask, QueryTriggerInteraction.Ignore))
                break;
            pos += forwardFlat * pushStep;
        }

        Quaternion rot;
        if (faceUser)
        {
            Vector3 toUserFlat = Vector3.ProjectOnPlane(playerHead.position - pos, Vector3.up).normalized;
            if (toUserFlat.sqrMagnitude < 0.001f)
                toUserFlat = -forwardFlat;

            rot = Quaternion.LookRotation(toUserFlat, Vector3.up);
        }
        else
        {
            rot = Quaternion.LookRotation(forwardFlat, Vector3.up);
        }

        // Send spawn request to server with calculated pos/rot
        SpawnCanvasServerRpc(pos, rot);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnCanvasServerRpc(Vector3 pos, Quaternion rot)
    {
        // Spawn canvas on network — visible to ALL clients
        GameObject canvas = Instantiate(canvasPrefab, pos, rot);
        canvas.GetComponent<NetworkObject>().Spawn();

        if (paintbrushPrefab != null)
        {
            CanvasBrushSpawner brushSpawner = canvas.GetComponent<CanvasBrushSpawner>();
            brushSpawner.paintbrushPrefab = paintbrushPrefab;
            brushSpawner.canvasTransform = canvas.transform;
            brushSpawner.brushOffset = brushOffset;
            brushSpawner.brushRotationEuler = brushRotationEuler;
            brushSpawner.SpawnBrush();
        }
    }
}