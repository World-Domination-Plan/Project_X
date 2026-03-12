using UnityEngine;

public class CanvasSpawner : MonoBehaviour
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
    public float brushDespawnDelay = 20f;

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
            Debug.LogError("[CanvasSpawner] playerHead not assigned (XR Camera / CenterEye).");
            return;
        }

        Vector3 forwardFlat = Vector3.ProjectOnPlane(playerHead.forward, Vector3.up).normalized;
        if (forwardFlat.sqrMagnitude < 0.001f) forwardFlat = playerHead.forward;

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
            if (toUserFlat.sqrMagnitude < 0.001f) toUserFlat = -forwardFlat;
            rot = Quaternion.LookRotation(toUserFlat, Vector3.up);
        }
        else
        {
            rot = Quaternion.LookRotation(forwardFlat, Vector3.up);
        }

        GameObject canvas = Instantiate(canvasPrefab, pos, rot);

        if (paintbrushPrefab != null)
        {
            CanvasBrushSpawner brushSpawner = canvas.AddComponent<CanvasBrushSpawner>();
            brushSpawner.paintbrushPrefab = paintbrushPrefab;
            brushSpawner.canvasTransform = canvas.transform;
            brushSpawner.brushOffset = brushOffset;
            brushSpawner.brushRotationEuler = brushRotationEuler;
            brushSpawner.brushDespawnDelay = brushDespawnDelay;
            brushSpawner.SpawnBrush();
        }

        // var display = canvas.GetComponentInChildren<PaintingDisplay>(true);
        // if (display && testTexture) display.SetTexture(testTexture);
    }
}
