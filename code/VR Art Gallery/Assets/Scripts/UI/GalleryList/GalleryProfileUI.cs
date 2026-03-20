using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRGallery.Authentication;
using VRGallery.Cloud;

public class GalleryProfileUI : MonoBehaviour
{
    [SerializeField] Transform m_GalleryListParent;
    [SerializeField] GameObject m_GalleryItemPrefab;  // Prefab with GalleryItemUI script
    [SerializeField] TMP_Text m_LoadingText;
    [SerializeField] GameObject m_CreateGalleryButtonContainer;  // Empty state container

    private IArtistRepository m_ArtistRepository;
    private IGalleryRepository m_GalleryRepository;
    private List<GalleryItemUI> m_ActiveGalleryItems = new List<GalleryItemUI>();

    private async void Start()
    {
        // Initialize repositories
        if (!SupabaseClientManager.IsInitialized)
            await SupabaseClientManager.InitializeAsync();

        var supabaseClientWrapper = new SupabaseClientWrapper(SupabaseClientManager.Instance);
        m_ArtistRepository = new SupabaseArtistRepository(supabaseClientWrapper);
        m_GalleryRepository = await SupabaseGalleryRepository.CreateAsync();

        // Load user's galleries
        await RefreshUserGalleries();

        LogDebug("GalleryProfileUI initialized.");
    }

    /// <summary>
    /// Fetches and displays only the current user's galleries.
    /// </summary>
    public async Task RefreshUserGalleries()
    {
        m_LoadingText.gameObject.SetActive(true);
        m_LoadingText.text = "Loading your galleries...";

        try
        {
            // Get current user ID
            if (!AuthenticationManager.Instance.IsAuthenticated)
            {
                throw new InvalidOperationException("User not authenticated.");
            }

            string userId = AuthenticationManager.Instance.CurrentUser.Id;
            LogDebug($"Fetching galleries for user: {userId}");

            // Get artist profile to access managed_gallery array
            ArtistProfile profile = await m_ArtistRepository.GetArtistProfileAsync(userId);

            if (profile == null)
            {
                m_LoadingText.gameObject.SetActive(true);
                m_LoadingText.text = "Could not load profile.";
                LogError("Artist profile not found for user.");
                return;
            }

            // Extract gallery IDs from managed_gallery array
            List<GalleryData> userGalleries = new List<GalleryData>();

            if (profile.managed_gallery != null && profile.managed_gallery.Length > 0)
            {
                // Fetch each gallery by ID
                foreach (var galleryIdStr in profile.managed_gallery)
                {
                    if (string.IsNullOrWhiteSpace(galleryIdStr))
                        continue;

                    try
                    {
                        if (int.TryParse(galleryIdStr, out int galleryId))
                        {
                            GalleryData gallery = await m_GalleryRepository.GetGalleryAsync(galleryId);
                            if (gallery != null)
                            {
                                userGalleries.Add(gallery);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error fetching gallery {galleryIdStr}: {ex.Message}");
                    }
                }
            }

            DisplayUserGalleries(userGalleries);
        }
        catch (Exception ex)
        {
            m_LoadingText.gameObject.SetActive(true);
            m_LoadingText.text = $"Error loading galleries: {ex.Message}";
            LogError($"RefreshUserGalleries failed: {ex}");
        }
    }

    private void DisplayUserGalleries(List<GalleryData> galleries)
    {
        // Clear existing gallery items
        foreach (var item in m_ActiveGalleryItems)
            Destroy(item.gameObject);
        m_ActiveGalleryItems.Clear();

        // Show empty state if no galleries
        if (galleries == null || galleries.Count == 0)
        {
            m_LoadingText.gameObject.SetActive(true);
            m_LoadingText.text = "You haven't created any galleries yet.";
            if (m_CreateGalleryButtonContainer != null)
                m_CreateGalleryButtonContainer.SetActive(true);
            return;
        }

        // Hide loading text and empty state
        m_LoadingText.gameObject.SetActive(false);
        if (m_CreateGalleryButtonContainer != null)
            m_CreateGalleryButtonContainer.SetActive(false);

        // Instantiate gallery items
        foreach (var gallery in galleries)
        {
            GameObject itemObj = Instantiate(m_GalleryItemPrefab, m_GalleryListParent);
            GalleryItemUI itemUI = itemObj.GetComponent<GalleryItemUI>();
            if (itemUI != null)
            {
                itemUI.InitializeGalleryItem(gallery, this);
                m_ActiveGalleryItems.Add(itemUI);
            }
        }

        LogDebug($"Displayed {galleries.Count} gallery/galleries.");
    }

    // ── Callback methods for action buttons ────────────────────────────────────

    public void ViewGallery(GalleryData gallery)
    {
        LogDebug($"Viewing gallery: {gallery.name}");
        // TODO: Load the VR space with this gallery
    }

    public void EditGallery(GalleryData gallery)
    {
        LogDebug($"Editing gallery: {gallery.name}");
        // TODO: Open edit panel with gallery properties
    }

    public async void DeleteGallery(int galleryId)
    {
        try
        {
            LogDebug($"Deleting gallery: {galleryId}");
            await m_GalleryRepository.DeleteGalleryAsync(galleryId);
            await RefreshUserGalleries();
        }
        catch (Exception ex)
        {
            LogError($"Error deleting gallery: {ex.Message}");
        }
    }

    // ── For Dependency Injection (Testing) ────────────────────────────────────

    public void SetRepositories(IArtistRepository artistRepo, IGalleryRepository galleryRepo)
    {
        m_ArtistRepository = artistRepo;
        m_GalleryRepository = galleryRepo;
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private void LogDebug(string message)
    {
        Debug.Log($"[GalleryProfileUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[GalleryProfileUI] {message}");
    }
}
