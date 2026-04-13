using Unity.Netcode;
using UnityEngine;

public class CanvasSpawner : NetworkBehaviour
{
    [Header("Prefab")]
    public GameObject workspacePrefab;   // ← drag the WORKSPACE root prefab here
    public GameObject paintbrushPrefab;

    [Header("VR Camera / Head Transform (CenterEye)")]
    public Transform playerHead;

    [Header("Spawn Tuning")]
    public float spawnDistance = 1.5f;
    public float heightOffset = -0.15f;
    public bool faceUser = true;

    [Header("Brush Spawn")]
    public Vector3 brushOffset = new Vector3(0.35f, -0.2f, 0f);
    public Vector3 brushRotationEuler = Vector3.zero;

    [Header("Anti-overlap (optional)")]
    public float checkRadius = 0.25f;
    public float pushStep = 0.3f;
    public int maxPushTries = 8;
    public LayerMask overlapMask = ~0;

    [Header("Test image (optional override)")]
    public Texture2D testTexture;

    [Header("HUD Follow")]
    public string hudPanelName = "Gallery Selection UI";
    public string hudFallbackName = "World Space Canvas";
    public float workspaceBehindHudDistance = 0.35f;
    public float workspaceBehindHudHeightOffset = -1.1f;

    private NetworkObject m_SpawnedWorkspace;
    private bool m_IsWorkspaceVisible;
    private NetworkObjectReference m_SpawnedBrushRef;

    public void Create()
    {
        if (!workspacePrefab)
        {
            Debug.LogError("[CanvasSpawner] workspacePrefab not assigned.");
            return;
        }

        Transform activeHead = playerHead;
        if (!activeHead && Camera.main != null)
            activeHead = Camera.main.transform;

        if (!activeHead)
        {
            Debug.LogError("[CanvasSpawner] playerHead not assigned.");
            return;
        }

        Vector3 pos;
        Quaternion rot;

        if (!TryBuildSpawnPoseBehindHud(activeHead, out pos, out rot))
        {
            Vector3 forwardFlat = Vector3.ProjectOnPlane(activeHead.forward, Vector3.up).normalized;
            if (forwardFlat.sqrMagnitude < 0.001f)
                forwardFlat = activeHead.forward;

            pos = activeHead.position + forwardFlat * spawnDistance + Vector3.up * heightOffset;

            for (int i = 0; i < maxPushTries; i++)
            {
                if (!Physics.CheckSphere(pos, checkRadius, overlapMask, QueryTriggerInteraction.Ignore))
                    break;
                pos += forwardFlat * pushStep;
            }

            if (faceUser)
            {
                Vector3 toUserFlat = Vector3.ProjectOnPlane(activeHead.position - pos, Vector3.up).normalized;
                if (toUserFlat.sqrMagnitude < 0.001f)
                    toUserFlat = -forwardFlat;

                rot = Quaternion.LookRotation(toUserFlat, Vector3.up);
            }
            else
            {
                rot = Quaternion.LookRotation(forwardFlat, Vector3.up);
            }
        }

        CreateOrToggleWorkspaceServerRpc(pos, rot);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CreateOrToggleWorkspaceServerRpc(Vector3 pos, Quaternion rot)
    {
        if (m_SpawnedWorkspace != null && m_SpawnedWorkspace.IsSpawned)
        {
            SetWorkspaceVisibility(!m_IsWorkspaceVisible, pos, rot);
            return;
        }

        // Instantiate the full Workspace prefab (root has NetworkObject)
        GameObject workspace = Instantiate(workspacePrefab, pos, rot);
        NetworkObject netObj = workspace.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError("[CanvasSpawner] NetworkObject not found on workspacePrefab root! Make sure it is on the Workspace root, not a child.");
            return;
        }

        StripNestedNetworkObjectsForSpawn(workspace, netObj);
        netObj.Spawn();
        m_SpawnedWorkspace = netObj;
        m_IsWorkspaceVisible = true;

        ApplyWorkspacePlacementLocal(workspace, pos, rot, true);

        if (paintbrushPrefab != null)
        {
            // Use GetComponentInChildren in case CanvasBrushSpawner is not on the root
            CanvasBrushSpawner brushSpawner = workspace.GetComponentInChildren<CanvasBrushSpawner>();

            if (brushSpawner == null)
            {
                Debug.LogWarning("[CanvasSpawner] CanvasBrushSpawner not found in workspace hierarchy.");
                return;
            }

            brushSpawner.paintbrushPrefab = paintbrushPrefab;
            brushSpawner.canvasTransform = workspace.transform;
            brushSpawner.brushOffset = brushOffset;
            brushSpawner.brushRotationEuler = brushRotationEuler;
            brushSpawner.SpawnBrush();

            NetworkObject spawnedBrush = FindClosestSpawnedBrush(workspace.transform.position);
            if (spawnedBrush != null)
                m_SpawnedBrushRef = spawnedBrush;
        }
    }

