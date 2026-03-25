using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VRGallery.Cloud;

/// <summary>
/// Dedicated test for SupabaseArtistRepository fetching logic.
/// Isalotes SELECT operations to debug why GetAllArtistsAsync returns 0.
/// </summary>
public class ArtistRepositoryTest
{
    private SupabaseArtistRepository artistRepository;
    private const string TARGET_USER_ID = "c3acc493-9042-4a53-8728-6aff45d46da8";

    [SetUp]
    public async Task SetUpAsync()
    {
        artistRepository = await SupabaseArtistRepository.CreateAsync();
        Debug.Log("[ArtistRepositoryTest] SetUp: ArtistRepository initialized");
    }

    /// <summary>
    /// Test fetching all artists.
    /// If this returns 0, it confirms an RLS policy issue or empty table.
    /// </summary>
    [Test]
    public async Task GetAllArtists_ShouldReturnResults()
    {
        // Act
        var allArtists = await artistRepository.GetAllArtistsAsync();

        // Assert
        Assert.IsNotNull(allArtists, "Result list should not be null");
        Assert.Greater(allArtists.Count, 0, "No artists found in database. RLS is likely blocking SELECT.");
        
        Debug.Log($"[ArtistRepositoryTest] Successfully fetched {allArtists.Count} artists.");
        foreach (var artist in allArtists)
        {
            Debug.Log($"[ArtistRepositoryTest] Artist: user_id={artist.user_id}, Username={artist.username}, AuthID={artist.auth_user_id}");
        }
    }

    /// <summary>
    /// Test fetching a specific artist by Auth User ID.
    /// If this works while GetAllArtistsAsync fails, it indicates a targeted RLS policy.
    /// </summary>
    [Test]
    public async Task GetSpecificArtist_ShouldReturnProfile()
    {
        // Act
        var profile = await artistRepository.GetArtistProfileAsync(TARGET_USER_ID);

        // Assert
        if (profile == null)
        {
            Debug.LogError($"[ArtistRepositoryTest] Failed to fetch profile for user {TARGET_USER_ID}. This might be an RLS issue.");
            Assert.Fail($"Profile for {TARGET_USER_ID} not found.");
        }
        else
        {
            Debug.Log($"[ArtistRepositoryTest] Successfully fetched profile: user_id={profile.user_id}, Username={profile.username}");
            Assert.AreEqual(TARGET_USER_ID, profile.auth_user_id, "Auth user ID should match");
        }
    }
}
