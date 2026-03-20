using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ColorPicker : MonoBehaviour
{
    public delegate void ColorEvent(Color c);

    private static ColorPicker instance;
    public static bool done = true;

    private static ColorEvent onCC;
    private static ColorEvent onCS;

    private static Color32 originalColor;
    private static Color32 modifiedColor;
    private static HSV modifiedHsv;
    private static bool useA;

    private bool interact;

    [Header("Optional Panel Root")]
    [SerializeField] private GameObject panelRoot; // drag ColorPickerCanvasWS here

    [Header("Target Brush")]
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

    private void Awake()
    {
        instance = this;

        if (panelRoot == null)
        {
            if (transform.parent != null)
                panelRoot = transform.parent.gameObject;
            else
                panelRoot = gameObject;
        }
    }

    private void Start()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// Open picker for a specific brush.
    /// </summary>
    public static bool OpenForBrush(
        BrushToolState brush,
        string message = "Colour Picker",
        bool useAlpha = false,
        ColorEvent onColorChanged = null,
        ColorEvent onColorSelected = null)
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<ColorPicker>(FindObjectsInactive.Include);

            if (instance == null)
            {
                Debug.LogError("[ColorPicker] No active ColorPicker instance in scene.");
                return false;
            }
        }

        if (brush == null)
        {
            Debug.LogWarning("[ColorPicker] OpenForBrush called with null brush.");
            return false;
        }

        instance.targetBrush = brush;

        return Create(
            brush.SelectedColor,
            message,
            onColorChanged,
            onColorSelected,
            useAlpha
        );
    }

    /// <summary>
    /// Original static create API kept for compatibility.
    /// </summary>
    public static bool Create(
        Color original,
        string message,
        ColorEvent onColorChanged,
        ColorEvent onColorSelected,
        bool useAlpha = false)
    {
        if (instance == null)
        {
            Debug.LogError("[ColorPicker] No ColorPicker instance in scene.");
            return false;
        }

        if (!done)
        {
            Done();
            return false;
        }

        done = false;
        originalColor = original;
        modifiedColor = original;
        onCC = onColorChanged;
        onCS = onColorSelected;
        useA = useAlpha;

        if (instance.panelRoot != null)
            instance.panelRoot.SetActive(true);
        else
            instance.gameObject.SetActive(true);

        // Title text: ColorPicker_UI -> Title
        Transform title = instance.transform.Find("Title");
        if (title != null)
        {
            Text txt = title.GetComponent<Text>();
            if (txt != null) txt.text = message;
        }

        if (instance.aComponent != null)
            instance.aComponent.gameObject.SetActive(useAlpha);

        if (instance.positionIndicator == null ||
            instance.mainComponent == null ||
            instance.rComponent == null ||
            instance.gComponent == null ||
            instance.bComponent == null ||
            instance.hexaComponent == null ||
            instance.colorComponent == null)
        {
            Debug.LogError("[ColorPicker] Missing core UI references.");
            return false;
        }

        instance.RecalculateMenu(true);

        if (instance.hexaComponent != null && instance.hexaComponent.placeholder != null)
        {
            Text ph = instance.hexaComponent.placeholder.GetComponent<Text>();
            if (ph != null)
                ph.text = "RRGGBB" + (useAlpha ? "AA" : "");
        }

        return true;
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

        if (useA)
        {
            aComponent.value = modifiedColor.a;
            if (aInput != null) aInput.text = modifiedColor.a.ToString();
        }

        mainComponent.value = (float)modifiedHsv.H;

        if (rGradientMax != null) rGradientMax.color = new Color32(255, modifiedColor.g, modifiedColor.b, 255);
        if (rGradientMin != null) rGradientMin.color = new Color32(0,   modifiedColor.g, modifiedColor.b, 255);

        if (gGradientMax != null) gGradientMax.color = new Color32(modifiedColor.r, 255, modifiedColor.b, 255);
        if (gGradientMin != null) gGradientMin.color = new Color32(modifiedColor.r, 0,   modifiedColor.b, 255);

        if (bGradientMax != null) bGradientMax.color = new Color32(modifiedColor.r, modifiedColor.g, 255, 255);
        if (bGradientMin != null) bGradientMin.color = new Color32(modifiedColor.r, modifiedColor.g, 0,   255);

        if (useA && aGradientFill != null)
            aGradientFill.color = new Color32(modifiedColor.r, modifiedColor.g, modifiedColor.b, 255);

        positionIndicator.parent.GetChild(0).GetComponent<RawImage>().color =
            new HSV(modifiedHsv.H, 1d, 1d).ToColor();

        positionIndicator.anchorMin = new Vector2((float)modifiedHsv.S, (float)modifiedHsv.V);
        positionIndicator.anchorMax = positionIndicator.anchorMin;

        hexaComponent.text = useA
            ? ColorUtility.ToHtmlStringRGBA(modifiedColor)
            : ColorUtility.ToHtmlStringRGB(modifiedColor);

        colorComponent.color = modifiedColor;

        // live callback
        onCC?.Invoke(modifiedColor);

        // live update brush
        if (targetBrush != null)
            targetBrush.SetColor(modifiedColor);

        interact = true;
    }

    /// <summary>
    /// For mouse + XR ray.
    /// Bind this to EventTrigger PointerDown + Drag on the SV square.
    /// </summary>
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
            if (!useA) c.a = 1f;
            modifiedColor = c;
            RecalculateMenu(true);
        }
        else
        {
            hexaComponent.text = useA
                ? ColorUtility.ToHtmlStringRGBA(modifiedColor)
                : ColorUtility.ToHtmlStringRGB(modifiedColor);
        }
    }

    public void CCancel() => Cancel();
    public void CDone() => Done();

    public static void Cancel()
    {
        modifiedColor = originalColor;

        if (instance != null && instance.targetBrush != null)
            instance.targetBrush.SetColor(originalColor);

        Done();
    }

    public static void Done()
    {
        done = true;

        onCC?.Invoke(modifiedColor);
        onCS?.Invoke(modifiedColor);

        if (instance != null && instance.targetBrush != null)
            instance.targetBrush.SetColor(modifiedColor);

        if (instance != null)
        {
            if (instance.panelRoot != null)
                instance.panelRoot.SetActive(false);
            else
                instance.gameObject.SetActive(false);
        }
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

            float hue = (float)H;
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