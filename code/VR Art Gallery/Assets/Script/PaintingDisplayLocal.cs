using UnityEngine;

[DisallowMultipleComponent]
public class PaintingDisplayLocal : MonoBehaviour
{
    [Header("Local image source")]
    public Texture2D image;              // Assign in Inspector (recommended)
    public string resourcesPath = "";    // Optional: "trailPhoto" if in Assets/Resources/trailPhoto.png

    [Header("Target")]
    public Renderer targetRenderer;      // Assign Painting Display's Renderer (or leave empty to auto-find)

    private MaterialPropertyBlock _mpb;

    // Works for Built-in (_MainTex) and URP (_BaseMap)
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
        // If not assigned in inspector, try Resources
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

        targetRenderer.GetPropertyBlock(_mpb);

        // Set both; the shader will use whichever exists
        _mpb.SetTexture(MainTex, tex);
        _mpb.SetTexture(BaseMap, tex);

        // Make sure tint is white
        _mpb.SetColor(ColorId, Color.white);
        _mpb.SetColor(BaseColorId, Color.white);

        targetRenderer.SetPropertyBlock(_mpb);
    }
}

