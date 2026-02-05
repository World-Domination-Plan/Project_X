using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading.Tasks;

public class ArtworkRepositoryTests
{
    private IArtworkRepository repository;
    
    [SetUp]
    public void SetUp()
    {
        // We'll create a mock implementation here
        repository = new MockArtworkRepository();
    }
    
    [Test]
    public async Task CreateArtwork_ShouldGenerateUniqueId()
    {
        // Arrange
        var artwork = new ArtworkData
        {
            title = "Test Artwork",
            ownerUserId = "user123",
            imageUrl = "testurl",
            thumbnailUrl = "thumbnailurl",
            galleryId = "galleryId"
        };
        
        // Act
        var created = await repository.CreateArtworkAsync(artwork);
        
        // Assert
        Assert.IsNotNull(created.id);
        Assert.IsNotEmpty(created.id);
    }
    
    [Test]
    public async Task GetArtwork_ShouldReturnCreatedArtwork()
    {
        // Arrange
        var artwork = new ArtworkData
        {
            title = "Test Artwork",
            ownerUserId = "user123",
            imageUrl = "testurl",
            thumbnailUrl = "thumbnailurl",
            galleryId = "galleryId"
        };
        var created = await repository.CreateArtworkAsync(artwork);
        
        // Act
        var retrieved = await repository.GetArtworkAsync(created.id);
        
        // Assert
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(created.id, retrieved.id);
        Assert.AreEqual(created.title, retrieved.title);
        Assert.AreEqual(created.ownerUserId, retrieved.ownerUserId);
        Assert.AreEqual(created.imageUrl, retrieved.imageUrl);
        Assert.AreEqual(created.thumbnailUrl, retrieved.thumbnailUrl);
        Assert.AreEqual(created.galleryId, retrieved.galleryId);
    }
    
    [Test]
    public async Task GetArtwork_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await repository.GetArtworkAsync("invalid-id-123");
        
        // Assert
        Assert.IsNull(result);
    }
}
