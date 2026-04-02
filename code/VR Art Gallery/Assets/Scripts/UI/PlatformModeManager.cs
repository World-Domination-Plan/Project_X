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

        Debug.Log($"[PlatformModeManager] Mode applied. VR={isVR}, Desktop={isDesktop}");
    }

    public void ForceMode(PlatformMode mode)
    {
        CurrentMode = mode;
        ApplyMode(mode);
    }
}