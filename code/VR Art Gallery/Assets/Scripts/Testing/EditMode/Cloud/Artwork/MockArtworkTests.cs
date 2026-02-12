using System.Collections.Generic;
using System.Threading.Tasks;

public class MockArtworkRepository : IArtworkRepository
{
    private Dictionary<int, ArtworkData> artworks = new Dictionary<int, ArtworkData>();
    
    public Task<ArtworkData> CreateArtworkAsync(ArtworkData artwork)
    {
        artwork.id = artworks.Count;
        artworks[artwork.id] = artwork;
        return Task.FromResult(artwork);
    }
    
    public Task<ArtworkData> GetArtworkAsync(int id)
    {
        if (artworks.TryGetValue(id, out var artwork))
        {
            return Task.FromResult(artwork);
        }
        return Task.FromResult<ArtworkData>(null);
    }
}
