using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using VRGallery.Cloud;

/// <summary>
/// Minimalized automated test for Gallery Repository.
/// Combines all CRUD operations and data validation into a single lifecycle test
/// to minimize redundant API calls and environment setup.
/// </summary>
public class GalleryRepositoryTestAndPopulate
{
    private SupabaseGalleryRepository galleryRepository;
    private SupabaseArtworkRepository artworkRepository;
    private SupabaseArtistRepository artistRepository;

    private const string TEST_GALLERY_NAME = "Lifecycle Test Gallery";
    private const string TEST_GALLERY_DESCRIPTION = "Automated Lifecycle Test Description";
    private const int ARTWORK_SLOTS = 9;

    private ArtistProfile testArtist;
    private List<ArtworkData> selectedArtworks;
    private int? lastCreatedGalleryId;

    [SetUp]
    public async Task SetUpAsync()
    {
        galleryRepository = await SupabaseGalleryRepository.CreateAsync();
        artworkRepository = await SupabaseArtworkRepository.CreateAsync();
        artistRepository = await SupabaseArtistRepository.CreateAsync();
        lastCreatedGalleryId = null;
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        if (lastCreatedGalleryId.HasValue)
        {
            try
            {
                await galleryRepository.DeleteGalleryAsync(lastCreatedGalleryId.Value);
                Debug.Log($"[GalleryRepositoryTestAndPopulate] Cleaned up test gallery {lastCreatedGalleryId.Value}");
            }
            catch { }
            finally { lastCreatedGalleryId = null; }
        }
    }

    [Test]
    public async Task FullGalleryLifeCycle()
    {
        Debug.Log("[GalleryRepositoryTestAndPopulate] Starting Consolidating Gallery Lifecycle Test");

        // 1. DATA PREPARATION (Fetch artist and artworks)
        testArtist = await FetchRandomArtistAsync();
        selectedArtworks = await FetchRandomArtworksAsync(ARTWORK_SLOTS);
        
        Assert.IsNotNull(testArtist, "Artist must be available for testing");
        Assert.GreaterOrEqual(selectedArtworks.Count, ARTWORK_SLOTS, "Insufficient artworks for testing");

        // 2. CREATE
        var galleryToCreate = new GalleryData
        {
            name = TEST_GALLERY_NAME,
            description = TEST_GALLERY_DESCRIPTION,
            owner_id = testArtist.user_id,
            artwork_ids = selectedArtworks.Select(a => a.id).ToList(),
            artwork_map = CreateArtworkMap(selectedArtworks),
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        var created = await galleryRepository.CreateGalleryAsync(galleryToCreate);
        lastCreatedGalleryId = created.id;

        Assert.IsNotNull(created, "Gallery creation failed");
        Assert.Greater(created.id, 0, "Gallery ID should be auto-generated");
        Debug.Log($"[GalleryRepositoryTestAndPopulate] ✓ CREATE: ID {created.id}");

        // 3. READ & VERIFY
        var retrieved = await galleryRepository.GetGalleryAsync(created.id);
        Assert.IsNotNull(retrieved, "Gallery should be readable after creation");
        Assert.AreEqual(TEST_GALLERY_NAME, retrieved.name, "Name mismatch");
        Assert.AreEqual(ARTWORK_SLOTS, retrieved.artwork_ids.Count, "Artwork count mismatch");
        Debug.Log("[GalleryRepositoryTestAndPopulate] ✓ READ: Data persists correctly");

        // 4. UPDATE & TIMESTAMP
        var originalTimestamp = retrieved.updated_at;
        await Task.Delay(1100); // 1.1s delay to cross the 1s boundary for Postgres timestamp precision
        
        retrieved.name = TEST_GALLERY_NAME + " (Updated)";
        var updated = await galleryRepository.UpdateGalleryAsync(retrieved);
        
        Assert.AreEqual(TEST_GALLERY_NAME + " (Updated)", updated.name, "Update failed to change name");
        // We use GreaterOrEqual or just compare that it's NOT the same if possible
        Assert.True(updated.updated_at != originalTimestamp, $"Timestamp should change. Old: {originalTimestamp:O}, New: {updated.updated_at:O}");
        Debug.Log("[GalleryRepositoryTestAndPopulate] ✓ UPDATE: Name and timestamp changed");

        // 5. LOOKUP INVENTORY (Specific details)
        var firstArtworkId = updated.artwork_ids[0];
        var artworkDetails = await artworkRepository.GetArtworkAsync(firstArtworkId);
        Assert.IsNotNull(artworkDetails, "Should be able to look up artwork details from gallery inventory");
        Assert.IsFalse(string.IsNullOrEmpty(artworkDetails.title), "Artwork title should be present");
        Debug.Log($"[GalleryRepositoryTestAndPopulate] ✓ LOOKUP: Artwork \"{artworkDetails.title}\" verified");

        // 6. DELETE
        await galleryRepository.DeleteGalleryAsync(created.id);
        var afterDelete = await galleryRepository.GetGalleryAsync(created.id);
        Assert.IsNull(afterDelete, "Gallery should be null after deletion");
        lastCreatedGalleryId = null; // Mark as already cleaned up for TearDown
        Debug.Log("[GalleryRepositoryTestAndPopulate] ✓ DELETE: Successfully removed");

        Debug.Log("[GalleryRepositoryTestAndPopulate] FULL LIFECYCLE COMPLETED SUCCESSFULLY");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ArtistProfile> FetchRandomArtistAsync()
    {
        var allArtists = await artistRepository.GetAllArtistsAsync();
        if (allArtists == null || allArtists.Count == 0)
            throw new InvalidOperationException("No artists found.");
        return allArtists[0];
    }

    private async Task<List<ArtworkData>> FetchRandomArtworksAsync(int count)
    {
        var allArtworks = await artworkRepository.GetAllArtworksAsync();
        if (allArtworks == null || allArtworks.Count < count)
            throw new InvalidOperationException($"Insufficient artworks (found {allArtworks?.Count ?? 0}, need {count})");
        return allArtworks.OrderBy(x => UnityEngine.Random.value).Take(count).ToList();
    }

    private Dictionary<int, int> CreateArtworkMap(List<ArtworkData> artworks)
    {
        var map = new Dictionary<int, int>();
        for (int i = 0; i < artworks.Count; i++) map[i] = artworks[i].id;
        return map;
    }
}
