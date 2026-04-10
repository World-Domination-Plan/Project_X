using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BucketColorPaletteOpener : MonoBehaviour
{
    [Header("UI")]
    public GameObject colorPickerUI;
    public Transform paletteAnchor;

    [Header("Message")]
    public string pickerTitle = "Colour Picker";
    public bool useAlpha = false;

    [Header("Input")]
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

        if (colorPickerUI != null)
            colorPickerUI.SetActive(false);
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

        CachePicker();

        if (cachedPicker != null && cachedPicker.IsOpen)
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
        if (paletteAnchor != null)
        {
            colorPickerUI.transform.position = paletteAnchor.position;
            colorPickerUI.transform.rotation = paletteAnchor.rotation;
        }
        else
        {
            Debug.LogWarning("[Bucket] paletteAnchor is null");
        }

        if (!colorPickerUI.activeSelf)
            colorPickerUI.SetActive(true);

        yield return null;

        CachePicker();

        if (cachedPicker == null)
        {
            Debug.LogWarning("[Bucket] No ColorPicker found under colorPickerUI");
            colorPickerUI.SetActive(false);
            openRoutine = null;
            yield break;
        }

        if (!cachedPicker.gameObject.activeSelf)
            cachedPicker.gameObject.SetActive(true);

        if (SharedBrushSettings.Instance == null)
        {
            Debug.LogWarning("[Bucket] SharedBrushSettings.Instance is null");
            colorPickerUI.SetActive(false);
            openRoutine = null;
            yield break;
        }

        bool opened = cachedPicker.OpenForSharedSettings(
            pickerTitle,
            useAlpha,
            c => { },
            c => { }
        );

        Debug.Log("[Bucket] Palette open requested = " + opened);

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

        CachePicker();

        if (cachedPicker != null && cachedPicker.IsOpen)
            cachedPicker.ClosePicker(true);

        if (colorPickerUI != null)
            colorPickerUI.SetActive(false);
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