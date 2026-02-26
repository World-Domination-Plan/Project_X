using UnityEngine;

[ExecuteAlways]
public class SurfaceSizeDebug : MonoBehaviour
{
    [ContextMenu("Log Surface Size")]
    void LogSurfaceSize()
    {
        var mf = GetComponent<MeshFilter>();
        var r  = GetComponent<Renderer>();

        if (!mf || !mf.sharedMesh)
        {
            Debug.LogWarning("[SurfaceSizeDebug] Missing MeshFilter/sharedMesh", this);
            return;
        }

        Vector3 local = mf.sharedMesh.bounds.size;     // mesh local bounds
        Vector3 s = transform.lossyScale;              // world scaling (incl parents)

        // pick the “thin” axis (smallest), remaining two are the surface axes
        int thin = 0;
        if (local.y < local.x && local.y < local.z) thin = 1;
        else if (local.z < local.x && local.z < local.y) thin = 2;

        int a = (thin + 1) % 3;
        int b = (thin + 2) % 3;

        float worldA = local[a] * Mathf.Abs(s[a]);
        float worldB = local[b] * Mathf.Abs(s[b]);

        Debug.Log($"[SurfaceSizeDebug] localMeshSize={local} lossyScale={s}", this);
        Debug.Log($"[SurfaceSizeDebug] surface axes = {AxisName(a)} & {AxisName(b)}", this);
        Debug.Log($"[SurfaceSizeDebug] TRUE surface size ≈ {worldA:F3} x {worldB:F3} (world units)", this);

        if (r) Debug.Log($"[SurfaceSizeDebug] AABB bounds.size={r.bounds.size} (may be inflated if rotated)", this);
    }

    static string AxisName(int i) => i == 0 ? "X" : i == 1 ? "Y" : "Z";
}
