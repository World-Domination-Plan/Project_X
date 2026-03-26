using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using Unity.Netcode;

public class BrushRespawnOnGrab : NetworkBehaviour
{
    public CanvasBrushSpawner spawner;
    public float despawnDelay = 20f;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    bool hasSpawnedReplacement = false;
    Coroutine despawnRoutine;
    public bool IsGrabbed { get; private set; }

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (despawnRoutine != null)
        {
            StopCoroutine(despawnRoutine);
            despawnRoutine = null;
        }

        if (!hasSpawnedReplacement)
        {
            hasSpawnedReplacement = true;
            // Ask the server to spawn a replacement brush
            RequestReplacementBrushServerRpc();
        }
        IsGrabbed = true;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (despawnRoutine != null)
            StopCoroutine(despawnRoutine);

        despawnRoutine = StartCoroutine(DespawnAfterDelay());
    }

    IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(despawnDelay);

        if (grabInteractable != null && !grabInteractable.isSelected)
        {
            // Ask the server to despawn this brush for all clients
            DespawnBrushServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestReplacementBrushServerRpc()
    {
        if (spawner != null)
            spawner.SpawnReplacementBrush();
    }

    [ServerRpc(RequireOwnership = false)]
    void DespawnBrushServerRpc()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Despawn(); // Destroys on all clients by default
        IsGrabbed = false;
    }
}