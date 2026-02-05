using System.Collections.Generic;
using System.Threading.Tasks;

public class MockArtworkRepository : IArtworkRepository
{
    private Dictionary<string, ArtworkData> artworks = new Dictionary<string, ArtworkData>();
    
    public Task<ArtworkData> CreateArtworkAsync(ArtworkData artwork)
    {
        artworks[artwork.id] = artwork;
        return Task.FromResult(artwork);
    }
    
    public Task<ArtworkData> GetArtworkAsync(string id)
    {
        if (artworks.TryGetValue(id, out var artwork))
        {
            return Task.FromResult(artwork);
        }
        return Task.FromResult<ArtworkData>(null);
    }
}
