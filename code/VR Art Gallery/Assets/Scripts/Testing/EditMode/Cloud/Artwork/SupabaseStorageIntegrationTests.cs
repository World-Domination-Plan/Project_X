using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using VRGallery.Cloud;

public class SupabaseStorageIntegrationTests
{
    private const string BucketName = "artworks";
    private const int SignedUrlExpirySeconds = 300;

    [Test]
    public async Task UploadFetchDelete_OneFixedImage_Works()
    {
        var imagePath = Path.Combine(Application.dataPath, "Scripts", "Testing", "test_noise.png");
        if (!File.Exists(imagePath))
            Assert.Fail($"Fixed test image missing: {imagePath}");

        long ownerId = 1;
        var ownerIdEnv = Environment.GetEnvironmentVariable("TEST_OWNER_ID");
        if (!string.IsNullOrWhiteSpace(ownerIdEnv) && long.TryParse(ownerIdEnv, out var parsedOwnerId))
            ownerId = parsedOwnerId;

        var repository = await SupabaseArtworkRepository.CreateAsync();
        var imageBytes = File.ReadAllBytes(imagePath);

        Assert.IsNotEmpty(imageBytes, "Fixed test image is empty.");
        var sourceSize = ReadPngDimensions(imageBytes);
        Assert.AreEqual(1024, sourceSize.width, "Fixed test image width must be 1024.");
        Assert.AreEqual(1024, sourceSize.height, "Fixed test image height must be 1024.");

        var artwork = new ArtworkData
        {
            title = $"CI Storage Test - {DateTime.UtcNow:yyyyMMdd_HHmmss}",
            owner_id = ownerId,
            image_url = string.Empty,
            thumbnail_url = string.Empty,
            filesize_bytes = 0
        };

        ArtworkData createdArtwork = null;

        try
        {
            createdArtwork = await repository.CreateArtworkWithUploadAsync(
                artwork,
                imageBytes,
                thumbnailBytes: null,
                bucketName: BucketName,
                extension: "png",
                contentType: "image/png");

            Assert.IsFalse(string.IsNullOrWhiteSpace(createdArtwork.image_url), "image_url path was not set.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(createdArtwork.thumbnail_url), "thumbnail_url path was not set.");

            var signedUrl = await repository.CreateSignedUrlAsync(BucketName, createdArtwork.image_url, SignedUrlExpirySeconds);
            Assert.IsTrue(signedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase), "Signed URL is invalid.");

            var fetchedBytes = await repository.DownloadWithSignedUrlAsync(signedUrl);
            Assert.IsNotEmpty(fetchedBytes, "Downloaded image bytes were empty.");

            var thumbnailSignedUrl = await repository.CreateSignedUrlAsync(BucketName, createdArtwork.thumbnail_url, SignedUrlExpirySeconds);
            var thumbnailBytes = await repository.DownloadWithSignedUrlAsync(thumbnailSignedUrl);
            Assert.IsNotEmpty(thumbnailBytes, "Downloaded thumbnail bytes were empty.");

            var thumbnailSize = ReadPngDimensions(thumbnailBytes);
            Assert.AreEqual(512, thumbnailSize.width, "Generated thumbnail width should be 512.");
            Assert.AreEqual(512, thumbnailSize.height, "Generated thumbnail height should be 512.");
        }
        finally
        {
            if (createdArtwork != null)
            {
                if (!string.IsNullOrWhiteSpace(createdArtwork.image_url))
                    await repository.DeleteObjectAsync(BucketName, createdArtwork.image_url);

                if (!string.IsNullOrWhiteSpace(createdArtwork.thumbnail_url))
                    await repository.DeleteObjectAsync(BucketName, createdArtwork.thumbnail_url);
            }
        }
    }
    // Helper method to read PNG dimensions directly from the byte array without fully decoding the image
    private static (int width, int height) ReadPngDimensions(byte[] pngBytes)
    {
        if (pngBytes == null || pngBytes.Length < 24) // Minimum PNG file size to contain signature and IHDR chunk header
            throw new ArgumentException("PNG byte array is invalid.", nameof(pngBytes));
        // PNG signature + IHDR chunk header is 24 bytes. Dimensions are at fixed offsets within the IHDR chunk.
        var pngSig = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }; // Standard PNG file signature
        for (var i = 0; i < pngSig.Length; i++)
        {
            if (pngBytes[i] != pngSig[i])
                throw new InvalidOperationException("Data is not a PNG file.");
        }
        // Dimensions are stored in the IHDR chunk, which starts immediately after the 8-byte signature and 8-byte chunk header (length + type)
        var width = (pngBytes[16] << 24) | (pngBytes[17] << 16) | (pngBytes[18] << 8) | pngBytes[19];
        var height = (pngBytes[20] << 24) | (pngBytes[21] << 16) | (pngBytes[22] << 8) | pngBytes[23];
        return (width, height);
    }
}
