using System.Threading.Tasks;

namespace VRGallery.Cloud
{
    public interface IArtistRepository
    {
        Task<bool> CreateArtistProfileAsync(string userId, string username);
        Task<ArtistProfile> GetArtistProfileAsync(string userId);
        Task<bool> UpdateArtistProfileAsync(ArtistProfile profile);
    }
}