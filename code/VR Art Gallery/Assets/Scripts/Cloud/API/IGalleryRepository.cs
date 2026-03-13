using System.Collections.Generic;
using System.Threading.Tasks;

public interface IGalleryRepository
{
    // ── Gallery CRUD ──────────────────────────────────────────────────────────
    Task<GalleryData> CreateGalleryAsync(GalleryData gallery);
    Task<GalleryData> GetGalleryAsync(int id);
    Task<List<GalleryData>> GetAllGalleriesAsync();
    Task<GalleryData> UpdateGalleryAsync(GalleryData gallery);
    Task DeleteGalleryAsync(int id);

    // ── Artwork inventory management ──────────────────────────────────────────

    /// <summary>
    /// Adds an artwork to the gallery's inventory and persists the change.
    /// </summary>
    Task<GalleryData> AddArtworkToGalleryAsync(int galleryId, int artworkId);

    /// <summary>
    /// Removes an artwork from the gallery's inventory and any slot it
    /// occupies, then persists the change.
    /// </summary>
    Task<GalleryData> RemoveArtworkFromGalleryAsync(int galleryId, int artworkId);

    // ── Layout management ─────────────────────────────────────────────────────
    Dictionary<int, int> MapWorldToGallery(Dictionary<int, int> worldMapCoord, GalleryData gallery);
    GalleryData SwapArtworks(int from_index, int to_index, GalleryData galleryObject);
}
