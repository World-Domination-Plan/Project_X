using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Threading.Tasks;

namespace VRGallery.Authentication
{
    public class AuthenticationManager : MonoBehaviour
    {
        public static AuthenticationManager Instance { get; private set; }

        public string PlayerId { get; private set; }
        public string AccessToken { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(PlayerId);

        public event Action OnLoginSuccess;
        public event Action OnLogout;

        private async void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
        }

        public async Task SignIn(string email, string password)
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Already signed in — skipping SignIn");
                CacheSession();
                OnLoginSuccess?.Invoke();
                return;
            }

            await AuthenticationService.Instance
                .SignInWithUsernamePasswordAsync(email, password);

            CacheSession();
            OnLoginSuccess?.Invoke();
        }

        public async Task SignUp(string email, string password)
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Already signed in — skipping SignUp");
                CacheSession();
                OnLoginSuccess?.Invoke();
                return;
            }

            await AuthenticationService.Instance
                .SignUpWithUsernamePasswordAsync(email, password);

            CacheSession();
            OnLoginSuccess?.Invoke();
        }

        public void Logout()
        {
            AuthenticationService.Instance.SignOut();
            PlayerId = null;
            AccessToken = null;
            OnLogout?.Invoke();
        }

        private void CacheSession()
        {
            PlayerId = AuthenticationService.Instance.PlayerId;
            AccessToken = AuthenticationService.Instance.AccessToken;
        }
    }
}
