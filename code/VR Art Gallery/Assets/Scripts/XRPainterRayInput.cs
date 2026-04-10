using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
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
    public float pressThreshold = 0.1f;
#endif

    Vector2 _lastUV;
    bool _hasLast;

    CanvasStrokeSyncNgo _activeSync;
    ulong _activeStrokeId;
    bool _strokeOpen;

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (drawAction.action != null)
            drawAction.action.Enable();
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (drawAction.action != null)
            drawAction.action.Disable();
#endif
        EndStroke();
    }

    BrushToolState GetHeldBrushToolState()
    {
        if (rightHandInteractor == null)
            return null;

        if (rightHandInteractor.interactablesSelected.Count == 0)
            return null;

        var interactable = rightHandInteractor.interactablesSelected[0];
        if (interactable == null)
            return null;

        var brushTag = interactable.transform.GetComponent<PaintbrushTag>();
        if (brushTag == null)
            brushTag = interactable.transform.GetComponentInParent<PaintbrushTag>();

        if (brushTag == null)
            return null;

        var brushToolState = interactable.transform.GetComponent<BrushToolState>();
        if (brushToolState != null)
            return brushToolState;

        return interactable.transform.GetComponentInParent<BrushToolState>();
    }

    bool IsDrawing()
    {
#if ENABLE_INPUT_SYSTEM
        if (drawAction.action != null)
            return drawAction.action.ReadValue<float>() > pressThreshold;

        if (mouseFallback && Mouse.current != null)
            return Mouse.current.leftButton.isPressed;

        return false;
#else
        return false;
#endif
    }

    void Update()
    {
        if (!rayOrigin)
            rayOrigin = transform;

        if (paintMask.value == 0)
            paintMask = ~0;

        BrushToolState brushToolState = GetHeldBrushToolState();

        if (brushToolState == null || !IsDrawing())
        {
            EndStroke();
            return;
        }

        if (!Physics.Raycast(
                rayOrigin.position,
                rayOrigin.forward,
                out RaycastHit hit,
                maxDistance,
                paintMask,
                QueryTriggerInteraction.Ignore))
        {
            EndStroke();
            return;
        }

        PaintableSurfaceRT surface = hit.collider.GetComponentInParent<PaintableSurfaceRT>();
        if (!surface)
        {
            EndStroke();
            return;
        }

        BrushState brush = brushToolState.CurrentBrushState;

        var sync = surface.GetComponent<CanvasStrokeSyncNgo>();
        if (!sync)
        {
            surface.PaintAt(hit.textureCoord, brush);
            _lastUV = hit.textureCoord;
            _hasLast = true;
            return;
        }

        if (!_strokeOpen || _activeSync != sync)
        {
            EndStroke();
            _activeSync = sync;
            _activeStrokeId = sync.CreateLocalStrokeId();
            sync.LocalStrokeBegin(_activeStrokeId, brush);
            _strokeOpen = true;
            _hasLast = false;
        }

        Vector2 uv = hit.textureCoord;

        if (_hasLast)
        {
            float dist = Vector2.Distance(_lastUV, uv);
            float step = Mathf.Max(0.0005f, brush.radius * 0.5f);
            int steps = Mathf.Clamp(Mathf.CeilToInt(dist / step), 1, 64);

            ushort[] points = new ushort[steps * 2];
            for (int s = 1; s <= steps; s++)
            {
                Vector2 p = Vector2.Lerp(_lastUV, uv, s / (float)steps);
                points[(s - 1) * 2] = CanvasStrokeSyncNgo.EncodeAxis(p.x);
                points[(s - 1) * 2 + 1] = CanvasStrokeSyncNgo.EncodeAxis(p.y);

                surface.PaintAt(p, brush);
            }

            sync.LocalStrokePoints(_activeStrokeId, points);
        }
        else
        {
            surface.PaintAt(uv, brush);
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
            _activeSync.LocalStrokeEnd(_activeStrokeId);

        _strokeOpen = false;
        _activeSync = null;
        _hasLast = false;
    }
}