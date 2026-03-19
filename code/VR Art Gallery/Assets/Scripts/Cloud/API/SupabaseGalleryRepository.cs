using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supabase;
using VRGallery.Cloud;
using UnityEngine;

public class SupabaseGalleryRepository : IGalleryRepository
{
    private const string TableName = "gallery";
    public Supabase.Client SupabaseClientInstance { get; private set; }

    private SupabaseGalleryRepository(Supabase.Client client)
    {
        SupabaseClientInstance = client ?? throw new ArgumentNullException(nameof(client));
    }

    public static async Task<SupabaseGalleryRepository> CreateAsync()
    {
        if (!SupabaseClientManager.IsInitialized)
            await SupabaseClientManager.InitializeAsync();
        return new SupabaseGalleryRepository(SupabaseClientManager.Instance);
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    public async Task<GalleryData> CreateGalleryAsync(GalleryData gallery)
    {
        if (gallery == null) throw new ArgumentNullException(nameof(gallery));

        try
        {
            gallery.created_at = DateTime.UtcNow;
            gallery.updated_at = DateTime.UtcNow;

            // Insert gallery row
            var insertResult = await SupabaseClientInstance
                .From<GalleryData>()
                .Insert(gallery);

            var createdGallery = insertResult.Model ?? gallery;

            // Attempt to add created gallery id to the owner's managed_gallery array.
            // The artists table model maps to ArtistProfile; managed_gallery is stored as string[] in the model.
            try
            {
                // Fetch artist profile by numeric user_id (artists.user_id)
                var profile = await SupabaseClientInstance
                    .From<ArtistProfile>()
                    .Where(x => x.user_id == createdGallery.owner_id)
                    .Single();

                if (profile != null)
                {
                    var managed = new List<string>();
                    if (profile.managed_gallery != null)
                        managed.AddRange(profile.managed_gallery);

                    // Ensure we don't add duplicates
                    var galleryIdString = createdGallery.id.ToString();
                    if (!managed.Contains(galleryIdString))
                    {
                        managed.Add(galleryIdString);
                        profile.managed_gallery = managed.ToArray();

                        // Persist updated profile back to the artists table
                        await SupabaseClientInstance
                            .From<ArtistProfile>()
                            .Update(profile);
                    }
                }
                else
                {
                    Debug.LogWarning($"[SupabaseGalleryRepository] Artist profile not found for user_id {createdGallery.owner_id}; managed_gallery not updated.");
                }
            }
            catch (Exception exProfile)
            {
                // Profile update failed: rollback created gallery and surface an error so caller must retry.
                Debug.LogError($"[SupabaseGalleryRepository] Failed to update artist managed_gallery for owner_id {createdGallery.owner_id}: {exProfile.Message}");

                // Attempt to roll back the created gallery if we have a valid id
                try
                {
                    if (createdGallery != null && createdGallery.id > 0)
                    {
                        await SupabaseClientInstance
                            .From<GalleryData>()
                            .Where(x => x.id == createdGallery.id)
                            .Delete();

                        Debug.LogError($"[SupabaseGalleryRepository] Rolled back gallery id {createdGallery.id} due to profile update failure.");
                    }
                    else
                    {
                        Debug.LogError("[SupabaseGalleryRepository] Created gallery id missing; cannot roll back automatically.");
                    }
                }
                catch (Exception exDelete)
                {
                    Debug.LogError($"[SupabaseGalleryRepository] Failed to rollback gallery id {createdGallery?.id}: {exDelete.Message}");
                    // Even if rollback fails, we still surface the original profile update failure.
                }

                // Surface a clear error so the caller must create the gallery again.
                throw new Exception("Failed to update artist profile after creating gallery. The created gallery has been rolled back (if possible). Please try creating the gallery again.", exProfile);
            }

            return createdGallery;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating gallery: {ex.Message}");
            throw;
        }
    }

    public async Task<GalleryData> GetGalleryAsync(int id)
    {
        try
        {
            return await SupabaseClientInstance
                .From<GalleryData>()
                .Where(x => x.id == id)
                .Single();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error retrieving gallery {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<GalleryData>> GetAllGalleriesAsync()
    {
        try
        {
            var result = await SupabaseClientInstance
                .From<GalleryData>()
                .Get();
            return result.Models;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error retrieving all galleries: {ex.Message}");
            throw;
        }
    }

    public async Task<GalleryData> UpdateGalleryAsync(GalleryData gallery)
    {
        if (gallery == null) throw new ArgumentNullException(nameof(gallery));

        try
        {
            gallery.updated_at = DateTime.UtcNow;
            var result = await SupabaseClientInstance
                .From<GalleryData>()
                .Update(gallery);
            return result.Model ?? gallery;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating gallery {gallery?.id}: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteGalleryAsync(int id)
    {
        try
        {
            await SupabaseClientInstance
                .From<GalleryData>()
                .Where(x => x.id == id)
                .Delete();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deleting gallery {id}: {ex.Message}");
            throw;
        }
    }

    // ── Artwork inventory management ──────────────────────────────────────────

    public async Task<GalleryData> AddArtworkToGalleryAsync(int galleryId, int artworkId)
    {
        try
        {
            var gallery = await GetGalleryAsync(galleryId);
            if (gallery == null)
                throw new InvalidOperationException($"Gallery {galleryId} not found.");

            // Ensure artwork_ids contains the artwork
            gallery.AddArtwork(artworkId);

            // Ensure artwork_map exists
            if (gallery.artwork_map == null)
                gallery.artwork_map = new Dictionary<int, int>();

            // If artwork is already placed in the map, do nothing regarding map placement
            if (gallery.artwork_map.ContainsValue(artworkId))
            {
                Debug.Log($"[SupabaseGalleryRepository] Artwork {artworkId} already present in gallery {galleryId} map; added to list only (if not present).");
                return await UpdateGalleryAsync(gallery);
            }

            // Determine next index: max existing key + 1 (or 0 if empty)
            int nextIndex;
            if (gallery.artwork_map.Count == 0)
            {
                nextIndex = 0;
            }
            else
            {
                nextIndex = gallery.artwork_map.Keys.Max() + 1;
            }

            // Place artwork at the next index
            gallery.artwork_map[nextIndex] = artworkId;
            Debug.Log($"[SupabaseGalleryRepository] Placed artwork {artworkId} into gallery {galleryId} slot {nextIndex}.");

            return await UpdateGalleryAsync(gallery);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error adding artwork {artworkId} to gallery {galleryId}: {ex.Message}");
            throw;
        }
    }

    public async Task<GalleryData> RemoveArtworkFromGalleryAsync(int galleryId, int artworkId)
    {
        try
        {
            var gallery = await GetGalleryAsync(galleryId);
            gallery.RemoveArtwork(artworkId);
            return await UpdateGalleryAsync(gallery);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error removing artwork {artworkId} from gallery {galleryId}: {ex.Message}");
            throw;
        }
    }

    // ── Layout management ─────────────────────────────────────────────────────

    public Dictionary<int, int> MapWorldToGallery(Dictionary<int, int> worldMapCoord, GalleryData gallery)
    {
        var gallerySlotMap = new Dictionary<int, int>();
        foreach (var entry in worldMapCoord)
        {
            if (gallery.artwork_map != null && gallery.artwork_map.TryGetValue(entry.Key, out int artworkId))
                gallerySlotMap[entry.Key] = artworkId;
            else
                gallerySlotMap[entry.Key] = entry.Value;
        }
        return gallerySlotMap;
    }

    public GalleryData SwapArtworks(int from_index, int to_index, GalleryData galleryObject)
    {
        var artwork_table = galleryObject.artwork_map;
        if (!artwork_table.ContainsKey(from_index) || !artwork_table.ContainsKey(to_index))
            throw new ArgumentOutOfRangeException("Swap indices do not exist in artwork_map.");
        int temp = artwork_table[to_index];
        artwork_table[to_index] = artwork_table[from_index];
        artwork_table[from_index] = temp;
        galleryObject.artwork_map = artwork_table;
        return galleryObject;
    }
}
