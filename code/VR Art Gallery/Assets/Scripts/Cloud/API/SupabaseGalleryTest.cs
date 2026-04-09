using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRGallery.Cloud;

/// <summary>
/// Simplified manual test script for Gallery operations.
/// Allows manual entry of all fields for gallery creation and updates.
/// </summary>
public class SupabaseGalleryTest : MonoBehaviour
{
    [System.Serializable]
    public struct SlotMapping
    {
        public int slotIndex;
        public int artworkId;
    }

    [Header("Gallery Data Input")]
    [SerializeField] private string galleryName = "Manual Test Gallery";
    [SerializeField] private string galleryDescription = "Manually entered description";
    [SerializeField] private int ownerId = 1;
    [SerializeField] private List<int> artworkIds = new List<int>();
    [SerializeField] private List<SlotMapping> artworkMapInput = new List<SlotMapping>();

    [Header("Test Controls")]
    [SerializeField] private bool testOnStart = false;
    [SerializeField] private int targetGalleryId = 0;

    private SupabaseGalleryRepository _repository;

    private async void Start()
    {
        if (testOnStart)
        {
            await ManualTestCreateFromFields();
        }
    }

    private async Task EnsureRepository()
    {
        if (_repository == null)
            _repository = await SupabaseGalleryRepository.CreateAsync();
    }

    [ContextMenu("Create Gallery from Fields")]
    public async Task ManualTestCreateFromFields()
    {
        Debug.Log("[SupabaseGalleryTest] Creating gallery from manual fields...");
        try
        {
            await EnsureRepository();
            
            var map = new Dictionary<int, int>();
            foreach (var mapping in artworkMapInput)
            {
                map[mapping.slotIndex] = mapping.artworkId;
            }

            var gallery = new GalleryData
            {
                name = galleryName,
                description = galleryDescription,
                owner_id = ownerId,
                artwork_ids = new List<int>(artworkIds),
                artwork_map = map
            };

            var created = await _repository.CreateGalleryAsync(gallery);
            targetGalleryId = created.id;
            Debug.Log($"[SupabaseGalleryTest] SUCCESS! Created Gallery ID: {created.id}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] Create FAILED: {ex.Message}");
        }
    }

    [ContextMenu("Fetch Gallery by ID")]
    public async Task ManualTestFetchById()
    {
        if (targetGalleryId <= 0) return;
        Debug.Log($"[SupabaseGalleryTest] Fetching Gallery ID: {targetGalleryId}");
        try
        {
            await EnsureRepository();
            var fetched = await _repository.GetGalleryAsync(targetGalleryId);
            Debug.Log($"[SupabaseGalleryTest] Fetched: {fetched.name}, Owner: {fetched.owner_id}, Artworks: {fetched.artwork_ids.Count}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] Fetch FAILED: {ex.Message}");
        }
    }

    [ContextMenu("Delete Target Gallery")]
    public async Task ManualTestDelete()
    {
        if (targetGalleryId <= 0) return;
        Debug.Log($"[SupabaseGalleryTest] Deleting Gallery ID: {targetGalleryId}");
        try
        {
            await EnsureRepository();
            await _repository.DeleteGalleryAsync(targetGalleryId);
            Debug.Log($"[SupabaseGalleryTest] Deleted Gallery ID: {targetGalleryId}");
            targetGalleryId = 0;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SupabaseGalleryTest] Delete FAILED: {ex.Message}");
        }
    }
}
