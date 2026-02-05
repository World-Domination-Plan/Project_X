using System.Threading.Tasks;

public interface IArtworkRepository
{
    Task<ArtworkData> CreateArtworkAsync(ArtworkData artwork);
    Task<ArtworkData> GetArtworkAsync(string id);
}
