using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopPainterRayInput : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public DesktopBrushInteractor brushInteractor;

    [Header("Ray")]
    public float maxDistance = 10f;
    public LayerMask paintMask = ~0;

    private Vector2 lastUV;
    private bool hasLast;

    void Update()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null || brushInteractor == null)
            return;

        BrushToolState brushToolState = brushInteractor.HeldBrushToolState;

        if (brushToolState == null)
        {
            hasLast = false;
            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
        {
            hasLast = false;
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, paintMask, QueryTriggerInteraction.Ignore))
        {
            hasLast = false;
            return;
        }

        PaintableSurfaceRT surface = hit.collider.GetComponentInParent<PaintableSurfaceRT>();
        if (surface == null)
        {
            hasLast = false;
            return;
        }

        Vector2 uv = hit.textureCoord;
        BrushState brush = brushToolState.CurrentBrushState;

        if (hasLast)
        {
            float dist = Vector2.Distance(lastUV, uv);
            float step = Mathf.Max(0.0005f, brush.radius * 0.5f);
            int steps = Mathf.Clamp(Mathf.CeilToInt(dist / step), 1, 64);

            for (int s = 1; s <= steps; s++)
            {
                Vector2 lerpedUV = Vector2.Lerp(lastUV, uv, s / (float)steps);
                surface.PaintAt(lerpedUV, brush);
            }
        }
        else
        {
            surface.PaintAt(uv, brush);
        }

        lastUV = uv;
        hasLast = true;
    }
}