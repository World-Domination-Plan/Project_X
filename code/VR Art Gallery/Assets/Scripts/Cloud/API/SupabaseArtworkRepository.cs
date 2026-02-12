using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supabase;
using VRGallery.Cloud;

public class SupabaseArtworkRepository : IArtworkRepository
{
    private const string TableName = "artwork";
    public Supabase.Client SupabaseClientInstance { get; private set; }

    private SupabaseArtworkRepository(Supabase.Client client)
    {
        SupabaseClientInstance = client;
    }

    public static async Task<SupabaseArtworkRepository> CreateAsync()
    {
        if (SupabaseClientManager.Instance == null)
        {
            await SupabaseClientManager.InitializeAsync();
        }
        return new SupabaseArtworkRepository(SupabaseClientManager.Instance);
    }
    
    public async Task<ArtworkData> CreateArtworkAsync(ArtworkData artwork)
    {
        try
        {
            // Set timestamps
            artwork.created_at = DateTime.UtcNow;
            artwork.updated_at = DateTime.UtcNow;
            
            // Insert into Supabase
           
            var result = await SupabaseClientInstance
                .From<ArtworkData>()
                .Insert(artwork);
            
            return result.Model ?? artwork;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error creating artwork in Supabase: {ex.Message}");
            throw;
        }
    }
    
    public async Task<ArtworkData> GetArtworkAsync(int id)
    {
        try
        {
            var result = await SupabaseClientInstance
                .From<ArtworkData>()
                .Where(x => x.id == id)
                .Single();
            
            return result;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error retrieving artwork from Supabase: {ex.Message}");
            throw;
        }
    }
}
