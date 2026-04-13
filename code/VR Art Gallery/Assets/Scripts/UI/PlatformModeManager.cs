using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using SandboxXRI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public enum PlatformMode { VR, Desktop }

public class PlatformModeManager : MonoBehaviour
{
    public static PlatformModeManager Instance { get; private set; }
    public static PlatformMode CurrentMode { get; set; } = PlatformMode.Desktop;

    [Header("XR Rig Children — disable in Desktop mode")]
    public GameObject leftController;
    public GameObject rightController;
    public GameObject leftHand;
    public GameObject rightHand;
    public GameObject locomotionSystem;

    [Header("Desktop Controller")]
    public DesktopFirstPersonController desktopController;

    [Header("XR Device Simulator")]
    public XRDeviceSimulator xrDeviceSimulator;

    [Header("Other")]
    public GameObject teleportAreaSetup;
    public XRPainterRayInput painterRayInput;
    public EventSystem eventSystem;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!leftController) leftController = GameObject.Find("Left Controller");
        if (!rightController) rightController = GameObject.Find("Right Controller");
        if (!leftHand) leftHand = GameObject.Find("Left Hand");
        if (!rightHand) rightHand = GameObject.Find("Right Hand");
        if (!locomotionSystem) locomotionSystem = GameObject.Find("Locomotion System");
        if (!teleportAreaSetup) teleportAreaSetup = GameObject.Find("Reset Area Setup");

        if (painterRayInput == null) painterRayInput = FindObjectOfType<XRPainterRayInput>();
        if (desktopController == null) desktopController = FindObjectOfType<DesktopFirstPersonController>();
        if (eventSystem == null) eventSystem = FindObjectOfType<EventSystem>();

        if (xrDeviceSimulator == null)
            xrDeviceSimulator = FindFirstObjectByType<XRDeviceSimulator>(FindObjectsInactive.Include);
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
        bool isDesktop = mode == PlatformMode.Desktop;

        if (leftController) leftController.SetActive(isVR);
        if (rightController) rightController.SetActive(isVR);
        if (leftHand) leftHand.SetActive(isVR);
        if (rightHand) rightHand.SetActive(isVR);
        if (locomotionSystem) locomotionSystem.SetActive(isVR);
        if (teleportAreaSetup) teleportAreaSetup.SetActive(isVR);

        if (painterRayInput != null)
            painterRayInput.mouseFallback = isDesktop;

        if (desktopController != null)
            desktopController.enabled = isDesktop;

        if (xrDeviceSimulator != null)
            xrDeviceSimulator.gameObject.SetActive(isDesktop);
        else
            Debug.LogWarning("[PlatformModeManager] XR Device Simulator not found in scene.");

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