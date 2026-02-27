using UnityEngine;

public class CanvasSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject canvasPrefab;

    [Header("VR Camera / Head Transform (CenterEye)")]
    public Transform playerHead; // assign XR Camera (CenterEye)

    [Header("Spawn Tuning")]
    public float spawnDistance = 1.5f;   // meters in front of user
    public float heightOffset = -0.15f;  // slightly below eye level
    public bool faceUser = true;

    [Header("Anti-overlap (optional)")]
    public float checkRadius = 0.25f;    // roughly canvas half-width
    public float pushStep = 0.3f;
    public int maxPushTries = 8;
    public LayerMask overlapMask = ~0;   // set to only your canvases if you want

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

        // Use yaw-only forward so it spawns horizontally in front of the user
        Vector3 forwardFlat = Vector3.ProjectOnPlane(playerHead.forward, Vector3.up).normalized;
        if (forwardFlat.sqrMagnitude < 0.001f) forwardFlat = playerHead.forward;

        Vector3 pos = playerHead.position + forwardFlat * spawnDistance + Vector3.up * heightOffset;

        // Push forward if overlapping something
        for (int i = 0; i < maxPushTries; i++)
        {
            if (!Physics.CheckSphere(pos, checkRadius, overlapMask, QueryTriggerInteraction.Ignore))
                break;

            pos += forwardFlat * pushStep;
        }

        Quaternion rot;
        if (faceUser)
        {
            // Make the canvas face the user (again yaw-only)
            Vector3 toUserFlat = Vector3.ProjectOnPlane(playerHead.position - pos, Vector3.up).normalized;
            if (toUserFlat.sqrMagnitude < 0.001f) toUserFlat = -forwardFlat;
            rot = Quaternion.LookRotation(toUserFlat, Vector3.up);
        }
        else
        {
            rot = Quaternion.LookRotation(forwardFlat, Vector3.up);
        }

        var go = Instantiate(canvasPrefab, pos, rot);
        FindObjectOfType<GalleryArtworkLoader>()?.LoadAfterSpawn();


        // Optional: apply test texture to the Painting Display script inside the prefab
        //var display = go.GetComponentInChildren<PaintingDisplay>(true);
        //if (display && testTexture) display.SetTexture(testTexture);
    }


}
