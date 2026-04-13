using UnityEngine;
using UnityEngine.InputSystem;
using XRMultiplayer;

public class HUDSpawner : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference vrSpawnButton;      // VR controller binding
    public InputActionReference desktopSpawnButton; // Keyboard/mouse binding

    [Header("HUD")]
    public GameObject hudObject;
    public string hudPanelName = "Gallery Selection UI";
    public Transform headCamera;
    public float spawnDistance = 1.0f;
    public float spawnHeightOffset = 1.0f;

    [Header("Spawn Comfort")]
    public float minVisibleDistance = 1.0f;
    public float maxVisibleDistance = 1.5f;
    public float minVisibleHeightOffset = 1f;
    public float maxVisibleHeightOffset = 1.5f;
    public bool keepPanelLevel = false;
    public Vector3 panelRotationOffsetEuler = new Vector3(0f, 180f, 0f);

    private InputAction m_RuntimeVrAction;
    private InputAction m_RuntimeDesktopAction;
    private bool m_WarnedMissingHud;
    private bool m_WarnedMissingHudPanel;
    private GameObject m_HudPanelObject;

    private void OnEnable()
    {
        SubscribeAction(GetOrCreateVrAction());
        SubscribeAction(GetOrCreateDesktopAction());
    }

    private void OnDisable()
    {
        UnsubscribeAction(GetOrCreateVrAction());
        UnsubscribeAction(GetOrCreateDesktopAction());
    }

    private void OnDestroy()
    {
        m_RuntimeVrAction?.Dispose();
        m_RuntimeDesktopAction?.Dispose();
    }

    private InputAction GetOrCreateVrAction()
    {
        if (vrSpawnButton != null && vrSpawnButton.action != null)
            return vrSpawnButton.action;

        if (m_RuntimeVrAction == null)
            m_RuntimeVrAction = new InputAction("ToggleHUD_VR", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");

        return m_RuntimeVrAction;
    }

    private InputAction GetOrCreateDesktopAction()
    {
        if (desktopSpawnButton != null && desktopSpawnButton.action != null)
            return desktopSpawnButton.action;

        if (m_RuntimeDesktopAction == null)
            m_RuntimeDesktopAction = new InputAction("ToggleHUD_Desktop", InputActionType.Button, "<Keyboard>/slash");

        return m_RuntimeDesktopAction;
    }

    private void SubscribeAction(InputAction action)
    {
        if (action == null)
            return;

        action.performed -= OnTogglePressed;
        action.performed += OnTogglePressed;
        action.Enable();
    }

    private void UnsubscribeAction(InputAction action)
    {
        if (action == null)
            return;

        action.performed -= OnTogglePressed;

        if (action == m_RuntimeVrAction || action == m_RuntimeDesktopAction)
            action.Disable();
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        if (hudObject == null)
            hudObject = GameObject.Find("World Space Canvas");

        if (hudObject == null)
        {
            if (!m_WarnedMissingHud)
            {
                Debug.LogWarning("[HUDSpawner] No HUD object assigned. Assign hudObject in inspector or ensure 'World Space Canvas' exists.");
                m_WarnedMissingHud = true;
            }
            return;
        }

        GameObject panelToToggle = ResolveHudPanelObject();
        if (panelToToggle == null)
        {
            if (!m_WarnedMissingHudPanel)
            {
                Debug.LogWarning($"[HUDSpawner] Could not find HUD panel '{hudPanelName}' under '{hudObject.name}'.");
                m_WarnedMissingHudPanel = true;
            }
            return;
        }

        bool show = !panelToToggle.activeSelf;

        if (show)
        {
            Transform currentHead = ResolveCurrentHeadCamera();

            if (currentHead == null)
                return;

            float clampedDistance = Mathf.Clamp(spawnDistance, minVisibleDistance, maxVisibleDistance);
            float clampedHeightOffset = Mathf.Clamp(spawnHeightOffset, minVisibleHeightOffset, maxVisibleHeightOffset);

            Vector3 forward = keepPanelLevel
                ? Vector3.ProjectOnPlane(currentHead.forward, Vector3.up)
                : currentHead.forward;

            if (forward.sqrMagnitude < 0.0001f)
                forward = currentHead.forward;

            forward.Normalize();

            Vector3 upAxis = keepPanelLevel ? Vector3.up : currentHead.up;
            Vector3 pos = currentHead.position + forward * clampedDistance + upAxis * clampedHeightOffset;

            Vector3 lookDirection = keepPanelLevel
                ? Vector3.ProjectOnPlane(currentHead.position - pos, Vector3.up)
                : (currentHead.position - pos);

            if (lookDirection.sqrMagnitude < 0.0001f)
                lookDirection = -forward;

            Vector3 lookUp = keepPanelLevel ? Vector3.up : currentHead.up;
            Quaternion baseRotation = Quaternion.LookRotation(lookDirection.normalized, lookUp);
            Quaternion finalRotation = baseRotation * Quaternion.Euler(panelRotationOffsetEuler);

            panelToToggle.transform.SetPositionAndRotation(pos, finalRotation);
        }

        panelToToggle.SetActive(show);
    }

    private GameObject ResolveHudPanelObject()
    {
        if (m_HudPanelObject != null)
            return m_HudPanelObject;

        if (hudObject == null)
            return null;

        if (string.IsNullOrWhiteSpace(hudPanelName))
        {
            m_HudPanelObject = hudObject;
            return m_HudPanelObject;
        }

        GameObject globalMatch = GameObject.Find(hudPanelName);
        if (globalMatch != null)
        {
            m_HudPanelObject = globalMatch;
            return m_HudPanelObject;
        }

        if (hudObject.name == hudPanelName)
        {
            m_HudPanelObject = hudObject;
            return m_HudPanelObject;
        }

        Transform[] descendants = hudObject.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            if (descendants[i].name == hudPanelName)
            {
                m_HudPanelObject = descendants[i].gameObject;
                return m_HudPanelObject;
            }
        }

        return null;
    }

    private Transform ResolveCurrentHeadCamera()
    {
        if (Camera.main != null)
        {
            headCamera = Camera.main.transform;
            return headCamera;
        }

        return headCamera;
    }
}