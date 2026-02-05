using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Threading.Tasks;

public class UnityAuth
{
    public string PlayerId { get; private set; }

    public async Task SignIn()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        PlayerId = AuthenticationService.Instance.PlayerId;
        Debug.Log($"[Auth] PlayerId = {PlayerId}");
	
    }

#if UNITY_EDITOR
    // Used ONLY for tests
    public void SetPlayerIdForTest(string id)
    {
        PlayerId = id;
    }
#endif
}
