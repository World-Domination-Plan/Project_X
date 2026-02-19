using System;
using System.Linq;
using System.Threading.Tasks;
using VRGallery.Cloud.Models;

namespace VRGallery.Cloud
{
    public class SupabaseArtistsRepository
    {
        private readonly Supabase.Client _client;

        public SupabaseArtistsRepository(Supabase.Client client)
        {
            _client = client;
        }

        public async Task EnsureProfileExistsAsync(string userId)
        {
            // Query first to avoid overwriting existing fields on login
            var existing = await _client
                .From<ArtistProfile>()
                .Where(x => x.user_id == userId)
                .Get();

            if (existing.Models != null && existing.Models.Any())
                return;

            var profile = new ArtistProfile
            {
                user_id = userId,
                created_at = DateTime.UtcNow,
                managed_gallery = Array.Empty<object>(),
                gallery_access = Array.Empty<object>()
            };

            await _client.From<ArtistProfile>().Insert(profile);
        }

        public async Task SetUsernameAsync(string userId, string username)
        {
            // Update only username (don’t touch arrays/created_at)
            var patch = new ArtistProfile
            {
                user_id = userId,
                username = username
            };

            await _client.From<ArtistProfile>().Upsert(patch);
        }
    }
}
