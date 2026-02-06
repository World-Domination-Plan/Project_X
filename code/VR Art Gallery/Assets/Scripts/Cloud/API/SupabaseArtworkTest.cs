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
    [SerializeField] private string testOwnerUserId = "test-user-id";

    private SupabaseArtworkRepository _repository;

    private async void Start()
    {
        if (!testOnStart)
            return;

        Debug.Log("[SupabaseArtworkTest] Starting artwork creation test...");

        try
        {
            // Initialize the Supabase client if not already done
            if (!SupabaseClientManager.IsInitialized)
            {
                Debug.Log("[SupabaseArtworkTest] Initializing Supabase client...");
                await SupabaseClientManager.InitializeAsync();
            }

            // Create repository instance
            _repository = new SupabaseArtworkRepository();
            Debug.Log("[SupabaseArtworkTest] Repository created successfully");

            // Create test artwork
            var testArtwork = new ArtworkData
            {
                id = System.Guid.NewGuid().ToString(),
                title = testArtworkTitle,
                ownerUserId = testOwnerUserId,
                imageUrl = "https://example.com/test-image.png",
                thumbnailUrl = "https://example.com/test-thumbnail.png",
                fileSizeBytes = 1024000
            };

            Debug.Log($"[SupabaseArtworkTest] Creating artwork: {testArtwork.title} (ID: {testArtwork.id})");

            // Call CreateArtworkAsync
            var createdArtwork = await _repository.CreateArtworkAsync(testArtwork);

            // Log success
            Debug.Log($"[SupabaseArtworkTest] SUCCESS! Artwork created in Supabase:");
            Debug.Log($"  - ID: {createdArtwork.id}");
            Debug.Log($"  - Title: {createdArtwork.title}");
            Debug.Log($"  - Owner: {createdArtwork.ownerUserId}");
            Debug.Log($"  - Created At: {createdArtwork.createdAt}");
            Debug.Log($"  - Updated At: {createdArtwork.updatedAt}");
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
            if (!SupabaseClientManager.IsInitialized)
            {
                await SupabaseClientManager.InitializeAsync();
            }

            _repository = new SupabaseArtworkRepository();

            var testArtwork = new ArtworkData
            {
                title = $"Manual Test - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                ownerUserId = testOwnerUserId,
                imageUrl = "https://example.com/manual-test.png",
                thumbnailUrl = "https://example.com/manual-test-thumb.png",
                fileSizeBytes = 512000
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
