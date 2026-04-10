using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ColorPicker : MonoBehaviour
{
    public delegate void ColorEvent(Color c);

    [Header("Optional Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Target Brush (optional)")]
    [SerializeField] private BrushToolState targetBrush;

    [Header("UI Refs")]
    public RectTransform positionIndicator;
    public Slider mainComponent;
    public Slider rComponent;
    public Slider gComponent;
    public Slider bComponent;
    public Slider aComponent;
    public InputField hexaComponent;
    public RawImage colorComponent;

    [Header("Per-channel UI Refs")]
    public InputField rInput;
    public InputField gInput;
    public InputField bInput;
    public InputField aInput;

    public RawImage rGradientMax;
    public RawImage rGradientMin;
    public RawImage gGradientMax;
    public RawImage gGradientMin;
    public RawImage bGradientMax;
    public RawImage bGradientMin;
    public RawImage aGradientFill;

    public bool IsOpen => isOpen;

    private bool isOpen;
    private bool interact;
    private bool useAlpha;

    private ColorEvent onColorChanged;
    private ColorEvent onColorSelected;

    private Color32 originalColor;
    private Color32 modifiedColor;
    private HSV modifiedHsv;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;
    }

    private void Start()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        isOpen = false;
    }

    public bool OpenForSharedSettings(
        string message = "Colour Picker",
        bool useAlpha = true,
        ColorEvent onChanged = null,
        ColorEvent onSelected = null)
    {
        if (SharedBrushSettings.Instance == null)
        {
            Debug.LogWarning("[ColorPicker] SharedBrushSettings.Instance is null.");
            return false;
        }

        targetBrush = null;

        return OpenInternal(
            SharedBrushSettings.Instance.CurrentColor,
            message,
            useAlpha,
            onChanged,
            onSelected
        );
    }

    public bool OpenForBrush(
        BrushToolState brush,
        string message = "Colour Picker",
        bool useAlpha = true,
        ColorEvent onChanged = null,
        ColorEvent onSelected = null)
    {
        if (brush == null)
        {
            Debug.LogWarning("[ColorPicker] OpenForBrush called with null brush.");
            return false;
        }

        targetBrush = brush;

        return OpenInternal(
            brush.SelectedColor,
            message,
            useAlpha,
            onChanged,
            onSelected
        );
    }

    private bool OpenInternal(
        Color original,
        string message,
        bool alphaEnabled,
        ColorEvent onChanged,
        ColorEvent onSelected)
    {
        if (panelRoot == null)
        {
            Debug.LogError("[ColorPicker] panelRoot is null.");
            return false;
        }

        if (isOpen)
            return false;

        if (positionIndicator == null ||
            mainComponent == null ||
            rComponent == null ||
            gComponent == null ||
            bComponent == null ||
            hexaComponent == null ||
            colorComponent == null)
        {
            Debug.LogError("[ColorPicker] Missing core UI references.");
            return false;
        }

        isOpen = true;
        useAlpha = alphaEnabled;
        originalColor = original;
        modifiedColor = original;
        onColorChanged = onChanged;
        onColorSelected = onSelected;

        panelRoot.SetActive(true);
        gameObject.SetActive(true);

        Canvas.ForceUpdateCanvases();

        Text titleText = FindTextByName(panelRoot.transform, "Title");
        if (titleText != null)
            titleText.text = message;

        if (aComponent != null)
            aComponent.gameObject.SetActive(useAlpha);

        if (aInput != null)
            aInput.gameObject.SetActive(useAlpha);

        if (aGradientFill != null)
            aGradientFill.gameObject.SetActive(useAlpha);

        if (hexaComponent.placeholder != null)
        {
            Text ph = hexaComponent.placeholder.GetComponent<Text>();
            if (ph != null)
                ph.text = useAlpha ? "RRGGBBAA" : "RRGGBB";
        }

        RecalculateMenu(true);
        return true;
    }

    public void ClosePicker(bool applyCurrentColor = true)
    {
        if (!isOpen)
            return;

        if (!applyCurrentColor)
            ApplyColorToTarget(originalColor);
        else
            ApplyColorToTarget(modifiedColor);

        if (applyCurrentColor)
        {
            onColorChanged?.Invoke(modifiedColor);
            onColorSelected?.Invoke(modifiedColor);
        }

        isOpen = false;
        interact = false;
        onColorChanged = null;
        onColorSelected = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void CCancel()
    {
        ClosePicker(false);
    }

    public void CDone()
    {
        ClosePicker(true);
    }

    private void ApplyColorToTarget(Color color)
    {
        Debug.Log($"[ColorPicker] ApplyColorToTarget alpha = {color.a}");
        if (targetBrush != null)
            targetBrush.SetColor(color);
        else if (SharedBrushSettings.Instance != null)
            SharedBrushSettings.Instance.SetColor(color);
    }

    private void RecalculateMenu(bool recalculateHSV)
    {
        interact = false;

        if (recalculateHSV)
            modifiedHsv = new HSV(modifiedColor);
        else
            modifiedColor = modifiedHsv.ToColor();

        rComponent.value = modifiedColor.r;
        if (rInput != null) rInput.text = modifiedColor.r.ToString();

        gComponent.value = modifiedColor.g;
        if (gInput != null) gInput.text = modifiedColor.g.ToString();

        bComponent.value = modifiedColor.b;
        if (bInput != null) bInput.text = modifiedColor.b.ToString();

        if (useAlpha && aComponent != null)
        {
            aComponent.value = modifiedColor.a;
            if (aInput != null) aInput.text = modifiedColor.a.ToString();
        }

        mainComponent.value = (float)modifiedHsv.H;

        if (rGradientMax != null) rGradientMax.color = new Color32(255, modifiedColor.g, modifiedColor.b, 255);
        if (rGradientMin != null) rGradientMin.color = new Color32(0, modifiedColor.g, modifiedColor.b, 255);

        if (gGradientMax != null) gGradientMax.color = new Color32(modifiedColor.r, 255, modifiedColor.b, 255);
        if (gGradientMin != null) gGradientMin.color = new Color32(modifiedColor.r, 0, modifiedColor.b, 255);

        if (bGradientMax != null) bGradientMax.color = new Color32(modifiedColor.r, modifiedColor.g, 255, 255);
        if (bGradientMin != null) bGradientMin.color = new Color32(modifiedColor.r, modifiedColor.g, 0, 255);

        if (useAlpha && aGradientFill != null)
            aGradientFill.color = new Color32(modifiedColor.r, modifiedColor.g, modifiedColor.b, 255);

        if (positionIndicator != null && positionIndicator.parent != null && positionIndicator.parent.childCount > 0)
        {
            RawImage svBg = positionIndicator.parent.GetChild(0).GetComponent<RawImage>();
            if (svBg != null)
                svBg.color = new HSV(modifiedHsv.H, 1d, 1d).ToColor();
        }

        positionIndicator.anchorMin = new Vector2((float)modifiedHsv.S, (float)modifiedHsv.V);
        positionIndicator.anchorMax = positionIndicator.anchorMin;

        hexaComponent.text = useAlpha
            ? ColorUtility.ToHtmlStringRGBA(modifiedColor)
            : ColorUtility.ToHtmlStringRGB(modifiedColor);

        colorComponent.color = modifiedColor;

        onColorChanged?.Invoke(modifiedColor);
        ApplyColorToTarget(modifiedColor);

        interact = true;
    }

    public void SetChooserFromEvent(BaseEventData eventData)
    {
        if (!interact) return;

        PointerEventData ped = eventData as PointerEventData;
        if (ped == null) return;

        RectTransform rt = positionIndicator.parent as RectTransform;
        Camera eventCam = ped.pressEventCamera != null ? ped.pressEventCamera : ped.enterEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, ped.position, eventCam, out Vector2 localPoint))
        {
            Vector2 normalized = Rect.PointToNormalized(rt.rect, localPoint);
            normalized.x = Mathf.Clamp01(normalized.x);
            normalized.y = Mathf.Clamp01(normalized.y);

            if (positionIndicator.anchorMin != normalized)
            {
                positionIndicator.anchorMin = normalized;
                positionIndicator.anchorMax = normalized;
                modifiedHsv.S = normalized.x;
                modifiedHsv.V = normalized.y;
                RecalculateMenu(false);
            }
        }
    }

    public void SetMain(float value)
    {
        if (!interact) return;
        modifiedHsv.H = value;
        RecalculateMenu(false);
    }

    public void SetR(float value)
    {
        if (!interact) return;
        modifiedColor.r = (byte)value;
        RecalculateMenu(true);
    }

    public void SetR(string value)
    {
        if (!interact) return;
        if (int.TryParse(value, out int parsed))
        {
            modifiedColor.r = (byte)Mathf.Clamp(parsed, 0, 255);
            RecalculateMenu(true);
        }
    }

    public void SetG(float value)
    {
        if (!interact) return;
        modifiedColor.g = (byte)value;
        RecalculateMenu(true);
    }

    public void SetG(string value)
    {
        if (!interact) return;
        if (int.TryParse(value, out int parsed))
        {
            modifiedColor.g = (byte)Mathf.Clamp(parsed, 0, 255);
            RecalculateMenu(true);
        }
    }

    public void SetB(float value)
    {
        if (!interact) return;
        modifiedColor.b = (byte)value;
        RecalculateMenu(true);
    }

    public void SetB(string value)
    {
        if (!interact) return;
        if (int.TryParse(value, out int parsed))
        {
            modifiedColor.b = (byte)Mathf.Clamp(parsed, 0, 255);
            RecalculateMenu(true);
        }
    }

    public void SetA(float value)
    {
        if (!interact) return;
        modifiedHsv.A = (byte)value;
        RecalculateMenu(false);
    }

    public void SetA(string value)
    {
        if (!interact) return;
        if (int.TryParse(value, out int parsed))
        {
            modifiedHsv.A = (byte)Mathf.Clamp(parsed, 0, 255);
            RecalculateMenu(false);
        }
    }

    public void SetHexa(string value)
    {
        if (!interact) return;

        if (ColorUtility.TryParseHtmlString("#" + value, out Color c))
        {
            if (!useAlpha) c.a = 1f;
            modifiedColor = c;
            RecalculateMenu(true);
        }
        else
        {
            hexaComponent.text = useAlpha
                ? ColorUtility.ToHtmlStringRGBA(modifiedColor)
                : ColorUtility.ToHtmlStringRGB(modifiedColor);
        }
    }

    private static Text FindTextByName(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
                return t.GetComponent<Text>();
        }
        return null;
    }

    private sealed class HSV
    {
        public double H = 0, S = 1, V = 1;
        public byte A = 255;

        public HSV() { }

        public HSV(double h, double s, double v)
        {
            H = h;
            S = s;
            V = v;
        }

        public HSV(Color color)
        {
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));

            float hue = 0f;
            if (min != max)
            {
                if (max == color.r)
                    hue = (color.g - color.b) / (max - min);
                else if (max == color.g)
                    hue = 2f + (color.b - color.r) / (max - min);
                else
                    hue = 4f + (color.r - color.g) / (max - min);

                hue *= 60f;
                if (hue < 0) hue += 360f;
            }

            H = hue;
            S = (max == 0) ? 0 : 1d - ((double)min / max);
            V = max;
            A = (byte)(color.a * 255);
        }

        public Color32 ToColor()
        {
            int hi = Convert.ToInt32(Math.Floor(H / 60)) % 6;
            double f = H / 60 - Math.Floor(H / 60);

            double value = V * 255;
            byte v = (byte)Convert.ToInt32(value);
            byte p = (byte)Convert.ToInt32(value * (1 - S));
            byte q = (byte)Convert.ToInt32(value * (1 - f * S));
            byte t = (byte)Convert.ToInt32(value * (1 - (1 - f) * S));

            switch (hi)
            {
                case 0: return new Color32(v, t, p, A);
                case 1: return new Color32(q, v, p, A);
                case 2: return new Color32(p, v, t, A);
                case 3: return new Color32(p, q, v, A);
                case 4: return new Color32(t, p, v, A);
                case 5: return new Color32(v, p, q, A);
                default: return new Color32();
            }
        }
    }
}