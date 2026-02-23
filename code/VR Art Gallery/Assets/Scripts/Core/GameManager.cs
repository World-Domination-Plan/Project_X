using UnityEngine;
using VRGallery.Authentication;
using System.Threading.Tasks;

namespace VRGallery.Core
{
    /// <summary>
    /// Main application manager that handles authentication state and scene management
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string galleryScene = "Gallery";
        [SerializeField] private string authenticationScene = "Authentication";

        [Header("Settings")]
        [SerializeField] private bool requireAuthenticationForGallery = true;
        [SerializeField] private UserRole minimumRoleForGallery = UserRole.Guest;

        // Singleton pattern
        public static GameManager Instance { get; private set; }

        // Current game state
        public bool IsUserAuthenticated => AuthenticationManager.Instance?.IsAuthenticated ?? false;
        public UserRole CurrentUserRole { get; private set; } = UserRole.Guest;

        // Events
        public System.Action<bool> OnAuthenticationStateChanged;
        public System.Action<UserRole> OnUserRoleChanged;

        private AuthenticationManager authManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private async void InitializeGame()
        {
            Debug.Log("[GameManager] Initializing game...");

            // Wait for AuthenticationManager to be ready
            while (AuthenticationManager.Instance == null)
            {
                await Task.Delay(100);
            }

            authManager = AuthenticationManager.Instance;

            // Subscribe to authentication events
            authManager.OnUserLoggedIn += HandleUserLoggedIn;
            authManager.OnUserLoggedOut += HandleUserLoggedOut;
            authManager.OnUserRoleChanged += HandleUserRoleChanged;

            // Initialize current state
            if (authManager.IsAuthenticated)
            {
                CurrentUserRole = await authManager.GetCurrentUserRole();
                OnAuthenticationStateChanged?.Invoke(true);
                OnUserRoleChanged?.Invoke(CurrentUserRole);
            }

            Debug.Log("[GameManager] Game initialization complete");
        }

        #region Authentication Event Handlers

        private async void HandleUserLoggedIn(Supabase.Gotrue.User user)
        {
            Debug.Log($"[GameManager] User logged in: {user.Email}");

            CurrentUserRole = await authManager.GetCurrentUserRole();
            OnAuthenticationStateChanged?.Invoke(true);
            OnUserRoleChanged?.Invoke(CurrentUserRole);
        }

        private void HandleUserLoggedOut()
        {
            Debug.Log("[GameManager] User logged out");

            CurrentUserRole = UserRole.Guest;
            OnAuthenticationStateChanged?.Invoke(false);
            OnUserRoleChanged?.Invoke(CurrentUserRole);
        }

        private void HandleUserRoleChanged(UserRole newRole)
        {
            Debug.Log($"[GameManager] User role changed to: {newRole}");

            CurrentUserRole = newRole;
            OnUserRoleChanged?.Invoke(CurrentUserRole);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Check if current user can access a feature based on required role
        /// </summary>
        public bool CanAccessFeature(UserRole requiredRole)
        {
            return (int)CurrentUserRole >= (int)requiredRole;
        }

        /// <summary>
        /// Check if current user can create art
        /// </summary>
        public bool CanCreateArt()
        {
            return CurrentUserRole.CanCreateArt();
        }

        /// <summary>
        /// Check if current user can moderate content
        /// </summary>
        public bool CanModerateContent()
        {
            return CurrentUserRole.CanModerateContent();
        }

        /// <summary>
        /// Check if current user can manage other users
        /// </summary>
        public bool CanManageUsers()
        {
            return CurrentUserRole.CanManageUsers();
        }

        /// <summary>
        /// Get current user display name
        /// </summary>
        public async Task<string> GetCurrentUserDisplayName()
        {
            if (authManager == null)
                return "Guest";

            return await authManager.GetUserDisplayName();
        }

        /// <summary>
        /// Force logout current user
        /// </summary>
        public async Task LogoutCurrentUser()
        {
            if (authManager != null)
            {
                await authManager.LogoutUser();
            }
        }

        /// <summary>
        /// Check if user meets gallery access requirements
        /// </summary>
        public bool CanAccessGallery()
        {
            if (!requireAuthenticationForGallery)
                return true;

            return IsUserAuthenticated && CanAccessFeature(minimumRoleForGallery);
        }

        #endregion

        #region Scene Management

        /// <summary>
        /// Load main menu scene
        /// </summary>
        public void LoadMainMenu()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
        }

        /// <summary>
        /// Load gallery scene if user has access
        /// </summary>
        public void LoadGallery()
        {
            if (CanAccessGallery())
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(galleryScene);
            }
            else
            {
                Debug.LogWarning("[GameManager] User does not have access to gallery");

                if (!IsUserAuthenticated)
                {
                    // Show authentication UI or load auth scene
                    LoadAuthenticationScene();
                }
            }
        }

        /// <summary>
        /// Load authentication scene
        /// </summary>
        public void LoadAuthenticationScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(authenticationScene);
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (Instance == this)
            {
                if (authManager != null)
                {
                    authManager.OnUserLoggedIn -= HandleUserLoggedIn;
                    authManager.OnUserLoggedOut -= HandleUserLoggedOut;
                    authManager.OnUserRoleChanged -= HandleUserRoleChanged;
                }
                Instance = null;
            }
        }

        #endregion

        #region Debug Methods

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void OnValidate()
        {
            // Validate scene names in editor
            if (string.IsNullOrEmpty(mainMenuScene))
                mainMenuScene = "MainMenu";
            if (string.IsNullOrEmpty(galleryScene))
                galleryScene = "Gallery";
            if (string.IsNullOrEmpty(authenticationScene))
                authenticationScene = "Authentication";
        }

        #endregion
    }
}