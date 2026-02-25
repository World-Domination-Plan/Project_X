using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Concurrent;

[DisallowMultipleComponent]
[RequireComponent(typeof(PaintableSurfaceRT))]
public class CanvasStrokeSyncNgo : NetworkBehaviour
{
    struct PaintOperation
    {
        public Vector2 uv;
        public BrushState brush;
    }

    [SerializeField] PaintableSurfaceRT surface;
    [SerializeField, Min(1)] int maxPaintOpsPerFrame = 200;

    readonly Dictionary<ulong, BrushState> _activeStrokes = new();
    readonly HashSet<ulong> _localStrokes = new();
    readonly ConcurrentQueue<PaintOperation> _paintQueue = new();

    ulong _localStrokeCounter;

    public PaintableSurfaceRT Surface => surface;

    void Awake()
    {
        if (!surface) surface = GetComponent<PaintableSurfaceRT>();
    }

    void Update()
    {
        if (!surface) return;

        int processed = 0;
        while (processed < maxPaintOpsPerFrame && _paintQueue.TryDequeue(out var op))
        {
            surface.PaintAt(op.uv, op.brush);
            processed++;
        }
    }

    public ulong CreateLocalStrokeId()
    {
        _localStrokeCounter++;
        return (GetLocalClientId() << 32) | _localStrokeCounter;
    }

    public void LocalStrokeBegin(ulong strokeId, BrushState brush)
    {
        _localStrokes.Add(strokeId);
        _activeStrokes[strokeId] = brush;

        if (IsSpawned)
            StrokeBeginServerRpc(strokeId, brush, GetLocalClientId());
    }

    public void LocalStrokePoints(ulong strokeId, ushort[] uvPoints)
    {
        if (uvPoints == null || uvPoints.Length < 2) return;
        if (!_activeStrokes.TryGetValue(strokeId, out var brush)) return;

        EnqueuePaintOperations(uvPoints, brush);

        if (IsSpawned)
            StrokePointsServerRpc(strokeId, uvPoints);
    }

    public void LocalStrokeEnd(ulong strokeId)
    {
        _activeStrokes.Remove(strokeId);
        _localStrokes.Remove(strokeId);

        if (IsSpawned)
            StrokeEndServerRpc(strokeId);
    }

    [ServerRpc(RequireOwnership = false)]
    void StrokeBeginServerRpc(ulong strokeId, BrushState brush, ulong senderId, ServerRpcParams rpcParams = default)
    {
        ulong resolvedSender = rpcParams.Receive.SenderClientId;
        if (senderId != resolvedSender)
            senderId = resolvedSender;

        _activeStrokes[strokeId] = brush;
        StrokeBeginClientRpc(strokeId, brush, senderId);
    }

    [ServerRpc(RequireOwnership = false)]
    void StrokePointsServerRpc(ulong strokeId, ushort[] uvPoints)
    {
        if (uvPoints == null || uvPoints.Length < 2) return;
        if (!_activeStrokes.ContainsKey(strokeId)) return;

        StrokePointsClientRpc(strokeId, uvPoints);
    }

    [ServerRpc(RequireOwnership = false)]
    void StrokeEndServerRpc(ulong strokeId)
    {
        _activeStrokes.Remove(strokeId);
        StrokeEndClientRpc(strokeId);
    }

    [ClientRpc]
    void StrokeBeginClientRpc(ulong strokeId, BrushState brush, ulong senderId)
    {
        if (_localStrokes.Contains(strokeId)) return;
        _activeStrokes[strokeId] = brush;
    }

    [ClientRpc]
    void StrokePointsClientRpc(ulong strokeId, ushort[] uvPoints)
    {
        if (_localStrokes.Contains(strokeId)) return;
        if (uvPoints == null || uvPoints.Length < 2) return;
        if (!_activeStrokes.TryGetValue(strokeId, out var brush)) return;

        EnqueuePaintOperations(uvPoints, brush);
    }

    [ClientRpc]
    void StrokeEndClientRpc(ulong strokeId)
    {
        if (_localStrokes.Contains(strokeId)) return;
        _activeStrokes.Remove(strokeId);
    }

    void EnqueuePaintOperations(ushort[] uvPoints, BrushState brush)
    {
        int usableLength = uvPoints.Length - (uvPoints.Length % 2);
        for (int i = 0; i < usableLength; i += 2)
        {
            _paintQueue.Enqueue(new PaintOperation
            {
                uv = new Vector2(DecodeAxis(uvPoints[i]), DecodeAxis(uvPoints[i + 1])),
                brush = brush
            });
        }
    }

    ulong GetLocalClientId()
    {
        if (NetworkManager != null && NetworkManager.IsListening)
            return NetworkManager.LocalClientId;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            return NetworkManager.Singleton.LocalClientId;

        return 0;
    }

    public static ushort EncodeAxis(float v) => (ushort)(Mathf.Clamp01(v) * 65535f);

    public static float DecodeAxis(ushort v) => v / 65535f;
}
