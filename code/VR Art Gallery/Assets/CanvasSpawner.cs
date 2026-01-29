using UnityEngine;

public class CanvasSpawner : MonoBehaviour
{
    [Header("Prefab to spawn")]
    public GameObject canvasPrefab;

    [Header("Spawn transform")]
    public Transform spawnPoint;

    //[Header("Test image (optional override)")]
    //public Texture2D testTexture;

    public void Create()
    {
        if (!canvasPrefab)
        {
            Debug.LogError("[CanvasSpawner] canvasPrefab not assigned.");
            return;
        }

        var pos = spawnPoint ? spawnPoint.position : Vector3.zero;
        var rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        var go = Instantiate(canvasPrefab, pos, rot);

        // Find the Painting Display script inside the prefab instance
        var display = go.GetComponentInChildren<Canvas>(true);
        if (!display)
        {
            Debug.LogError("[CanvasSpawner] No PaintingDisplay script found in prefab children.");
            return;
        }

        // If you want to override whatever is on the prefab:
        //if (testTexture) display.SetTexture(testTexture);
    }
}
