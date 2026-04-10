using UnityEngine;
using UnityEngine.UI;

public class GalleryInfoArtworkDisplay : MonoBehaviour
{
    [SerializeField] private RawImage m_ThumbnailImage;
    [SerializeField] private Button m_SwapButton;

    private int _artworkId;
    private GalleryInfoUI _parentUI;

    public void Initialize(int artworkId, Texture2D thumbnail, GalleryInfoUI parentUI)
    {
        _artworkId = artworkId;
        _parentUI = parentUI;

        if (m_ThumbnailImage != null && thumbnail != null)
        {
            m_ThumbnailImage.texture = thumbnail;
            m_ThumbnailImage.color = Color.white;
        }
        else if (m_ThumbnailImage != null)
        {
            // Reset to grey if no thumbnail
            m_ThumbnailImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            m_ThumbnailImage.texture = null;
        }

        if (m_SwapButton != null)
        {
            m_SwapButton.onClick.RemoveAllListeners();
            m_SwapButton.onClick.AddListener(OnSwapButtonClicked);
        }
    }

    private void OnSwapButtonClicked()
    {
        GalleryInfoUI.HandleArtworkClicked(_artworkId);
    }
}
