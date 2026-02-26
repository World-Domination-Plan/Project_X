using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)] // ensure RT exists before we seed it
public class PaintingDisplayLocal : MonoBehaviour
{
    [Header("Local image source")]
    public Texture2D image;
    public string resourcesPath = "";

    [Header("Target")]
    public Renderer targetRenderer;

    [Header("Integration")]
    public bool useDrawableSurfaceIfPresent = true;

    private MaterialPropertyBlock _mpb;

    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (!image && !string.IsNullOrWhiteSpace(resourcesPath))
        {
            image = Resources.Load<Texture2D>(resourcesPath);
            if (!image)
                Debug.LogError($"[PaintingDisplayLocal] Could not load texture from Resources path: {resourcesPath}", this);
        }

        if (image) ApplyTexture(image);
        else Debug.LogWarning("[PaintingDisplayLocal] No image assigned.", this);
    }

    public void SetImage(Texture2D tex)
    {
        image = tex;
        if (image) ApplyTexture(image);
    }

    private void ApplyTexture(Texture2D tex)
    {
        if (!targetRenderer)
        {
            Debug.LogError("[PaintingDisplayLocal] targetRenderer missing.", this);
            return;
        }

        // NEW: If we have a drawable RT surface, seed it (display stays correct + drawing will work)
        if (useDrawableSurfaceIfPresent && TryGetComponent<PaintableSurfaceRT>(out var surface) && surface != null)
        {
            surface.SetBackground(tex);
            return;
        }

        // Fallback: old behavior
        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetTexture(MainTex, tex);
        _mpb.SetTexture(BaseMap, tex);
        _mpb.SetColor(ColorId, Color.white);
        _mpb.SetColor(BaseColorId, Color.white);
        targetRenderer.SetPropertyBlock(_mpb);
    }
}
