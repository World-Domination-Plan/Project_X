using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class XRPainterRayInput : MonoBehaviour
{
    [Header("Ray")]
    public Transform rayOrigin;     // set to Right Hand/Aim Pose (recommended)
    public float maxDistance = 5f;
    public LayerMask paintMask;

#if ENABLE_INPUT_SYSTEM
    [Header("Input System")]
    public InputActionProperty drawAction;   // bind to RightHand Activate/Select
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
            // Works for trigger axis (0..1) and button (0/1)
            return drawAction.action.ReadValue<float>() > 0.1f;
        }
#endif
        // Fallback: hold left mouse for quick test
        return Input.GetMouseButton(0);
    }

    //bool IsDrawing() => Input.GetMouseButton(0);

/*
    void Update()
    {
        if (!rayOrigin) rayOrigin = transform;

        if (!IsDrawing()) { _hasLast = false; return; }

        if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit,
                maxDistance, paintMask, QueryTriggerInteraction.Ignore))
        {
            _hasLast = false;
            return;
        }

        var surface = hit.collider.GetComponentInParent<PaintableSurfaceRT>();
        if (!surface) { _hasLast = false; return; }

        var uv = hit.textureCoord;

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

        if (hit.collider == null || !hit.collider.TryGetComponent<PaintableSurfaceRT>(out surface))
        {
            _hasLast = false;
            return;
        }

        _lastUV = uv;
        _hasLast = true;
    }
*/
    void Update()
    {
        if (!rayOrigin) rayOrigin = transform;

        if (!IsDrawing()) return;

        Debug.Log("DRAWING ON");
        Debug.DrawRay(rayOrigin.position, rayOrigin.forward * maxDistance, Color.green);

        if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit,
                maxDistance, paintMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("RAY MISS");
            return;
        }

        Debug.Log($"HIT: {hit.collider.name}  layer={hit.collider.gameObject.layer}  uv={hit.textureCoord}");

        var surface = hit.collider.GetComponentInParent<PaintableSurfaceRT>();
        Debug.Log(surface ? $"FOUND PaintableSurfaceRT, mode={surface.mode}" : "NO PaintableSurfaceRT on hit object");

        if (surface) surface.TryPaintAt(hit.textureCoord);
    }

}
