public class GalleryItemUI : MonoBehaviour
{
    [SerializeField] TMP_Text m_GalleryNameText;
    [SerializeField] TMP_Text m_ArtworkCountText;
    [SerializeField] Button m_ViewButton;
    [SerializeField] Button m_EditButton;
    [SerializeField] Button m_DeleteButton;

    private GalleryData m_GalleryData;
    private GalleryDashboardUI m_DashboardUI;

    public void InitializeGalleryItem(GalleryData gallery, GalleryDashboardUI dashboardUI)
    {
        m_GalleryData = gallery;
        m_DashboardUI = dashboardUI;

        // Populate UI fields from GalleryData
        m_GalleryNameText.text = gallery.name;
        m_DescriptionText.text = gallery.description;
        m_ArtworkCountText.text = $"{gallery.artwork_ids.Count} artworks";

        // Hook up button callbacks
        m_ViewButton.onClick.AddListener(() => m_DashboardUI.ViewGallery(gallery));
        m_EditButton.onClick.AddListener(() => m_DashboardUI.EditGallery(gallery));
        m_DeleteButton.onClick.AddListener(() => m_DashboardUI.DeleteGallery(gallery.id));
    }

    public GalleryData GetGalleryData() => m_GalleryData;
}