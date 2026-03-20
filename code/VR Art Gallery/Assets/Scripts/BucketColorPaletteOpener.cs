using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BucketColorPaletteOpener : MonoBehaviour
{
    [Header("UI")]
    public GameObject colorPickerUI;      // drag ColorPickerCanvasWS here
    public Transform paletteAnchor;       // drag ColorPickerAnchor here

    [Header("Message")]
    public string pickerTitle = "Colour Picker";
    public bool useAlpha = false;

    [Header("Target Brush (optional)")]
    public BrushToolState targetBrush;    // optional manual reference; auto-finds if left empty

    private XRSimpleInteractable simpleInteractable;
    private ColorPicker cachedPicker;

    void Awake()
    {
        simpleInteractable = GetComponent<XRSimpleInteractable>();

        if (colorPickerUI != null)
        {
            colorPickerUI.SetActive(false);
            cachedPicker = colorPickerUI.GetComponentInChildren<ColorPicker>(true);
        }
    }

    void OnEnable()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.hoverEntered.AddListener(OnHoverEntered);
            simpleInteractable.selectEntered.AddListener(OnSelected);
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
        ToggleOrShowPalette();
    }

    private void OnActivated(ActivateEventArgs args)
    {
        Debug.Log("[Bucket] XR activated");
        ToggleOrShowPalette();
    }

    void OnMouseDown()
    {
        Debug.Log("[Bucket] Mouse clicked");
        ToggleOrShowPalette();
    }

    void ToggleOrShowPalette()
    {
        if (colorPickerUI == null)
        {
            Debug.LogWarning("[Bucket] colorPickerUI is null");
            return;
        }

        if (paletteAnchor != null)
        {
            colorPickerUI.transform.position = paletteAnchor.position;
            colorPickerUI.transform.rotation = paletteAnchor.rotation;
        }
        else
        {
            Debug.LogWarning("[Bucket] paletteAnchor is null");
        }

        if (cachedPicker == null)
            cachedPicker = colorPickerUI.GetComponentInChildren<ColorPicker>(true);

        if (cachedPicker == null)
        {
            Debug.LogWarning("[Bucket] No ColorPicker found under colorPickerUI");
            return;
        }

        BrushToolState brush = targetBrush;
        if (brush == null)
            brush = FindFirstObjectByType<BrushToolState>();

        if (brush == null)
        {
            Debug.LogWarning("[Bucket] No BrushToolState found in scene");
            return;
        }

        // already open -> close
        if (colorPickerUI.activeSelf && !ColorPicker.done)
        {
            ColorPicker.Done();
            Debug.Log("[Bucket] Palette closed");
            return;
        }

        bool opened = ColorPicker.OpenForBrush(
            brush,
            pickerTitle,
            useAlpha,
            c => { },   // optional live callback
            c => { }    // optional final callback
        );

        Debug.Log("[Bucket] Palette open requested = " + opened);
    }

    public void ClosePalette()
    {
        if (!ColorPicker.done)
            ColorPicker.Done();
        else if (colorPickerUI != null)
            colorPickerUI.SetActive(false);
    }
}