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
    [SerializeField] private int testOwnerUserId = 1;

    private SupabaseArtworkRepository _repository;

    private async void Start()
    {
        if (!testOnStart)
            return;

        Debug.Log("[SupabaseArtworkTest] Starting artwork creation test...");

        try
        {
            // Create repository instance (handles initialization internally)
            _repository = await SupabaseArtworkRepository.CreateAsync();
            Debug.Log("[SupabaseArtworkTest] Repository created successfully");

            // Create test artwork
            var testArtwork = new ArtworkData
            {
                title = testArtworkTitle,
                owner_id = testOwnerUserId,
                image_url = "https://example.com/test-image.png",
                thumbnail_url = "https://example.com/test-thumbnail.png",
                filesize_bytes = 1024000
            };

            Debug.Log($"[SupabaseArtworkTest] Creating artwork: {testArtwork.title} (ID: {testArtwork.id})");

            // Call CreateArtworkAsync
            var createdArtwork = await _repository.CreateArtworkAsync(testArtwork);

            // Log success
            Debug.Log($"[SupabaseArtworkTest] SUCCESS! Artwork created in Supabase:");
            Debug.Log($"  - ID: {createdArtwork.id}");
            Debug.Log($"  - Title: {createdArtwork.title}");
            Debug.Log($"  - Owner: {createdArtwork.owner_id}");
            Debug.Log($"  - Created At: {createdArtwork.created_at}");
            Debug.Log($"  - Updated At: {createdArtwork.updated_at}");
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

            var testArtwork = new ArtworkData
            {
                title = $"Manual Test - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                owner_id = testOwnerUserId,
                image_url = "https://example.com/manual-test.png",
                thumbnail_url = "https://example.com/manual-test-thumb.png",
                filesize_bytes = 512000
            };

            var createdArtwork = await _repository.CreateArtworkAsync(testArtwork);

            Debug.Log($"[SupabaseArtworkTest] Manual test SUCCESS! Created artwork ID: {createdArtwork.id}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseArtworkTest] Manual test FAILED! Error: {ex.Message}");
        }
    }
}
