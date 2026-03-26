using UnityEngine;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private bool getThumbnailsInWorkflow = false;
    [SerializeField] private bool inviteUserAfterWorkflow = false;
    [SerializeField] private string inviteeUsername = "nm";
    [SerializeField] private bool deleteAfterWorkflow = true;

    [Header("Status (Read Only)")]
    [SerializeField] private int fetchedOwnerId = 0;
    [SerializeField] private List<int> createdArtworkIds = new List<int>();
    [SerializeField] private int createdGalleryId = 0;
    [SerializeField] private int fetchedInviteeId = 0;

    private SupabaseGalleryRepository _galleryRepo;
    private SupabaseArtistRepository _artistRepo;
    private SupabaseArtworkRepository _artworkRepo;

    public async Task<int> FetchArtistIdByUsername(string username)
    {
        Debug.Log($"[FullWorkflowTest] Fetching artist ID for: {username}");
        try
        {
            _artistRepo ??= await SupabaseArtistRepository.CreateAsync();
            var artists = await _artistRepo.GetAllArtistsAsync();
            var artist = artists.Find(a => a.username.Equals(username, System.StringComparison.OrdinalIgnoreCase));

            if (artist != null)
            {
                Debug.Log($"[FullWorkflowTest] Found Artist: {artist.username}, ID: {artist.user_id}");
                return artist.user_id;
            }
            else
            {
                Debug.LogWarning($"[FullWorkflowTest] Artist not found: {username}");
                return 0;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FullWorkflowTest] FetchArtistIdByUsername FAILED: {ex.Message}");
            return 0;
        }
    }

    public async Task FetchArtistIdByName()
    {
        fetchedOwnerId = await FetchArtistIdByUsername(artistSearchUsername);
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

            // 6. Return Bucket Paths
            Debug.Log("[FullWorkflowTest] Step 4: Retrieving Artwork Bucket Paths...");
            var artworkPaths = await _galleryRepo.GetArtworkPaths(createdGalleryId, -1, getThumbnailsInWorkflow);
            Debug.Log($"  - Retieved {artworkPaths.Count} paths:");
            foreach (var entry in artworkPaths)
            {
                (string imageUrl, string thumbUrl) = entry.Value;
                Debug.Log($"    - Slot {entry.Key}: Image={imageUrl}, Thumb={thumbUrl ?? "N/A"}");
            }

            // 7. Optional Invitation
            if (inviteUserAfterWorkflow)
            {
                Debug.Log($"[FullWorkflowTest] Step 5: Inviting User '{inviteeUsername}'...");
                fetchedInviteeId = await FetchArtistIdByUsername(inviteeUsername);
                if (fetchedInviteeId > 0)
                {
                    var allArtists = await _artistRepo.GetAllArtistsAsync();
                    var invitee = allArtists.Find(a => a.user_id == fetchedInviteeId);
                    if (invitee != null)
                    {
                        var accessList = new List<string>();
                        if (invitee.gallery_access != null) accessList.AddRange(invitee.gallery_access);
                        
                        string gidStr = createdGalleryId.ToString();
                        if (!accessList.Contains(gidStr))
                        {
                            accessList.Add(gidStr);
                            invitee.gallery_access = accessList.ToArray();
                            await _artistRepo.UpdateArtistProfileAsync(invitee);
                            Debug.Log($"  - User '{inviteeUsername}' (ID: {fetchedInviteeId}) invited successfully.");
                        }
                        else
                        {
                            Debug.Log($"  - User '{inviteeUsername}' already has access.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"  - Could not find invitee '{inviteeUsername}', skipping invitation.");
                }
            }

            // 8. Optional Deletion
            if (deleteAfterWorkflow)
            {
                Debug.Log("[FullWorkflowTest] Step 6: Cleaning up (Deleting Gallery and Artworks)...");
                
                // A. Cleanup Invitee Access
                if (fetchedInviteeId > 0)
                {
                    var allArtists = await _artistRepo.GetAllArtistsAsync();
                    var invitee = allArtists.Find(a => a.user_id == fetchedInviteeId);
                    if (invitee != null && invitee.gallery_access != null)
                    {
                        var accessList = invitee.gallery_access.ToList();
                        if (accessList.Remove(createdGalleryId.ToString()))
                        {
                            invitee.gallery_access = accessList.ToArray();
                            await _artistRepo.UpdateArtistProfileAsync(invitee);
                            Debug.Log("  - Removed gallery from invitee access.");
                        }
                    }
                }

                // B. Cleanup Owner Managed Gallery
                if (fetchedOwnerId > 0)
                {
                    var allArtists = await _artistRepo.GetAllArtistsAsync();
                    var owner = allArtists.Find(a => a.user_id == fetchedOwnerId);
                    if (owner != null && owner.managed_gallery != null)
                    {
                        var managedList = owner.managed_gallery.ToList();
                        if (managedList.Remove(createdGalleryId.ToString()))
                        {
                            owner.managed_gallery = managedList.ToArray();
                            await _artistRepo.UpdateArtistProfileAsync(owner);
                            Debug.Log("  - Removed gallery from owner managed list.");
                        }
                    }
                }

                // C. Delete Artworks (Storage + DB)
                foreach (int artworkId in createdArtworkIds)
                {
                    var artwork = await _artworkRepo.GetArtworkAsync(artworkId);
                    if (artwork != null)
                    {
                        // Delete storage files
                        try {
                            if (!string.IsNullOrEmpty(artwork.image_url))
                                await _artworkRepo.DeleteObjectAsync(workflowStorageBucket, artwork.image_url);
                            if (!string.IsNullOrEmpty(artwork.thumbnail_url))
                                await _artworkRepo.DeleteObjectAsync(workflowStorageBucket, artwork.thumbnail_url);
                            Debug.Log($"  - Deleted storage files for Artwork {artworkId}.");
                        } catch (System.Exception ex) {
                            Debug.LogWarning($"  - Failed to delete storage for Artwork {artworkId}: {ex.Message}");
                        }

                        // Delete DB record
                        await _artworkRepo.SupabaseClientInstance.From<ArtworkData>().Where(x => x.id == artworkId).Delete();
                        Debug.Log($"  - Deleted DB record for Artwork {artworkId}.");
                    }
                }

                // D. Delete Gallery
                await _galleryRepo.DeleteGalleryAsync(createdGalleryId);
                Debug.Log($"  - Deleted Gallery {createdGalleryId}.");
            }

            Debug.Log($"[FullWorkflowTest] Workflow SUCCESS! Gallery {createdGalleryId} finalized.");
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
