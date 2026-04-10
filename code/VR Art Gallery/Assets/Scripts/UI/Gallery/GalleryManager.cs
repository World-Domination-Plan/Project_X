using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VRGallery.Cloud;
using VRGallery.Authentication;


public class GalleryManager : MonoBehaviour
{
    [Header("Artwork Display References")]
    [SerializeField] private ArtworkDisplay[] artworkDisplays;
    [SerializeField] private int maxArtworks = 9;

    private IArtworkRepository _artworkRepo;
    private IArtistRepository _artistRepo;
    private IGalleryRepository _galleryRepo;

    private long _ownerId = 49;//WorkflowArtist, just in case
    private int _currentGalleryId = 141;//WorkflowArtist's gallery, just in case

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private IGalleryRepository m_GalleryRepository;

    public long CurrentOwnerId => _ownerId;
    public int CurrentGalleryId => _currentGalleryId;

    private async void Start()
    {
        try
        {
            // Initialize Supabase client if needed
            if (!SupabaseClientManager.IsInitialized)
                await SupabaseClientManager.InitializeAsync();

            m_GalleryRepository = await SupabaseGalleryRepository.CreateAsync();
            LogDebug("GalleryManager initialized.");

            // await InitializeCloudAsync();
            // await LoadGalleryAsync(_currentGalleryId);


        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize GalleryManager: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads and displays all artworks for the given gallery ID.
    /// Automatically finds instantiated ArtworkDisplay objects if array is empty.
    /// </summary>
    
    public async Task InitializeCloudAsync()
    {
        try
        {
            if (!SupabaseClientManager.IsInitialized)
                await SupabaseClientManager.InitializeAsync();

            _artworkRepo = await SupabaseArtworkRepository.CreateAsync();
            _artistRepo = await SupabaseArtistRepository.CreateAsync();

            var authUser = AuthenticationManager.Instance?.CurrentUser;
            if (authUser != null)
            {
                var profile = await _artistRepo.GetArtistProfileAsync(authUser.Id);
                if (profile != null)
                {
                    _ownerId = profile.user_id;

                    if (profile.managed_gallery != null && profile.managed_gallery.Length > 0)
                    {
                        if (int.TryParse(profile.managed_gallery[0], out int galId))
                        {
                            _currentGalleryId = galId;
                        }
                    }
                }
            }
            Debug.Log($"[PaintableSurfaceRT] Cloud Sync initialized. OwnerId: {_ownerId}, GalleryId: {_currentGalleryId}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PaintableSurfaceRT] Cloud Sync init failed: {ex.Message}");
        }
    }
    public async Task LoadGalleryAsync(int galleryId)
    {
        try
        {
            LogDebug($"Loading gallery {galleryId}...");

            // Ensure repository is initialized
            if (m_GalleryRepository == null)
            {
                LogError("Repository not initialized. Call Start() first.");
                return;
            }

            // Automatically grab spline-instantiated objects if array is empty
            if (artworkDisplays == null || artworkDisplays.Length == 0)
            {
                artworkDisplays = FindObjectsByType<ArtworkDisplay>(FindObjectsSortMode.None);
                if (artworkDisplays.Length == 0)
                {
                    LogError("No ArtworkDisplay objects found in scene! Ensure the spline has finished instantiating.");
                    return;
                }
                LogDebug($"Found {artworkDisplays.Length} ArtworkDisplay objects dynamically.");

                // Sort them by hierarchy/name to match spline instantiation order
                SortDisplaysBySpline();
            }

            // Get artwork paths from the gallery
            var artworkPaths = await m_GalleryRepository.GetArtworkPaths(galleryId, maxArtworks, getThumbnails: false);

            // Clear existing displays
            ClearDisplays();

            // Load and display each artwork using slot indices for proper placement
            int loadedCount = 0;
            foreach (var kvp in artworkPaths)
            {
                int slotIndex = kvp.Key;
                string imageUrl = kvp.Value.Item1;

                // Use slotIndex to place art in the correct frame on the spline
                if (slotIndex >= 0 && slotIndex < artworkDisplays.Length)
                {
                    // Load texture from URL
                    Texture2D texture = await LoadTextureFromUrlAsync(imageUrl);
                    if (texture != null)
                    {
                        // Use the display's Populate function
                        artworkDisplays[slotIndex].Populate(texture);
                        loadedCount++;
                        LogDebug($"Loaded artwork into slot {slotIndex}");
                    }
                    else
                    {
                        LogDebug($"Failed to load texture for artwork at slot {slotIndex}");
                    }
                }
                else
                {
                    LogDebug($"Slot index {slotIndex} out of range for {artworkDisplays.Length} displays");
                }
            }

            LogDebug($"Gallery {galleryId} loaded with {loadedCount} artworks.");
        }
        catch (Exception ex)
        {
            LogError($"Error loading gallery {galleryId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Sorts ArtworkDisplay objects by their hierarchy/instantiation order.
    /// This ensures artwork appears in the correct sequence along the spline.
    /// </summary>
    private void SortDisplaysBySpline()
    {
        System.Array.Sort(artworkDisplays, (a, b) =>
        {
            // Sort by sibling index first (hierarchy order along spline)
            int siblingComparison = a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
            if (siblingComparison != 0) return siblingComparison;

            // If sibling index is the same, sort by name (e.g., "DisplayStructure(0)", "DisplayStructure(1)")
            return a.name.CompareTo(b.name);
        });

        LogDebug($"Sorted {artworkDisplays.Length} displays by spline order.");
    }

    /// <summary>
    /// Loads a texture from a URL using UnityWebRequest.
    /// </summary>
    private async Task<Texture2D> LoadTextureFromUrlAsync(string url)
    {
        try
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                var operation = request.SendWebRequest();

                // Wait for the request to complete
                while (!operation.isDone)
                    await Task.Delay(10);

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return DownloadHandlerTexture.GetContent(request);
                }
                else
                {
                    LogError($"Failed to download texture from {url}: {request.error}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Error loading texture from {url}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Clears all artwork displays.
    /// </summary>
    private void ClearDisplays()
    {
        foreach (var display in artworkDisplays)
        {
            if (display != null)
            {
                display.Populate(null);
            }
        }
    }

    /// <summary>
    /// Public entry point to initialize and load the gallery.
    /// </summary>
    public async Task InitializeAndLoadGalleryAsync()
    {
        await InitializeCloudAsync();
        await LoadGalleryAsync(_currentGalleryId);
    }

    /// <summary>
    /// Resolves an artist profile from Supabase auth user ID and loads that artist's gallery.
    /// Falls back to current-user gallery loading if the host profile cannot be resolved.
    /// </summary>
    public async Task InitializeAndLoadGalleryByAuthUserIdAsync(string hostAuthUserId)
    {
        if (string.IsNullOrWhiteSpace(hostAuthUserId))
        {
            await InitializeAndLoadGalleryAsync();
            return;
        }

        await EnsureRepositoriesInitializedAsync();

        ArtistProfile hostProfile = await _artistRepo.GetArtistProfileAsync(hostAuthUserId);
        if (hostProfile == null)
        {
            LogError($"Host artist profile was not found for auth user ID: {hostAuthUserId}. Falling back to local gallery.");
            await InitializeAndLoadGalleryAsync();
            return;
        }

        if (hostProfile.managed_gallery != null && hostProfile.managed_gallery.Length > 0)
        {
            if (int.TryParse(hostProfile.managed_gallery[0], out int managedGalleryId))
            {
                await LoadGalleryAsync(managedGalleryId);
                return;
            }
        }

        await InitializeAndLoadGalleryByOwnerIdAsync(hostProfile.user_id);
    }

    /// <summary>
    /// Loads the latest gallery owned by a given owner_id.
    /// </summary>
    public async Task InitializeAndLoadGalleryByOwnerIdAsync(long ownerId)
    {
        if (ownerId <= 0)
        {
            LogError($"Invalid owner_id passed to gallery load: {ownerId}");
            return;
        }

        await EnsureRepositoriesInitializedAsync();

        int? galleryId = await ResolveLatestGalleryIdForOwnerAsync(ownerId);
        if (!galleryId.HasValue)
        {
            LogError($"No gallery found for owner_id {ownerId}.");
            return;
        }

        await LoadGalleryAsync(galleryId.Value);
    }

    /// <summary>
    /// Loads a known gallery id directly.
    /// </summary>
    public async Task InitializeAndLoadGalleryByGalleryIdAsync(int galleryId)
    {
        if (galleryId <= 0)
        {
            LogError($"Invalid gallery id passed to gallery load: {galleryId}");
            return;
        }

        await EnsureRepositoriesInitializedAsync();
        await LoadGalleryAsync(galleryId);
    }

    public string GetCurrentAuthUserId()
    {
        return AuthenticationManager.Instance?.CurrentUser?.Id;
    }

    async Task EnsureRepositoriesInitializedAsync()
    {
        if (!SupabaseClientManager.IsInitialized)
            await SupabaseClientManager.InitializeAsync();

        if (m_GalleryRepository == null)
            m_GalleryRepository = await SupabaseGalleryRepository.CreateAsync();

        if (_artistRepo == null)
            _artistRepo = await SupabaseArtistRepository.CreateAsync();
    }

    async Task<int?> ResolveLatestGalleryIdForOwnerAsync(long ownerId)
    {
        var allGalleries = await m_GalleryRepository.GetAllGalleriesAsync();
        if (allGalleries == null || allGalleries.Count == 0)
            return null;

        GalleryData latestGallery = null;
        foreach (var gallery in allGalleries)
        {
            if (gallery == null || gallery.owner_id != ownerId)
                continue;

            if (latestGallery == null || gallery.updated_at > latestGallery.updated_at)
                latestGallery = gallery;
        }

        return latestGallery?.id;
    }

    // ── Logging ───────────────────────────────────────────────────────────────
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[GalleryManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[GalleryManager] {message}");
    }
}
