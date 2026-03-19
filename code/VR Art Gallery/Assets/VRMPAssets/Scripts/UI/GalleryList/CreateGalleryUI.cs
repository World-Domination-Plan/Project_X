using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRGallery.Authentication;

public class CreateGalleryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject m_CreateGalleryPanel;
    [SerializeField] TMP_InputField m_NameInputField;
    [SerializeField] Button m_CreateButton;
    [SerializeField] Button m_CancelButton;
    [SerializeField] TMP_Text m_ErrorText;
    [SerializeField] TMP_Text m_LoadingText;

    [Header("Dashboard Reference")]
    [SerializeField] GalleryDashboardUI m_GalleryDashboard;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private IGalleryRepository m_GalleryRepository;

    private async void Start()
    {
        // Initialize repository once
        m_GalleryRepository = await SupabaseGalleryRepository.CreateAsync();

        // Hook up button listeners
        m_CreateButton.onClick.AddListener(OnCreateButtonClicked);
        m_CancelButton.onClick.AddListener(OnCancelButtonClicked);

        // Panel starts hidden
        m_CreateGalleryPanel.SetActive(false);

        LogDebug("CreateGalleryUI initialized.");
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

        m_CreateGalleryPanel.SetActive(true);
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

            // Get currently authenticated user
            if (!AuthenticationManager.Instance.IsAuthenticated)
            {
                throw new InvalidOperationException("User not authenticated.");
            }

            string ownerId = AuthenticationManager.Instance.CurrentUser.Id;

            var newGallery = new GalleryData
            {
                name = m_NameInputField.text.Trim(),
                owner_id = ownerId,
                artwork_ids = new List<int>(),
                artwork_map = new Dictionary<int, int>()
            };

            LogDebug($"Creating gallery: {newGallery.name} for user {ownerId}");

            var createdGallery = await m_GalleryRepository.CreateGalleryAsync(newGallery);

            m_LoadingText.text = "Gallery created successfully!";
            LogDebug($"Gallery created with ID: {createdGallery.id}");

            await Task.Delay(1000); // Brief success feedback

            // Refresh the dashboard
            await m_GalleryDashboard.RefreshGalleryList();

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