using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public enum PlatformMode { VR, Desktop }

public class PlatformModeManager : MonoBehaviour
{
    public static PlatformModeManager Instance { get; private set; }
    public static PlatformMode CurrentMode { get; set; } = PlatformMode.Desktop;

    [Header("Mode Roots")]
    [SerializeField] private GameObject vrRoot;
    [SerializeField] private GameObject desktopRoot;

    [Header("Optional")]
    [SerializeField] private EventSystem eventSystem;

    [Header("XR Helpers")]
    [SerializeField] private GameObject[] disableInDesktop;
    [SerializeField] private Behaviour[] disableBehavioursInDesktop;

    [Header("Cameras and Canvases")]
    [SerializeField] private Canvas[] modeDependentCanvases;
    [SerializeField] private Camera desktopCamera;
    [SerializeField] private Camera vrCamera;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (eventSystem == null)
            eventSystem = FindFirstObjectByType<EventSystem>();
    }

    IEnumerator Start()
    {
        yield return null;
        yield return null;

        Debug.Log($"[PlatformModeManager] Applying mode: {CurrentMode}");
        ApplyMode(CurrentMode);
    }

    public void ApplyMode(PlatformMode mode)
    {
        bool isVR = mode == PlatformMode.VR;
        bool isDesktop = !isVR;

        if (vrRoot != null)
            vrRoot.SetActive(isVR);

        if (desktopRoot != null)
            desktopRoot.SetActive(isDesktop);

        if (disableInDesktop != null)
        {
            foreach (var go in disableInDesktop)
            {
                if (go != null)
                    go.SetActive(!isDesktop);
            }
        }

        if (disableBehavioursInDesktop != null)
        {
            foreach (var b in disableBehavioursInDesktop)
            {
                if (b != null)
                    b.enabled = !isDesktop;
            }
        }

        if (eventSystem != null)
        {
            foreach (var module in eventSystem.GetComponents<BaseInputModule>())
            {
                string typeName = module.GetType().Name;

                if (typeName == "PointableCanvasModule")
                    module.enabled = isVR;

                if (typeName == "XRUIInputModule")
                    module.enabled = isVR;

                if (typeName == "InputSystemUIInputModule")
                    module.enabled = isDesktop;
            }
        }

        if (modeDependentCanvases != null)
        {
            foreach (var canvas in modeDependentCanvases)
            {
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                    canvas.worldCamera = isVR ? vrCamera : desktopCamera;
            }
        }

        Debug.Log($"[PlatformModeManager] Mode applied. VR={isVR}, Desktop={isDesktop}");
    }

    public void ForceMode(PlatformMode mode)
    {
        CurrentMode = mode;
        ApplyMode(mode);
    }
}