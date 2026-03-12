using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WebSocketSharp;
using Unity.Services.Vivox;

namespace XRMultiplayer
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Testing")]
        [SerializeField] bool m_TestMode = false;

        [Header("Lobby List")]
        [SerializeField] List<Transform> m_LobbyListParents = new List<Transform>();
        [SerializeField] GameObject m_LobbyListPrefab;
        [SerializeField] Button m_RefreshButton;
        [SerializeField] Image m_CooldownImage;
        [SerializeField] float m_AutoRefreshTime = 5.0f;
        [SerializeField] float m_RefreshCooldownTime = .5f;

        [Header("Navigation Toggles & Panels")]
        [SerializeField] private Toggle m_HomeToggle;
        [SerializeField] private Toggle m_PrivateGalleriesToggle;
        [SerializeField] private Toggle m_WorkspacesToggle;
        [SerializeField] private GameObject m_HomePanel;
        [SerializeField] private GameObject m_PrivateGalleriesPanel;
        [SerializeField] private GameObject m_WorkspacesPanel;

        [Header("Connection Texts")]
        [SerializeField] TMP_Text m_ConnectionUpdatedText;
        [SerializeField] TMP_Text m_ConnectionSuccessText;
        [SerializeField] TMP_Text m_ConnectionFailedText;

        [Header("Connection Buttons")]
        [SerializeField] Button m_CancelButton;                        // shows while attempting to connect
        [SerializeField] Button m_ConnectionSuccessDoneButton;        // shown when a connection succeeds
        [SerializeField] Button m_ConnectionFailedDoneButton;         // shown when a connection fails
        [SerializeField] Button m_RetryConnectionButton;              // shown when there is no internet

        [Header("Room Creation")]
        [SerializeField] TMP_InputField m_RoomNameText;
        [SerializeField] Toggle m_PrivacyToggle;

        [SerializeField] GameObject[] m_ConnectionSubPanels;

        VoiceChatManager m_VoiceChatManager;

        Coroutine m_UpdateLobbiesRoutine;
        Coroutine m_CooldownFillRoutine;

        bool m_Private = false;
        int m_PlayerCount;

        private void Awake()
        {
            m_VoiceChatManager = FindFirstObjectByType<VoiceChatManager>();
            LobbyManager.status.Subscribe(ConnectedUpdated);
            m_CooldownImage.enabled = false;
        }

        private void Start()
        {
            //m_PrivacyToggle.onValueChanged.AddListener(TogglePrivacy);

            m_PlayerCount = XRINetworkGameManager.maxPlayers / 2;

            XRINetworkGameManager.Instance.connectionFailedAction += FailedToConnect;
            XRINetworkGameManager.Instance.connectionUpdated += ConnectedUpdated;

            ClearLobbyParents();

            // Create test lobby UI components
            CreateTestLobbies();

            // Set up navigation toggles
            m_HomeToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.Home); });
            m_PrivateGalleriesToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.PrivateGalleries); });
            m_WorkspacesToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.Workspaces); });

            // Show only the Home panel at start and set Home toggle on
            m_HomeToggle.isOn = true;
            m_PrivateGalleriesToggle.isOn = false;
            m_WorkspacesToggle.isOn = false;
            ShowPanel(PanelType.Home);

            // hookup connection button callbacks
            if (m_CancelButton != null)
                m_CancelButton.onClick.AddListener(CancelConnection);

            if (m_ConnectionSuccessDoneButton != null)
                m_ConnectionSuccessDoneButton.onClick.AddListener(() => ToggleConnectionSubPanel(0));

            if (m_ConnectionFailedDoneButton != null)
                m_ConnectionFailedDoneButton.onClick.AddListener(() => ToggleConnectionSubPanel(0));

            if (m_RetryConnectionButton != null)
                m_RetryConnectionButton.onClick.AddListener(CheckForInternet);

        }

        private enum PanelType { Home, PrivateGalleries, Workspaces }

        private void ShowPanel(PanelType panel)
        {
            m_HomePanel.SetActive(panel == PanelType.Home);
            m_PrivateGalleriesPanel.SetActive(panel == PanelType.PrivateGalleries);
            m_WorkspacesPanel.SetActive(panel == PanelType.Workspaces);
        }

        // Testing method to create fake lobbies
        void CreateTestLobbies()
        {
            if (!m_TestMode) return;

            // Create multiple fake lobbies for testing
            var fakeLobby1 = CreateFakeLobby("Testing Room 1", 2, 4);
            var fakeLobby2 = CreateFakeLobby("VR Art Gallery", 4, 8);
            var fakeLobby3 = CreateFakeLobby("Full Room", 6, 6);
            var fakeLobby4 = CreateFakeLobby("Empty Room", 0, 4);

            // Instantiate UI for each fake lobby
            CreateLobbyUIForAllParents(fakeLobby1);
            CreateLobbyUIForAllParents(fakeLobby2);
            CreateLobbyUIForAllParents(fakeLobby3);
            CreateLobbyUIForAllParents(fakeLobby4);

            // Example of a non-joinable lobby
            var fakeIncompatibleLobby = CreateFakeLobby("Old Version Room", 3, 6);
            CreateNonJoinableLobbyUIForAllParents(fakeIncompatibleLobby, "Version Conflict");
        }

        // Helper method to create a fake lobby
        Lobby CreateFakeLobby(string name, int currentPlayers, int maxPlayers)
        {
            var lobby = new Lobby(
                id: System.Guid.NewGuid().ToString(),
                lobbyCode: "TEST" + UnityEngine.Random.Range(1000, 9999),
                name: name,
                maxPlayers: maxPlayers,
                availableSlots: maxPlayers - currentPlayers,
                isPrivate: false,
                isLocked: false,
                hasPassword: false,
                created: System.DateTime.UtcNow,
                lastUpdated: System.DateTime.UtcNow,
                hostId: "fake-host-id",
                players: CreateFakePlayers(currentPlayers),
                data: new System.Collections.Generic.Dictionary<string, DataObject>()
            );

            return lobby;
        }

        // Helper method to create fake player list
        System.Collections.Generic.List<Player> CreateFakePlayers(int count)
        {
            var players = new System.Collections.Generic.List<Player>();
            for (int i = 0; i < count; i++)
            {
                players.Add(new Player(
                    id: $"player-{i}",
                    data: new System.Collections.Generic.Dictionary<string, PlayerDataObject>()
                ));
            }
            return players;
        }

        private void OnEnable()
        {
            CheckInternetAsync();
        }

        private void OnDisable()
        {
            HideLobbies();
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Instance.connectionFailedAction -= FailedToConnect;
            XRINetworkGameManager.Instance.connectionUpdated -= ConnectedUpdated;

            LobbyManager.status.Unsubscribe(ConnectedUpdated);
        }
        public async void CheckInternetAsync()
        {
            if (m_TestMode) return;

            if (!XRINetworkGameManager.Instance.IsAuthenticated())
            {
                ToggleConnectionSubPanel(5);
                await XRINetworkGameManager.Instance.Authenticate();
            }
            CheckForInternet();
        }

        void CheckForInternet()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ToggleConnectionSubPanel(5);
            }
            else
            {
                ToggleConnectionSubPanel(0);
            }
        }

        public void CreateLobby()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);

            if (m_RoomNameText.text.IsNullOrEmpty() || m_RoomNameText.text == "<Room Name>")
            {
                m_RoomNameText.text = $"{XRINetworkGameManager.LocalPlayerName.Value}'s Room";
            }

            XRINetworkGameManager.Instance.CreateNewLobby(m_RoomNameText.text, m_Private, m_PlayerCount);

            m_ConnectionSuccessText.text = $"Joining {m_RoomNameText.text}";
        }


        public void UpdatePlayerCount(int count)
        {
            m_PlayerCount = Mathf.Clamp(count, 1, XRINetworkGameManager.maxPlayers);
        }

        public void CancelConnection()
        {
            XRINetworkGameManager.Instance.CancelMatchmaking();
            // return to the lobby list and reset status text
            ToggleConnectionSubPanel(0);
            m_ConnectionUpdatedText.text = string.Empty;
        }

        /// <summary>
        /// Set the room name
        /// </summary>
        /// <param name="roomName">The name of the room</param>
        /// <remarks> This function is called from <see cref="XRIKeyboardDisplay"/>
        public void SetRoomName(string roomName)
        {
            if (!string.IsNullOrEmpty(roomName))
            {
                m_RoomNameText.text = roomName;
            }
        }

        /// <summary>
        /// Join a room by code
        /// </summary>
        /// <param name="roomCode">The room code to join</param>
        /// <remarks> This function is called from <see cref="XRIKeyboardDisplay"/>
        public void EnterRoomCode(string roomCode)
        {
            ToggleConnectionSubPanel(2);
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.Instance.JoinLobbyByCode(roomCode.ToUpper());
            m_ConnectionSuccessText.text = $"Joining Room: {roomCode.ToUpper()}";
        }

        public void JoinLobby(Lobby lobby)
        {
            ToggleConnectionSubPanel(2);
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.Instance.JoinLobbySpecific(lobby);
            m_ConnectionSuccessText.text = $"Joining {lobby.Name}";
        }

        public void QuickJoinLobby()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.Instance.QuickJoinLobby();
            m_ConnectionSuccessText.text = "Joining Random";
        }

        public void SetVoiceChatAudidibleDistance(int audibleDistance)
        {
            if (audibleDistance <= m_VoiceChatManager.ConversationalDistance)
            {
                audibleDistance = m_VoiceChatManager.ConversationalDistance + 1;
            }
            m_VoiceChatManager.AudibleDistance = audibleDistance;
        }

        public void SetVoiceChatConversationalDistance(int conversationalDistance)
        {
            m_VoiceChatManager.ConversationalDistance = conversationalDistance;
        }

        public void SetVoiceChatAudioFadeIntensity(float fadeIntensity)
        {
            m_VoiceChatManager.AudioFadeIntensity = fadeIntensity;
        }

        public void SetVoiceChatAudioFadeModel(int fadeModel)
        {
            m_VoiceChatManager.AudioFadeModel = (AudioFadeModel)fadeModel;
        }

        public void TogglePrivacy(bool toggle)
        {
            m_Private = toggle;
        }

        public void ToggleConnectionSubPanel(int panelId)
        {
            for (int i = 0; i < m_ConnectionSubPanels.Length; i++)
            {
                m_ConnectionSubPanels[i].SetActive(i == panelId);
            }


            if (panelId == 0)
            {
                ShowLobbies();
            }
            else
            {
                HideLobbies();
            }
        }

        void OnConnected(bool connected)
        {
            if (connected)
            {
                ToggleConnectionSubPanel(3);
                XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
            }
        }

        void ConnectedUpdated(string update)
        {
            m_ConnectionUpdatedText.text = $"<b>Status:</b> {update}";
        }

        public void FailedToConnect(string reason)
        {
            ToggleConnectionSubPanel(4);
            m_ConnectionFailedText.text = $"<b>Error:</b> {reason}";
        }

        public void HideLobbies()
        {
            EnableRefresh();
            if (m_UpdateLobbiesRoutine != null) StopCoroutine(m_UpdateLobbiesRoutine);
        }

        public void ShowLobbies()
        {
            if (m_TestMode) return;

            GetAllLobbies();
            if (m_UpdateLobbiesRoutine != null) StopCoroutine(m_UpdateLobbiesRoutine);
            m_UpdateLobbiesRoutine = StartCoroutine(UpdateAvailableLobbies());
        }

        IEnumerator UpdateAvailableLobbies()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_AutoRefreshTime);
                GetAllLobbies();
            }
        }

        void EnableRefresh()
        {
            m_CooldownImage.enabled = false;
            m_RefreshButton.interactable = true;
        }

        IEnumerator UpdateButtonCooldown()
        {
            m_RefreshButton.interactable = false;

            m_CooldownImage.enabled = true;
            for (float i = 0; i < m_RefreshCooldownTime; i += Time.deltaTime)
            {
                m_CooldownImage.fillAmount = Mathf.Clamp01(i / m_RefreshCooldownTime);
                yield return null;
            }
            EnableRefresh();
        }

        async void GetAllLobbies()
        {
            if (m_TestMode) return;
            if (m_CooldownImage.enabled || (int)XRINetworkGameManager.CurrentConnectionState.Value < 2) return;
            if (m_CooldownFillRoutine != null) StopCoroutine(m_CooldownFillRoutine);
            m_CooldownFillRoutine = StartCoroutine(UpdateButtonCooldown());

            QueryResponse lobbies = await LobbyManager.GetLobbiesAsync();

            ClearLobbyParents();

            if (lobbies.Results != null || lobbies.Results.Count > 0)
            {
                foreach (var lobby in lobbies.Results)
                {
                    if (LobbyManager.CheckForLobbyFilter(lobby))
                    {
                        continue;
                    }

                    if (LobbyManager.CheckForIncompatibilityFilter(lobby))
                    {
                        CreateNonJoinableLobbyUIForAllParents(lobby, "Version Conflict");
                        continue;
                    }

                    if (LobbyManager.CanJoinLobby(lobby))
                    {
                        CreateLobbyUIForAllParents(lobby);
                    }
                }
            }
        }

        IEnumerable<Transform> GetLobbyParents()
        {
            if (m_LobbyListParents == null || m_LobbyListParents.Count == 0)
            {
                yield break;
            }

            foreach (var parent in m_LobbyListParents)
            {
                if (parent != null)
                {
                    yield return parent;
                }
            }
        }

        void ClearLobbyParents()
        {
            foreach (var parent in GetLobbyParents())
            {
                foreach (Transform t in parent)
                {
                    Destroy(t.gameObject);
                }
            }
        }

        void CreateLobbyUIForAllParents(Lobby lobby)
        {
            foreach (var parent in GetLobbyParents())
            {
                LobbyListSlotUI newLobbyUI = Instantiate(m_LobbyListPrefab, parent).GetComponent<LobbyListSlotUI>();
                newLobbyUI.CreateLobbyUI(lobby, this);
            }
        }

        void CreateNonJoinableLobbyUIForAllParents(Lobby lobby, string reason)
        {
            foreach (var parent in GetLobbyParents())
            {
                LobbyListSlotUI newLobbyUI = Instantiate(m_LobbyListPrefab, parent).GetComponent<LobbyListSlotUI>();
                newLobbyUI.CreateNonJoinableLobbyUI(lobby, this, reason);
            }
        }
    }
}