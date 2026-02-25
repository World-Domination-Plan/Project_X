using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public class XRPainterRayInput : MonoBehaviour
{
    [Header("Ray")]
    public Transform rayOrigin;     // assign Aim Pose if you have one
    public float maxDistance = 5f;
    public LayerMask paintMask = ~0; // default: Everything

#if ENABLE_INPUT_SYSTEM
    [Header("Input System")]
    public InputActionProperty drawAction;   // bind to RightHand Activate/Select
    public bool mouseFallback = true;        // for editor testing
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

    bool IsDrawing()
    {
#if ENABLE_INPUT_SYSTEM
        // Primary: XR action
        if (drawAction.action != null)
        {
            // Robust for both button + axis
            var ctrl = drawAction.action.activeControl;
            if (ctrl is ButtonControl bc) return bc.isPressed;
            return drawAction.action.ReadValue<float>() > 0.1f;
        }

        // Optional editor fallback (Input System mouse, not UnityEngine.Input)
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

        // If you forgot to set a mask, don’t silently fail
        if (paintMask.value == 0) paintMask = ~0;

        if (!IsDrawing())
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

        // Interpolate between last UV and current UV to avoid gaps
        if (_hasLast)
        {
            float dist = Vector2.Distance(_lastUV, uv);

            // surface.radius is UV-space (0..1). Step at half radius.
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
