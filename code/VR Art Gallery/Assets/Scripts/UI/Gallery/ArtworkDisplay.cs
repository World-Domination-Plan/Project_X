using UnityEngine;
using TMPro;

public class ArtworkDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private TextMeshProUGUI titleText;

    private Material dynamicMaterial;

    private void Start()
    {
        // Auto-find if not assigned in Inspector
        if (meshRenderer == null)
            meshRenderer = GetComponent<Renderer>();
        if (titleText == null)
            titleText = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// Populates the mesh with artwork texture and title.
    /// Creates a unique material instance to avoid affecting other meshes.
    /// If no title is provided, uses a default title.
    /// </summary>
    public void Populate(string title, Texture2D texture)
    {
        // Use default title if none provided
        if (string.IsNullOrWhiteSpace(title))
            title = "Artwork X";

        if (titleText != null)
            titleText.text = title;

        if (meshRenderer != null && texture != null)
        {
            // Create a unique material instance so we don't change ALL paintings at once
            if (dynamicMaterial == null)
            {
                dynamicMaterial = new Material(meshRenderer.material);
                meshRenderer.material = dynamicMaterial;
            }

            // Set the texture on the material (usually "_MainTex" or "_BaseMap")
            dynamicMaterial.mainTexture = texture;
        }
    }

    /// <summary>
    /// Clears the artwork display.
    /// </summary>
    public void Clear()
    {
        if (titleText != null)
            titleText.text = "";

        if (dynamicMaterial != null)
        {
            dynamicMaterial.mainTexture = null;
        }
    }
}