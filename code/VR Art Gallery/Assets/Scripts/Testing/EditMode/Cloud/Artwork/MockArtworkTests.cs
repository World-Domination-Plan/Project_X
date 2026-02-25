using System.Collections.Generic;
using System.Threading.Tasks;

public class MockArtworkRepository : IArtworkRepository
{
    private Dictionary<long, ArtworkData> artworks = new Dictionary<long, ArtworkData>();

    public Task<ArtworkData> CreateArtworkAsync(ArtworkData artwork)
    {
        artwork.id = (long)artworks.Count;
        artworks[artwork.id] = artwork;
        return Task.FromResult(artwork);
    }

    public Task<ArtworkData> GetArtworkAsync(long id)
    {
        if (artworks.TryGetValue(id, out var artwork))
        {
            return Task.FromResult(artwork);
        }
        return Task.FromResult<ArtworkData>(null);
    }
}
