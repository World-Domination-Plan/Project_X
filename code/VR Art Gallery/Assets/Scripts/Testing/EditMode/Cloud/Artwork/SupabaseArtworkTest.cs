using System;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Supabase;
using UnityEngine;
using VRGallery.Cloud;

public class SupabaseArtworkTest
{
    private const string BucketName = "artworks";
    private const int SignedUrlExpirySeconds = 300;

    private bool _supabaseInitialized;

    private static string SupabaseUrl => Environment.GetEnvironmentVariable("SUPABASE_URL");
    private static string AnonKey => Environment.GetEnvironmentVariable("SUPABASE_KEY");

    // Fixed test accounts (no extra env vars required).
    // These are intended for CI-only integration testing; they are created if missing.
    private const string OwnerEmail = "supabase-integration-owner@example.com";
    private const string CollabEmail = "supabase-integration-collab@example.com";
    private const string TestPassword = "P@ssw0rd123!";

    private Client _ownerClient;
    private int _ownerArtistId;
    private bool _didCreateOwnerUser;

    private Client _collabClient;
    private int _collabArtistId;
    private bool _didCreateCollabUser;

    private static readonly HttpClient _httpClient = new HttpClient();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        try
        {
            await SupabaseClientManager.InitializeAsync();
            _supabaseInitialized = true;
        }
        catch
        {
            // Supabase not configured for this environment; tests will be skipped.
            _supabaseInitialized = false;
            return;
        }

        (_ownerClient, _didCreateOwnerUser) = await SignInOrRegisterUserAsync(OwnerEmail, TestPassword);
        _ownerArtistId = await GetOrCreateArtistIdAsync(_ownerClient);

        (_collabClient, _didCreateCollabUser) = await SignInOrRegisterUserAsync(CollabEmail, TestPassword);
        _collabArtistId = await GetOrCreateArtistIdAsync(_collabClient);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (!_supabaseInitialized)
            return;

        // Only attempt to delete users we created (avoid accidentally deleting existing accounts).
        if (_didCreateOwnerUser && _ownerClient?.Auth?.CurrentSession?.User != null)
            await TryDeleteUserAsync(_ownerClient.Auth.CurrentSession.User.Id);

