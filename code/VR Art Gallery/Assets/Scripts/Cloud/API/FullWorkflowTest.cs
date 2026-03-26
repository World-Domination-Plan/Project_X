using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VRGallery.Cloud;
using VRGallery.Authentication;

/// <summary>
/// Specialized test for complex multi-step workflows in Supabase.
/// This script handles artist lookup, creating multiple artworks, 
/// creating a gallery, and linking them all together.
/// </summary>
public class FullWorkflowTest : MonoBehaviour
{
    [Header("Workflow Configuration")]
    [SerializeField] private bool useAuthManager = false;
    [SerializeField] private string authEmail = "workflow_test@example.com";
    [SerializeField] private string authPassword = "Password123!";
    [SerializeField] private string authUsername = "WorkflowArtist";
    [SerializeField] private string artistSearchUsername = "guest1";
    [SerializeField] private int numArtworksToCreate = 1;
    [SerializeField] private Texture2D testWorkflowTexture;
    [SerializeField] private string workflowStorageBucket = "artworks";
    [SerializeField] private bool getThumbnailsInWorkflow = false;
    [SerializeField] private bool inviteUserAfterWorkflow = false;
    [SerializeField] private string inviteeUsername = "nm";

    [Header("Deletion Configuration")]
    [SerializeField] private bool deleteGalleryWork = true;
    [SerializeField] private bool deleteArtworksWork = true;
    [SerializeField] private bool deleteUserWork = true;

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
            _artistRepo = await SupabaseArtistRepository.CreateAsync();

            // 0. Authentication Step
            if (useAuthManager)
            {
                Debug.Log($"[FullWorkflowTest] Step 0: Authenticating '{authEmail}'...");
                
                if (AuthenticationManager.Instance == null)
                    throw new System.Exception("AuthenticationManager instance is null.");

                bool loggedIn = await AuthenticationManager.Instance.LoginUser(authEmail, authPassword);
                if (!loggedIn)
                {
                    Debug.Log("[FullWorkflowTest] Login failed, attempting Registration...");
                    bool registered = await AuthenticationManager.Instance.RegisterUser(authEmail, authPassword, authUsername);
                    if (!registered)
                        throw new System.Exception("Could not Login or Register user for workflow.");
                    
                    // Try login again after registration
                    loggedIn = await AuthenticationManager.Instance.LoginUser(authEmail, authPassword);
                    if (!loggedIn)
                        throw new System.Exception("Registration succeeded but Login failed (Check email confirmation settings).");
                }

                var user = AuthenticationManager.Instance.CurrentUser;
                if (user == null) throw new System.Exception("Auth succeeded but CurrentUser is null.");

                var profile = await _artistRepo.GetArtistProfileAsync(user.Id);
                
                // Recreate profile if it was deleted by a previous test cleanup but the GoTrue user remains
                if (profile == null)
                {
                    Debug.LogWarning($"[FullWorkflowTest] Auth user exists but Profile is missing. Recreating Profile for {authUsername}...");
                    await _artistRepo.CreateArtistProfileAsync(user.Id, authUsername);
                    profile = await _artistRepo.GetArtistProfileAsync(user.Id);
                }

                if (profile == null) throw new System.Exception("Could not fetch profile for authenticated user.");
                
                fetchedOwnerId = profile.user_id;
                Debug.Log($"[FullWorkflowTest] Step 0 SUCCESS: Authenticated as '{profile.username}' (Profile UserID: {fetchedOwnerId}, Auth UserID: {user.Id})");
            }

