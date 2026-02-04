using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRGallery.Authentication;
using System.Threading.Tasks;

namespace VRGallery.UI
{
    /// <summary>
    /// UI controller for authentication (login/registration) interface
    /// </summary>
    public class AuthenticationUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject authenticatedPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Login UI")]
        [SerializeField] private TMP_InputField loginEmailField;
        [SerializeField] private TMP_InputField loginPasswordField;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button showRegisterButton;

        [Header("Register UI")]
        [SerializeField] private TMP_InputField registerEmailField;
        [SerializeField] private TMP_InputField registerPasswordField;
        [SerializeField] private TMP_InputField confirmPasswordField;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button showLoginButton;

        [Header("Authenticated UI")]
        [SerializeField] private TextMeshProUGUI welcomeText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private Button logoutButton;

        [Header("Error Display")]
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private Button closeErrorButton;

        [Header("Settings")]
        [SerializeField] private bool hideUIWhenAuthenticated = true;
        [SerializeField] private float errorDisplayDuration = 5f;

        private AuthenticationManager authManager;

        private void Awake()
        {
            SetupButtonListeners();
            ShowLoadingPanel();
        }

        private void Start()
        {
            authManager = AuthenticationManager.Instance;
            if (authManager == null)
            {
                ShowError("Authentication system not available");
                return;
            }

            // Subscribe to authentication events
            authManager.OnUserLoggedIn += HandleUserLoggedIn;
            authManager.OnUserLoggedOut += HandleUserLoggedOut;
            authManager.OnUserRoleChanged += HandleUserRoleChanged;
            authManager.OnAuthenticationError += HandleAuthenticationError;

            // Check if user is already authenticated
            if (authManager.IsAuthenticated)
            {
                HandleUserLoggedIn(authManager.CurrentUser);
            }
            else
            {
                ShowLoginPanel();
            }
        }

        private void OnDestroy()
        {
            if (authManager != null)
            {
                authManager.OnUserLoggedIn -= HandleUserLoggedIn;
                authManager.OnUserLoggedOut -= HandleUserLoggedOut;
                authManager.OnUserRoleChanged -= HandleUserRoleChanged;
                authManager.OnAuthenticationError -= HandleAuthenticationError;
            }
        }

        #region Button Setup

        private void SetupButtonListeners()
        {
            if (loginButton) loginButton.onClick.AddListener(OnLoginClick);
            if (registerButton) registerButton.onClick.AddListener(OnRegisterClick);
            if (logoutButton) logoutButton.onClick.AddListener(OnLogoutClick);
            if (showRegisterButton) showRegisterButton.onClick.AddListener(ShowRegisterPanel);
            if (showLoginButton) showLoginButton.onClick.AddListener(ShowLoginPanel);
            if (closeErrorButton) closeErrorButton.onClick.AddListener(HideError);
        }

        #endregion

        #region UI State Management

        private void ShowPanel(GameObject panel)
        {
            HideAllPanels();
            if (panel) panel.SetActive(true);
        }

        private void HideAllPanels()
        {
            if (loginPanel) loginPanel.SetActive(false);
            if (registerPanel) registerPanel.SetActive(false);
            if (authenticatedPanel) authenticatedPanel.SetActive(false);
            if (loadingPanel) loadingPanel.SetActive(false);
            if (errorPanel) errorPanel.SetActive(false);
        }

        private void ShowLoginPanel()
        {
            ShowPanel(loginPanel);
            ClearInputFields();
        }

        private void ShowRegisterPanel()
        {
            ShowPanel(registerPanel);
            ClearInputFields();
        }

        private void ShowAuthenticatedPanel()
        {
            if (hideUIWhenAuthenticated)
            {
                HideAllPanels();
            }
            else
            {
                ShowPanel(authenticatedPanel);
            }
        }

        private void ShowLoadingPanel()
        {
            ShowPanel(loadingPanel);
        }

        private void ClearInputFields()
        {
            if (loginEmailField) loginEmailField.text = "";
            if (loginPasswordField) loginPasswordField.text = "";
            if (registerEmailField) registerEmailField.text = "";
            if (registerPasswordField) registerPasswordField.text = "";
            if (confirmPasswordField) confirmPasswordField.text = "";
        }

        #endregion

