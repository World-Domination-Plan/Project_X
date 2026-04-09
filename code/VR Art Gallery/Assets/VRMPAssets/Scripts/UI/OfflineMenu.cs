using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XRMultiplayer
{
    public class OfflineMenu : MonoBehaviour
    {
        [Header("Player Colors")]
        [SerializeField, Tooltip("Colors to choose from for the player")]
        private Color[] m_PlayerColors;

        [Header("Player Info")]
        [SerializeField, Tooltip("Default name for the player")]
        private string m_DefaultPlayerName = "Unity Creator";
        [SerializeField] private TMP_Text m_PlayerNameText;
        [SerializeField] private TMP_Text m_PlayerInitialText;
        [SerializeField] private Image[] m_PlayerColorIcons;
        [SerializeField] private Image m_VolumeIndicator;
        [SerializeField] private Image m_MicIcon;
        [SerializeField] private Sprite m_MutedSprite;
        [SerializeField] private Sprite m_UnmutedSprite;

        [Header("Panel Objects")]
        [SerializeField] private GameObject m_CustomizationPanel;
        [SerializeField] private GameObject m_ConnectionPanel;
        [SerializeField] private GameObject m_Door;

        private VoiceChatManager m_VoiceChatManager;

        private void Awake()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.LocalPlayerName.Subscribe(SetPlayerName);
            XRINetworkGameManager.LocalPlayerColor.Subscribe(SetPlayerColor);
            OfflinePlayerAvatar.voiceAmp.Subscribe(UpdateMicIcon);

            m_VoiceChatManager = FindFirstObjectByType<VoiceChatManager>();
            if (m_VoiceChatManager != null)
            {
                m_VoiceChatManager.selfMuted.Subscribe(MutedChanged);
            }

            SetupPlayerDefaults();
        }

        private void Start()
        {
            if (XRINetworkGameManager.Instance != null)
            {
                XRINetworkGameManager.Instance.connectionFailedAction += ConnectionFailed;
            }
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
            XRINetworkGameManager.LocalPlayerName.Unsubscribe(SetPlayerName);
            XRINetworkGameManager.LocalPlayerColor.Unsubscribe(SetPlayerColor);
            OfflinePlayerAvatar.voiceAmp.Unsubscribe(UpdateMicIcon);

            if (m_VoiceChatManager != null)
            {
                m_VoiceChatManager.selfMuted.Unsubscribe(MutedChanged);
            }

            if (XRINetworkGameManager.Instance != null)
            {
                XRINetworkGameManager.Instance.connectionFailedAction -= ConnectionFailed;
            }
        }

        private void SetupPlayerDefaults()
        {
            XRINetworkGameManager.LocalPlayerName.Value = m_DefaultPlayerName;

            if (m_PlayerColors != null && m_PlayerColors.Length > 0)
            {
                XRINetworkGameManager.LocalPlayerColor.Value =
                    m_PlayerColors[Random.Range(0, m_PlayerColors.Length)];
            }
        }

        private void SetPlayerName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                SetupPlayerDefaults();
                return;
            }

            if (m_PlayerNameText != null)
            {
                m_PlayerNameText.text = name;
                m_PlayerNameText.rectTransform.sizeDelta = new Vector2(
                    m_PlayerNameText.preferredWidth * 0.25f,
                    m_PlayerNameText.rectTransform.sizeDelta.y
                );
            }

            if (m_PlayerInitialText != null)
            {
                m_PlayerInitialText.text = name.Substring(0, 1);
            }
        }

        private void SetPlayerColor(Color color)
        {
            if (m_PlayerColorIcons == null) return;

            foreach (var icon in m_PlayerColorIcons)
            {
                if (icon != null)
                    icon.color = color;
            }
        }

        private void UpdateMicIcon(float amp)
        {
            if (m_VolumeIndicator != null)
                m_VolumeIndicator.fillAmount = amp;
        }

        private void MutedChanged(bool muted)
        {
            if (m_MicIcon != null)
                m_MicIcon.sprite = muted ? m_MutedSprite : m_UnmutedSprite;
        }

        public void CompleteCustomization()
        {
            if (m_CustomizationPanel != null)
                m_CustomizationPanel.SetActive(false);
        }

        private void OnConnected(bool connected)
        {
            if (connected)
            {
                if (m_CustomizationPanel != null)
                    m_CustomizationPanel.SetActive(false);

                if (m_ConnectionPanel != null)
                    m_ConnectionPanel.SetActive(false);

                if (m_Door != null)
                    m_Door.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        private void ConnectionFailed(string reason)
        {
            Debug.LogWarning($"[OfflineMenu] Connection failed: {reason}");

            if (m_ConnectionPanel != null)
                m_ConnectionPanel.SetActive(false);
        }
    }
}
