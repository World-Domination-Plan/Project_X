using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using VRGallery.Cloud;

/// <summary>
/// Test script to manually verify GalleryRepository functionality.
/// Attach this to a GameObject to test gallery operations on start.
/// </summary>
public class SupabaseGalleryTest : MonoBehaviour
{
    [SerializeField] private bool testOnStart = false;
    [SerializeField] private string testGalleryName = "Test Gallery";
    [SerializeField] private string secondGalleryName = "Test Gallery 2";
    [SerializeField] private string testGalleryDescription = "A test gallery created by SupabaseGalleryTest";
    [SerializeField] private string testOwnerId = "test-owner-uuid";
    [SerializeField] private bool alsoDeleteFirstGallery = true;

    private SupabaseGalleryRepository _repository;

    private async void Start()
    {
        if (!testOnStart)
            return;

        Debug.Log("[SupabaseGalleryTest] Starting gallery test...");

        try
        {
            _repository = await SupabaseGalleryRepository.CreateAsync();
            Debug.Log("[SupabaseGalleryTest] Repository created successfully");

            // ── Create two galleries ──────────────────────────────────────────

            var firstGallery = new GalleryData
            {
                name        = testGalleryName,
                description = testGalleryDescription,
                owner_id    = testOwnerId,
                artwork_map = new Hashtable()
            };

            var secondGallery = new GalleryData
            {
                name        = secondGalleryName,
                description = testGalleryDescription,
                owner_id    = testOwnerId,
                artwork_map = new Hashtable()
            };

            var createdFirst  = await _repository.CreateGalleryAsync(firstGallery);
            var createdSecond = await _repository.CreateGalleryAsync(secondGallery);

            Debug.Log($"[SupabaseGalleryTest] Created 2 galleries:");
            Debug.Log($"  - First  ID: {createdFirst.id},  name: {createdFirst.name},  created_at: {createdFirst.created_at}");
            Debug.Log($"  - Second ID: {createdSecond.id}, name: {createdSecond.name}, created_at: {createdSecond.created_at}");

            // ── GetGalleryAsync ───────────────────────────────────────────────

            var fetched = await _repository.GetGalleryAsync(createdFirst.id);
            Debug.Log($"[SupabaseGalleryTest] Fetched gallery by ID {createdFirst.id}: name = {fetched.name}");

            // ── GetAllGalleriesAsync ──────────────────────────────────────────

            var allGalleries = await _repository.GetAllGalleriesAsync();
            Debug.Log($"[SupabaseGalleryTest] Total galleries in DB: {allGalleries.Count}");

            // ── UpdateGalleryAsync ────────────────────────────────────────────

            createdFirst.name        = testGalleryName + " (Updated)";
            createdFirst.description = "Updated by SupabaseGalleryTest";
            var updated = await _repository.UpdateGalleryAsync(createdFirst);
            Debug.Log($"[SupabaseGalleryTest] Updated first gallery: name = {updated.name}, updated_at = {updated.updated_at}");

            // ── SwapArtworks ──────────────────────────────────────────────────

            var galleryWithArtworks = new GalleryData
            {
                id          = updated.id,
                name        = updated.name,
                owner_id    = updated.owner_id,
                artwork_map = new Hashtable { { 0, 101 }, { 1, 202 }, { 2, 303 } }
            };

            Debug.Log($"[SupabaseGalleryTest] artwork_map before swap: [0]={galleryWithArtworks.artwork_map[0]}, [1]={galleryWithArtworks.artwork_map[1]}");
            var swapped = _repository.SwapArtworks(0, 1, galleryWithArtworks);
            Debug.Log($"[SupabaseGalleryTest] artwork_map after swap:  [0]={swapped.artwork_map[0]}, [1]={swapped.artwork_map[1]}");

            // ── MapWorldToGallery ─────────────────────────────────────────────

            var worldMap = new Hashtable
            {
                { "slot_A", "world_pos_1" },
                { "slot_B", "world_pos_2" },
                { "slot_C", "world_pos_3" }
            };

            var galleryForMapping = new GalleryData
            {
                artwork_map = new Hashtable
                {
                    { "slot_A", 101 },
                    { "slot_B", 202 }
                    // slot_C intentionally absent to test fallback
                }
            };

            var mapped = _repository.MapWorldToGallery(worldMap, galleryForMapping);
            Debug.Log($"[SupabaseGalleryTest] MapWorldToGallery results:");
            foreach (DictionaryEntry entry in mapped)
                Debug.Log($"  - {entry.Key} => {entry.Value}");

            // ── DeleteGalleryAsync ────────────────────────────────────────────

            await _repository.DeleteGalleryAsync(createdSecond.id);
            Debug.Log($"[SupabaseGalleryTest] Deleted second gallery (ID: {createdSecond.id})");

            if (alsoDeleteFirstGallery)
            {
                await _repository.DeleteGalleryAsync(createdFirst.id);
                Debug.Log($"[SupabaseGalleryTest] Also deleted first gallery (ID: {createdFirst.id})");
            }

            Debug.Log("[SupabaseGalleryTest] All tests PASSED!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] FAILED! Error: {ex.Message}");
            Debug.LogError($"[SupabaseGalleryTest] Stack Trace: {ex.StackTrace}");
        }
    }

