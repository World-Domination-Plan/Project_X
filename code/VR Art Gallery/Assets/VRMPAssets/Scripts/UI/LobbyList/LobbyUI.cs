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
        [SerializeField] private Toggle m_LoginToggle;
        [SerializeField] private GameObject m_LoginUI;
        [SerializeField] private Transform m_HeadCamera;
        [SerializeField] private float m_UIDistance = 1.5f;
        [SerializeField] private GameObject m_HomePanel;
        [SerializeField] private GameObject m_PrivateGalleriesPanel;
        [SerializeField] private GameObject m_WorkspacesPanel;
        // CHANGED: renamed from loginUIPrefab to track single active instance
        private GameObject m_LoginUIInstance;

        [Header("Connection Texts")]
        [SerializeField] TMP_Text m_ConnectionUpdatedText;
        [SerializeField] TMP_Text m_ConnectionSuccessText;
        [SerializeField] TMP_Text m_ConnectionFailedText;

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
            m_PlayerCount = XRINetworkGameManager.maxPlayers / 2;

            XRINetworkGameManager.Instance.connectionFailedAction += FailedToConnect;
            XRINetworkGameManager.Instance.connectionUpdated += ConnectedUpdated;

            ClearLobbyParents();
            CreateTestLobbies();

            m_HomeToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.Home); });
            m_PrivateGalleriesToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.PrivateGalleries); });
            m_WorkspacesToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.Workspaces); });
            // CHANGED: toggle now calls ShowLoginUI/HideLoginUI instead of initLoginPrefab
            m_LoginToggle.onValueChanged.AddListener((isOn) => { if (isOn) ShowLoginUI(); else HideLoginUI(); });

            m_HomeToggle.isOn = true;
            m_PrivateGalleriesToggle.isOn = false;
            m_WorkspacesToggle.isOn = false;
            ShowPanel(PanelType.Home);
        }

        private enum PanelType { Home, PrivateGalleries, Workspaces }

        // CHANGED: replaces initLoginPrefab(); destroys old clone, spawns once in front of camera
        private void ShowLoginUIBox()
        {
            if (m_LoginUIInstance != null) Destroy(m_LoginUIInstance);
            if (m_HeadCamera == null) m_HeadCamera = Camera.main.transform;

            // Spawn a visible debug cube first
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = m_HeadCamera.position + m_HeadCamera.forward * 1.5f;
            cube.transform.localScale = Vector3.one * 0.1f;

            // Then spawn UI
            m_LoginUIInstance = Instantiate(m_LoginUI);
            
        }
        private void ShowLoginUI()
        {
            if (m_LoginUIInstance != null)
                Destroy(m_LoginUIInstance);

            if (m_HeadCamera == null) m_HeadCamera = Camera.main.transform;

            Vector3 spawnPos = m_HeadCamera.position + m_HeadCamera.forward * m_UIDistance;
            spawnPos.y = m_HeadCamera.position.y;
            m_LoginUIInstance = Instantiate(m_LoginUI, spawnPos, Quaternion.LookRotation(m_HeadCamera.forward));
            m_LoginUIInstance.transform.localScale = Vector3.one * 0.001f;
        }

        // CHANGED: new method to clean up instance when toggle turns off
        private void HideLoginUI()
        {
            if (m_LoginUIInstance != null)
            {
                Destroy(m_LoginUIInstance);
                m_LoginUIInstance = null;
            }
        }

        // CHANGED: fixed feedback loop; uses camera forward directly instead of direction from UI position
        //private void Update()
        //{
        //    if (m_LoginUIInstance != null && m_HeadCamera != null)
        //    {
        //        Vector3 targetPos = m_HeadCamera.position + m_HeadCamera.forward * m_UIDistance;
        //        targetPos.y = m_HeadCamera.position.y;
        //        m_LoginUIInstance.transform.position = targetPos;
        //        m_LoginUIInstance.transform.rotation = Quaternion.LookRotation(m_HeadCamera.forward);
        //    }
        //}

        private void ShowPanel(PanelType panel)
        {
            m_HomePanel.SetActive(panel == PanelType.Home);
            m_PrivateGalleriesPanel.SetActive(panel == PanelType.PrivateGalleries);
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
                ToggleConnectionSubPanel(5);
            else
                ToggleConnectionSubPanel(0);
        }

        public void CreateLobby()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            if (m_RoomNameText.text.IsNullOrEmpty() || m_RoomNameText.text == "<Room Name>")
                m_RoomNameText.text = $"{XRINetworkGameManager.LocalPlayerName.Value}'s Room";
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

        public void OpenLogin(bool isOn)
        {
            if (isOn) m_LoginUI.SetActive(true);
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
