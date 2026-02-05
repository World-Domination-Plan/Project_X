using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class XRPainterRayInput : MonoBehaviour
{
    [Header("Ray")]
    public Transform rayOrigin;              // assign controller ray origin (or leave empty = this transform)
    public float maxDistance = 5f;
    public LayerMask paintMask;

#if ENABLE_INPUT_SYSTEM
    [Header("Input (XR)")]
    public InputActionProperty drawAction;   // bind to XRI RightHand Interaction/Activate (or Select)
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
        if (drawAction.action != null)
        {
            // supports float trigger or button
            if (drawAction.action.valueType == typeof(float))
                return drawAction.action.ReadValue<float>() > 0.1f;
            return drawAction.action.ReadValueAsButton();
        }
#endif
#if UNITY_EDITOR
        // fallback: hold left mouse to draw (useful for quick sanity checks)
        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    void Update()
    {
        if (!rayOrigin) rayOrigin = transform;

        if (!IsDrawing()) { _hasLast = false; return; }

        if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit, maxDistance, paintMask, QueryTriggerInteraction.Ignore))
        {
            _hasLast = false;
            return;
        }

        var surface = hit.collider.GetComponentInParent<PaintableSurfaceRT>();
        if (!surface) { _hasLast = false; return; }

        // Mode must not be DisplayOnly
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
