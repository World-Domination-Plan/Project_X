using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRGallery.Authentication;
using VRGallery.Core;

namespace VRGallery.UI
{
    /// <summary>
    /// User interface component that displays current user information and authentication status
    /// Can be used in main menu, gallery, or any other scene to show user status
    /// </summary>
    public class UserStatusDisplay : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject authenticatedPanel;
        [SerializeField] private GameObject guestPanel;
        [SerializeField] private TextMeshProUGUI userNameText;
        [SerializeField] private TextMeshProUGUI userRoleText;
        [SerializeField] private TextMeshProUGUI guestText;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Image roleIcon;

        [Header("Role Icons")]
        [SerializeField] private Sprite guestIcon;
        [SerializeField] private Sprite artistIcon;
        [SerializeField] private Sprite adminIcon;

        [Header("Settings")]
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private string guestDisplayText = "Welcome, Guest!";

        private GameManager gameManager;
        private AuthenticationManager authManager;

        private void Start()
        {
            InitializeComponents();
            SetupButtonListeners();
            UpdateDisplay();
        }

        private void InitializeComponents()
        {
            gameManager = GameManager.Instance;
            authManager = AuthenticationManager.Instance;

            if (gameManager != null)
            {
                gameManager.OnAuthenticationStateChanged += OnAuthenticationStateChanged;
                gameManager.OnUserRoleChanged += OnUserRoleChanged;
            }
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnAuthenticationStateChanged -= OnAuthenticationStateChanged;
                gameManager.OnUserRoleChanged -= OnUserRoleChanged;
            }
        }

        private void SetupButtonListeners()
        {
            if (loginButton)
                loginButton.onClick.AddListener(OnLoginButtonClick);

            if (logoutButton)
                logoutButton.onClick.AddListener(OnLogoutButtonClick);
        }

        #region Event Handlers

        private void OnAuthenticationStateChanged(bool isAuthenticated)
        {
            if (autoUpdate)
                UpdateDisplay();
        }

        private void OnUserRoleChanged(UserRole role)
        {
            if (autoUpdate)
                UpdateDisplay();
        }

        private void OnLoginButtonClick()
        {
            // You can implement different behaviors here:
            // 1. Show login UI panel
            // 2. Load authentication scene
            // 3. Trigger authentication UI component

            var authUI = Object.FindFirstObjectByType<AuthenticationUI>();
            if (authUI != null)
            {
                authUI.ShowAuthenticationUI();
            }
            else
            {
                // Fallback: load authentication scene
                gameManager?.LoadAuthenticationScene();
            }
        }

        private async void OnLogoutButtonClick()
        {
            if (gameManager != null)
            {
                await gameManager.LogoutCurrentUser();
            }
        }

        #endregion

        #region Display Updates

        /// <summary>
        /// Update the display based on current authentication status
        /// </summary>
        public void UpdateDisplay()
        {
            bool isAuthenticated = gameManager?.IsUserAuthenticated ?? false;

            if (isAuthenticated)
            {
                ShowAuthenticatedView();
            }
            else
            {
                ShowGuestView();
            }
        }

        private async void ShowAuthenticatedView()
        {
            if (authenticatedPanel) authenticatedPanel.SetActive(true);
            if (guestPanel) guestPanel.SetActive(false);

            // Update user name
            if (userNameText && gameManager != null)
            {
                userNameText.text = await gameManager.GetCurrentUserDisplayName();
            }

            // Update user role
            if (userRoleText && gameManager != null)
            {
                var role = gameManager.CurrentUserRole;
                userRoleText.text = role.ToDisplayString();

                // Update role icon
                UpdateRoleIcon(role);
            }
        }

        private void ShowGuestView()
        {
            if (authenticatedPanel) authenticatedPanel.SetActive(false);
            if (guestPanel) guestPanel.SetActive(true);

            if (guestText)
            {
                guestText.text = guestDisplayText;
            }

            UpdateRoleIcon(UserRole.Guest);
        }

        private void UpdateRoleIcon(UserRole role)
        {
            if (roleIcon == null) return;

            Sprite iconToUse = role switch
            {
                UserRole.Artist => artistIcon,
                UserRole.Admin => adminIcon,
                _ => guestIcon
            };

            if (iconToUse != null)
            {
                roleIcon.sprite = iconToUse;
                roleIcon.gameObject.SetActive(true);
            }
            else
            {
                roleIcon.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually refresh the display
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateDisplay();
        }

        /// <summary>
        /// Set custom guest display text
        /// </summary>
        public void SetGuestText(string text)
        {
            guestDisplayText = text;
            if (!gameManager?.IsUserAuthenticated ?? true)
            {
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Enable or disable auto-update
        /// </summary>
        public void SetAutoUpdate(bool enabled)
        {
            autoUpdate = enabled;
        }

        #endregion
    }
}