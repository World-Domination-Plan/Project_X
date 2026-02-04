using UnityEngine;
using TMPro;
using VRGallery.Authentication;

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
        [SerializeField] private GameObject doorObject;

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
                doorObject.SetActive(true);
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
            if (doorObject) doorObject.SetActive(false);
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

        await backend.RegisterUserIfNeeded();

        Debug.Log("USER READY — LOGIN + BACKEND REGISTER COMPLETE");
    }
}
