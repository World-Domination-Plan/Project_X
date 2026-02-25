using System;
using System.Threading.Tasks;
using UnityEngine;

namespace VRGallery.Cloud
{
    public class SupabaseAuthenticationService : IAuthenticationService
    {
        private readonly ISupabaseClient supabaseClient;
        private readonly IArtistRepository artistRepository;

        public bool IsAuthenticated => supabaseClient?.IsAuthenticated ?? false;

        public string CurrentUserId => supabaseClient?.CurrentUserId ?? "";

        public string CurrentUserEmail => supabaseClient?.CurrentUserEmail ?? "";

        public event Action OnLoginSuccess;
        public event Action OnLogoutSuccess;
        public event Action<string> OnAuthError;

        public SupabaseAuthenticationService(ISupabaseClient client, IArtistRepository artistRepository)
        {
            this.supabaseClient = client ?? throw new ArgumentNullException(nameof(client));
            this.artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));

            // Subscribe to auth state changes
            supabaseClient.AddAuthStateChangedListener(OnAuthStateChanged);
        }

        private void OnAuthStateChanged(object sender, Supabase.Gotrue.Constants.AuthState state)
        {
            Debug.Log($"[AuthService] Auth state changed: {state}");

            switch (state)
            {
                case Supabase.Gotrue.Constants.AuthState.SignedIn:
                    OnLoginSuccess?.Invoke();
                    break;

                case Supabase.Gotrue.Constants.AuthState.SignedOut:
                    OnLogoutSuccess?.Invoke();
                    break;
            }
        }

        public async Task<bool> SignUpAsync(string email, string password, string username)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(username))
            {
                OnAuthError?.Invoke("Email, password, and username are required");
                return false;
            }

            try
            {
                var session = await supabaseClient.SignUpAsync(email, password);

                if (session?.User != null)
                {
                    Debug.Log($"[AuthService] User signed up: {session.User.Email}");

                    // Create artist profile
                    bool profileCreated = await artistRepository.CreateArtistProfileAsync(
                        session.User.Id,
                        username
                    );

                    if (!profileCreated)
                    {
                        Debug.LogWarning("[AuthService] User created but profile creation failed");
                    }

                    return true;
                }

                OnAuthError?.Invoke("Sign up failed");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthService] Sign up error: {ex.Message}");
                OnAuthError?.Invoke(ex.Message);
                return false;
            }
        }

        public async Task<bool> SignInAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                OnAuthError?.Invoke("Email and password are required");
                return false;
            }

            try
            {
                var session = await supabaseClient.SignInAsync(email, password);

                if (session?.User != null)
                {
                    Debug.Log($"[AuthService] User signed in: {session.User.Email}");
                    return true;
                }

                OnAuthError?.Invoke("Sign in failed");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthService] Sign in error: {ex.Message}");
                OnAuthError?.Invoke(ex.Message);
                return false;
            }
        }

        public async Task<bool> SignOutAsync()
        {
            try
            {
                await supabaseClient.SignOutAsync();
                Debug.Log("[AuthService] User signed out");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthService] Sign out error: {ex.Message}");
                OnAuthError?.Invoke(ex.Message);
                return false;
            }
        }

        public async Task<bool> RefreshSessionAsync()
        {
            try
            {
                var session = await supabaseClient.RefreshSessionAsync();

                if (session?.User != null)
                {
                    Debug.Log("[AuthService] Session refreshed");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthService] Refresh error: {ex.Message}");
                OnAuthError?.Invoke(ex.Message);
                return false;
            }
        }
    }
}