    // ── Manual inspector-callable tests ──────────────────────────────────────

    /// <summary>
    /// Quickly create and fetch a single gallery. Call from inspector or code.
    /// </summary>
    public async void ManualTestCreateAndFetch()
    {
        Debug.Log("[SupabaseGalleryTest] Manual create+fetch test initiated...");
        try
        {
            _repository = await SupabaseGalleryRepository.CreateAsync();

            var gallery = new GalleryData
            {
                name        = $"Manual Test - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                description = "Manually triggered test gallery",
                owner_id    = testOwnerId,
                artwork_map = new Hashtable()
            };

            var created = await _repository.CreateGalleryAsync(gallery);
            Debug.Log($"[SupabaseGalleryTest] Created gallery ID: {created.id}, name: {created.name}");

            var fetched = await _repository.GetGalleryAsync(created.id);
            Debug.Log($"[SupabaseGalleryTest] Fetched back gallery ID: {fetched.id}, name: {fetched.name}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] Manual create+fetch FAILED: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetch and log all galleries currently in the database.
    /// </summary>
    public async void ManualTestGetAll()
    {
        Debug.Log("[SupabaseGalleryTest] Manual get-all test initiated...");
        try
        {
            _repository = await SupabaseGalleryRepository.CreateAsync();
            var all = await _repository.GetAllGalleriesAsync();
            Debug.Log($"[SupabaseGalleryTest] Found {all.Count} galleries:");
            foreach (var g in all)
                Debug.Log($"  - ID: {g.id}, name: {g.name}, owner: {g.owner_id}, updated_at: {g.updated_at}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] Manual get-all FAILED: {ex.Message}");
        }
    }

    /// <summary>
    /// Test SwapArtworks locally without any network calls.
    /// </summary>
    public void ManualTestSwapArtworks()
    {
        Debug.Log("[SupabaseGalleryTest] Manual swap test initiated...");
        try
        {
            var gallery = new GalleryData
            {
                artwork_map = new Hashtable { { 0, 111 }, { 1, 222 }, { 2, 333 } }
            };

            Debug.Log($"[SupabaseGalleryTest] Before swap: [0]={gallery.artwork_map[0]}, [1]={gallery.artwork_map[1]}, [2]={gallery.artwork_map[2]}");

            // Need a repo instance for this; create a dummy one if not already present
            if (_repository == null)
            {
                Debug.LogWarning("[SupabaseGalleryTest] Repository not initialized, swap test requires CreateAsync first.");
                return;
            }

            var result = _repository.SwapArtworks(0, 2, gallery);
            Debug.Log($"[SupabaseGalleryTest] After swap(0,2): [0]={result.artwork_map[0]}, [1]={result.artwork_map[1]}, [2]={result.artwork_map[2]}");
            Debug.Log("[SupabaseGalleryTest] Swap test PASSED!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] Swap test FAILED: {ex.Message}");
        }
    }
}