            // 2. Fetch User ID if not set (Fallback path)
            if (fetchedOwnerId <= 0)
            {
                Debug.Log($"[FullWorkflowTest] Step 0.5: No Auth ID, searching for artist '{artistSearchUsername}'...");
                await FetchArtistIdByName();
                if (fetchedOwnerId <= 0) throw new System.Exception("Could not resolve owner ID for workflow.");
                Debug.Log($"[FullWorkflowTest] Step 0.5 SUCCESS: Resolved '{artistSearchUsername}' to ID {fetchedOwnerId}");
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
                Debug.Log($"[FullWorkflowTest] Step 1.{i+1}: Creating Artwork {i+1}/{numArtworksToCreate} (Owner: {fetchedOwnerId})...");
                var artworkData = new ArtworkData
                {
                    title = $"Workflow Artwork {i+1} - {System.DateTime.Now:HHmm}",
                    owner_id = fetchedOwnerId,
                    image_url = string.Empty,
                    thumbnail_url = string.Empty
                };

                Debug.Log($"[FullWorkflowTest]   - Sending insert for: {artworkData.title} by {artworkData.owner_id}");
                var created = await _artworkRepo.CreateArtworkWithUploadAsync(artworkData, imageBytes, bucketName: workflowStorageBucket);
                createdArtworkIds.Add(created.id);
                Debug.Log($"  - Created ID: {created.id}");
                
                // Add minimum 1-second delay so that filename timestamp (yyyyMMdd_HHmmss) is guaranteed unique
                if (i < numArtworksToCreate - 1)
                {
                    await Task.Delay(1100);
                }
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

            // 8. Granular Cleanup
            Debug.Log("[FullWorkflowTest] Step 6: Starting Granular Cleanup...");

            // A. Gallery Deletion (Cascade managed/access)
            if (deleteGalleryWork && createdGalleryId > 0)
            {
                Debug.Log($"  - Deleting Gallery {createdGalleryId}...");
                
                // Cleanup Invitee Access
                if (fetchedInviteeId > 0)
                {
                    var invitee = (await _artistRepo.GetAllArtistsAsync()).Find(a => a.user_id == fetchedInviteeId);
                    if (invitee != null && invitee.gallery_access != null)
                    {
                        var accessList = invitee.gallery_access.ToList();
                        if (accessList.Remove(createdGalleryId.ToString()))
                        {
                            invitee.gallery_access = accessList.ToArray();
                            await _artistRepo.UpdateArtistProfileAsync(invitee);
                        }
                    }
                }

                // Cleanup Owner Managed List
                if (fetchedOwnerId > 0)
                {
                    var owner = (await _artistRepo.GetAllArtistsAsync()).Find(a => a.user_id == fetchedOwnerId);
                    if (owner != null && owner.managed_gallery != null)
                    {
                        var managedList = owner.managed_gallery.ToList();
                        if (managedList.Remove(createdGalleryId.ToString()))
                        {
                            owner.managed_gallery = managedList.ToArray();
                            await _artistRepo.UpdateArtistProfileAsync(owner);
                        }
                    }
                }

                await _galleryRepo.DeleteGalleryAsync(createdGalleryId);
                Debug.Log("    - Gallery and its access references deleted.");
            }

            // B. Artwork Deletion
            if (deleteArtworksWork && createdArtworkIds.Count > 0)
            {
                Debug.Log($"  - Deleting {createdArtworkIds.Count} Artworks...");
                foreach (int artworkId in createdArtworkIds)
                {
                    var artwork = await _artworkRepo.GetArtworkAsync(artworkId);
                    if (artwork != null)
                    {
                        try {
                            if (!string.IsNullOrEmpty(artwork.image_url))
                                await _artworkRepo.DeleteObjectAsync(workflowStorageBucket, artwork.image_url);
                            if (!string.IsNullOrEmpty(artwork.thumbnail_url))
                                await _artworkRepo.DeleteObjectAsync(workflowStorageBucket, artwork.thumbnail_url);
                        } catch (System.Exception ex) {
                            Debug.LogWarning($"    - Failed to delete storage for Artwork {artworkId}: {ex.Message}");
                        }
                        await _artworkRepo.SupabaseClientInstance.From<ArtworkData>().Where(x => x.id == artworkId).Delete();
                    }
                }
                Debug.Log("    - Artworks deleted (Storage + DB).");
            }

            // C. User Deletion (Cascade Everything if requested)
            if (deleteUserWork && fetchedOwnerId > 0)
            {
                Debug.Log($"  - Deleting User Profile {fetchedOwnerId} and cascading associated content...");

                // 1. Delete associated Galleries if not already done
                if (!deleteGalleryWork && createdGalleryId > 0)
                {
                    await _galleryRepo.DeleteGalleryAsync(createdGalleryId);
                    Debug.Log("    - Cascaded Gallery deletion.");
                }

                // 2. Delete associated Artworks if not already done
                if (!deleteArtworksWork && createdArtworkIds.Count > 0)
                {
                    foreach (int artworkId in createdArtworkIds)
                    {
                        var artwork = await _artworkRepo.GetArtworkAsync(artworkId);
                        if (artwork != null)
                        {
                            try {
                                if (!string.IsNullOrEmpty(artwork.image_url))
                                    await _artworkRepo.DeleteObjectAsync(workflowStorageBucket, artwork.image_url);
                                if (!string.IsNullOrEmpty(artwork.thumbnail_url))
                                    await _artworkRepo.DeleteObjectAsync(workflowStorageBucket, artwork.thumbnail_url);
                            } catch {}
                            await _artworkRepo.SupabaseClientInstance.From<ArtworkData>().Where(x => x.id == artworkId).Delete();
                        }
                    }
                    Debug.Log("    - Cascaded Artworks deletion.");
                }
                
                // 3. Delete Profile
                await _artistRepo.DeleteArtistProfileAsync(fetchedOwnerId);
                Debug.Log("    - Artist Profile deleted.");
                
                if (useAuthManager)
                {
                    await AuthenticationManager.Instance.LogoutUser();
                    Debug.Log("    - Logged out authenticated user.");
                }
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
