using UnityEngine;
using Supabase;
using Supabase.Gotrue;
using System.Threading.Tasks;
using System;
using VRGallery.Cloud;

namespace VRGallery.Authentication
{
    /// <summary>
    /// Main authentication manager for the VR Art Gallery Unity project
    /// Handles user registration, login, and role management with Supabase
    /// </summary>
    public class AuthenticationManager : MonoBehaviour
    {

        // -------------------------
        // Dev Test (optional)
        // -------------------------
        [Header("Dev Test (optional)")]
        [SerializeField] private bool runDevTestOnStart = false;
        [SerializeField] private string testEmail = "test@example.com";
        [SerializeField] private string testPassword = "password123";
        [SerializeField] private string testUsername = "user12345";

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // Singleton pattern
        public static AuthenticationManager Instance { get; private set; }

        // Current user session
        public Session CurrentSession { get; private set; }
        public Supabase.Client SupabaseClientInstance { get; private set; }
        public User CurrentUser => CurrentSession?.User;
        public bool IsAuthenticated => CurrentUser != null;

        // Events for UI updates
        public event Action<User> OnUserLoggedIn;
        public event Action OnUserLoggedOut;
        public event Action<UserRole> OnUserRoleChanged;
        public event Action<string> OnAuthenticationError;

        // Artist repository for profile management
        private IArtistRepository artistRepository;

        private bool _supabaseInitialized = false;
        private bool _supabaseInitFailed = false;
        private bool _devTestRan = false;


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSupabase();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private async void Start()
        {
            // Wait until InitializeSupabase() finishes (or fails)
            await WaitForSupabaseInitAsync();

            if (_supabaseInitFailed)
            {
                LogError("Supabase init failed; skipping Dev Test.");
                return;
            }

            if (runDevTestOnStart && !_devTestRan)
            {
                _devTestRan = true;
                await RunDevAuthFlowAsync();
            }
        }

        private async Task WaitForSupabaseInitAsync(int timeoutMs = 15000)
        {
            var start = DateTime.UtcNow;

            while (!_supabaseInitialized && !_supabaseInitFailed)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                {
                    _supabaseInitFailed = true;
                    LogError("Timeout waiting for Supabase initialization.");
                    break;
                }

                await Task.Delay(50);
            }
        }

