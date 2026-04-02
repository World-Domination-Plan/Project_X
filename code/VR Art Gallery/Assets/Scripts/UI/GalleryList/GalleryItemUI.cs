using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GalleryItemUI : MonoBehaviour
{
    [SerializeField] TMP_Text m_GalleryNameText;
    [SerializeField] TMP_Text m_ArtworkCountText;
    [SerializeField] Button m_ViewButton;
    [SerializeField] Button m_EditButton;
    [SerializeField] Button m_DeleteButton;

    private GalleryData m_GalleryData;
    private GalleryProfileUI m_GalleryProfile;

    public void InitializeGalleryItem(GalleryData gallery, GalleryProfileUI galleryProfile)
    {
        m_GalleryData = gallery;
        m_GalleryProfile = galleryProfile;

        // Populate UI fields from GalleryData
        m_GalleryNameText.text = gallery.name;
        m_ArtworkCountText.text = $"{gallery.artwork_ids.Count} artworks";

        // Hook up button callbacks
        m_ViewButton.onClick.AddListener(() => m_GalleryProfile.ViewGallery(gallery));
        m_EditButton.onClick.AddListener(() => m_GalleryProfile.EditGallery(gallery));
        m_DeleteButton.onClick.AddListener(() => m_GalleryProfile.DeleteGallery(gallery.id));
    }

    public GalleryData GetGalleryData() => m_GalleryData;
}
