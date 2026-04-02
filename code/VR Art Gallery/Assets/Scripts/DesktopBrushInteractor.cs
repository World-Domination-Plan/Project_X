using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopBrushInteractor : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform holdPoint;

    [Header("Settings")]
    public float grabDistance = 5f;
    public LayerMask interactMask = ~0;
    public Key dropKey = Key.Q;

    public BrushToolState HeldBrushToolState { get; private set; }

    private GameObject heldBrushObject;
    private BrushGrabState heldGrabState;
    private Rigidbody heldRb;
    private Collider[] heldColliders;

    void Update()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (heldBrushObject == null)
                TryGrabBrush();
        }

        if (heldBrushObject != null)
        {
            FollowHoldPoint();

            if (Keyboard.current != null && Keyboard.current[dropKey].wasPressedThisFrame)
                DropBrush();
        }
    }

    void TryGrabBrush()
    {
        if (Mouse.current == null)
            return;

        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, grabDistance, interactMask, QueryTriggerInteraction.Ignore))
            return;

        BrushToolState brushTool = hit.collider.GetComponentInParent<BrushToolState>();
        if (brushTool == null)
            return;

        heldBrushObject = brushTool.gameObject;
        HeldBrushToolState = brushTool;

        heldGrabState = heldBrushObject.GetComponent<BrushGrabState>();
        if (heldGrabState == null)
            heldGrabState = heldBrushObject.AddComponent<BrushGrabState>();

        heldRb = heldBrushObject.GetComponent<Rigidbody>();
        heldColliders = heldBrushObject.GetComponentsInChildren<Collider>(true);

        if (heldRb != null)
        {
            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;
            heldRb.isKinematic = true;
            heldRb.useGravity = false;
        }

        if (heldColliders != null)
        {
            foreach (var c in heldColliders)
                c.enabled = false;
        }

        heldBrushObject.transform.SetParent(holdPoint, false);
        heldBrushObject.transform.localPosition = Vector3.zero;
        heldBrushObject.transform.localRotation = Quaternion.identity;

        if (heldGrabState != null)
            heldGrabState.SetGrabbed(true);
    }

    void FollowHoldPoint()
    {
        if (heldBrushObject == null || holdPoint == null)
            return;

        heldBrushObject.transform.position = holdPoint.position;
        heldBrushObject.transform.rotation = holdPoint.rotation;
    }

    public void DropBrush()
    {
        if (heldBrushObject == null)
            return;

        heldBrushObject.transform.SetParent(null, true);

        if (heldRb != null)
        {
            heldRb.isKinematic = false;
            heldRb.useGravity = true;
        }

        if (heldColliders != null)
        {
            foreach (var c in heldColliders)
                c.enabled = true;
        }

        if (heldGrabState != null)
            heldGrabState.SetGrabbed(false);

        heldBrushObject = null;
        HeldBrushToolState = null;
        heldGrabState = null;
        heldRb = null;
        heldColliders = null;
    }
}