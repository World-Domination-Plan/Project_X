using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    [SerializeField] private int testOwnerId = 1;
    [SerializeField] private bool alsoDeleteFirstGallery = true;

    private SupabaseGalleryRepository _repository;

    private async void Start()
    {
        if (!testOnStart)
            return;

        Debug.Log("[SupabaseGalleryTest] Starting gallery tests...");

        try
        {
            _repository = await SupabaseGalleryRepository.CreateAsync();
            Debug.Log("[SupabaseGalleryTest] Repository created successfully");

            // ── Create ────────────────────────────────────────────────────────

            var firstGallery = new GalleryData
            {
                name        = testGalleryName,
                description = testGalleryDescription,
                owner_id    = testOwnerId,
                artwork_ids = new List<int> { 1, 2, 3 },
                artwork_map = new Dictionary<int, int> { { 0, 1 }, { 1, 2 }, { 2, 3 } }
            };

            var secondGallery = new GalleryData
            {
                name        = secondGalleryName,
                description = testGalleryDescription,
                owner_id    = testOwnerId,
                artwork_ids = new List<int> { 4, 5 },
                artwork_map = new Dictionary<int, int> { { 0, 4 }, { 1, 5 } }
            };

            var createdFirst  = await _repository.CreateGalleryAsync(firstGallery);
            var createdSecond = await _repository.CreateGalleryAsync(secondGallery);

            Debug.Log($"[SupabaseGalleryTest] Created 2 galleries:");
            Debug.Log($"  - First  ID: {createdFirst.id},  name: {createdFirst.name},  artwork_ids: [{string.Join(", ", createdFirst.artwork_ids)}]");
            Debug.Log($"  - Second ID: {createdSecond.id}, name: {createdSecond.name}, artwork_ids: [{string.Join(", ", createdSecond.artwork_ids)}]");

            // ── GetGalleryAsync ───────────────────────────────────────────────

            var fetched = await _repository.GetGalleryAsync(createdFirst.id);
            Debug.Log($"[SupabaseGalleryTest] Fetched gallery ID {fetched.id}: name = {fetched.name}, artworks = [{string.Join(", ", fetched.artwork_ids)}]");

            // ── GetAllGalleriesAsync ──────────────────────────────────────────

            var all = await _repository.GetAllGalleriesAsync();
            Debug.Log($"[SupabaseGalleryTest] Total galleries in DB: {all.Count}");

            // ── UpdateGalleryAsync ────────────────────────────────────────────

            createdFirst.name        = testGalleryName + " (Updated)";
            createdFirst.description = "Updated by SupabaseGalleryTest";
            var updated = await _repository.UpdateGalleryAsync(createdFirst);
            Debug.Log($"[SupabaseGalleryTest] Updated gallery: name = {updated.name}, updated_at = {updated.updated_at}");

            // ── AddArtworkToGalleryAsync ──────────────────────────────────────

            var afterAdd = await _repository.AddArtworkToGalleryAsync(createdFirst.id, 99);
            Debug.Log($"[SupabaseGalleryTest] After AddArtwork(99): artwork_ids = [{string.Join(", ", afterAdd.artwork_ids)}]");

            // Verify duplicate add is ignored
            var afterDuplicateAdd = await _repository.AddArtworkToGalleryAsync(createdFirst.id, 99);
            Debug.Log($"[SupabaseGalleryTest] After duplicate AddArtwork(99): artwork_ids count = {afterDuplicateAdd.artwork_ids.Count} (should be unchanged)");

            // ── PlaceArtworkInSlot (local helper) ─────────────────────────────

            afterAdd.PlaceArtworkInSlot(5, 99);
            Debug.Log($"[SupabaseGalleryTest] Placed artwork 99 in slot 5: artwork_map[5] = {afterAdd.artwork_map[5]}");

            // ── RemoveArtworkFromGalleryAsync ─────────────────────────────────

            var afterRemove = await _repository.RemoveArtworkFromGalleryAsync(createdFirst.id, 99);
            bool removedFromIds = !afterRemove.artwork_ids.Contains(99);
            bool removedFromMap = !afterRemove.artwork_map.ContainsValue(99);
            Debug.Log($"[SupabaseGalleryTest] After RemoveArtwork(99): removed from ids = {removedFromIds}, removed from map = {removedFromMap}");

            // ── SwapArtworks ──────────────────────────────────────────────────

            Debug.Log($"[SupabaseGalleryTest] artwork_map before swap: [0]={createdFirst.artwork_map[0]}, [1]={createdFirst.artwork_map[1]}");
            var swapped = _repository.SwapArtworks(0, 1, createdFirst);
            Debug.Log($"[SupabaseGalleryTest] artwork_map after swap:  [0]={swapped.artwork_map[0]}, [1]={swapped.artwork_map[1]}");

            // ── MapWorldToGallery ─────────────────────────────────────────────

            var worldMap = new Dictionary<int, int> { { 0, 10 }, { 1, 20 }, { 2, 30 } };
            var galleryForMapping = new GalleryData
            {
                artwork_map = new Dictionary<int, int> { { 0, 101 }, { 1, 202 } }
                // slot 2 intentionally absent — tests fallback to worldMap value
            };
            var mapped = _repository.MapWorldToGallery(worldMap, galleryForMapping);
            Debug.Log("[SupabaseGalleryTest] MapWorldToGallery results:");
            foreach (var entry in mapped)
                Debug.Log($"  - slot {entry.Key} => artwork ID {entry.Value}");

            // ── Delete ────────────────────────────────────────────────────────

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
    /// Creates a gallery and fetches it back by ID.
    /// </summary>
    public async void ManualTestCreateAndFetch()
    {
        Debug.Log("[SupabaseGalleryTest] Manual create+fetch initiated...");
        try
        {
            _repository = await SupabaseGalleryRepository.CreateAsync();
            var gallery = new GalleryData
            {
                name        = $"Manual Test - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                description = "Manually triggered test gallery",
                owner_id    = testOwnerId,
                artwork_ids = new List<int>(),
                artwork_map = new Dictionary<int, int>()
            };
            var created = await _repository.CreateGalleryAsync(gallery);
            var fetched = await _repository.GetGalleryAsync(created.id);
            Debug.Log($"[SupabaseGalleryTest] Created & fetched ID: {fetched.id}, name: {fetched.name}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] ManualTestCreateAndFetch FAILED: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs all galleries currently in the database.
    /// </summary>
    public async void ManualTestGetAll()
    {
        Debug.Log("[SupabaseGalleryTest] Manual get-all initiated...");
        try
        {
            _repository = await SupabaseGalleryRepository.CreateAsync();
            var all = await _repository.GetAllGalleriesAsync();
            Debug.Log($"[SupabaseGalleryTest] Found {all.Count} galleries:");
            foreach (var g in all)
                Debug.Log($"  - ID: {g.id}, name: {g.name}, artworks: [{string.Join(", ", g.artwork_ids)}], updated_at: {g.updated_at}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] ManualTestGetAll FAILED: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests Add, duplicate-add guard, PlaceInSlot, and Remove on a live gallery.
    /// </summary>
    public async void ManualTestArtworkInventory()
    {
        Debug.Log("[SupabaseGalleryTest] Manual inventory test initiated...");
        try
        {
            _repository = await SupabaseGalleryRepository.CreateAsync();

            var gallery = new GalleryData
            {
                name        = $"Inventory Test - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                description = "Artwork inventory test",
                owner_id    = testOwnerId,
                artwork_ids = new List<int> { 10, 20 },
                artwork_map = new Dictionary<int, int> { { 0, 10 }, { 1, 20 } }
            };

            var created = await _repository.CreateGalleryAsync(gallery);
            Debug.Log($"[SupabaseGalleryTest] Created gallery ID: {created.id}, ids: [{string.Join(", ", created.artwork_ids)}]");

            // Add
            var afterAdd = await _repository.AddArtworkToGalleryAsync(created.id, 30);
            Debug.Log($"[SupabaseGalleryTest] After Add(30): [{string.Join(", ", afterAdd.artwork_ids)}]");

            // Duplicate add — should stay the same count
            var afterDup = await _repository.AddArtworkToGalleryAsync(created.id, 30);
            Debug.Log($"[SupabaseGalleryTest] After duplicate Add(30): [{string.Join(", ", afterDup.artwork_ids)}] (count should be unchanged)");

            // Place in slot
            afterAdd.PlaceArtworkInSlot(2, 30);
            Debug.Log($"[SupabaseGalleryTest] Placed 30 in slot 2: artwork_map[2] = {afterAdd.artwork_map[2]}");

            // Remove — should also clear from slot
            var afterRemove = await _repository.RemoveArtworkFromGalleryAsync(created.id, 30);
            Debug.Log($"[SupabaseGalleryTest] After Remove(30): ids = [{string.Join(", ", afterRemove.artwork_ids)}], map has slot 2 = {afterRemove.artwork_map.ContainsKey(2)}");

            await _repository.DeleteGalleryAsync(created.id);
            Debug.Log("[SupabaseGalleryTest] Cleaned up. ManualTestArtworkInventory PASSED!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] ManualTestArtworkInventory FAILED: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests SwapArtworks locally — no network calls needed.
    /// </summary>
    public void ManualTestSwapArtworks()
    {
        Debug.Log("[SupabaseGalleryTest] Manual swap test initiated...");
        try
        {
            if (_repository == null)
            {
                Debug.LogWarning("[SupabaseGalleryTest] Repository not initialized. Call ManualTestCreateAndFetch first.");
                return;
            }
            var gallery = new GalleryData
            {
                artwork_ids = new List<int> { 111, 222, 333 },
                artwork_map = new Dictionary<int, int> { { 0, 111 }, { 1, 222 }, { 2, 333 } }
            };
            Debug.Log($"[SupabaseGalleryTest] Before swap(0,2): [0]={gallery.artwork_map[0]}, [1]={gallery.artwork_map[1]}, [2]={gallery.artwork_map[2]}");
            var result = _repository.SwapArtworks(0, 2, gallery);
            Debug.Log($"[SupabaseGalleryTest] After  swap(0,2): [0]={result.artwork_map[0]}, [1]={result.artwork_map[1]}, [2]={result.artwork_map[2]}");
            Debug.Log("[SupabaseGalleryTest] ManualTestSwapArtworks PASSED!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] ManualTestSwapArtworks FAILED: {ex.Message}");
        }
    }



    /// <summary>
    /// Tests MapWorldToGallery locally — no network calls needed.
    /// </summary>
    public void ManualTestMapWorldToGallery()
    {
        Debug.Log("[SupabaseGalleryTest] Manual map test initiated...");
        try
        {
            if (_repository == null)
            {
                Debug.LogWarning("[SupabaseGalleryTest] Repository not initialized. Call ManualTestCreateAndFetch first.");
                return;
            }
            var worldMap = new Dictionary<int, int> { { 0, 10 }, { 1, 20 }, { 2, 30 } };
            var gallery  = new GalleryData
            {
                artwork_map = new Dictionary<int, int> { { 0, 101 }, { 1, 202 } }
                // slot 2 absent — should fall back to worldMap value (30)
            };
            var result = _repository.MapWorldToGallery(worldMap, gallery);
            Debug.Log("[SupabaseGalleryTest] MapWorldToGallery results:");
            foreach (var entry in result)
                Debug.Log($"  - slot {entry.Key} => {entry.Value} (expected: {(entry.Key == 2 ? "30 (fallback)" : "gallery value")})");
            Debug.Log("[SupabaseGalleryTest] ManualTestMapWorldToGallery PASSED!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] ManualTestMapWorldToGallery FAILED: {ex.Message}");
        }
    }
}
