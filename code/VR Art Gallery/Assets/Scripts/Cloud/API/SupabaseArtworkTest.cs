using UnityEngine;
using System.Threading.Tasks;
using VRGallery.Cloud;

/// <summary>
/// Test script to manually verify CreateArtworkAsync functionality
/// Attach this to a GameObject to test artwork creation on start
/// </summary>
public class SupabaseArtworkTest : MonoBehaviour
{
    [SerializeField] private bool testOnStart = false;
    [SerializeField] private string testArtworkTitle = "Test Artwork";
    [SerializeField] private string secondArtworkTitle = "Test Artwork 2";
    [SerializeField] private int testOwnerUserId = 1;
    [SerializeField] private Texture2D testTexture;
    [SerializeField] private string storageBucket = "artworks";
    [SerializeField] private int signedUrlExpirySeconds = 300;
    [SerializeField] private bool alsoDeleteFirstArtwork = false;

    private SupabaseArtworkRepository _repository;

    private async void Start()
    {
        if (!testOnStart)
            return;

        Debug.Log("[SupabaseArtworkTest] Starting artwork creation test...");

        try
        {
            _repository = await SupabaseArtworkRepository.CreateAsync();
            Debug.Log("[SupabaseArtworkTest] Repository created successfully");

            if (testTexture == null)
            {
                Debug.LogError("[SupabaseArtworkTest] testTexture is not assigned.");
                return;
            }

            var imageBytes = EncodeTextureToPngSafe(testTexture);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Debug.LogError("[SupabaseArtworkTest] Failed to encode testTexture to PNG.");
                return;
            }

            var firstArtwork = new ArtworkData
            {
                title = testArtworkTitle,
                owner_id = testOwnerUserId,
                image_url = string.Empty,
                thumbnail_url = string.Empty,
                filesize_bytes = 0
            };

            var secondArtwork = new ArtworkData
            {
                title = secondArtworkTitle,
                owner_id = testOwnerUserId,
                image_url = string.Empty,
                thumbnail_url = string.Empty,
                filesize_bytes = 0
            };

            var createdFirst = await _repository.CreateArtworkWithUploadAsync(
                firstArtwork,
                imageBytes,
                thumbnailBytes: null,
                bucketName: storageBucket,
                extension: "png",
                contentType: "image/png");

            var createdSecond = await _repository.CreateArtworkWithUploadAsync(
                secondArtwork,
                imageBytes,
                thumbnailBytes: null,
                bucketName: storageBucket,
                extension: "png",
                contentType: "image/png");

            Debug.Log($"[SupabaseArtworkTest] Uploaded 2 artworks:");
            Debug.Log($"  - First ID: {createdFirst.id}, image path: {createdFirst.image_url}, thumb path: {createdFirst.thumbnail_url}");
            Debug.Log($"  - Second ID: {createdSecond.id}, image path: {createdSecond.image_url}, thumb path: {createdSecond.thumbnail_url}");

            var signedUrl = await _repository.CreateSignedUrlAsync(storageBucket, createdFirst.image_url, signedUrlExpirySeconds);
            var downloadedBytes = await _repository.DownloadWithSignedUrlAsync(signedUrl);

            Debug.Log($"[SupabaseArtworkTest] Fetched first image via signed URL ({signedUrlExpirySeconds}s): {downloadedBytes.Length} bytes");

            await _repository.DeleteObjectAsync(storageBucket, createdSecond.image_url);
            await _repository.DeleteObjectAsync(storageBucket, createdSecond.thumbnail_url);

            Debug.Log("[SupabaseArtworkTest] Deleted second artwork image + thumbnail from bucket");

            if (alsoDeleteFirstArtwork)
            {
                await _repository.DeleteObjectAsync(storageBucket, createdFirst.image_url);
                await _repository.DeleteObjectAsync(storageBucket, createdFirst.thumbnail_url);
                Debug.Log("[SupabaseArtworkTest] Also deleted first artwork image + thumbnail from bucket");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseArtworkTest] FAILED! Error: {ex.Message}");
            Debug.LogError($"[SupabaseArtworkTest] Stack Trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Manual test method you can call from inspector or code
    /// </summary>
    public async void ManualTestArtworkCreation()
    {
        Debug.Log("[SupabaseArtworkTest] Manual test initiated...");

        try
        {
            _repository = await SupabaseArtworkRepository.CreateAsync();

            if (testTexture == null)
            {
                Debug.LogError("[SupabaseArtworkTest] testTexture is not assigned.");
                return;
            }

            var imageBytes = EncodeTextureToPngSafe(testTexture);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Debug.LogError("[SupabaseArtworkTest] Failed to encode testTexture to PNG.");
                return;
            }

            var testArtwork = new ArtworkData
            {
                title = $"Manual Test - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                owner_id = testOwnerUserId,
                image_url = string.Empty,
                thumbnail_url = string.Empty,
                filesize_bytes = 0
            };

            var createdArtwork = await _repository.CreateArtworkWithUploadAsync(
                testArtwork,
                imageBytes,
                thumbnailBytes: null,
                bucketName: storageBucket,
                extension: "png",
                contentType: "image/png");

            var signedUrl = await _repository.CreateSignedUrlAsync(storageBucket, createdArtwork.image_url, signedUrlExpirySeconds);
            Debug.Log($"[SupabaseArtworkTest] Manual test SUCCESS! Created artwork ID: {createdArtwork.id}");
            Debug.Log($"[SupabaseArtworkTest] Manual test signed URL: {signedUrl}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseArtworkTest] Manual test FAILED! Error: {ex.Message}");
        }
    }

    private static byte[] EncodeTextureToPngSafe(Texture2D source)
    {
        if (source == null)
            return null;

        try
        {
            var direct = source.EncodeToPNG();
            if (direct != null && direct.Length > 0)
                return direct;
        }
        catch
        {
        }

        var renderTarget = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);

        var previous = RenderTexture.active;

        try
        {
            Graphics.Blit(source, renderTarget);
            RenderTexture.active = renderTarget;

            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply(false, false);

            var bytes = readable.EncodeToPNG();
            Destroy(readable);
            return bytes;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTarget);
        }
    }
}
