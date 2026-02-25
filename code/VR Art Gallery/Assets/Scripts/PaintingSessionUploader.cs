using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using VRGallery.Cloud;

[RequireComponent(typeof(PaintableSurfaceRT))]
public class PaintingSessionUploader : MonoBehaviour
{
    [Header("Artwork Metadata")]
    public string authUserId;
    public string artworkTitle = "Untitled Painting";

    const string BucketName = "artworks";

    PaintableSurfaceRT _surface;
    bool _uploadInProgress = false;
    string _pendingPath = null;

    void Awake()
    {
        _surface = GetComponent<PaintableSurfaceRT>();

        if (_surface == null)
        {
            Debug.LogError("[PaintingSessionUploader] No PaintableSurfaceRT found on this GameObject!");
            return;
        }

        _surface.OnSessionSaveReady += HandleSessionSaveReady;
        Debug.Log("[PaintingSessionUploader] Subscribed to OnSessionSaveReady.");
    }

    void HandleSessionSaveReady(string localFilePath)
    {
        Debug.Log($"[PaintingSessionUploader] Save ready received: {localFilePath}");

        if (_uploadInProgress)
        {
            // Queue it — will be picked up when current upload finishes
            _pendingPath = localFilePath;
            Debug.Log("[PaintingSessionUploader] Upload in progress, queued for after.");
            return;
        }

        _ = UploadAsync(localFilePath);
    }

    async Task UploadAsync(string localFilePath)
    {
        _uploadInProgress = true;

        try
        {
            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
            {
                Debug.LogWarning($"[PaintingSessionUploader] File not found: {localFilePath}");
                return;
            }

            if (string.IsNullOrEmpty(authUserId))
            {
                Debug.LogError("[PaintingSessionUploader] authUserId is not set!");
                return;
            }

            Debug.Log("[PaintingSessionUploader] Resolving owner...");
            long internalOwnerId = await ResolveOwnerIdAsync(authUserId);
            Debug.Log($"[PaintingSessionUploader] Owner id={internalOwnerId}, reading file...");

            byte[] imageBytes = await File.ReadAllBytesAsync(localFilePath);

            var artwork = new ArtworkData
            {
                title = artworkTitle,
                owner_id = internalOwnerId,
            };

            var repo = await SupabaseArtworkRepository.CreateAsync();

            ArtworkData saved = await repo.CreateArtworkWithUploadAsync(
                artwork,
                imageBytes,
                thumbnailBytes: null,
                bucketName: BucketName,
                extension: "png",
                contentType: "image/png",
                ownerFolder: authUserId
            );

            Debug.Log($"[PaintingSessionUploader] ✓ Uploaded — db id={saved.id}, path={saved.image_url}");
            File.Delete(localFilePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PaintingSessionUploader] Upload failed: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            _uploadInProgress = false;

            // If a newer save came in while we were uploading, process it now
            if (_pendingPath != null)
            {
                var next = _pendingPath;
                _pendingPath = null;
                _ = UploadAsync(next);
            }
        }
    }

    async Task<long> ResolveOwnerIdAsync(string authUuid)
    {
        var client = SupabaseClientManager.Instance;
        var result = await client
            .From<ArtistProfile>()
            .Where(x => x.auth_user_id == authUuid)
            .Single();

        if (result == null)
            throw new System.Exception($"No artist profile found for auth_user_id={authUuid}");

        return result.id;
    }
}