        #region Button Handlers

        private async void OnLoginClick()
        {
            if (!ValidateLoginInput()) return;

            ShowLoadingPanel();
            SetButtonsInteractable(false);

            string email = loginEmailField.text.Trim();
            string password = loginPasswordField.text;

            bool success = await authManager.LoginUser(email, password);

            SetButtonsInteractable(true);

            if (!success)
            {
                ShowLoginPanel();
            }
        }

        private async void OnRegisterClick()
        {
            if (!ValidateRegisterInput()) return;

            ShowLoadingPanel();
            SetButtonsInteractable(false);

            string email = registerEmailField.text.Trim();
            string password = registerPasswordField.text;

            bool success = await authManager.RegisterUser(email, password);

            SetButtonsInteractable(true);

            if (success)
            {
                ShowError("Registration successful! Please check your email for verification.", false);
                ShowLoginPanel();
            }
            else
            {
                ShowRegisterPanel();
            }
        }

        private async void OnLogoutClick()
        {
            ShowLoadingPanel();
            await authManager.LogoutUser();
        }

        #endregion

        #region Input Validation

        private bool ValidateLoginInput()
        {
            if (string.IsNullOrWhiteSpace(loginEmailField.text))
            {
                ShowError("Please enter your email address");
                return false;
            }

            if (string.IsNullOrWhiteSpace(loginPasswordField.text))
            {
                ShowError("Please enter your password");
                return false;
            }

            return true;
        }

        private bool ValidateRegisterInput()
        {
            string email = registerEmailField.text.Trim();
            string password = registerPasswordField.text;
            string confirmPassword = confirmPasswordField.text;

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Please enter your email address");
                return false;
            }

            if (!email.Contains("@"))
            {
                ShowError("Please enter a valid email address");
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter a password");
                return false;
            }

            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters long");
                return false;
            }

            if (password != confirmPassword)
            {
                ShowError("Passwords do not match");
                return false;
            }

            return true;
        }

        #endregion

        #region Authentication Event Handlers

        private async void HandleUserLoggedIn(Supabase.Gotrue.User user)
        {
            if (welcomeText)
            {
                welcomeText.text = $"Welcome, {user.Email}!";
            }

            // Get and display user role
            var role = await authManager.GetCurrentUserRole();
            HandleUserRoleChanged(role);

            ShowAuthenticatedPanel();
        }

        private void HandleUserLoggedOut()
        {
            ShowLoginPanel();
        }

        private void HandleUserRoleChanged(UserRole role)
        {
            if (roleText)
            {
                roleText.text = $"Role: {role.ToDisplayString()}";
            }
        }

        private void HandleAuthenticationError(string errorMessage)
        {
            ShowError(errorMessage);
        }

        #endregion

        #region Error Handling

        private void ShowError(string message, bool isError = true)
        {
            if (errorText) errorText.text = message;
            if (errorPanel)
            {
                errorPanel.SetActive(true);
                // Auto-hide after duration
                if (isError)
                {
                    Invoke(nameof(HideError), errorDisplayDuration);
                }
            }

            if (isError)
            {
                Debug.LogError($"[AuthUI] {message}");
            }
            else
            {
                Debug.Log($"[AuthUI] {message}");
            }
        }

        private void HideError()
        {
            if (errorPanel) errorPanel.SetActive(false);
        }

        #endregion

        #region Utility

        private void SetButtonsInteractable(bool interactable)
        {
            if (loginButton) loginButton.interactable = interactable;
            if (registerButton) registerButton.interactable = interactable;
            if (logoutButton) logoutButton.interactable = interactable;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Show the authentication UI (call this to manually show login screen)
        /// </summary>
        public void ShowAuthenticationUI()
        {
            if (authManager != null && authManager.IsAuthenticated)
            {
                ShowAuthenticatedPanel();
            }
            else
            {
                ShowLoginPanel();
            }
        }

        /// <summary>
        /// Hide the authentication UI
        /// </summary>
        public void HideAuthenticationUI()
        {
            HideAllPanels();
        }

        /// <summary>
        /// Force logout and show login screen
        /// </summary>
        public async void ForceLogout()
        {
            if (authManager != null)
            {
                await authManager.LogoutUser();
            }
        }

        #endregion
    }
}