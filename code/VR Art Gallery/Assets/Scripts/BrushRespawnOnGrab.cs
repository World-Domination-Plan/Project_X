using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class BrushRespawnOnGrab : MonoBehaviour
{
    public CanvasBrushSpawner spawner;
    public float despawnDelay = 20f;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    bool hasSpawnedReplacement = false;
    Coroutine despawnRoutine;

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

        if (spawner != null)
            spawner.SpawnReplacementBrush();
        }
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
            Destroy(gameObject);
    }
}
