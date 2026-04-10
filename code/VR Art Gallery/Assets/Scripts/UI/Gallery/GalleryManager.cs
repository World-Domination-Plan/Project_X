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

        // Automatically assign slot indices based on their sorted order
        for (int i = 0; i < artworkDisplays.Length; i++)
        {
            if (artworkDisplays[i] != null)
            {
                artworkDisplays[i].SlotIndex = i;
            }
        }

        LogDebug($"Sorted {artworkDisplays.Length} displays by spline order and assigned SlotIndices.");
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
