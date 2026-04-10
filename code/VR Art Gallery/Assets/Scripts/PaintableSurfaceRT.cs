using UnityEngine;
using System.IO;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class PaintableSurfaceRT : MonoBehaviour
{
    public enum SurfaceMode
    {
        DisplayOnly,
        DrawOnBlank,
        DrawOverImage
    }

    [SerializeField] bool forceUniqueMaterialInstance = true;
    [SerializeField] bool reapplyInLateUpdate = true;

    Material _displayMat;

    [Header("Mode")]
    public SurfaceMode mode = SurfaceMode.DisplayOnly;

    [Header("Canvas")]
    public int resolution = 1024;
    public Color clearColor = Color.magenta;

    [Header("Brush")]
    public Material brushBlitMaterial;
    public Texture2D brushMask;

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
    static readonly int BrushParams = Shader.PropertyToID("_BrushParams");

    [Header("Autosave")]
    public bool enableAutoSave = true;
    public float autoSaveInterval = 10f;

    float _autoSaveTimer;

    void Update()
    {
        if (!enableAutoSave || _a == null) return;

        _autoSaveTimer += Time.deltaTime;
        if (_autoSaveTimer >= autoSaveInterval)
        {
            _autoSaveTimer = 0.0f;
            string fileName = $"painting_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            SaveCanvasToPNG(fileName);
        }
    }

    public void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();

        if (forceUniqueMaterialInstance)
        {
            _displayMat = new Material(targetRenderer.sharedMaterial);
            targetRenderer.material = _displayMat;
        }
        else
        {
            _displayMat = targetRenderer.material;
        }

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
        if (!targetRenderer || !_displayMat) return;

        _displayMat.SetTexture(MainTex, tex);
        _displayMat.SetTexture(BaseMap, tex);
        _displayMat.SetTextureScale("_BaseMap", Vector2.one);
        _displayMat.SetTextureOffset("_BaseMap", Vector2.zero);
        _displayMat.SetColor(ColorId, Color.white);
        _displayMat.SetColor(BaseColorId, Color.white);
    }

    public void SetBackground(Texture2D bg)
    {
        if (!bg)
        {
            ClearCanvas();
            return;
        }

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
            ClearCanvas();
        else if (mode == SurfaceMode.DrawOverImage)
            SetBackground(optionalBackground);
    }

    public bool PaintAt(Vector2 uv, BrushState brush)
    {
        if (mode == SurfaceMode.DisplayOnly) return false;
        if (!brushBlitMaterial) return false;

        brushBlitMaterial.SetTexture(BrushTex, brushMask ? brushMask : Texture2D.whiteTexture);
        Debug.Log($"[PaintAt] brush alpha = {brush.color.a}");
        brushBlitMaterial.SetColor(BrushColorID, brush.color);
        brushBlitMaterial.SetVector(BrushParams, new Vector4(uv.x, uv.y, brush.radius, brush.hardness));

        Graphics.Blit(_a, _b, brushBlitMaterial);
        (_a, _b) = (_b, _a);

        ApplyToRenderer(_a);
        return true;
    }

    void LateUpdate()
    {
        if (reapplyInLateUpdate && _a != null)
            ApplyToRenderer(_a);
    }

    public string SaveCanvasToPNG(string fileName, string directoryOverride = null)
    {
        if (_a == null)
            throw new System.InvalidOperationException("No RenderTexture to save.");

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _a;

        Texture2D tex = new Texture2D(_a.width, _a.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, _a.width, _a.height), 0, 0);
        tex.Apply();

        RenderTexture.active = previous;

        byte[] pngBytes = tex.EncodeToPNG();
        DestroyImmediate(tex);

        string dir = directoryOverride ?? Path.Combine(Application.persistentDataPath, "Paintings");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, fileName);
        File.WriteAllBytes(path, pngBytes);

        Debug.Log($"[PaintableSurfaceRT] Saved painting to {path}", this);
        return path;
    }

    public bool LoadCanvasFromPNG(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.Log($"[PaintableSurfaceRT] File not found: {filePath}", this);
            return false;
        }

        byte[] bytes = File.ReadAllBytes(filePath);

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Debug.Log($"[PaintableSurfaceRT] Failed to decode PNG: {filePath}", this);
            DestroyImmediate(tex);
            return false;
        }

        Graphics.Blit(tex, _a);
        Graphics.Blit(tex, _b);

        ApplyToRenderer(_a);
        DestroyImmediate(tex);

        Debug.Log($"[PaintableSurfaceRT] Loaded painting from {filePath}", this);
        return true;
    }
}