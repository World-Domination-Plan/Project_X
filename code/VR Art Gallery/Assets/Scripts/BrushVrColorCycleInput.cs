using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class BrushVrColorCycleInput : MonoBehaviour
{
    [Header("Brush")]
    [SerializeField] private BrushToolState brushToolState;
    [SerializeField] private BrushGrabState brushGrabState;
    [SerializeField] private bool requireGrabbed = true;

    [Header("Input Actions")]
    [SerializeField] private InputActionProperty nextColorAction;
    [SerializeField] private InputActionProperty previousColorAction;

    [Header("Input Settings")]
    [SerializeField] private float pressThreshold = 0.5f;

    private bool _nextWasPressed;
    private bool _prevWasPressed;

    private void Awake()
    {
        if (brushToolState == null)
            brushToolState = GetComponent<BrushToolState>() ?? GetComponentInParent<BrushToolState>();

        if (brushGrabState == null)
            brushGrabState = GetComponent<BrushGrabState>() ?? GetComponentInParent<BrushGrabState>();
    }

    private void OnEnable()
    {
        if (nextColorAction.action != null)
            nextColorAction.action.Enable();

        if (previousColorAction.action != null)
            previousColorAction.action.Enable();
    }

    private void OnDisable()
    {
        if (nextColorAction.action != null)
            nextColorAction.action.Disable();

        if (previousColorAction.action != null)
            previousColorAction.action.Disable();
    }

    private void Update()
    {
        if (brushToolState == null)
            return;

        if (requireGrabbed && brushGrabState != null && !brushGrabState.IsGrabbed)
            return;

        bool nextPressed = IsPressed(nextColorAction.action);
        bool prevPressed = IsPressed(previousColorAction.action);

        if (nextPressed && !_nextWasPressed)
            brushToolState.NextPresetColor();

        if (prevPressed && !_prevWasPressed)
            brushToolState.PreviousPresetColor();

        _nextWasPressed = nextPressed;
        _prevWasPressed = prevPressed;
    }

    private bool IsPressed(InputAction action)
    {
        if (action == null)
            return false;

        if (action.activeControl is ButtonControl button)
            return button.isPressed;

        return action.ReadValue<float>() > pressThreshold;
    }
}