using UnityEngine;
using UnityEngine.InputSystem;
using XRMultiplayer;

public class HUDSpawner : MonoBehaviour
{
    public InputActionReference spawnButton; // Assign in Inspector
    public GameObject prefabToSpawn;
    private GameObject spawnedInstance; // To keep track of the spawned object
    public Transform headCamera;
    public float spawnDistance = 1.5f;
    public float spawnHeightOffset = 0.5f;
    public LobbyUI m_PrefabScript;

    private void OnEnable()
    {
        spawnButton.action.performed += OnSpawnPressed;
        spawnButton.action.Enable();
    }

    private void OnDisable()
    {
        spawnButton.action.performed -= OnSpawnPressed;
        spawnButton.action.Disable();
    }

    private void OnSpawnPressed(InputAction.CallbackContext ctx)
    {
        if (headCamera == null) headCamera = Camera.main.transform;
        Vector3 spawnPos = headCamera.position + headCamera.forward * spawnDistance;
        spawnPos.y = headCamera.position.y + spawnHeightOffset;
        if (spawnedInstance != null)
        {

            m_PrefabScript = spawnedInstance.GetComponent<LobbyUI>();
            m_PrefabScript.HideLoginUI();
            Destroy(spawnedInstance); // Optional: Destroy previous instance if needed
        }
        spawnedInstance = Instantiate(prefabToSpawn, spawnPos, Quaternion.LookRotation(headCamera.forward));
    }
}