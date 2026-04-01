using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using VRGallery.Cloud;

/// <summary>
/// Panel that displays information about a single gallery and its artworks in a 3x3 grid.
/// </summary>
public class GalleryInfoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text m_GalleryNameText;
    [SerializeField] private TMP_Text m_ArtworkCountText;
    [SerializeField] private GameObject m_GridContainer; // Should have exactly 9 buttons as children
    [SerializeField] private Button m_LoadGalleryButton;

    [Header("Dependencies")]
    [SerializeField] private MonoBehaviour m_LobbyUI; // The script that controls the lobby/room creation

    private GalleryData m_CurrentGallery;
    private CreateGalleryUI m_ParentProfile;
    private List<Button> m_SlotButtons = new List<Button>();

    private void Awake()
    {
        // Hook up Load Gallery button
        if (m_LoadGalleryButton != null)
        {
            m_LoadGalleryButton.onClick.AddListener(OnLoadGalleryClicked);
        }

        // Gather slots from grid container
        if (m_GridContainer != null)
        {
            foreach (Transform child in m_GridContainer.transform)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null)
                {
                    int index = m_SlotButtons.Count;
                    btn.onClick.AddListener(() => OnSlotClicked(index));
                    m_SlotButtons.Add(btn);
                }
            }
        }
    }

    /// <summary>
    /// Populates the UI with gallery data.
    /// </summary>
    public void InitializeInfo(GalleryData gallery, CreateGalleryUI profile)
    {
        m_CurrentGallery = gallery;
        m_ParentProfile = profile;

        if (m_GalleryNameText != null)
            m_GalleryNameText.text = gallery.name;

        UpdateArtworkCount();
        UpdateSlotVisuals();
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
        // For now, we'll just indicate which slots have art. 
        // In a real implementation, you might show a thumbnail or name.
        if (m_CurrentGallery.artwork_map == null) return;

        for (int i = 0; i < m_SlotButtons.Count; i++)
        {
            // Check if slot exists in the gallery's map
            bool hasArt = m_CurrentGallery.artwork_map.ContainsKey(i);
            
            // You can change button color or icon here
            ColorBlock cb = m_SlotButtons[i].colors;
            cb.normalColor = hasArt ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            m_SlotButtons[i].colors = cb;
            
            // Optional: Update text on button if it exists
            TMP_Text txt = m_SlotButtons[i].GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = hasArt ? $"Art Slot {i}" : "Empty Slot";
            }
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        Debug.Log($"Slot {slotIndex} clicked for gallery {m_CurrentGallery.id}");
        // TODO: Open ArtworkSelectionUI to pick an image for this slot.
    }

    private void OnLoadGalleryClicked()
    {
        Debug.Log("Loading gallery to LobbyUI...");
        if (m_LobbyUI != null)
        {
            // Activate the Lobby Panel
            m_LobbyUI.gameObject.SetActive(true);
            
            // If you need to switch view or inform Lobby UI about the gallery, do it here.
            // Example: m_LobbyUI.PrepareRoomForGallery(m_CurrentGallery.id);
        }
        else
        {
            Debug.LogError("LobbyUI reference missing in GalleryInfoUI!");
        }
    }
}
