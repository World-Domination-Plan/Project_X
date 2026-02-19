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
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize Supabase: {ex.Message}");
                OnAuthenticationError?.Invoke($"Initialization failed: {ex.Message}");
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

                // Validate inputs
                if (string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(username))
                {
                    OnAuthenticationError?.Invoke("Email, password, and username are required");
                    return false;
                }

                // Sign up user with Supabase Auth
                var response = await SupabaseClientInstance.Auth.SignUp(email, password);

                if (response?.User != null)
                {
                    LogDebug($"User registered successfully: {response.User.Id}");

                    // Create artist profile with username
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
                LogError($"Registration error: {ex.Message}");
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

                var response = await SupabaseClientInstance.Auth.SignIn(email, password);

                if (response?.User != null)
                {
                    CurrentSession = response;
                    OnUserLoggedIn?.Invoke(response.User);
                    LogDebug($"User logged in successfully: {response.User.Id}");

                    // Get and notify of user role
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
                LogError($"Login error: {ex.Message}");
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
                await SupabaseClientInstance.Auth.SignOut();
                CurrentSession = null;
                OnUserLoggedOut?.Invoke();
                LogDebug("User logged out successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Logout error: {ex.Message}");
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
                LogDebug($"Getting role for user: {userId}");

                var profile = await artistRepository.GetArtistProfileAsync(userId);

                if (profile == null)
                {
                    LogDebug($"No profile found for user {userId}, returning Guest role");
                    return UserRole.Guest;
                }

                // Determine role based on profile data
                // Check if user is an admin (you may need to add an is_admin field to your profile)
                // For now, users with profiles are Artists, others are Guests
                UserRole role = UserRole.Artist;

                LogDebug($"Retrieved role {role} for user: {userId}");
                return role;
            }
            catch (Exception ex)
            {
                LogError($"Error getting role for user {userId}: {ex.Message}");
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
                LogError($"Error getting username for user {userId}: {ex.Message}");
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