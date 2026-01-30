using UnityEngine;
using TMPro;
using VRGallery.Authentication;
using VRGallery.Core;

namespace VRGallery.UI
{
    public class UserStatusDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statusText;

        private void Start()
        {
            var auth = AuthenticationManager.Instance;

            auth.OnLoginSuccess += UpdateStatus;
            auth.OnLogout += UpdateStatus;

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (AuthenticationManager.Instance.IsAuthenticated)
                statusText.text = "Logged In";
            else
                statusText.text = "Guest";
        }
    }
}
