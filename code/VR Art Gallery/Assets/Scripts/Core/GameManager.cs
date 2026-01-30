using UnityEngine;
using VRGallery.Authentication;

namespace VRGallery.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public bool IsUserAuthenticated =>
            AuthenticationManager.Instance != null &&
            AuthenticationManager.Instance.IsAuthenticated;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            var auth = AuthenticationManager.Instance;

            auth.OnLoginSuccess += HandleLogin;
            auth.OnLogout += HandleLogout;
        }

        private void HandleLogin()
        {
            Debug.Log("[GameManager] User logged in");
        }

        private void HandleLogout()
        {
            Debug.Log("[GameManager] User logged out");
        }
    }
}
