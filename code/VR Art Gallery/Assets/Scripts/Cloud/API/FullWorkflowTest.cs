using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRGallery.Cloud;

/// <summary>
/// Specialized test for complex multi-step workflows in Supabase.
/// This script handles artist lookup, creating multiple artworks, 
/// creating a gallery, and linking them all together.
/// </summary>
public class FullWorkflowTest : MonoBehaviour
{
    [Header("Workflow Configuration")]
    [SerializeField] private string artistSearchUsername = "guest1";
    [SerializeField] private int numArtworksToCreate = 1;
    [SerializeField] private Texture2D testWorkflowTexture;
    [SerializeField] private string workflowStorageBucket = "artworks";

    [Header("Status (Read Only)")]
    [SerializeField] private int fetchedOwnerId = 0;
    [SerializeField] private List<int> createdArtworkIds = new List<int>();
    [SerializeField] private int createdGalleryId = 0;

    private SupabaseGalleryRepository _galleryRepo;
    private SupabaseArtistRepository _artistRepo;
    private SupabaseArtworkRepository _artworkRepo;

    public async Task FetchArtistIdByName()
    {
        Debug.Log($"[FullWorkflowTest] Fetching artist ID for: {artistSearchUsername}");
        try
        {
            _artistRepo ??= await SupabaseArtistRepository.CreateAsync();
            var artists = await _artistRepo.GetAllArtistsAsync();
            var artist = artists.Find(a => a.username.Equals(artistSearchUsername, System.StringComparison.OrdinalIgnoreCase));

            if (artist != null)
            {
                fetchedOwnerId = artist.user_id;
                Debug.Log($"[FullWorkflowTest] Found Artist: {artist.username}, ID: {fetchedOwnerId}");
            }
            else
            {
                Debug.LogWarning($"[FullWorkflowTest] Artist not found: {artistSearchUsername}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FullWorkflowTest] FetchArtistIdByName FAILED: {ex.Message}");
        }
    }

    public async void RunWorkflowTest()
    {
        Debug.Log("[FullWorkflowTest] Starting Integrated Workflow...");
        createdArtworkIds.Clear();

        try 
        {
            // 1. Initialize Repositories
            _galleryRepo = await SupabaseGalleryRepository.CreateAsync();
            _artworkRepo = await SupabaseArtworkRepository.CreateAsync();

            // 2. Fetch User ID if not set
            if (fetchedOwnerId <= 0)
            {
                await FetchArtistIdByName();
                if (fetchedOwnerId <= 0) throw new System.Exception("Could not resolve owner ID for workflow.");
            }

            // 3. Create Artworks
            if (testWorkflowTexture == null)
            {
                Debug.LogError("[FullWorkflowTest] No workflow texture assigned.");
                return;
            }

            var imageBytes = SupabaseArtworkTestHelper.EncodeTextureToPngSafe(testWorkflowTexture);
            
            for (int i = 0; i < numArtworksToCreate; i++)
            {
                Debug.Log($"[FullWorkflowTest] Step 1.{i+1}: Creating Artwork {i+1}/{numArtworksToCreate}...");
                var artworkData = new ArtworkData
                {
                    title = $"Workflow Artwork {i+1} - {System.DateTime.Now:HHmm}",
                    owner_id = fetchedOwnerId,
                    image_url = string.Empty,
                    thumbnail_url = string.Empty
                };

                var created = await _artworkRepo.CreateArtworkWithUploadAsync(artworkData, imageBytes, bucketName: workflowStorageBucket);
                createdArtworkIds.Add(created.id);
                Debug.Log($"  - Created ID: {created.id}");
            }

            // 4. Create Gallery
            Debug.Log("[FullWorkflowTest] Step 2: Creating Gallery...");
            var galleryData = new GalleryData
            {
                name = $"Workflow Gallery - {System.DateTime.Now:HHmm}",
                description = $"Integrated test with {numArtworksToCreate} artworks.",
                owner_id = fetchedOwnerId,
                artwork_ids = new List<int>(),
                artwork_map = new Dictionary<int, int>()
            };
            var createdGallery = await _galleryRepo.CreateGalleryAsync(galleryData);
            createdGalleryId = createdGallery.id;
            Debug.Log($"  - Created Gallery ID: {createdGalleryId}");

            // 5. Link Artworks
            Debug.Log("[FullWorkflowTest] Step 3: Linking Artworks to Gallery...");
            GalleryData updatedGallery = createdGallery;
            for (int i = 0; i < createdArtworkIds.Count; i++)
            {
                updatedGallery = await _galleryRepo.AddArtworkToGalleryAsync(createdGalleryId, createdArtworkIds[i]);
                // Force into specific slot (0-8)
                if (i < 9)
                {
                    updatedGallery.PlaceArtworkInSlot(i, createdArtworkIds[i]);
                }
            }
            // Final update to persist map changes if any manual mapping was done (PlaceArtworkInSlot is local)
            await _galleryRepo.UpdateGalleryAsync(updatedGallery);

            Debug.Log($"[FullWorkflowTest] Workflow SUCCESS! Gallery {createdGalleryId} finalized with {updatedGallery.artwork_ids.Count} artworks.");
        }
        catch (System.Exception ex)
        {
             Debug.LogError($"[FullWorkflowTest] Workflow FAILED: {ex.Message}");
        }
    }
}

public static class SupabaseArtworkTestHelper
{
    public static byte[] EncodeTextureToPngSafe(Texture2D source)
    {
        if (source == null) return null;

        // Compressed textures (DXT, ASTC, etc.) cannot be encoded directly. 
        // We always use the RenderTexture blit path to ensure we have an uncompressed readable copy.
        var renderTarget = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var previous = RenderTexture.active;

        try
        {
            Graphics.Blit(source, renderTarget);
            RenderTexture.active = renderTarget;
            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply(false, false);
            var bytes = readable.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(readable);
            return bytes;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseArtworkTestHelper] Encoding failed: {ex.Message}");
            return null;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTarget);
        }
    }
}
