using UnityEngine;
using UnityEngine.InputSystem;
using XRMultiplayer;

public class HUDSpawner : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference spawnButton;

    [Header("Prefab")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private float spawnDistance = 1.5f;
    [SerializeField] private float spawnHeightOffset = 0.5f;

    [Header("Cameras")]
    [SerializeField] private Transform vrHeadCamera;
    [SerializeField] private Transform desktopCamera;

    private GameObject spawnedInstance;
    private LobbyUI m_PrefabScript;

    private void OnEnable()
    {
        if (spawnButton != null && spawnButton.action != null)
        {
            spawnButton.action.performed += OnSpawnPressed;
            spawnButton.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (spawnButton != null && spawnButton.action != null)
        {
            spawnButton.action.performed -= OnSpawnPressed;
            spawnButton.action.Disable();
        }
    }

    private void OnSpawnPressed(InputAction.CallbackContext ctx)
    {
        Transform activeCamera = GetActiveCamera();
        if (activeCamera == null || prefabToSpawn == null)
            return;

        Vector3 flatForward = activeCamera.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = activeCamera.forward;

        flatForward.Normalize();

        Vector3 spawnPos = activeCamera.position + flatForward * spawnDistance;
        spawnPos.y = activeCamera.position.y + spawnHeightOffset;

        if (spawnedInstance != null)
        {
            LobbyUI existingScript = spawnedInstance.GetComponent<LobbyUI>();
            if (existingScript != null)
                existingScript.HideLoginUI();

            Destroy(spawnedInstance);
        }

        Quaternion spawnRot = Quaternion.LookRotation(flatForward, Vector3.up);
        spawnedInstance = Instantiate(prefabToSpawn, spawnPos, spawnRot);

        m_PrefabScript = spawnedInstance.GetComponent<LobbyUI>();
    }

    private Transform GetActiveCamera()
    {
        if (vrHeadCamera != null && vrHeadCamera.gameObject.activeInHierarchy)
            return vrHeadCamera;

        if (desktopCamera != null && desktopCamera.gameObject.activeInHierarchy)
            return desktopCamera;

        if (vrHeadCamera != null)
            return vrHeadCamera;

        if (desktopCamera != null)
            return desktopCamera;

        return Camera.main != null ? Camera.main.transform : null;
    }
}