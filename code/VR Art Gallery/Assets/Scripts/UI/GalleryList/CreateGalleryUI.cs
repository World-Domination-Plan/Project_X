using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRGallery.Authentication;
using VRGallery.Cloud;

public class CreateGalleryUI : MonoBehaviour
{
    [Header("Gallery State Management")]
    [SerializeField] GameObject m_CreateGalleryButtonContainer;
    [SerializeField] GalleryInfoUI m_GalleryInfoUI;

    [Header("UI References")]
    [SerializeField] GameObject m_CreateGalleryPanel;
    [SerializeField] TMP_InputField m_NameInputField;
    [SerializeField] Button m_CreateButton;
    [SerializeField] Button m_CancelButton;
    [SerializeField] TMP_Text m_ErrorText;
    [SerializeField] TMP_Text m_LoadingText;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private IGalleryRepository m_GalleryRepository;
    public IGalleryRepository GalleryRepository => m_GalleryRepository;
    private IArtistRepository m_ArtistRepository;
    private IArtworkRepository m_ArtworkRepository;
    public IArtworkRepository ArtworkRepository => m_ArtworkRepository;

    private async void Start()
    {
        // Initialize repositories
        if (!SupabaseClientManager.IsInitialized)
            await SupabaseClientManager.InitializeAsync();

        var supabaseClientWrapper = new SupabaseClientWrapper(SupabaseClientManager.Instance);
        m_ArtistRepository = new SupabaseArtistRepository(supabaseClientWrapper);
        m_GalleryRepository = await SupabaseGalleryRepository.CreateAsync();
        m_ArtworkRepository = await SupabaseArtworkRepository.CreateAsync();

        // Hook up button listeners
        m_CreateButton.onClick.AddListener(OnCreateButtonClicked);
        m_CancelButton.onClick.AddListener(OnCancelButtonClicked);

        // Panel starts hidden
        m_CreateGalleryPanel.SetActive(false);

        // Initial fetch
        await RefreshGalleryStatus();

        LogDebug("CreateGalleryUI initialized.");
    }

    private async void OnEnable()
    {
        // Re-fetch when the tab is opened, but make sure Start has finished initializing repos
        if (m_GalleryRepository != null && m_ArtistRepository != null)
        {
            await RefreshGalleryStatus();
        }
    }

    /// <summary>
    /// Fetches the user's gallery from the cloud and toggles the UI state.
    /// </summary>
    public async Task RefreshGalleryStatus()
    {
        if (m_LoadingText != null) m_LoadingText.text = "Fetching gallery status...";

        try
        {
            if (!AuthenticationManager.Instance.IsAuthenticated)
            {
                LogDebug("User not authenticated — skipping status refresh.");
                return;
            }

            string userId = AuthenticationManager.Instance.CurrentUser.Id;
            ArtistProfile profile = await m_ArtistRepository.GetArtistProfileAsync(userId);

            if (profile != null && profile.managed_gallery != null && profile.managed_gallery.Length > 0)
            {
                // Fetch the first gallery for 1x1 workflow
                if (int.TryParse(profile.managed_gallery[0], out int galleryId))
                {
                    GalleryData gallery = await m_GalleryRepository.GetGalleryAsync(galleryId);
                    if (gallery != null)
                    {
                        ShowInfoPanel(gallery);
                        return;
                    }
                }
            }

            // Fallback: No gallery found
            ShowCreateButton();
        }
        catch (Exception ex)
        {
            LogError($"RefreshGalleryStatus failed: {ex.Message}");
            ShowCreateButton(); // Default to create on error
        }
    }

    private void ShowCreateButton()
    {
        if (m_CreateGalleryButtonContainer != null) m_CreateGalleryButtonContainer.SetActive(true);
        if (m_GalleryInfoUI != null) m_GalleryInfoUI.gameObject.SetActive(false);
        if (m_CreateGalleryPanel != null) m_CreateGalleryPanel.SetActive(false);
    }

    private void ShowInfoPanel(GalleryData gallery)
    {
        if (m_CreateGalleryButtonContainer != null) m_CreateGalleryButtonContainer.SetActive(false);
        if (m_CreateGalleryPanel != null) m_CreateGalleryPanel.SetActive(false);
        
        if (m_GalleryInfoUI != null)
        {
            m_GalleryInfoUI.gameObject.SetActive(true);
            m_GalleryInfoUI.InitializeInfo(gallery);
        }
    }

