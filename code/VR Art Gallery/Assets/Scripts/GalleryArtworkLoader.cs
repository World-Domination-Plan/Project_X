using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRGallery.Authentication;
using VRGallery.Cloud;

/// <summary>
/// MVP gallery loader: fetches the logged-in user's saved artworks from Supabase
/// and applies them to the pre-placed canvas slots in the scene.
/// </summary>
public class GalleryArtworkLoader : MonoBehaviour
{
    [Header("Canvas Slots")]
    public PaintableSurfaceRT[] canvasSlots;

    [Header("Storage")]
    public string bucketName = "artworks";
    public int signedUrlExpirySeconds = 600;

    private async void Start()
    {
        float waited = 0f;
        while (AuthenticationManager.Instance == null
               || !AuthenticationManager.Instance.IsAuthenticated)
        {
            await Task.Delay(200);
            waited += 0.2f;
            if (waited >= 16f)
            {
                Debug.LogWarning("GalleryArtworkLoader: Timed out waiting for auth.");
                return;
            }
        }

        string authUserId = AuthenticationManager.Instance.CurrentUser.Id;
        await LoadGallery(authUserId);
    }


    private async Task LoadGallery(string authUserId)
    {
        try
        {
            var repo = await SupabaseArtworkRepository.CreateAsync();
            long ownerId = await ResolveOwnerId(authUserId);

            var artworks = await repo.GetArtworksByOwnerAsync(ownerId);
            Debug.Log($"GalleryArtworkLoader: found {artworks.Count} artworks");

            // Auto-find ALL PaintableSurfaceRT in the scene at load time
            // no need to drag anything in the Inspector
            var slots = FindObjectsByType<PaintableSurfaceRT>(FindObjectsSortMode.None);
            Debug.Log($"GalleryArtworkLoader: found {slots.Length} canvas slots in scene");

            int count = Mathf.Min(artworks.Count, slots.Length);
            for (int i = 0; i < count; i++)
            {
                var imagePath = artworks[i].GetType()
                    .GetProperty("imageurl")?.GetValue(artworks[i]) as string;
                if (string.IsNullOrEmpty(imagePath)) continue;

                string url = await repo.CreateSignedUrlAsync(bucketName, imagePath, signedUrlExpirySeconds);
                StartCoroutine(ApplyToSlot(url, slots[i]));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GalleryArtworkLoader: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public async void LoadAfterSpawn()
    {
        if (AuthenticationManager.Instance == null || !AuthenticationManager.Instance.IsAuthenticated)
        {
            Debug.LogWarning("GalleryArtworkLoader: Not authenticated, skipping load.");
            return;
        }
        string authUserId = AuthenticationManager.Instance.CurrentUser.Id;
        await LoadGallery(authUserId);
    }


    private IEnumerator ApplyToSlot(string url, PaintableSurfaceRT slot)
    {
        using var uwr = UnityWebRequestTexture.GetTexture(url);
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"GalleryArtworkLoader: download failed — {uwr.error}");
            yield break;
        }

        slot.SetBackground(DownloadHandlerTexture.GetContent(uwr));
    }

    private async Task<long> ResolveOwnerId(string authUuid)
    {
        // Identical to PaintingSessionUploader.ResolveOwnerIdAsync
        var client = SupabaseClientManager.Instance;
        var result = await client
            .From<ArtistProfile>()
            .Filter("authuserid", Postgrest.Constants.Operator.Equals, authUuid)
            .Single();

        if (result == null)
            throw new System.Exception($"No ArtistProfile for {authUuid}");

        return result.id;
    }
}
