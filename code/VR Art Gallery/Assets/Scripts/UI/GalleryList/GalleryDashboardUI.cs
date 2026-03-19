using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


public class GalleryDashboardUI : MonoBehaviour
{
    [SerializeField] Transform m_GalleryListParent;
    [SerializeField] GameObject m_GalleryItemPrefab;  // Prefab with GalleryItemUI script
    [SerializeField] TMP_Text m_LoadingText;

    private IGalleryRepository m_GalleryRepository;
    private List<GalleryItemUI> m_ActiveGalleryItems = new List<GalleryItemUI>();

    private async void Start()
    {
        m_GalleryRepository = await SupabaseGalleryRepository.CreateAsync();
        await RefreshGalleryList();
    }

    public async Task RefreshGalleryList()
    {
        m_LoadingText.gameObject.SetActive(true);
        m_LoadingText.text = "Loading galleries...";

        try
        {
            List<GalleryData> galleries = await m_GalleryRepository.GetAllGalleriesAsync();
            DisplayGalleries(galleries);
        }
        catch (Exception ex)
        {
            m_LoadingText.gameObject.SetActive(true);
            m_LoadingText.text = $"Error loading galleries: {ex.Message}";
            Debug.LogError(ex);
        }
    }

    private void DisplayGalleries(List<GalleryData> galleries)
    {
        foreach (var item in m_ActiveGalleryItems)
            Destroy(item.gameObject);
        m_ActiveGalleryItems.Clear();

        if (galleries == null || galleries.Count == 0)
        {
            m_LoadingText.gameObject.SetActive(true);
            m_LoadingText.text = "No galleries yet.";
            return;
        }

        foreach (var gallery in galleries)
        {
            GameObject itemObj = Instantiate(m_GalleryItemPrefab, m_GalleryListParent);
            GalleryItemUI itemUI = itemObj.GetComponent<GalleryItemUI>();
            itemUI.InitializeGalleryItem(gallery, this);
            m_ActiveGalleryItems.Add(itemUI);
        }

        m_LoadingText.gameObject.SetActive(false);
    }

    // Callback methods for action buttons
    public void ViewGallery(GalleryData gallery)
    {
        Debug.Log($"Viewing gallery: {gallery.name}");
        // Load the VR space with this gallery
    }

    public void EditGallery(GalleryData gallery)
    {
        Debug.Log($"Editing gallery: {gallery.name}");
        // Open edit panel with gallery properties
    }

    public async void DeleteGallery(int galleryId)
    {
        try
        {
            await m_GalleryRepository.DeleteGalleryAsync(galleryId);
            await RefreshGalleryList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deleting gallery: {ex.Message}");
        }
    }
}