        if (_didCreateCollabUser && _collabClient?.Auth?.CurrentSession?.User != null)
            await TryDeleteUserAsync(_collabClient.Auth.CurrentSession.User.Id);
    }

    [Test]
    public async Task CreateUploadDownloadAndCleanup_Works()
    {
        if (!_supabaseInitialized)
            Assert.Ignore("Supabase is not configured for integration tests.");

        // Some CI runners don't have a default user account, so allow overriding.
        var ownerId = 1;
        var ownerIdEnv = Environment.GetEnvironmentVariable("TEST_OWNER_ID");
        if (!string.IsNullOrWhiteSpace(ownerIdEnv) && int.TryParse(ownerIdEnv, out var parsedOwnerId))
            ownerId = parsedOwnerId;

        var repository = await SupabaseArtworkRepository.CreateAsync();

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        var imageBytes = texture.EncodeToPNG();
        Assert.IsNotNull(imageBytes);
        Assert.IsNotEmpty(imageBytes, "Generated texture should encode to a non-empty PNG.");

        var artwork = new ArtworkData
        {
            title = $"CI test - {DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}",
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

            Assert.IsNotNull(createdArtwork, "CreateArtworkWithUploadAsync returned null.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(createdArtwork.image_url), "image_url should be set.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(createdArtwork.thumbnail_url), "thumbnail_url should be set.");

            var signedUrl = await repository.CreateSignedUrlAsync(BucketName, createdArtwork.image_url, SignedUrlExpirySeconds);
            Assert.IsTrue(signedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase), "Signed URL should start with http.");

            var downloadedBytes = await repository.DownloadWithSignedUrlAsync(signedUrl);
            Assert.IsNotEmpty(downloadedBytes, "Downloaded image bytes should not be empty.");

            var thumbSignedUrl = await repository.CreateSignedUrlAsync(BucketName, createdArtwork.thumbnail_url, SignedUrlExpirySeconds);
            var thumbBytes = await repository.DownloadWithSignedUrlAsync(thumbSignedUrl);
            Assert.IsNotEmpty(thumbBytes, "Downloaded thumbnail bytes should not be empty.");
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

            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    [Test]
    public async Task InsertPolicy_AllowsOnlyMatchingOwnerId()
    {
        if (!_supabaseInitialized)
            Assert.Ignore("Supabase is not configured for integration tests.");

        var okArtwork = await InsertArtworkAsync(_ownerClient, _ownerArtistId);
        Assert.AreEqual(_ownerArtistId, okArtwork.owner_id);
        await DeleteArtworkByIdAsync(okArtwork.id, _ownerClient);

        var badArtwork = new ArtworkData
        {
            title = $"Policy test - {Guid.NewGuid():N}",
            owner_id = _collabArtistId,
            image_url = string.Empty,
            thumbnail_url = string.Empty,
            filesize_bytes = 0,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        Assert.ThrowsAsync<Exception>(async () => await _ownerClient.From<ArtworkData>().Insert(badArtwork));
    }

    [Test]
    public async Task UpdatePolicy_OwnerCanUpdateButCannotChangeOwnerId()
    {
        if (!_supabaseInitialized)
            Assert.Ignore("Supabase is not configured for integration tests.");

        var artwork = await InsertArtworkAsync(_ownerClient, _ownerArtistId);
        try
        {
            artwork.title = "Owner Updated Title";
            var updated = await _ownerClient.From<ArtworkData>().Update(artwork);
            Assert.AreEqual("Owner Updated Title", updated.Model.title);

            artwork.owner_id = _collabArtistId;
            Assert.ThrowsAsync<Exception>(async () => await _ownerClient.From<ArtworkData>().Update(artwork));
        }
        finally
        {
            await DeleteArtworkByIdAsync(artwork.id, _ownerClient);
        }
    }

    [Test]
    public async Task UpdatePolicy_CollaboratorCanUpdateWhenInAcl_ButCannotChangeOwnerId()
    {
        if (!_supabaseInitialized)
            Assert.Ignore("Supabase is not configured for integration tests.");

        var artwork = await InsertArtworkAsync(_ownerClient, _ownerArtistId);
        try
        {
            await EnsureAclEntryAsync(artwork.id, _collabArtistId);

            artwork.title = "Collaborator Updated Title";
            var updated = await _collabClient.From<ArtworkData>().Update(artwork);
            Assert.AreEqual("Collaborator Updated Title", updated.Model.title);

            artwork.owner_id = _collabArtistId;
            Assert.ThrowsAsync<Exception>(async () => await _collabClient.From<ArtworkData>().Update(artwork));
        }
        finally
        {
            await DeleteArtworkByIdAsync(artwork.id, _ownerClient);
        }
    }

    [Test]
    public async Task DeletePolicy_OnlyOwnerCanDelete()
    {
        if (!_supabaseInitialized)
            Assert.Ignore("Supabase is not configured for integration tests.");

        var artwork = await InsertArtworkAsync(_ownerClient, _ownerArtistId);
        try
        {
            Assert.ThrowsAsync<Exception>(async () => await _collabClient.From<ArtworkData>().Delete(artwork));

            await _ownerClient.From<ArtworkData>().Delete(artwork);
        }
        finally
        {
            try { await DeleteArtworkByIdAsync(artwork.id, _ownerClient); } catch { }
        }
    }

    private async Task<(Client client, bool didCreate)> SignInOrRegisterUserAsync(string email, string password)
    {
        var client = new Client(SupabaseUrl.TrimEnd('/'), AnonKey, new SupabaseOptions { AutoConnectRealtime = false });
        await client.InitializeAsync();

        try
        {
            var signIn = await client.Auth.SignIn(email, password);
            if (signIn?.User != null)
                return (client, false);
        }
        catch { }

        try
        {
            await client.Auth.SignUp(email, password);
            var signedIn = await client.Auth.SignIn(email, password);
            if (signedIn?.User != null)
                return (client, true);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to sign in or register user '{email}': {ex.Message}");
        }

        Assert.Fail($"Unable to acquire a session for user: {email}");
        return (null, false);
    }

    private async Task<int> GetOrCreateArtistIdAsync(Client client)
    {
        var userId = client.Auth.CurrentSession?.User?.Id;
        Assert.IsFalse(string.IsNullOrWhiteSpace(userId), "Authenticated Supabase client has no current user.");

        try
        {
            var profile = await client.From<ArtistProfile>()
                .Where(x => x.auth_user_id == userId)
                .Single();

            return Convert.ToInt32(profile.id);
        }
        catch { }

        var repo = new SupabaseArtistRepository(new SupabaseClientWrapper(client));
        var username = $"test_{Guid.NewGuid():N}";
        var ok = await repo.CreateArtistProfileAsync(userId, username);
        Assert.IsTrue(ok, "Failed to create artist profile.");

        var created = await client.From<ArtistProfile>()
            .Where(x => x.auth_user_id == userId)
            .Single();

        return Convert.ToInt32(created.id);
    }

    private async Task<ArtworkData> InsertArtworkAsync(Client client, int ownerId)
    {
        var artwork = new ArtworkData
        {
            title = $"Policy test - {Guid.NewGuid():N}",
            owner_id = ownerId,
            image_url = string.Empty,
            thumbnail_url = string.Empty,
            filesize_bytes = 0,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        var result = await client.From<ArtworkData>().Insert(artwork);
        return result.Model ?? artwork;
    }

    private async Task DeleteArtworkByIdAsync(int artworkId, Client client)
    {
        var placeholder = new ArtworkData { id = artworkId };
        await client.From<ArtworkData>().Delete(placeholder);
    }

    private async Task<AclArtworkEntry> EnsureAclEntryAsync(int artworkId, int artistId)
    {
        var entry = new AclArtworkEntry
        {
            artwork_id = artworkId,
            artist_id = artistId,
            status = "active"
        };

        // Only the artwork owner should be able to create ACL rows.
        var result = await _ownerClient.From<AclArtworkEntry>().Insert(entry);
        return result.Model ?? entry;
    }

    private async Task TryDeleteUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        try
        {
            var deleteUrl = $"{SupabaseUrl.TrimEnd('/')}/auth/v1/admin/users/{userId}";
            using var req = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
            req.Headers.Add("apikey", AnonKey);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AnonKey);

            using var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                Debug.LogWarning($"[SupabaseArtworkTest] Could not delete test user {userId}: {(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
            else
            {
                Debug.Log($"[SupabaseArtworkTest] Deleted test user {userId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SupabaseArtworkTest] Failed to delete test user {userId}: {ex.Message}");
        }
    }
}
