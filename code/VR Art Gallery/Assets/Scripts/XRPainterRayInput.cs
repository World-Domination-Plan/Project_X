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

    // --- Stroke state ---
    CanvasStrokeSyncNgo _activeSync;
    ulong _activeStrokeId;
    bool _strokeOpen;

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
        EndStroke(); // clean up if disabled mid-stroke
    }

    bool IsHoldingPaintbrush()
    {
        if (rightHandInteractor == null) return false;
        if (rightHandInteractor.interactablesSelected.Count == 0) return false;

        var interactable = rightHandInteractor.interactablesSelected[0];
        if (interactable == null) return false;

        return interactable.transform.GetComponent<PaintbrushTag>() != null
            || interactable.transform.GetComponentInParent<PaintbrushTag>() != null;
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

        bool canPaint = IsHoldingPaintbrush() && IsDrawing();

        if (!canPaint)
        {
            EndStroke();
            return;
        }

        if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit,
                maxDistance, paintMask, QueryTriggerInteraction.Ignore))
        {
            EndStroke();
            return;
        }

        var surface = hit.collider.GetComponentInParent<PaintableSurfaceRT>();
        if (!surface)
        {
            EndStroke();
            return;
        }

        // Get the sync component — it lives on the same GameObject as PaintableSurfaceRT
        var sync = surface.GetComponent<CanvasStrokeSyncNgo>();
        if (!sync)
        {
            // Fallback: no network sync present, paint locally as before
            surface.TryPaintAt(hit.textureCoord);
            _lastUV = hit.textureCoord;
            _hasLast = true;
            return;
        }

        Vector2 uv = hit.textureCoord;

        // Begin a new stroke if we just started painting or switched canvas
        if (!_strokeOpen || _activeSync != sync)
        {
            EndStroke(); // close previous stroke if we switched canvas
            _activeSync = sync;
            _activeStrokeId = sync.CreateLocalStrokeId();
            sync.LocalStrokeBegin(_activeStrokeId, sync.Surface.GetCurrentBrushState());
            _strokeOpen = true;
            _hasLast = false;
        }

        // Interpolate between last UV and current to fill gaps
        if (_hasLast)
        {
            float dist = Vector2.Distance(_lastUV, uv);
            float step = Mathf.Max(0.0005f, surface.radius * 0.5f);
            int steps = Mathf.Clamp(Mathf.CeilToInt(dist / step), 1, 64);

            // Batch all interpolated points into one RPC call
            ushort[] points = new ushort[steps * 2];
            for (int s = 1; s <= steps; s++)
            {
                Vector2 p = Vector2.Lerp(_lastUV, uv, s / (float)steps);
                points[(s - 1) * 2] = CanvasStrokeSyncNgo.EncodeAxis(p.x);
                points[(s - 1) * 2 + 1] = CanvasStrokeSyncNgo.EncodeAxis(p.y);
            }
            sync.LocalStrokePoints(_activeStrokeId, points);
        }
        else
        {
            sync.LocalStrokePoints(_activeStrokeId, new ushort[]
            {
                CanvasStrokeSyncNgo.EncodeAxis(uv.x),
                CanvasStrokeSyncNgo.EncodeAxis(uv.y)
            });
        }

        _lastUV = uv;
        _hasLast = true;
    }

    void EndStroke()
    {
        if (_strokeOpen && _activeSync != null)
        {
            _activeSync.LocalStrokeEnd(_activeStrokeId);
        }
        _strokeOpen = false;
        _activeSync = null;
        _hasLast = false;
    }
}