    // ── Public API for opening the panel ──────────────────────────────────────
    public void OpenCreateGalleryPanel()
    {
        if (!AuthenticationManager.Instance.IsAuthenticated)
        {
            LogError("User not authenticated. Cannot open create gallery panel.");
            m_ErrorText.text = "Please log in first.";
            return;
        }

        LogDebug($"Panel reference: {m_CreateGalleryPanel}");
        LogDebug($"Panel active before: {m_CreateGalleryPanel.activeSelf}");
        LogDebug($"Panel activeInHierarchy before: {m_CreateGalleryPanel.activeInHierarchy}");

        if (m_CreateGalleryPanel.transform.parent != null && !m_CreateGalleryPanel.transform.parent.gameObject.activeSelf)
        {
            LogDebug($"Parent was inactive, activating parent: {m_CreateGalleryPanel.transform.parent.name}");
            m_CreateGalleryPanel.transform.parent.gameObject.SetActive(true);
        }

        m_CreateGalleryPanel.SetActive(true);

        LogDebug($"Panel active after: {m_CreateGalleryPanel.activeSelf}");
        LogDebug($"Panel activeInHierarchy after: {m_CreateGalleryPanel.activeInHierarchy}");

        if (m_CreateGalleryPanel.transform.parent != null)
        {
            LogDebug($"Panel parent: {m_CreateGalleryPanel.transform.parent.name}, parent active: {m_CreateGalleryPanel.transform.parent.gameObject.activeSelf}");
        }

        m_NameInputField.text = "";
        m_ErrorText.text = "";
        m_LoadingText.text = "";
        m_CreateButton.interactable = true;

        LogDebug("Create Gallery panel opened.");
    }

    private void OnCancelButtonClicked()
    {
        ClosePanel();
    }

    private async void OnCreateButtonClicked()
    {
        if (!ValidateInputs())
            return;

        await CreateGallery();
    }

    private bool ValidateInputs()
    {
        m_ErrorText.text = "";

        // Validate name
        if (string.IsNullOrWhiteSpace(m_NameInputField.text))
        {
            m_ErrorText.text = "Gallery name is required.";
            LogDebug("Validation failed: empty name");
            return false;
        }

        if (m_NameInputField.text.Length < 3)
        {
            m_ErrorText.text = "Gallery name must be at least 3 characters.";
            LogDebug("Validation failed: name too short");
            return false;
        }

        if (m_NameInputField.text.Length > 100)
        {
            m_ErrorText.text = "Gallery name must be less than 100 characters.";
            LogDebug("Validation failed: name too long");
            return false;
        }

        return true;
    }

    private async Task CreateGallery()
    {
        try
        {
            m_CreateButton.interactable = false;
            m_LoadingText.text = "Creating gallery...";
            LogDebug("Starting gallery creation...");

            if (!AuthenticationManager.Instance.IsAuthenticated || AuthenticationManager.Instance.CurrentUser == null)
                throw new InvalidOperationException("User not authenticated.");

            // Get currently authenticated user
            string authUserId = AuthenticationManager.Instance.CurrentUser.Id;
            LogDebug($"Auth User ID: {authUserId}");

            // Look up artist profile to get the user_id
            ArtistProfile profile = await m_ArtistRepository.GetArtistProfileAsync(authUserId);
            if (profile == null)
            {
                throw new InvalidOperationException("Artist profile not found for user.");
            }

            LogDebug($"Artist Profile ID: {profile.user_id}, Auth User ID from profile: {profile.auth_user_id}");

            var newGallery = new GalleryData
            {
                name = m_NameInputField.text.Trim(),
                owner_id = profile.user_id, 
                artwork_ids = new List<int>(),
                artwork_map = new Dictionary<int, int>()
            };

            LogDebug($"Creating gallery: {newGallery.name} with owner_id: {newGallery.owner_id}");

            var createdGallery = await m_GalleryRepository.CreateGalleryAsync(newGallery);

            m_LoadingText.text = "Gallery created successfully!";
            LogDebug($"Gallery created with ID: {createdGallery.id}");

            await Task.Delay(1000); // Brief success feedback

            // Refresh our local state
            await RefreshGalleryStatus();

            ClosePanel();
        }
        catch (Exception ex)
        {
            m_ErrorText.text = $"Error creating gallery: {ex.Message}";
            LogError($"Create gallery failed: {ex}");
            m_CreateButton.interactable = true;
        }
    }

    private void ClosePanel()
    {
        m_CreateGalleryPanel.SetActive(false);
        m_CreateButton.interactable = true;
        m_LoadingText.text = "";
        LogDebug("Create Gallery panel closed.");
    }

    // ── For Dependency Injection (Testing) ────────────────────────────────────
    public void SetRepository(IGalleryRepository repository)
    {
        m_GalleryRepository = repository;
    }

    // ── Logging ───────────────────────────────────────────────────────────────
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CreateGalleryUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[CreateGalleryUI] {message}");
    }
}
