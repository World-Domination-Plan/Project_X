using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WebSocketSharp;
using Unity.Services.Vivox;
using System.Threading.Tasks;



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
        [SerializeField] float m_AutoRefreshTime = 120.0f;
        [SerializeField] float m_RefreshCooldownTime = .5f;

        [Header("Login UI Positioning")]
        [SerializeField] private float m_UIDistance = 1.5f;
        [SerializeField] private float m_UIHeightOffset = 0.0f;
        [SerializeField] private float m_UISideOffset = 0.0f;

        [Header("Navigation Toggles & Panels")]
        [SerializeField] private Toggle m_HomeToggle;
        [SerializeField] private Toggle m_PrivateGalleriesToggle;
        [SerializeField] private Toggle m_WorkspacesToggle;
        [SerializeField] private Toggle m_LoginToggle;
        [SerializeField] private GameObject m_LoginUI;        // Assign the SCENE object here
        [SerializeField] private Transform m_HeadCamera;
        // [SerializeField] private float m_UIDistance = 1.5f;
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
        bool m_IsLoadingConnectedLobbyGallery = false;
        bool m_IsCreateGalleryLoadInProgress = false;
        bool m_DidCreateGalleryLoadSucceed = false;

        /// <summary>
        /// Max players passed to <see cref="XRINetworkGameManager.CreateNewLobby"/>; kept in sync by UI (e.g. IntButtonUI) via <see cref="UpdatePlayerCount"/>.
        /// Defaults to <see cref="XRINetworkGameManager.maxPlayers"/> so capacity is valid before any UI runs and is not overwritten in <c>Start</c>.
        /// </summary>
        int m_PlayerCount = XRINetworkGameManager.maxPlayers;

        private void Awake()
        {
            m_VoiceChatManager = FindFirstObjectByType<VoiceChatManager>();
            LobbyManager.status.Subscribe(ConnectedUpdated);
            m_CooldownImage.enabled = false;
        }

        private void Start()
        {
            XRINetworkGameManager.Instance.connectionFailedAction += FailedToConnect;
            XRINetworkGameManager.Instance.connectionUpdated += ConnectedUpdated;

            ClearLobbyParents();
            CreateTestLobbies();

            // Set up navigation toggles (guard against missing references)
            if (m_HomeToggle != null)
                m_HomeToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.Home); });
            if (m_PrivateGalleriesToggle != null)
                m_PrivateGalleriesToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.PrivateGalleries); });
            if (m_WorkspacesToggle != null)
                m_WorkspacesToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.Workspaces); });

            // Show only the Home panel at start and set Home toggle on
            if (m_HomeToggle != null) m_HomeToggle.isOn = true;
            if (m_PrivateGalleriesToggle != null) m_PrivateGalleriesToggle.isOn = false;
            if (m_WorkspacesToggle != null) m_WorkspacesToggle.isOn = false;
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

        private void ShowLoginUI()
        {
            if (m_LoginUI == null)
            {
                Debug.Log("No login panel in scene — skipping.");
                return;
            }

            if (m_HeadCamera == null) m_HeadCamera = Camera.main.transform;

            // Reposition in front of camera then show
            Vector3 spawnPos = m_HeadCamera.position 
            + m_HeadCamera.forward * m_UIDistance
            + Vector3.up * m_UIHeightOffset
            + m_HeadCamera.right * m_UISideOffset;

            m_LoginUI.transform.position = spawnPos;
            m_LoginUI.transform.rotation = Quaternion.LookRotation(m_HeadCamera.forward);
            m_LoginUI.SetActive(true);

            Debug.Log("Login UI shown at: " + spawnPos);
        }

        public void HideLoginUI()
        {
            if (m_LoginUI == null) return;
            m_LoginUI.SetActive(false);
            Debug.Log("Login UI hidden.");
        }

        private void ShowPanel(PanelType panel)
        {
            if (m_HomePanel != null)
                m_HomePanel.SetActive(panel == PanelType.Home);
            if (m_PrivateGalleriesPanel != null)
                m_PrivateGalleriesPanel.SetActive(panel == PanelType.PrivateGalleries);
            if (m_WorkspacesPanel != null)
                m_WorkspacesPanel.SetActive(panel == PanelType.Workspaces);
        }

        void CreateTestLobbies()
        {
            if (!m_TestMode) return;

            var fakeLobby1 = CreateFakeLobby("Testing Room 1", 2, 4);
            var fakeLobby2 = CreateFakeLobby("VR Art Gallery", 4, 8);
            var fakeLobby3 = CreateFakeLobby("Full Room", 6, 6);
            var fakeLobby4 = CreateFakeLobby("Empty Room", 0, 4);

            CreateLobbyUIForAllParents(fakeLobby1);
            CreateLobbyUIForAllParents(fakeLobby2);
            CreateLobbyUIForAllParents(fakeLobby3);
            CreateLobbyUIForAllParents(fakeLobby4);

            var fakeIncompatibleLobby = CreateFakeLobby("Old Version Room", 3, 6);
            CreateNonJoinableLobbyUIForAllParents(fakeIncompatibleLobby, "Version Conflict");
        }

        Lobby CreateFakeLobby(string name, int currentPlayers, int maxPlayers)
        {
            return new Lobby(
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
                data: new Dictionary<string, DataObject>()
            );
        }

        List<Player> CreateFakePlayers(int count)
        {
            var players = new List<Player>();
            for (int i = 0; i < count; i++)
                players.Add(new Player(id: $"player-{i}", data: new Dictionary<string, PlayerDataObject>()));
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
                ToggleConnectionSubPanel(5);
            else
                ToggleConnectionSubPanel(0);
        }

        public async void CreateLobby()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);

            if (m_RoomNameText.text.IsNullOrEmpty() || m_RoomNameText.text == "<Room Name>")
                m_RoomNameText.text = $"{XRINetworkGameManager.LocalPlayerName.Value}'s Room";

            XRINetworkGameManager.Instance.CreateNewLobby(m_RoomNameText.text, m_Private, m_PlayerCount);
            m_ConnectionSuccessText.text = $"Joining {m_RoomNameText.text}";

            m_IsCreateGalleryLoadInProgress = true;
            m_DidCreateGalleryLoadSucceed = false;
            try
            {
                m_DidCreateGalleryLoadSucceed = await LoadLocalGalleryAfterCreateAsync();
            }
            finally
            {
                m_IsCreateGalleryLoadInProgress = false;
            }
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

        public void SetRoomName(string roomName)
        {
            if (!string.IsNullOrEmpty(roomName))
                m_RoomNameText.text = roomName;
        }

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
                audibleDistance = m_VoiceChatManager.ConversationalDistance + 1;
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
                m_ConnectionSubPanels[i].SetActive(i == panelId);

            if (panelId == 0) ShowLobbies();
            else HideLobbies();
        }

        async void OnConnected(bool connected)
        {
            if (connected)
            {
                ToggleConnectionSubPanel(3);
                XRINetworkGameManager.Connected.Unsubscribe(OnConnected);

                while (m_IsCreateGalleryLoadInProgress)
                    await Task.Yield();

                if (m_DidCreateGalleryLoadSucceed)
                {
                    m_DidCreateGalleryLoadSucceed = false;
                    return;
                }

                await LoadConnectedLobbyGalleryAsync();
            }
        }

        async Task<bool> LoadLocalGalleryAfterCreateAsync()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || behaviour.GetType().Name != "GalleryManager")
                    continue;

                var method = behaviour.GetType().GetMethod("InitializeAndLoadGalleryAsync");
                if (method == null)
                    return false;

                try
                {
                    if (method.Invoke(behaviour, null) is Task initTask)
                    {
                        await initTask;
                        return true;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to load gallery right after lobby creation: {ex.Message}");
                    return false;
                }

                return false;
            }

            Debug.LogWarning("GalleryManager was not found in the scene. Skipping create-time gallery load.");
            return false;
        }

        async Task LoadConnectedLobbyGalleryAsync()
        {
            if (m_IsLoadingConnectedLobbyGallery)
                return;

            m_IsLoadingConnectedLobbyGallery = true;

            try
            {
                string hostAuthUserId = null;
                Lobby connectedLobby = XRINetworkGameManager.Instance?.lobbyManager?.connectedLobby;

                if (connectedLobby != null &&
                    connectedLobby.Data != null &&
                    connectedLobby.Data.TryGetValue(LobbyManager.k_HostAuthUserIdKeyIdentifier, out DataObject hostAuthData))
                {
                    hostAuthUserId = hostAuthData?.Value;
                }

                if (string.IsNullOrWhiteSpace(hostAuthUserId) && connectedLobby != null)
                {
                    // Backward compatibility path for lobbies created before host metadata was added.
                    hostAuthUserId = connectedLobby.HostId;
                }

                var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (var behaviour in behaviours)
                {
                    if (behaviour == null || behaviour.GetType().Name != "GalleryManager")
                        continue;

                    if (!string.IsNullOrWhiteSpace(hostAuthUserId))
                    {
                        var hostGalleryMethod = behaviour.GetType().GetMethod("InitializeAndLoadGalleryByAuthUserIdAsync");
                        if (hostGalleryMethod != null &&
                            hostGalleryMethod.Invoke(behaviour, new object[] { hostAuthUserId }) is Task hostGalleryTask)
                        {
                            await hostGalleryTask;
                            return;
                        }
                    }

                    var fallbackMethod = behaviour.GetType().GetMethod("InitializeAndLoadGalleryAsync");
                    if (fallbackMethod != null && fallbackMethod.Invoke(behaviour, null) is Task fallbackTask)
                    {
                        await fallbackTask;
                    }

                    return;
                }

                Debug.LogWarning("GalleryManager was not found in the scene. Skipping gallery load after lobby connect.");
            }
            finally
            {
                m_IsLoadingConnectedLobbyGallery = false;
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

            if (lobbies.Results != null && lobbies.Results.Count > 0)
            {
                foreach (var lobby in lobbies.Results)
                {
                    if (LobbyManager.CheckForLobbyFilter(lobby)) continue;

                    if (LobbyManager.CheckForIncompatibilityFilter(lobby))
                    {
                        CreateNonJoinableLobbyUIForAllParents(lobby, "Version Conflict");
                        continue;
                    }

                    if (LobbyManager.CanJoinLobby(lobby))
                        CreateLobbyUIForAllParents(lobby);
                }
            }
        }

        IEnumerable<Transform> GetLobbyParents()
        {
            if (m_LobbyListParents == null || m_LobbyListParents.Count == 0) yield break;
            foreach (var parent in m_LobbyListParents)
                if (parent != null) yield return parent;
        }

        void ClearLobbyParents()
        {
            foreach (var parent in GetLobbyParents())
                foreach (Transform t in parent)
                    Destroy(t.gameObject);
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
