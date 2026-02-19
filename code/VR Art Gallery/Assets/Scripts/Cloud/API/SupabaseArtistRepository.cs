using System;
using System.Threading.Tasks;
using Supabase;

namespace VRGallery.Cloud
{
    /// <summary>
    /// Supabase implementation of IArtistRepository
    /// Handles artist profile CRUD operations
    /// </summary>
    public class SupabaseArtistRepository : IArtistRepository
    {
        private readonly ISupabaseClient supabaseClient;

        public SupabaseArtistRepository(ISupabaseClient client)
        {
            supabaseClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<bool> CreateArtistProfileAsync(string userId, string username)
        {
            try
            {
                var profile = new ArtistProfile
                {
                    user_id = userId,
                    username = username,
                    created_at = DateTime.UtcNow
                };

                var client = supabaseClient.GetClient();
                await client.From<ArtistProfile>().Insert(profile);
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SupabaseArtistRepository] Error creating profile: {ex.Message}");
                return false;
            }
        }

        public async Task<ArtistProfile> GetArtistProfileAsync(string userId)
        {
            try
            {
                var client = supabaseClient.GetClient();
                var response = await client.From<ArtistProfile>()
                    .Where(x => x.user_id == userId)
                    .Single();
                return response;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SupabaseArtistRepository] Error getting profile: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateArtistProfileAsync(ArtistProfile profile)
        {
            try
            {
                if (profile == null)
                    return false;

                var client = supabaseClient.GetClient();
                await client.From<ArtistProfile>().Update(profile);
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SupabaseArtistRepository] Error updating profile: {ex.Message}");
                return false;
            }
        }
    }
}