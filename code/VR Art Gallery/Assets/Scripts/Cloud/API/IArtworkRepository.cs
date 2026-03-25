using System.Collections.Generic;
using System.Threading.Tasks;

public interface IArtworkRepository
{
    Task<ArtworkData> CreateArtworkAsync(ArtworkData artwork);
    Task<ArtworkData> GetArtworkAsync(int id);
    Task<List<ArtworkData>> GetAllArtworksAsync();
    Task<ArtworkData> CreateArtworkWithUploadAsync(
        ArtworkData artwork,
        byte[] imageBytes,
        byte[] thumbnailBytes = null,
        string bucketName = "artwork-images",
        string extension = "png",
        string contentType = "image/png");
    Task<string> CreateSignedUrlAsync(string bucketName, string objectPath, int expiresInSeconds = 600);
    Task<byte[]> DownloadWithSignedUrlAsync(string signedUrl);
    Task DeleteObjectAsync(string bucketName, string objectPath);
}