        private async Task RunDevAuthFlowAsync()
        {
            if (SupabaseClientInstance == null)
            {
                LogError("Dev Test aborted: SupabaseClientInstance is null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(testEmail) || string.IsNullOrWhiteSpace(testPassword))
            {
                LogError("Dev Test aborted: testEmail/testPassword is empty.");
                return;
            }

            LogDebug($"[DevTest] Auth starting for {testEmail} ...");

            // 1) Try login
            var okLogin = await LoginUser(testEmail, testPassword);
            if (okLogin)
            {
                LogDebug("[DevTest] Login OK.");
                return;
            }

            LogDebug("[DevTest] Login failed. Trying Register...");

            // 2) Try register
            var okRegister = await RegisterUser(testEmail, testPassword, testUsername);
            if (!okRegister)
            {
                LogError("[DevTest] Register failed.");
                return;
            }

            LogDebug("[DevTest] Register OK. Trying Login again...");

            // 3) Try login again (in case SignUp doesn’t auto-create a usable session)
            okLogin = await LoginUser(testEmail, testPassword);
            if (!okLogin)
            {
                LogError("[DevTest] Login after Register failed. (Email confirmation might be ON.)");
            }
            else
            {
                LogDebug("[DevTest] Login after Register OK.");
            }
        }



        private async void InitializeSupabase()
        {
            try
            {
                if (SupabaseClientManager.Instance == null)
                {
                    await SupabaseClientManager.InitializeAsync();
                }
                SupabaseClientInstance = SupabaseClientManager.Instance;

                // Initialize artist repository with the correct implementation
                var clientWrapper = new SupabaseClientWrapper(SupabaseClientInstance);
                artistRepository = new SupabaseArtistRepository(clientWrapper);

                // Check for existing session
                var session = SupabaseClientInstance.Auth.CurrentSession;
                if (session?.User != null)
                {
                    CurrentSession = session;
                    OnUserLoggedIn?.Invoke(session.User);
                    LogDebug($"Restored session for user: {session.User.Email}");
                }

                LogDebug("Supabase authentication initialized successfully");
                _supabaseInitialized = true;

            }
            catch (Exception ex)
            {
                //LogError($"Failed to initialize Supabase: {ex.Message}");
                LogError($"Failed to initialize Supabase: {ex}");

                OnAuthenticationError?.Invoke($"Initialization failed: {ex.Message}");

                _supabaseInitFailed = true;
            }
        }

        #region Authentication Methods

        /// <summary>
        /// Register a new user with email, password, and username
        /// </summary>
        public async Task<bool> RegisterUser(string email, string password, string username)
        {
            try
            {
                LogDebug($"Attempting to register user: {email}");

                await WaitForSupabaseInitAsync();

                if (_supabaseInitFailed)
                {
                    OnAuthenticationError?.Invoke("Supabase initialization failed");
                    return false;
                }

                if (SupabaseClientInstance == null)
                {
                    LogError("Register failed: SupabaseClientInstance is null");
                    OnAuthenticationError?.Invoke("Authentication system not ready");
                    return false;
                }

                if (SupabaseClientInstance.Auth == null)
                {
                    LogError("Register failed: SupabaseClientInstance.Auth is null");
                    OnAuthenticationError?.Invoke("Authentication service is unavailable");
                    return false;
                }

                if (artistRepository == null)
                {
                    LogError("Register failed: artistRepository is null");
                    OnAuthenticationError?.Invoke("Profile service is unavailable");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(username))
                {
                    OnAuthenticationError?.Invoke("Email, password, and username are required");
                    return false;
                }

                var response = await SupabaseClientInstance.Auth.SignUp(email, password);

                if (response?.User != null)
                {
                    LogDebug($"User registered successfully: {response.User.Id}");

                    bool profileCreated = await artistRepository.CreateArtistProfileAsync(
                        response.User.Id,
                        username
                    );

                    if (!profileCreated)
                    {
                        LogError("Failed to create artist profile");
                        OnAuthenticationError?.Invoke("Registration succeeded but profile creation failed");
                    }

                    return true;
                }
                else
                {
                    LogError("User registration failed - no user returned");
                    OnAuthenticationError?.Invoke("Registration failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Registration error: {ex}");
                OnAuthenticationError?.Invoke($"Registration failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Login user with email and password
        /// </summary>
        public async Task<bool> LoginUser(string email, string password)
        {
            try
            {
                LogDebug($"Attempting to login user: {email}");

                await WaitForSupabaseInitAsync();

                if (_supabaseInitFailed)
                {
                    OnAuthenticationError?.Invoke("Supabase initialization failed");
                    return false;
                }

                if (SupabaseClientInstance == null)
                {
                    LogError("Login failed: SupabaseClientInstance is null");
                    OnAuthenticationError?.Invoke("Authentication system not ready");
                    return false;
                }

                if (SupabaseClientInstance.Auth == null)
                {
                    LogError("Login failed: SupabaseClientInstance.Auth is null");
                    OnAuthenticationError?.Invoke("Authentication service is unavailable");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    OnAuthenticationError?.Invoke("Email and password are required");
                    return false;
                }

                var response = await SupabaseClientInstance.Auth.SignIn(email, password);

                if (response?.User != null)
                {
                    CurrentSession = response;
                    OnUserLoggedIn?.Invoke(response.User);
                    LogDebug($"User logged in successfully: {response.User.Id}");

                    var role = await GetUserRole(response.User.Id);
                    OnUserRoleChanged?.Invoke(role);

                    return true;
                }
                else
                {
                    LogError("Login failed - no user returned");
                    OnAuthenticationError?.Invoke("Login failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Login error: {ex}");
                OnAuthenticationError?.Invoke($"Login failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        public async Task<bool> LogoutUser()
        {
            try
            {
                await WaitForSupabaseInitAsync();

                if (_supabaseInitFailed || SupabaseClientInstance == null || SupabaseClientInstance.Auth == null)
                {
                    OnAuthenticationError?.Invoke("Authentication system not ready");
                    return false;
                }

                await SupabaseClientInstance.Auth.SignOut();
                CurrentSession = null;
                OnUserLoggedOut?.Invoke();
                LogDebug("User logged out successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Logout error: {ex}");
                OnAuthenticationError?.Invoke($"Logout failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Role Management

        /// <summary>
        /// Get the role of a specific user based on their artist profile
        /// </summary>
        public async Task<UserRole> GetUserRole(string userId)
        {
            try
            {
                if (artistRepository == null)
                {
                    LogError("GetUserRole failed: artistRepository is null");
                    return UserRole.Guest;
                }

                LogDebug($"Getting role for user: {userId}");

                var profile = await artistRepository.GetArtistProfileAsync(userId);

                if (profile == null)
                {
                    LogDebug($"No profile found for user {userId}, returning Guest role");
                    return UserRole.Guest;
                }

                UserRole role = UserRole.Artist;

                LogDebug($"Retrieved role {role} for user: {userId}");
                return role;
            }
            catch (Exception ex)
            {
                LogError($"Error getting role for user {userId}: {ex}");
                return UserRole.Guest;
            }
        }

        /// <summary>
        /// Get the current user's username from their profile
        /// </summary>
        public async Task<string> GetUsername(string userId)
        {
            try
            {
                var profile = await artistRepository.GetArtistProfileAsync(userId);
                return profile?.username ?? "User";
            }
            catch (Exception ex)
            {
                //LogError($"Error getting username for user {userId}: {ex.Message}");
                LogError($"Error getting username for user {userId}: {ex}");
                return "User";
            }
        }

        /// <summary>
        /// Get the current user's role
        /// </summary>
        public async Task<UserRole> GetCurrentUserRole()
        {
            if (CurrentUser == null)
                return UserRole.Guest;

            return await GetUserRole(CurrentUser.Id);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if current user has a specific role or higher
        /// </summary>
        public async Task<bool> HasRole(UserRole requiredRole)
        {
            var currentRole = await GetCurrentUserRole();
            return (int)currentRole >= (int)requiredRole;
        }

        /// <summary>
        /// Get user display name or email
        /// </summary>
        public async Task<string> GetUserDisplayName()
        {
            if (CurrentUser == null)
                return "Guest";

            // Try to get username from profile
            string username = await GetUsername(CurrentUser.Id);
            if (!string.IsNullOrEmpty(username) && username != "User")
            {
                return username;
            }

            // Fallback to email
            return CurrentUser.Email ?? "User";
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[AuthManager] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[AuthManager] {message}");
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion
    }
}