    private void SetWorkspaceVisibility(bool visible, Vector3 pos, Quaternion rot)
    {
        if (m_SpawnedWorkspace == null)
            return;

        if (visible)
            ApplyWorkspacePlacementLocal(m_SpawnedWorkspace.gameObject, pos, rot, true);

        ApplyWorkspaceVisibilityLocal(m_SpawnedWorkspace.gameObject, visible);

        if (!m_SpawnedBrushRef.TryGet(out NetworkObject brushObj) || brushObj == null || !brushObj.IsSpawned)
        {
            NetworkObject refreshedBrush = FindClosestSpawnedBrush(m_SpawnedWorkspace.transform.position);
            if (refreshedBrush != null)
                m_SpawnedBrushRef = refreshedBrush;
        }

        if (m_SpawnedBrushRef.TryGet(out brushObj) && brushObj != null && brushObj.IsSpawned)
        {
            if (visible)
                ApplyBrushPlacementRelativeToWorkspaceLocal(brushObj.gameObject, m_SpawnedWorkspace.transform, brushOffset, brushRotationEuler);

            ApplyWorkspaceVisibilityLocal(brushObj.gameObject, visible);
        }

        if (IsServer)
            SetWorkspaceVisibilityClientRpc(m_SpawnedWorkspace, m_SpawnedBrushRef, visible, pos, rot);

        m_IsWorkspaceVisible = visible;
    }

    [ClientRpc]
    private void SetWorkspaceVisibilityClientRpc(NetworkObjectReference workspaceRef, NetworkObjectReference brushRef, bool visible, Vector3 pos, Quaternion rot)
    {
        if (!workspaceRef.TryGet(out NetworkObject workspace) || workspace == null)
            return;

        if (visible)
            ApplyWorkspacePlacementLocal(workspace.gameObject, pos, rot, true);

        ApplyWorkspaceVisibilityLocal(workspace.gameObject, visible);

        if (brushRef.TryGet(out NetworkObject brush) && brush != null)
        {
            if (visible)
                ApplyBrushPlacementRelativeToWorkspaceLocal(brush.gameObject, workspace.transform, brushOffset, brushRotationEuler);

            ApplyWorkspaceVisibilityLocal(brush.gameObject, visible);
        }
    }

    private bool TryBuildSpawnPoseBehindHud(Transform activeHead, out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;

        GameObject hudAnchor = FindSceneObjectByNameIncludingInactive(hudPanelName);

        if (hudAnchor == null)
            return false;

        Vector3 anchorPos = hudAnchor.transform.position;
        RectTransform rectTransform = hudAnchor.GetComponent<RectTransform>();
        if (rectTransform != null)
            anchorPos = GetRectTransformCenterWorld(rectTransform);

        Vector3 toUser = activeHead.position - anchorPos;
        Vector3 toUserFlat = Vector3.ProjectOnPlane(toUser, Vector3.up);

        if (toUserFlat.sqrMagnitude < 0.001f)
            toUserFlat = Vector3.ProjectOnPlane(-hudAnchor.transform.forward, Vector3.up);

        if (toUserFlat.sqrMagnitude < 0.001f)
            return false;

        toUserFlat.Normalize();

        pos = anchorPos - toUserFlat * workspaceBehindHudDistance;
        pos.y = anchorPos.y + workspaceBehindHudHeightOffset;
        rot = Quaternion.LookRotation(toUserFlat, Vector3.up);
        return true;
    }

