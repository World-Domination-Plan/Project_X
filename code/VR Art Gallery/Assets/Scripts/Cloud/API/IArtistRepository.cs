using System.Collections.Generic;
using System.Threading.Tasks;

namespace VRGallery.Cloud
{
    public interface IArtistRepository
    {
        Task<bool> CreateArtistProfileAsync(string userId, string username);
        Task<ArtistProfile> GetArtistProfileAsync(string userId);
        Task<List<ArtistProfile>> GetAllArtistsAsync();
        Task<bool> UpdateArtistProfileAsync(ArtistProfile profile);
    }
}