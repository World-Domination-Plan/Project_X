using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Supabase;
using VRGallery.Cloud;

using System.Net.Http;
using System.Net.Http.Headers;
using UnityEngine;

public class SupabaseArtworkRepository : IArtworkRepository
{
    private const string TableName = "artwork";
    public Supabase.Client SupabaseClientInstance { get; private set; }
    private static readonly HttpClient Http = new HttpClient();

    [Serializable]
    private class SignedUrlRequest
    {
        public int expiresIn;
    }

    [Serializable]
    private class SignedUrlResponse
    {
        public string signedURL;
        public string signedUrl;
    }
    private SupabaseArtworkRepository(Supabase.Client client)
    {
        SupabaseClientInstance = client;
    }

    public static async Task<SupabaseArtworkRepository> CreateAsync()
    {
        if (!SupabaseClientManager.IsInitialized)
        {
            await SupabaseClientManager.InitializeAsync();
        }
        return new SupabaseArtworkRepository(SupabaseClientManager.Instance);
    }
    
    public async Task<ArtworkData> CreateArtworkAsync(ArtworkData artwork)
    {
        try
        {
            // Set timestamps
            artwork.created_at = DateTime.UtcNow;
            artwork.updated_at = DateTime.UtcNow;
            
            // Insert into Supabase
           
            var result = await SupabaseClientInstance
                .From<ArtworkData>()
                .Insert(artwork);
            
            return result.Model ?? artwork;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error creating artwork in Supabase: {ex.Message}");
            throw;
        }
    }
    
    public async Task<ArtworkData> GetArtworkAsync(int id)
    {
        try
        {
            var result = await SupabaseClientInstance
                .From<ArtworkData>()
                .Where(x => x.id == id)
                .Single();
            
            return result;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error retrieving artwork from Supabase: {ex.Message}");
            throw;
        }
    }

    public async Task<List<ArtworkData>> GetAllArtworksAsync()
    {
        try
        {
            var result = await SupabaseClientInstance
                .From<ArtworkData>()
                .Get();
            
            return result.Models;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error retrieving all artworks from Supabase: {ex.Message}");
            throw;
        }
    }

    public async Task<ArtworkData> CreateArtworkWithUploadAsync(
        ArtworkData artwork,
        byte[] imageBytes,
        byte[] thumbnailBytes = null,
        string bucketName = "artworks",
        string extension = "png",
        string contentType = "image/png")
    {
        if (artwork == null) throw new ArgumentNullException(nameof(artwork));
        if (imageBytes == null || imageBytes.Length == 0) throw new ArgumentException("Image bytes are empty.");

        var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")?.TrimEnd('/');
        var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            throw new InvalidOperationException("SUPABASE_URL/SUPABASE_KEY missing.");

        var originalExtension = extension.TrimStart('.');
        var originalObjectPath = $"{artwork.owner_id}/{Guid.NewGuid():N}.{originalExtension}";
        var thumbnailObjectPath = $"{artwork.owner_id}/thumb_{Guid.NewGuid():N}.{originalExtension}";

        if (thumbnailBytes == null || thumbnailBytes.Length == 0)
        {
            thumbnailBytes = GenerateHalfSizePng(imageBytes);
        }

        await UploadObjectAsync(supabaseUrl, supabaseKey, bucketName, originalObjectPath, imageBytes, contentType);
        await UploadObjectAsync(supabaseUrl, supabaseKey, bucketName, thumbnailObjectPath, thumbnailBytes, contentType);

        artwork.image_url = originalObjectPath;
        artwork.thumbnail_url = thumbnailObjectPath;
        artwork.filesize_bytes = imageBytes.LongLength;

        return await CreateArtworkAsync(artwork);
    }