    private static GameObject FindSceneObjectByNameIncludingInactive(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == null)
                continue;

            if (!t.gameObject.scene.IsValid())
                continue;

            if (t.name == objectName)
                return t.gameObject;
        }

        return null;
    }

    private static Vector3 GetRectTransformCenterWorld(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    private static void StripNestedNetworkObjectsForSpawn(GameObject workspaceRoot, NetworkObject rootNetworkObject)
    {
        if (workspaceRoot == null || rootNetworkObject == null)
            return;

        NetworkObject[] nestedNetworkObjects = workspaceRoot.GetComponentsInChildren<NetworkObject>(true);
        for (int i = 0; i < nestedNetworkObjects.Length; i++)
        {
            NetworkObject nested = nestedNetworkObjects[i];
            if (nested == null || nested == rootNetworkObject)
                continue;

            NetworkBehaviour[] nestedBehaviours = nested.GetComponents<NetworkBehaviour>();
            for (int b = 0; b < nestedBehaviours.Length; b++)
                Object.DestroyImmediate(nestedBehaviours[b]);

            Object.DestroyImmediate(nested);
        }
    }

    private static void ApplyWorkspacePlacementLocal(GameObject workspaceRoot, Vector3 pos, Quaternion rot, bool alignVisualCenterToTargetY)
    {
        if (workspaceRoot == null)
            return;

        workspaceRoot.transform.SetPositionAndRotation(pos, rot);

        if (!alignVisualCenterToTargetY)
            return;

        if (!TryGetVisualCenter(workspaceRoot, out Vector3 visualCenter))
            return;

        float yDelta = pos.y - visualCenter.y;
        if (Mathf.Abs(yDelta) > 0.0001f)
            workspaceRoot.transform.position += Vector3.up * yDelta;
    }

    private static bool TryGetVisualCenter(GameObject root, out Vector3 center)
    {
        center = Vector3.zero;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return false;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        center = bounds.center;
        return true;
    }

    private static NetworkObject FindClosestSpawnedBrush(Vector3 fromPosition)
    {
        BrushRespawnOnGrab[] brushes = Object.FindObjectsByType<BrushRespawnOnGrab>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        NetworkObject closest = null;
        float closestSqrDist = float.MaxValue;

        for (int i = 0; i < brushes.Length; i++)
        {
            if (brushes[i] == null)
                continue;

            NetworkObject netObj = brushes[i].GetComponent<NetworkObject>();
            if (netObj == null || !netObj.IsSpawned)
                continue;

            float sqrDist = (netObj.transform.position - fromPosition).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closest = netObj;
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    private static void ApplyBrushPlacementRelativeToWorkspaceLocal(GameObject brushObject, Transform workspaceTransform, Vector3 localOffset, Vector3 localEulerOffset)
    {
        if (brushObject == null || workspaceTransform == null)
            return;

        Vector3 brushWorldPos = workspaceTransform.TransformPoint(localOffset);
        Quaternion brushWorldRot = workspaceTransform.rotation * Quaternion.Euler(localEulerOffset);
        brushObject.transform.SetPositionAndRotation(brushWorldPos, brushWorldRot);
    }

    private static void ApplyWorkspaceVisibilityLocal(GameObject workspaceRoot, bool visible)
    {
        if (workspaceRoot == null)
            return;

        Renderer[] renderers = workspaceRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = visible;

        Canvas[] canvases = workspaceRoot.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = visible;

        Collider[] colliders = workspaceRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = visible;

        Collider2D[] colliders2D = workspaceRoot.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders2D.Length; i++)
            colliders2D[i].enabled = visible;
    }
}