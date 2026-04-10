using System;
using System.Linq;
using System.Threading.Tasks;

namespace VRGallery.Cloud
{
    public class SupabaseArtworkAccessService
    {
        private readonly ISupabaseClient supabaseClient;

        public SupabaseArtworkAccessService(ISupabaseClient supabaseClient)
        {
            this.supabaseClient = supabaseClient ?? throw new ArgumentNullException(nameof(supabaseClient));
        }

        public async Task<bool> CanCurrentUserJoinArtworkAsync(int artworkId)
        {
            if (artworkId <= 0)
                return false;

            if (!supabaseClient.IsAuthenticated)
                return false;

            var client = supabaseClient.GetClient();
            if (client == null)
                return false;

            var currentAuthUserId = supabaseClient.CurrentUserId;
            if (string.IsNullOrWhiteSpace(currentAuthUserId))
                return false;

            ArtistProfile profile;
            try
            {
                profile = await client
                    .From<ArtistProfile>()
                    .Where(x => x.auth_user_id == currentAuthUserId)
                    .Single();
            }
            catch
            {
                return false;
            }

            if (profile == null)
                return false;

            var currentArtistId = profile.user_id;

            ArtworkData artwork;
            try
            {
                artwork = await client
                    .From<ArtworkData>()
                    .Where(x => x.id == artworkId)
                    .Single();
            }
            catch
            {
                return false;
            }

            if (artwork == null)
                return false;

            if (artwork.owner_id == currentArtistId)
                return true;

            try
            {
                var aclResponse = await client
                    .From<AclArtworkEntry>()
                    .Where(x => x.artwork_id == artworkId)
                    .Where(x => x.artist_id == currentArtistId)
                    .Where(x => x.status == "active")
                    .Get();

                return aclResponse?.Models != null && aclResponse.Models.Any();
            }
            catch
            {
                return false;
            }
        }
    }
}
