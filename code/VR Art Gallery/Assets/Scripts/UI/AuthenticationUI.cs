using UnityEngine;
using TMPro;
using VRGallery.Authentication;

public class AuthenticationUI : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField username;
    public TMP_InputField password;

    private void Start()
    {
        AuthenticationManager.Instance.OnLoginSuccess += OnLoginSuccess;
    }

    private void OnDestroy()
    {
        if (AuthenticationManager.Instance != null)
            AuthenticationManager.Instance.OnLoginSuccess -= OnLoginSuccess;
    }

    public async void LoginClicked()
    {
        await AuthenticationManager.Instance.SignIn(
            username.text,
            password.text
        );
    }

    public async void SignUpClicked()
    {
        await AuthenticationManager.Instance.SignUp(
            username.text,
            password.text
        );
    }

    private async void OnLoginSuccess()
    {
        var backend = FindObjectOfType<BackendClient>();

        if (backend == null)
        {
            Debug.LogError("BackendClient not found in scene");
            return;
        }

        await backend.RegisterUserIfNeeded();

        Debug.Log("USER READY — LOGIN + BACKEND REGISTER COMPLETE");
    }
}
