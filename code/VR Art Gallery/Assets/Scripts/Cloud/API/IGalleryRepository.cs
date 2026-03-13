using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IGalleryRepository
{
    // Gallery CRUD
    Task<GalleryData> CreateGalleryAsync(GalleryData gallery);
    Task<GalleryData> GetGalleryAsync(int id);
    Task<List<GalleryData>> GetAllGalleriesAsync();
    Task<GalleryData> UpdateGalleryAsync(GalleryData gallery);
    Task DeleteGalleryAsync(int id);

    // Gallery layout management
    Hashtable MapWorldToGallery(Hashtable worldMapCoord, GalleryData gallery);
    GalleryData SwapArtworks(int from_index, int to_index, GalleryData galleryObject);
}
