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

    bool _uploadInProgress = false;
    string _pendingPath = null;
    string _pendingTimestamp = null;

    void Start()
    {
        if (string.IsNullOrEmpty(authUserId))
            Debug.LogError("[PaintingSessionUploader] *** authUserId is EMPTY ***");
        else
            Debug.Log($"[PaintingSessionUploader] Ready. authUserId={authUserId}");
    }

    public void TriggerUpload(string localFilePath, string timestamp)
    {
        Debug.Log($"[PaintingSessionUploader] TriggerUpload: {localFilePath}");

        if (_uploadInProgress)
        {
            _pendingPath = localFilePath;
            _pendingTimestamp = timestamp;
            Debug.Log("[PaintingSessionUploader] Upload busy — queued.");
            return;
        }

        _ = UploadAsync(localFilePath, timestamp);
    }

    async Task UploadAsync(string localFilePath, string timestamp)
    {
        _uploadInProgress = true;
        Debug.Log($"[PaintingSessionUploader] Starting upload: {localFilePath}");

        try
        {
            if (!File.Exists(localFilePath))
            {
                Debug.LogWarning($"[PaintingSessionUploader] File missing: {localFilePath}");
                return;
            }

            if (string.IsNullOrEmpty(authUserId))
            {
                Debug.LogError("[PaintingSessionUploader] authUserId is empty — aborting.");
                return;
            }

            long internalOwnerId = await ResolveOwnerIdAsync(authUserId);
            Debug.Log($"[PaintingSessionUploader] Owner id={internalOwnerId}");

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
                ownerFolder: authUserId,
                timestamp: timestamp
            );

            Debug.Log($"[PaintingSessionUploader] ✓ Done — id={saved.id}, path={saved.image_url}");
            File.Delete(localFilePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PaintingSessionUploader] FAILED: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            _uploadInProgress = false;

            if (_pendingPath != null)
            {
                var next = _pendingPath;
                var nextTs = _pendingTimestamp;
                _pendingPath = null;
                _pendingTimestamp = null;
                _ = UploadAsync(next, nextTs);
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
            throw new System.Exception($"No ArtistProfile found for auth_user_id={authUuid}");

        if (result.id == 0)
            throw new System.Exception($"ArtistProfile resolved to id=0 — check [Column(\"user_id\")] on ArtistProfile. auth_user_id={result.auth_user_id}");

        return result.id;
    }
}