    public async Task<string> CreateSignedUrlAsync(string bucketName, string objectPath, int expiresInSeconds = 600)
    {
        if (string.IsNullOrWhiteSpace(bucketName)) throw new ArgumentException("Bucket name is required.", nameof(bucketName));
        if (string.IsNullOrWhiteSpace(objectPath)) throw new ArgumentException("Object path is required.", nameof(objectPath));

        var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")?.TrimEnd('/');
        var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            throw new InvalidOperationException("SUPABASE_URL/SUPABASE_KEY missing.");

        var signUrl = $"{supabaseUrl}/storage/v1/object/sign/{bucketName}/{objectPath}";
        var body = JsonUtility.ToJson(new SignedUrlRequest { expiresIn = expiresInSeconds });

        using var req = new HttpRequestMessage(HttpMethod.Post, signUrl);
        req.Headers.Add("apikey", supabaseKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var resp = await Http.SendAsync(req);
        var responseBody = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Create signed URL failed ({(int)resp.StatusCode}): {responseBody}");

        var parsed = JsonUtility.FromJson<SignedUrlResponse>(responseBody);
        var signedPath = parsed?.signedURL;
        if (string.IsNullOrEmpty(signedPath))
            signedPath = parsed?.signedUrl;

        if (string.IsNullOrEmpty(signedPath))
            throw new Exception($"Signed URL missing in response: {responseBody}");

        if (signedPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return signedPath;

        if (signedPath.StartsWith("/storage/v1/", StringComparison.OrdinalIgnoreCase))
            return $"{supabaseUrl}{signedPath}";

        if (signedPath.StartsWith("/", StringComparison.Ordinal))
            return $"{supabaseUrl}/storage/v1{signedPath}";

        return $"{supabaseUrl}/storage/v1/{signedPath}";
    }

    public async Task<byte[]> DownloadWithSignedUrlAsync(string signedUrl)
    {
        if (string.IsNullOrWhiteSpace(signedUrl)) throw new ArgumentException("Signed URL is required.", nameof(signedUrl));

        var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");

        using var req = new HttpRequestMessage(HttpMethod.Get, signedUrl);
        if (!string.IsNullOrEmpty(supabaseKey))
        {
            req.Headers.Add("apikey", supabaseKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
        }

        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new Exception($"Signed URL download failed ({(int)resp.StatusCode}): {body}");
        }

        return await resp.Content.ReadAsByteArrayAsync();
    }

    public async Task DeleteObjectAsync(string bucketName, string objectPath)
    {
        if (string.IsNullOrWhiteSpace(bucketName)) throw new ArgumentException("Bucket name is required.", nameof(bucketName));
        if (string.IsNullOrWhiteSpace(objectPath)) throw new ArgumentException("Object path is required.", nameof(objectPath));

        var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")?.TrimEnd('/');
        var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            throw new InvalidOperationException("SUPABASE_URL/SUPABASE_KEY missing.");

        var deleteUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{objectPath}";

        using var req = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
        req.Headers.Add("apikey", supabaseKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);

        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new Exception($"Delete object failed ({(int)resp.StatusCode}): {body}");
        }
    }

    private static async Task UploadObjectAsync(
        string supabaseUrl,
        string supabaseKey,
        string bucketName,
        string objectPath,
        byte[] bytes,
        string contentType)
    {
        var uploadUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{objectPath}";

        using var req = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        req.Headers.Add("apikey", supabaseKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
        req.Headers.Add("x-upsert", "true");
        req.Content = new ByteArrayContent(bytes);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new Exception($"Storage upload failed ({(int)resp.StatusCode}) for '{objectPath}': {body}");
        }
    }

    private static byte[] GenerateHalfSizePng(byte[] originalPngBytes)
    {
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!source.LoadImage(originalPngBytes))
        {
            DestroyUnityObjectSafe(source);
            throw new InvalidOperationException("Input image is not a valid PNG/JPG payload.");
        }

        var targetWidth = Mathf.Max(1, source.width / 2);
        var targetHeight = Mathf.Max(1, source.height / 2);

        var srcPixels = source.GetPixels32();
        var dstPixels = new Color32[targetWidth * targetHeight];

        for (int y = 0; y < targetHeight; y++)
        {
            var srcY = Mathf.Min(source.height - 1, y * 2);
            for (int x = 0; x < targetWidth; x++)
            {
                var srcX = Mathf.Min(source.width - 1, x * 2);
                dstPixels[y * targetWidth + x] = srcPixels[srcY * source.width + srcX];
            }
        }

        var thumbnail = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        thumbnail.SetPixels32(dstPixels);
        thumbnail.Apply(false, false);
        var bytes = thumbnail.EncodeToPNG();

        DestroyUnityObjectSafe(source);
        DestroyUnityObjectSafe(thumbnail);

        if (bytes == null || bytes.Length == 0)
            throw new InvalidOperationException("Failed to encode thumbnail PNG.");

        return bytes;
    }

    private static void DestroyUnityObjectSafe(UnityEngine.Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(obj);
        else
            UnityEngine.Object.DestroyImmediate(obj);
    }
}
