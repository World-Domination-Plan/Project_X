using UnityEngine;
using System.Collections.Generic;

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

    [Header("Stroke")]
    [Min(1)] public int flushPointThreshold = 12;
    [Min(1)] public int maxInterpolationSteps = 64;

#if ENABLE_INPUT_SYSTEM
    [Header("Input System")]
    public InputActionProperty drawAction;   // bind to RightHand Activate/Select
    public bool mouseFallback = true;        // for editor testing
#endif

    Vector2 _lastUV;
    bool _hasLast;
    bool _strokeActive;

    ulong _strokeId;
    BrushState _strokeBrush;
    CanvasStrokeSyncNgo _syncBehaviour;

    readonly List<ushort> _batch = new();

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

        bool isDrawing = IsDrawing();
        bool hasTarget = TryGetStrokeTarget(out var sync, out var uv);

        if (isDrawing && hasTarget)
        {
            if (!_strokeActive)
            {
                BeginStroke(sync);
            }
            else if (sync != _syncBehaviour)
            {
                EndStroke();
                BeginStroke(sync);
            }

            if (_strokeActive)
            {
                AddStrokeSample(uv);
                if (_batch.Count >= flushPointThreshold * 2)
                    FlushBatch();
            }
        }
        else
        {
            EndStroke();
        }
    }

    bool TryGetStrokeTarget(out CanvasStrokeSyncNgo sync, out Vector2 uv)
    {
        sync = null;
        uv = default;

        if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit, maxDistance, paintMask, QueryTriggerInteraction.Ignore))
            return false;

        sync = hit.collider.GetComponentInParent<CanvasStrokeSyncNgo>();
        if (!sync)
            return false;

        uv = hit.textureCoord;
        return true;
    }

    void BeginStroke(CanvasStrokeSyncNgo sync)
    {
        if (!sync || !sync.Surface)
            return;

        _syncBehaviour = sync;
        _strokeId = sync.CreateLocalStrokeId();
        _strokeBrush = sync.Surface.GetCurrentBrushState();

        _batch.Clear();
        _hasLast = false;
        _strokeActive = true;

        _syncBehaviour.LocalStrokeBegin(_strokeId, _strokeBrush);
    }

    void EndStroke()
    {
        if (!_strokeActive)
        {
            _hasLast = false;
            return;
        }

        FlushBatch();
        _syncBehaviour.LocalStrokeEnd(_strokeId);

        _batch.Clear();
        _syncBehaviour = null;
        _strokeActive = false;
        _hasLast = false;
    }

    void AddStrokeSample(Vector2 uv)
    {
        if (!_strokeActive)
            return;

        if (_hasLast)
        {
            float dist = Vector2.Distance(_lastUV, uv);
            float step = Mathf.Max(0.0005f, _strokeBrush.radius * 0.5f);
            int steps = Mathf.Clamp(Mathf.CeilToInt(dist / step), 1, maxInterpolationSteps);

            for (int s = 1; s <= steps; s++)
                AddPoint(Vector2.Lerp(_lastUV, uv, s / (float)steps));
        }
        else
        {
            AddPoint(uv);
        }

        _lastUV = uv;
        _hasLast = true;
    }

    void AddPoint(Vector2 uv)
    {
        _batch.Add(CanvasStrokeSyncNgo.EncodeAxis(uv.x));
        _batch.Add(CanvasStrokeSyncNgo.EncodeAxis(uv.y));
    }

    void FlushBatch()
    {
        if (!_strokeActive || _syncBehaviour == null || _batch.Count == 0)
            return;

        _syncBehaviour.LocalStrokePoints(_strokeId, _batch.ToArray());
        _batch.Clear();
    }
}
