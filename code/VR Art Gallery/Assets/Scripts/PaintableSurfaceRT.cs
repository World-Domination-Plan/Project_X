using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class PaintableSurfaceRT : MonoBehaviour
{
    public enum SurfaceMode
    {
        DisplayOnly,     // show whatever background is set, ignore painting input
        DrawOnBlank,     // clear canvas and allow drawing
        DrawOverImage    // show background image and allow drawing over it
    }

    [Header("Mode")]
    public SurfaceMode mode = SurfaceMode.DisplayOnly;

    [Header("Canvas")]
    public int resolution = 1024;
    public Color clearColor = Color.white;

    [Header("Brush")]
    public Material brushBlitMaterial;   // Material using shader "Hidden/BrushBlit"
    public Texture2D brushMask;          // optional (soft circle mask)
    [Range(0.001f, 0.25f)] public float radius = 0.03f;
    [Range(0f, 1f)] public float hardness = 0.7f;
    public Color brushColor = Color.black;

    [Header("Target")]
    public Renderer targetRenderer;

    RenderTexture _a, _b;
    MaterialPropertyBlock _mpb;

    static readonly int MainTex = Shader.PropertyToID("_MainTex");
    static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    static readonly int BrushTex = Shader.PropertyToID("_BrushTex");
    static readonly int BrushColorID = Shader.PropertyToID("_BrushColor");
    static readonly int BrushParams = Shader.PropertyToID("_BrushParams"); // u,v,r,h

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();

        _a = CreateRT(resolution);
        _b = CreateRT(resolution);

        ClearRT(_a, clearColor);
        ClearRT(_b, clearColor);

        ApplyToRenderer(_a);
    }

    RenderTexture CreateRT(int size)
    {
        var rt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.filterMode = FilterMode.Bilinear;
        rt.Create();
        return rt;
    }

    void ClearRT(RenderTexture rt, Color c)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, c);
        RenderTexture.active = prev;
    }

    void ApplyToRenderer(Texture tex)
    {
        targetRenderer.GetPropertyBlock(_mpb);

        _mpb.SetTexture(MainTex, tex);
        _mpb.SetTexture(BaseMap, tex);

        _mpb.SetColor(ColorId, Color.white);
        _mpb.SetColor(BaseColorId, Color.white);

        targetRenderer.SetPropertyBlock(_mpb);
    }

    // --- Public API for your existing "display image" flow ---

    public void SetBackground(Texture2D bg)
    {
        if (!bg)
        {
            // no background => blank
            ClearCanvas();
            return;
        }

        // seed both buffers from the image so ping-pong starts identical
        Graphics.Blit(bg, _a);
        Graphics.Blit(bg, _b);
        ApplyToRenderer(_a);
    }

    public void ClearCanvas()
    {
        ClearRT(_a, clearColor);
        ClearRT(_b, clearColor);
        ApplyToRenderer(_a);
    }

    public void SetMode(SurfaceMode newMode, Texture2D optionalBackground = null)
    {
        mode = newMode;

        if (mode == SurfaceMode.DrawOnBlank)
        {
            ClearCanvas();
        }
        else if (mode == SurfaceMode.DrawOverImage)
        {
            SetBackground(optionalBackground);
        }
        // DisplayOnly: keep whatever is currently on the RT
    }

    // --- Painting entry point (call from your VR ray/brush script) ---

    public bool TryPaintAt(Vector2 uv)
    {
        if (mode == SurfaceMode.DisplayOnly) return false;
        if (!brushBlitMaterial) return false;

        brushBlitMaterial.SetTexture(BrushTex, brushMask);
        brushBlitMaterial.SetColor(BrushColorID, brushColor);
        brushBlitMaterial.SetVector(BrushParams, new Vector4(uv.x, uv.y, radius, hardness));

        Graphics.Blit(_a, _b, brushBlitMaterial);
        (_a, _b) = (_b, _a);

        ApplyToRenderer(_a);
        return true;
    }
}
