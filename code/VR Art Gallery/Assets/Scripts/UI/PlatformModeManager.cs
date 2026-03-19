using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using SandboxXRI;

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

    [Header("Other")]
    public GameObject teleportAreaSetup;
    public XRPainterRayInput painterRayInput;
    public EventSystem eventSystem;

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
        if (painterRayInput == null) painterRayInput = FindObjectOfType<XRPainterRayInput>();
        if (desktopController == null) desktopController = FindObjectOfType<DesktopFirstPersonController>();
        if (eventSystem == null) eventSystem = FindObjectOfType<EventSystem>();
    }

    IEnumerator Start()
    {
        // Wait two end-of-frames for all XR systems to fully initialise
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        Debug.Log($"[PlatformModeManager] Applying mode: {CurrentMode}");
        ApplyMode(CurrentMode);
    }

    public void ApplyMode(PlatformMode mode)
    {
        bool isVR = mode == PlatformMode.VR;

        if (leftController) leftController.SetActive(isVR);
        if (rightController) rightController.SetActive(isVR);
        if (leftHand) leftHand.SetActive(isVR);
        if (rightHand) rightHand.SetActive(isVR);
        if (locomotionSystem) locomotionSystem.SetActive(isVR);
        if (teleportAreaSetup) teleportAreaSetup.SetActive(isVR);

        if (painterRayInput != null)
            painterRayInput.mouseFallback = !isVR;

        if (desktopController != null)
            desktopController.enabled = !isVR;

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
