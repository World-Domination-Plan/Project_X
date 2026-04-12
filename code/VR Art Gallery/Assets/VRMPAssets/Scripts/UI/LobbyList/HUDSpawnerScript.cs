using UnityEngine;
using UnityEngine.InputSystem;
using XRMultiplayer;

public class HUDSpawner : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference vrSpawnButton;      // VR controller binding
    public InputActionReference desktopSpawnButton; // Keyboard/mouse binding

    [Header("HUD")]
    public GameObject hudObject;
    public Transform headCamera;
    public float spawnDistance = 1.5f;
    public float spawnHeightOffset = 0.5f;

    private InputActionReference ActiveAction =>
        PlatformModeManager.CurrentMode == PlatformMode.VR ? vrSpawnButton : desktopSpawnButton;

    private void OnEnable()
    {
        ActiveAction.action.performed += OnTogglePressed;
        ActiveAction.action.Enable();
    }

    private void OnDisable()
    {
        ActiveAction.action.performed -= OnTogglePressed;
        ActiveAction.action.Disable();
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        if (hudObject == null || hudObject.transform.childCount == 0) return;

        GameObject child = hudObject.transform.GetChild(0).gameObject;
        bool show = !child.activeSelf;

        if (show)
        {
            if (headCamera == null) headCamera = Camera.main.transform;
            Vector3 pos = headCamera.position + headCamera.forward * spawnDistance;
            pos.y += spawnHeightOffset;
            hudObject.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(headCamera.forward));
        }

        child.SetActive(show);
    }
}