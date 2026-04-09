using UnityEngine;
using TMPro;

public class ArtworkDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer meshRenderer;

    private Material dynamicMaterial;

    private void Start()
    {
        // Auto-find if not assigned in Inspector
        if (meshRenderer == null)
            meshRenderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// Populates the mesh with artwork texture
    /// Creates a unique material instance to avoid affecting other meshes.
    /// </summary>
    public void Populate(Texture2D texture)
    {
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
        if (dynamicMaterial != null)
            dynamicMaterial.mainTexture = null;
    }
}