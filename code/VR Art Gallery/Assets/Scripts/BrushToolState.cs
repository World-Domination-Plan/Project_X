using System;
using UnityEngine;

public class BrushToolState : MonoBehaviour
{
    [Header("Brush Settings")]
    [SerializeField] private Color selectedColor = Color.black;
    [SerializeField, Range(0.001f, 10f)] private float radius = 0.03f;
    [SerializeField, Range(0f, 1f)] private float hardness = 0.7f;

    [Header("Optional Visual Preview")]
    [SerializeField] private Renderer brushTipRenderer;
    [SerializeField] private string colorProperty = "_BaseColor";

    [Header("Preset Colors")]
    [SerializeField] private Color[] presetColors =
    {
        Color.black,
        Color.white,
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.cyan,
        Color.magenta
    };

    private int currentPresetIndex = 0;

    public event Action<Color> OnColorChanged;

    public Color SelectedColor => selectedColor;
    public float Radius => radius;
    public float Hardness => hardness;

    public BrushPaintSettings CurrentPaintSettings => new BrushPaintSettings(selectedColor, radius, hardness);
    public BrushState CurrentBrushState => new BrushState(selectedColor, radius, hardness);

    private void Awake()
    {
        ApplyPreviewColor(selectedColor);
    }

    public void SetColor(Color c)
    {
        selectedColor = c;
        ApplyPreviewColor(c);
        OnColorChanged?.Invoke(c);
    }

    public void SetColorByIndex(int index)
    {
        if (presetColors == null || presetColors.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, presetColors.Length - 1);
        currentPresetIndex = index;
        SetColor(presetColors[currentPresetIndex]);
    }

    public void NextPresetColor()
    {
        if (presetColors == null || presetColors.Length == 0)
            return;

        currentPresetIndex = (currentPresetIndex + 1) % presetColors.Length;
        SetColor(presetColors[currentPresetIndex]);
    }

    public void PreviousPresetColor()
    {
        if (presetColors == null || presetColors.Length == 0)
            return;

        currentPresetIndex--;
        if (currentPresetIndex < 0)
            currentPresetIndex = presetColors.Length - 1;

        SetColor(presetColors[currentPresetIndex]);
    }

    public void SetColorFromHtml(string html)
    {
        if (ColorUtility.TryParseHtmlString(html, out Color parsed))
            SetColor(parsed);
    }

    public void SetRadius(float value)
    {
        radius = Mathf.Clamp(value, 0.001f, 0.25f);
    }

    public void SetHardness(float value)
    {
        hardness = Mathf.Clamp01(value);
    }

    public void SetBlack()   => SetColor(Color.black);
    public void SetWhite()   => SetColor(Color.white);
    public void SetRed()     => SetColor(Color.red);
    public void SetGreen()   => SetColor(Color.green);
    public void SetBlue()    => SetColor(Color.blue);
    public void SetYellow()  => SetColor(Color.yellow);
    public void SetCyan()    => SetColor(Color.cyan);
    public void SetMagenta() => SetColor(Color.magenta);

    private void ApplyPreviewColor(Color c)
    {
        if (!brushTipRenderer) return;

        Material mat = brushTipRenderer.material;
        if (mat.HasProperty(colorProperty))
            mat.SetColor(colorProperty, c);
    }
}