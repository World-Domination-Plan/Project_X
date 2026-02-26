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
    public async Task GetArtwork_ShouldReturnCreatedArtwork()
    {
        // Arrange
        var artwork = new ArtworkData
        {
            title = "Test Artwork",
            owner_id = 1,
            image_url = "testurl",
            thumbnail_url = "thumbnailurl"
        };
        var created = await repository.CreateArtworkAsync(artwork);
        
        // Act
        var retrieved = await repository.GetArtworkAsync(created.id);
        
        // Assert
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(created.id, retrieved.id);
        Assert.AreEqual(created.title, retrieved.title);
        Assert.AreEqual(created.owner_id, retrieved.owner_id);
        Assert.AreEqual(created.image_url, retrieved.image_url);
        Assert.AreEqual(created.thumbnail_url, retrieved.thumbnail_url);
    }
    
    [Test]
    public async Task GetArtwork_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await repository.GetArtworkAsync(100);
        
        // Assert
        Assert.IsNull(result);
    }
}
