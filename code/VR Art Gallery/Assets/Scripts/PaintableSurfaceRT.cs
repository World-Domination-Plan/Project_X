using UnityEngine;
using System.IO;
using System.Collections;
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

    [SerializeField] bool forceUniqueMaterialInstance = true;
    [SerializeField] bool reapplyInLateUpdate = true;

    Material _displayMat;


    [Header("Mode")]
    public SurfaceMode mode = SurfaceMode.DisplayOnly;

    [Header("Canvas")]
    public int resolution = 1024;
    public Color clearColor = Color.magenta;

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
    static readonly int BrushParams = Shader.PropertyToID("_BrushParams");
    static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST"); // xy=tiling, zw=offset
    static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");


    [Header("Autosave")]
    public bool enableAutoSave = true;
    public float autoSaveInterval = 10f;   // seconds, tunable in Inspector

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
        //Debug.LogError($"[PaintableSurfaceRT] AWAKE on {name}, active={gameObject.activeInHierarchy}", this);

        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();

        if (forceUniqueMaterialInstance)
        {
            _displayMat = new Material(targetRenderer.sharedMaterial);
            targetRenderer.material = _displayMat;   // instance, not shared
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
    
    void Start()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh)
        {
            var uvs = mf.sharedMesh.uv;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            
            foreach (var uv in uvs)
            {
                min.x = Mathf.Min(min.x, uv.x);
                min.y = Mathf.Min(min.y, uv.y);
                max.x = Mathf.Max(max.x, uv.x);
                max.y = Mathf.Max(max.y, uv.y);
            }
            
            Debug.Log($"[PaintableSurfaceRT] UV range: min={min}, max={max}", this);
        }
    }

    public string SaveCanvasToPNG(string fileName, string directoryOverride = null)
    {
        if (_a == null)
            throw new System.InvalidOperationException("No RenderTexture to save.");

        RenderTexture currentRT = _a;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = currentRT;

        Texture2D tex = new Texture2D(currentRT.width, currentRT.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, currentRT.width, currentRT.height), 0, 0);
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

    static string GetPath(Transform t)
    {
        var p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
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
        
        // No property block needed since we're using instanced material
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

        brushBlitMaterial.SetTexture(BrushTex, brushMask ? brushMask : Texture2D.whiteTexture);
        brushBlitMaterial.SetColor(BrushColorID, brushColor);
        brushBlitMaterial.SetVector(BrushParams, new Vector4(uv.x, uv.y, radius, hardness));

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
}
