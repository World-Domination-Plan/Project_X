using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using SandboxXRI;

public class DesktopUIFocusBlocker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private DesktopFirstPersonController desktopController;
    [SerializeField] private DesktopBrushInteractor desktopBrushInteractor;
    [SerializeField] private DesktopPainterRayInput desktopPainterRayInput;

    private bool isBlocked;

    void Awake()
    {
        if (eventSystem == null)
            eventSystem = EventSystem.current;

        if (desktopController == null)
            desktopController = FindFirstObjectByType<DesktopFirstPersonController>();

        if (desktopBrushInteractor == null)
            desktopBrushInteractor = FindFirstObjectByType<DesktopBrushInteractor>();

        if (desktopPainterRayInput == null)
            desktopPainterRayInput = FindFirstObjectByType<DesktopPainterRayInput>();
    }

    void Update()
    {
        bool shouldBlock = IsTMPInputFocused();

        if (shouldBlock == isBlocked)
            return;

        isBlocked = shouldBlock;

        if (desktopController != null)
            desktopController.enabled = !shouldBlock;

        if (desktopBrushInteractor != null)
            desktopBrushInteractor.enabled = !shouldBlock;

        if (desktopPainterRayInput != null)
            desktopPainterRayInput.enabled = !shouldBlock;
    }

    private bool IsTMPInputFocused()
    {
        if (eventSystem == null)
            return false;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected == null)
            return false;

        TMP_InputField inputField = selected.GetComponentInParent<TMP_InputField>();
        return inputField != null && inputField.isFocused;
    }
}