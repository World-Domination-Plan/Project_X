using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRGallery.Cloud;
using UnityEngine.Networking;
using VRGallery.Authentication;

/// <summary>
/// Panel that displays information about a single gallery and its artworks in a 3x3 grid.
/// </summary>
public class GalleryInfoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text m_GalleryNameText;
    [SerializeField] private TMP_Text m_ArtworkCountText;
    [SerializeField] private Button m_LoadGalleryButton;
    [SerializeField] private GameObject m_GalleryInfoArtworkDisplayPrefab;
    [SerializeField] private Transform m_InventoryContainer;

    [Header("Dependencies")]
    [SerializeField] private MonoBehaviour m_LobbyUI; // The script that controls the lobby/room creation

    private GalleryData m_CurrentGallery;
    
    private IArtworkRepository m_ArtworkRepository;
    private IGalleryRepository m_GalleryRepository;
    
    public static int PendingSlotIndex = -1;
    public static int PendingArtworkId = -1;
    public static GalleryInfoUI ActiveInstance;

    private void OnEnable()
    {
        ActiveInstance = this;
    }

    private void OnDisable()
    {
        if (ActiveInstance == this) ActiveInstance = null;
    }

    private async void Start()
    {
        // Hook up Load Gallery button
        if (m_LoadGalleryButton != null)
        {
            m_LoadGalleryButton.onClick.AddListener(OnLoadGalleryClicked);
        }

        // Initialize our own repositories for decoupling
        if (!SupabaseClientManager.IsInitialized)
            await SupabaseClientManager.InitializeAsync();

        m_ArtworkRepository = await SupabaseArtworkRepository.CreateAsync();
        m_GalleryRepository = await SupabaseGalleryRepository.CreateAsync();

        // If not externally initialized, try to auto-fetch the gallery
        // DISABLED: Auto-load conflicts with external initialization from LobbyUI
        // Will be initialized externally via InitializeInfo() when needed
        // if (m_CurrentGallery == null)
        // {
        //     await AutoLoadUserGallery();
        // }
    }

    private int _defaultGalleryId = 141;

    private async Task AutoLoadUserGallery()
    {
        int galleryIdToLoad = _defaultGalleryId;
        
        try
        {
            if (AuthenticationManager.Instance != null && AuthenticationManager.Instance.IsAuthenticated && AuthenticationManager.Instance.CurrentUser != null)
            {
                string userId = AuthenticationManager.Instance.CurrentUser.Id;
                var artistRepo = new SupabaseArtistRepository(new SupabaseClientWrapper(SupabaseClientManager.Instance));
                var profile = await artistRepo.GetArtistProfileAsync(userId);
                
                if (profile != null && profile.managed_gallery != null && profile.managed_gallery.Length > 0)
                {
                    if (int.TryParse(profile.managed_gallery[0], out int galId))
                    {
                        galleryIdToLoad = galId;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GalleryInfoUI] Could not fetch authenticated profile, falling back to default. {ex.Message}");
        }

        Debug.Log($"[GalleryInfoUI] Auto-loading Gallery ID: {galleryIdToLoad}...");
        
        try
        {
            m_CurrentGallery = await m_GalleryRepository.GetGalleryAsync(galleryIdToLoad);
            UpdateArtworkCount();
            _ = PopulateArtworksAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GalleryInfoUI] Failed to load gallery {galleryIdToLoad}: {ex.Message}");
        }
    }

    /// <summary>
    /// Populates the UI with gallery data.
    /// </summary>
    public void InitializeInfo(GalleryData gallery)
    {
        m_CurrentGallery = gallery;

        if (m_GalleryNameText != null)
            m_GalleryNameText.text = gallery.name;

        UpdateArtworkCount();
        UpdateSlotVisuals();
        _ = PopulateArtworksAsync();
    }

    private async Task PopulateArtworksAsync()
    {
        Debug.Log($"[GalleryInfoUI] PopulateArtworksAsync started.");
        Debug.Log($"[GalleryInfoUI] InventoryContainer assigned: {m_InventoryContainer != null}, Prefab assigned: {m_GalleryInfoArtworkDisplayPrefab != null}");
        
        if (m_InventoryContainer == null || m_GalleryInfoArtworkDisplayPrefab == null)
        {
            Debug.LogError("[GalleryInfoUI] Aborting: Missing InventoryContainer or Prefab reference.");
            return;
        }
        
        if (m_ArtworkRepository == null)
        {
            Debug.LogError($"[GalleryInfoUI] Aborting: Missing ArtworkRepository.");
            return;
        }

        if (m_CurrentGallery == null || m_CurrentGallery.artwork_ids == null)
        {
            Debug.LogWarning("[GalleryInfoUI] Aborting: m_CurrentGallery is null or artwork_ids is null/empty.");
            return;
        }

        Debug.Log($"[GalleryInfoUI] Ready to populate {m_CurrentGallery.artwork_ids.Count} artworks.");

        // Clear existing children
        foreach (Transform child in m_InventoryContainer)
        {
            Destroy(child.gameObject);
        }

        // Populate from the full inventory concurrently
        var loadTasks = new List<Task>();
        foreach (int artworkId in m_CurrentGallery.artwork_ids)
        {
            Debug.Log($"[GalleryInfoUI] Queueing artwork {artworkId} for loading...");
            loadTasks.Add(LoadAndInstantiateArtworkUIAsync(artworkId));
        }

        await Task.WhenAll(loadTasks);
        Debug.Log($"[GalleryInfoUI] Finished UI population. Instantiated {loadTasks.Count} artwork items.");
    }

    private async Task LoadAndInstantiateArtworkUIAsync(int artworkId)
    {
        try
        {
            // Fetch artwork details from repo
            var artData = await m_ArtworkRepository.GetArtworkAsync(artworkId);
            if (artData == null)
            {
                Debug.LogWarning($"[GalleryInfoUI] Failed: Artwork {artworkId} returned null from repository");
                return;
            }
            
            Debug.Log($"[GalleryInfoUI] Loaded artwork data: ID={artworkId}, Title={artData.title}");

            // Get the storage path (thumbnail preferred, fall back to image)
            string storagePath = !string.IsNullOrEmpty(artData.thumbnail_url) ? artData.thumbnail_url : artData.image_url;
            Debug.Log($"[GalleryInfoUI] Image URL for {artworkId}: {storagePath}");

            Texture2D texture = null;
            if (!string.IsNullOrEmpty(storagePath))
            {
                // Convert storage path to signed URL for download access
                string signedUrl = null;
                try
                {
                    signedUrl = await m_ArtworkRepository.CreateSignedUrlAsync("artworks", storagePath);
                    Debug.Log($"[GalleryInfoUI] Generated signed URL for {artworkId}: {signedUrl}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GalleryInfoUI] Failed to create signed URL for {artworkId}: {ex.Message}");
                    signedUrl = null;
                }

                if (!string.IsNullOrEmpty(signedUrl))
                {
                    Debug.Log($"[GalleryInfoUI] Starting texture load for {artworkId} from signed URL");
                    texture = await LoadTextureFromUrlAsync(signedUrl);
                    if (texture != null)
                        Debug.Log($"[GalleryInfoUI] Successfully loaded texture for {artworkId}: {texture.width}x{texture.height}");
                    else
                        Debug.LogWarning($"[GalleryInfoUI] Texture load returned null for {artworkId}");
                }
                else
                {
                    Debug.LogWarning($"[GalleryInfoUI] Could not generate signed URL for artwork {artworkId}");
                }
            }
            else
            {
                Debug.LogWarning($"[GalleryInfoUI] No URL available for artwork {artworkId}");
            }

            // Instantiate prefab on main thread (which is fine after await)
            var obj = Instantiate(m_GalleryInfoArtworkDisplayPrefab, m_InventoryContainer);
            Debug.Log($"[GalleryInfoUI] Instantiated prefab for artwork {artworkId}");
            
            var displayScript = obj.GetComponent<GalleryInfoArtworkDisplay>();
            if (displayScript != null)
            {
                Debug.Log($"[GalleryInfoUI] Found GalleryInfoArtworkDisplay script, calling Initialize with texture={texture != null}");
                displayScript.Initialize(artworkId, texture, this);
            }
            else
            {
                Debug.LogError($"[GalleryInfoUI] Prefab for artwork {artworkId} does NOT have GalleryInfoArtworkDisplay script!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GalleryInfoUI] Error loading artwork UI {artworkId}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public static void HandleSlotClicked(int slotIndex)
    {
        PendingSlotIndex = slotIndex;
        Debug.Log($"[Gallery Swap] Physical Slot {slotIndex} selected.");
        CheckExecuteSwap();
    }

    public static void HandleArtworkClicked(int artworkId)
    {
        PendingArtworkId = artworkId;
        Debug.Log($"[Gallery Swap] UI Artwork ID {artworkId} selected.");
        CheckExecuteSwap();
    }

    private static void CheckExecuteSwap()
    {
        if (PendingSlotIndex != -1 && PendingArtworkId != -1)
        {
            if (ActiveInstance != null)
            {
                ActiveInstance.SwapAndSync(PendingArtworkId, PendingSlotIndex);
            }
            else
            {
                Debug.LogWarning("[Gallery Swap] Swap triggered but UI is not actively open to sync the cloud!");
            }
        }
    }

    private async void SwapAndSync(int artworkId, int slot)
    {
        if (m_CurrentGallery == null || m_GalleryRepository == null) return;

        Debug.Log($"[GalleryInfoUI] Executing Swap of Artwork {artworkId} into Slot {slot}...");
        
        // Ensure map exists
        if (m_CurrentGallery.artwork_map == null) 
            m_CurrentGallery.artwork_map = new Dictionary<int, int>();

        // Update local state
        m_CurrentGallery.artwork_map[slot] = artworkId;

        // Reset memory for next interaction
        PendingSlotIndex = -1;
        PendingArtworkId = -1;

        // Push to cloud
        await m_GalleryRepository.UpdateGalleryAsync(m_CurrentGallery);
        Debug.Log($"[GalleryInfoUI] Successfully synced slot {slot} with artwork {artworkId}");

        // Rerender Physical Gallery
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var behaviour in behaviours)
        {
            if (behaviour == null || behaviour.GetType().Name != "GalleryManager")
                continue;

            var method = behaviour.GetType().GetMethod("InitializeAndLoadGalleryAsync");
            if (method != null && method.Invoke(behaviour, null) is Task initTask)
                await initTask;

            break;
        }
    }

    private async Task<Texture2D> LoadTextureFromUrlAsync(string url)
    {
        try
        {
            Debug.Log($"[GalleryInfoUI] Downloading texture from: {url}");
            
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                    await Task.Delay(10);

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D result = DownloadHandlerTexture.GetContent(request);
                    Debug.Log($"[GalleryInfoUI] Successfully downloaded texture: {result.width}x{result.height}");
                    return result;
                }
                else
                {
                    Debug.LogError($"[GalleryInfoUI] Failed to download texture from {url}: {request.result} - {request.error}");
                    return null;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GalleryInfoUI] Exception loading texture from {url}: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    private void UpdateArtworkCount()
    {
        if (m_ArtworkCountText != null)
        {
            int count = (m_CurrentGallery.artwork_ids != null) ? m_CurrentGallery.artwork_ids.Count : 0;
            m_ArtworkCountText.text = $"{count} artworks";
        }
    }

    private void UpdateSlotVisuals()
    {
        // Now handled by instantiated prefabs.
    }

    private async void OnLoadGalleryClicked()
    {
        Debug.Log("Loading gallery to LobbyUI...");
        if (m_LobbyUI != null)
        {
            // Activate the Lobby Panel
            m_LobbyUI.gameObject.SetActive(true);
            
            // Re-render gallery similar to LobbyUI instantiation
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || behaviour.GetType().Name != "GalleryManager")
                    continue;

                var method = behaviour.GetType().GetMethod("InitializeAndLoadGalleryAsync");
                if (method != null && method.Invoke(behaviour, null) is System.Threading.Tasks.Task initTask)
                    await initTask;

                break;
            }
        }
        else
        {
            Debug.LogError("LobbyUI reference missing in GalleryInfoUI!");
        }
    }
}
