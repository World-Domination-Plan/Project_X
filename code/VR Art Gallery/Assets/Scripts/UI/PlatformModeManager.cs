using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using SandboxXRI;

public enum PlatformMode { VR, Desktop }

public class PlatformModeManager : MonoBehaviour
{
    public static PlatformModeManager Instance { get; private set; }
    public static PlatformMode CurrentMode { get; set; } = PlatformMode.Desktop;

    public DesktopFirstPersonController desktopController;

    [Header("XR Device Simulator")]
    public GameObject xrDeviceSimulator;

    [Header("XR Rig Children - Disable in Desktop mode")]
    public GameObject leftController;
    public GameObject rightController;
    public GameObject leftHand;
    public GameObject rightHand;
    public GameObject locomotionSystem;

    [Header("Desktop Controller")]
    public GameObject desktopControllerPrefab;

    [Header("Other")]
    public GameObject teleportAreaSetup;
    public XRPainterRayInput painterRayInput;
    public EventSystem eventSystem;

    // Cached internally — no need to assign in Inspector
    private CharacterControllerDriver _ccDriver;
    private CharacterController _characterController;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (!leftController) leftController = GameObject.Find("Left Controller");
        if (!rightController) rightController = GameObject.Find("Right Controller");
        if (!leftHand) leftHand = GameObject.Find("Left Hand");
        if (!rightHand) rightHand = GameObject.Find("Right Hand");
        if (!locomotionSystem) locomotionSystem = GameObject.Find("Locomotion System");
        if (!teleportAreaSetup) teleportAreaSetup = GameObject.Find("Reset Area Setup");

        if (painterRayInput == null)
            painterRayInput = FindObjectOfType<XRPainterRayInput>();

        if (desktopController == null)
            desktopController = FindObjectOfType<DesktopFirstPersonController>();

        if (desktopController == null && desktopControllerPrefab != null)
        {
            var go = Instantiate(desktopControllerPrefab);
            desktopController = go.GetComponent<DesktopFirstPersonController>();
        }

        if (eventSystem == null)
            eventSystem = FindObjectOfType<EventSystem>();

        // Find CharacterControllerDriver on the XR rig
        var xrRig = GameObject.Find("XR Interaction Setup MP Variant");
        if (xrRig != null)
        {
            _ccDriver = xrRig.GetComponent<CharacterControllerDriver>();
            _characterController = xrRig.GetComponent<CharacterController>();
        }
    }

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        Debug.Log($"[PlatformModeManager] Applying mode: {CurrentMode}");
        ApplyMode(CurrentMode);
    }

    public void ApplyMode(PlatformMode mode)
    {
        bool isVR = mode == PlatformMode.VR;

        // XR rig children
        if (leftController) leftController.SetActive(isVR);
        if (rightController) rightController.SetActive(isVR);
        if (leftHand) leftHand.SetActive(isVR);
        if (rightHand) rightHand.SetActive(isVR);
        if (locomotionSystem) locomotionSystem.SetActive(isVR);
        if (teleportAreaSetup) teleportAreaSetup.SetActive(isVR);

        // XR Device Simulator — only active in VR mode
        if (xrDeviceSimulator != null)
            xrDeviceSimulator.SetActive(isVR);

        // CharacterControllerDriver fights with desktop movement — disable it in Desktop mode
        if (_ccDriver != null)
            _ccDriver.enabled = isVR;

        // Reset the CharacterController to a sane state for desktop use
        if (_characterController != null && !isVR)
        {
            _characterController.enabled = false;
            _characterController.center = new Vector3(0f, 1f, 0f);
            _characterController.height = 2f;
            _characterController.enabled = true;
        }

        // Painter ray input
        if (painterRayInput != null)
            painterRayInput.mouseFallback = !isVR;

        // Desktop controller
        if (desktopController != null)
        {
            desktopController.enabled = !isVR;
            if (!isVR) desktopController.Activate();
        }

        // Disable Oculus input module in desktop mode
        if (eventSystem != null)
        {
            foreach (var module in eventSystem.GetComponents<BaseInputModule>())
            {
                if (module.GetType().Name == "PointableCanvasModule")
                    module.enabled = isVR;
            }
        }
    }

    public void ForceMode(PlatformMode mode)
    {
        CurrentMode = mode;
        ApplyMode(mode);
    }
}