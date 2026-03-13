using UnityEngine;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public class XRPainterRayInput : MonoBehaviour
{
    [Header("Ray")]
    public Transform rayOrigin;
    public float maxDistance = 5f;
    public LayerMask paintMask = ~0;

    [Header("Grab Check")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor rightHandInteractor;

#if ENABLE_INPUT_SYSTEM
    [Header("Input System")]
    public InputActionProperty drawAction;
    public bool mouseFallback = true;
#endif

    Vector2 _lastUV;
    bool _hasLast;

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (drawAction.action != null) drawAction.action.Enable();
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (drawAction.action != null) drawAction.action.Disable();
#endif
    }

    bool IsHoldingPaintbrush()
    {
        if (rightHandInteractor == null)
            return false;

        if (rightHandInteractor.interactablesSelected.Count == 0)
            return false;

        var interactable = rightHandInteractor.interactablesSelected[0];
        if (interactable == null)
            return false;

        var mb = interactable.transform.GetComponent<PaintbrushTag>();
        if (mb != null)
            return true;

        return interactable.transform.GetComponentInParent<PaintbrushTag>() != null;
    }

    bool IsDrawing()
    {
#if ENABLE_INPUT_SYSTEM
        if (drawAction.action != null)
        {
            var ctrl = drawAction.action.activeControl;
            if (ctrl is ButtonControl bc) return bc.isPressed;
            return drawAction.action.ReadValue<float>() > 0.1f;
        }

        if (mouseFallback && Mouse.current != null)
            return Mouse.current.leftButton.isPressed;

        return false;
#else
        return false;
#endif
    }

    void Update()
    {
        if (!rayOrigin) rayOrigin = transform;

        if (paintMask.value == 0) paintMask = ~0;

        if (!IsHoldingPaintbrush() || !IsDrawing())
        {
            _hasLast = false;
            return;
        }

        if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit,
                maxDistance, paintMask, QueryTriggerInteraction.Ignore))
        {
            _hasLast = false;
            return;
        }

        var surface = hit.collider.GetComponentInParent<PaintableSurfaceRT>();
        if (!surface)
        {
            _hasLast = false;
            return;
        }

        Vector2 uv = hit.textureCoord;

        if (_hasLast)
        {
            float dist = Vector2.Distance(_lastUV, uv);
            float step = Mathf.Max(0.0005f, surface.radius * 0.5f);
            int steps = Mathf.Clamp(Mathf.CeilToInt(dist / step), 1, 64);

            for (int s = 1; s <= steps; s++)
                surface.TryPaintAt(Vector2.Lerp(_lastUV, uv, s / (float)steps));
        }
        else
        {
            surface.TryPaintAt(uv);
        }

        _lastUV = uv;
        _hasLast = true;
    }
}
