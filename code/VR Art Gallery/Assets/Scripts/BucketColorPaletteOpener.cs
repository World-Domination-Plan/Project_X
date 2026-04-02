using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BucketColorPaletteOpener : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Assign the OUTERMOST root of the picker you want to move/show/hide. In your current hierarchy this should be the top-level 'Color Picker' object, not CanvasRoot and not ColorPicker_UI.")]
    public GameObject colorPickerUI;

    [Tooltip("World-space anchor where the picker should appear.")]
    public Transform paletteAnchor;

    [Header("Message")]
    public string pickerTitle = "Colour Picker";
    public bool useAlpha = false;

    [Header("Target Brush (optional)")]
    [Tooltip("Optional manual brush reference. If left empty, the script will find the first BrushToolState in the scene.")]
    public BrushToolState targetBrush;

    [Header("Input")]
    [Tooltip("Prevents select/activate from double-toggling on the same click.")]
    [Range(0.05f, 0.5f)]
    public float toggleDebounceSeconds = 0.15f;

    private XRSimpleInteractable simpleInteractable;
    private ColorPicker cachedPicker;
    private float lastToggleTime = -999f;
    private Coroutine openRoutine;

    void Awake()
    {
        simpleInteractable = GetComponent<XRSimpleInteractable>();

        CachePicker();

        // Start hidden.
        if (colorPickerUI != null)
            colorPickerUI.SetActive(false);
    }

    void OnEnable()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.hoverEntered.AddListener(OnHoverEntered);
            simpleInteractable.selectEntered.AddListener(OnSelected);

            // Keep this only if you really need Activate separately.
            // Debounce below prevents double toggles if both fire.
            simpleInteractable.activated.AddListener(OnActivated);
        }
    }

    void OnDisable()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            simpleInteractable.selectEntered.RemoveListener(OnSelected);
            simpleInteractable.activated.RemoveListener(OnActivated);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log("[Bucket] Hover entered");
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        Debug.Log("[Bucket] XR selected");
        RequestToggle();
    }

    private void OnActivated(ActivateEventArgs args)
    {
        Debug.Log("[Bucket] XR activated");
        RequestToggle();
    }

    void OnMouseDown()
    {
        Debug.Log("[Bucket] Mouse clicked");
        RequestToggle();
    }

    private void RequestToggle()
    {
        if (Time.unscaledTime - lastToggleTime < toggleDebounceSeconds)
            return;

        lastToggleTime = Time.unscaledTime;
        ToggleOrShowPalette();
    }

    private void ToggleOrShowPalette()
    {
        if (colorPickerUI == null)
        {
            Debug.LogWarning("[Bucket] colorPickerUI is null");
            return;
        }

        // Close if already open.
        if (IsPaletteVisible())
        {
            ClosePalette();
            Debug.Log("[Bucket] Palette closed");
            return;
        }

        if (openRoutine != null)
            StopCoroutine(openRoutine);

        openRoutine = StartCoroutine(OpenPaletteRoutine());
    }

    private IEnumerator OpenPaletteRoutine()
    {
        // Move the OUTER ROOT before showing it.
        if (paletteAnchor != null)
        {
            colorPickerUI.transform.position = paletteAnchor.position;
            colorPickerUI.transform.rotation = paletteAnchor.rotation;
        }
        else
        {
            Debug.LogWarning("[Bucket] paletteAnchor is null");
        }

        // IMPORTANT:
        // Enable the OUTER ROOT first, otherwise the child picker can be active
        // but still invisible because the parent hierarchy is disabled.
        if (!colorPickerUI.activeSelf)
            colorPickerUI.SetActive(true);

        // Let Awake/OnEnable run on the newly-enabled hierarchy.
        yield return null;

        CachePicker();

        if (cachedPicker == null)
        {
            Debug.LogWarning("[Bucket] No ColorPicker found under colorPickerUI");
            colorPickerUI.SetActive(false);
            openRoutine = null;
            yield break;
        }

        // Make sure the actual picker GameObject itself is active too.
        if (!cachedPicker.gameObject.activeSelf)
            cachedPicker.gameObject.SetActive(true);

        BrushToolState brush = targetBrush;
        if (brush == null)
            brush = FindFirstObjectByType<BrushToolState>();

        if (brush == null)
        {
            Debug.LogWarning("[Bucket] No BrushToolState found in scene");
            colorPickerUI.SetActive(false);
            openRoutine = null;
            yield break;
        }

        bool opened = ColorPicker.OpenForBrush(
            brush,
            pickerTitle,
            useAlpha,
            c => { },   // optional live callback
            c => { }    // optional final callback
        );

        Debug.Log("[Bucket] Palette open requested = " + opened);
        Debug.Log("[Bucket] Root activeSelf = " + colorPickerUI.activeSelf);
        Debug.Log("[Bucket] Root activeInHierarchy = " + colorPickerUI.activeInHierarchy);
        Debug.Log("[Bucket] Picker object = " + cachedPicker.gameObject.name);
        Debug.Log("[Bucket] Picker activeSelf = " + cachedPicker.gameObject.activeSelf);
        Debug.Log("[Bucket] Picker activeInHierarchy = " + cachedPicker.gameObject.activeInHierarchy);

        // If the static API says it didn't open, hide the root again.
        if (!opened)
            colorPickerUI.SetActive(false);

        openRoutine = null;
    }

    public void ClosePalette()
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        if (!ColorPicker.done)
            ColorPicker.Done();

        if (colorPickerUI != null)
            colorPickerUI.SetActive(false);
    }

    private bool IsPaletteVisible()
    {
        return colorPickerUI != null
               && colorPickerUI.activeInHierarchy
               && !ColorPicker.done;
    }

    private void CachePicker()
    {
        if (colorPickerUI == null)
        {
            cachedPicker = null;
            return;
        }

        cachedPicker = colorPickerUI.GetComponentInChildren<ColorPicker>(true);
    }
}