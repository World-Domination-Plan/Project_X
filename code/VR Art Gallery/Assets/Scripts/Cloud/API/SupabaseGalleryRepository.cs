using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supabase;
using VRGallery.Cloud;
using UnityEngine;

public class SupabaseGalleryRepository : IGalleryRepository
{
    public Supabase.Client SupabaseClientInstance { get; private set; }
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    private SupabaseGalleryRepository(Supabase.Client client)
    {
        SupabaseClientInstance = client;
        _supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")?.TrimEnd('/');
        _supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");
        if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_supabaseKey))
            throw new InvalidOperationException("SUPABASE_URL/SUPABASE_KEY missing.");
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
        try
        {
            gallery.created_at = DateTime.UtcNow;
            gallery.updated_at = DateTime.UtcNow;
            var result = await SupabaseClientInstance
                .From<GalleryData>()
                .Insert(gallery);
            return result.Model ?? gallery;
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
        try
        {
            gallery.updated_at = DateTime.UtcNow;
            var result = await SupabaseClientInstance
                .From<GalleryData>()
                .Where(x => x.id == gallery.id)
                .Update(gallery);
            return result.Model ?? gallery;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating gallery {gallery.id}: {ex.Message}");
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
            gallery.AddArtwork(artworkId);
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
