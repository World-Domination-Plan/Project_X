using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using VRGallery.Cloud;

[DisallowMultipleComponent]
public class NgoArtworkJoinGate : MonoBehaviour
{
    [Serializable]
    public class JoinBlockedEvent : UnityEvent<string> { }

    [SerializeField] private int artworkId;
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private bool startClientWhenAuthorized = true;
    [SerializeField] private UnityEvent onJoinAuthorized;
    [SerializeField] private JoinBlockedEvent onJoinBlocked;

    private SupabaseArtworkAccessService accessService;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;
    }

    public async void TryJoinArtworkSession()
    {
        await TryJoinArtworkSessionAsync(artworkId);
    }

    public async Task<bool> TryJoinArtworkSessionAsync(int targetArtworkId)
    {
        if (targetArtworkId <= 0)
            return BlockJoin("Invalid artwork id.");

        var supabase = await CreateSupabaseClientAsync();
        if (supabase == null)
            return BlockJoin("Unable to connect to Supabase.");

        accessService ??= new SupabaseArtworkAccessService(supabase);

        var canJoin = await accessService.CanCurrentUserJoinArtworkAsync(targetArtworkId);
        if (!canJoin)
            return BlockJoin("You do not have access to this artwork session.");

        // AFTER
        onJoinAuthorized?.Invoke();

        if (startClientWhenAuthorized && networkManager != null)
        {
            if (networkManager.IsListening)
            {
                // Already connected — don't start again, stale relay would fire here
                Debug.LogWarning("[NgoArtworkJoinGate] NetworkManager already listening, skipping StartClient.");
            }
            else
            {
                // Relay data MUST be set before this point by your RelayManager
                // If not set, this is what causes the allocation ID not found error
                networkManager.StartClient();
            }
        }

        return true;
    }

    private bool BlockJoin(string reason)
    {
        onJoinBlocked?.Invoke(reason);
        Debug.LogWarning($"[NgoArtworkJoinGate] Join blocked: {reason}");
        return false;
    }

    private static async Task<ISupabaseClient> CreateSupabaseClientAsync()
    {
        try
        {
            if (!SupabaseClientManager.IsInitialized)
                await SupabaseClientManager.InitializeAsync();

            if (SupabaseClientManager.Instance == null)
                return null;

            return new SupabaseClientWrapper(SupabaseClientManager.Instance);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NgoArtworkJoinGate] Supabase initialization failed: {ex.Message}");
            return null;
        }
    }

    private void OnDisable()
    {
        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
            Debug.Log("[NgoArtworkJoinGate] Shutdown NetworkManager on disable.");
        }
    }
}
