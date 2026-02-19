using System;
using System.Threading.Tasks;
using Supabase.Gotrue;
using UnityEngine;
using VRGallery.Cloud;

// Supabase PostgREST model helpers (supabase-csharp uses Postgrest.*)
using Postgrest.Attributes;
using Postgrest.Models;

public class SupabaseAuthManager : MonoBehaviour
{
    // -------------------------
    // Dev Test (optional)
    // -------------------------
    [Header("Dev Test (optional)")]
    [SerializeField] private bool runDevTestOnStart = false;
    [SerializeField] private string testEmail = "test@example.com";
    [SerializeField] private string testPassword = "password123";
    [SerializeField] private string testUsername = "user12345";

    // -------------------------
    // Public API
    // -------------------------
    public bool IsReady { get; private set; }
    public Session CurrentSession => _session;
    public string CurrentUserId => _session?.User?.Id;

    // -------------------------
    // Internals
    // -------------------------
    private Session _session;
    private SupabaseArtistsRepository _artistsRepo;

    private async void Awake()
    {
        try
        {
            await InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SupabaseAuthManager] Initialize failed: {e}");
        }
    }

    /// <summary>
    /// Runs after Awake. Good place to do "session start" work.
    /// </summary>
    private async void Start()
    {
        try
        {
            if (!IsReady) await InitializeAsync();

            // Optional dev auto-login for play mode testing.
            if (runDevTestOnStart)
                await RunDevAuthFlowAsync();

            // If a session exists (auto-restore or dev login), send artist/session info now.
            await SendArtistInfoAtSessionStartAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SupabaseAuthManager] Start flow failed: {e}");
        }
    }

    public async Task InitializeAsync()
    {
        if (IsReady) return;

        if (!SupabaseClientManager.IsInitialized)
            await SupabaseClientManager.InitializeAsync();

        _artistsRepo = new SupabaseArtistsRepository(SupabaseClientManager.Instance);

        // Restore persisted session if available
        _session = SupabaseClientManager.Instance.Auth.CurrentSession;

        IsReady = true;
        Debug.Log("[SupabaseAuthManager] Ready.");
    }

    /// <summary>
    /// Dev flow: try sign-in; if it fails due to missing user, attempt sign-up.
    /// </summary>
    private async Task RunDevAuthFlowAsync()
    {
        if (string.IsNullOrWhiteSpace(testEmail) || string.IsNullOrWhiteSpace(testPassword))
        {
            Debug.LogWarning("[SupabaseAuthManager] Dev test is ON but email/password is empty.");
            return;
        }

        Debug.Log($"[SupabaseAuthManager] Dev test auth starting for {testEmail} ...");

        // 1) Try sign-in first
        var (okIn, errIn) = await SignInAsync(testEmail, testPassword);
        if (okIn)
        {
            // optional: set username (useful for fresh databases)
            if (!string.IsNullOrEmpty(testUsername))
                await _artistsRepo.SetUsernameAsync(_session.User.Id, testUsername);

            Debug.Log("[SupabaseAuthManager] Dev test sign-in OK.");
            return;
        }

        Debug.LogWarning($"[SupabaseAuthManager] Dev test sign-in failed: {errIn}. Trying sign-up...");

        // 2) If sign-in fails, try sign-up
        var (okUp, errUp) = await SignUpAsync(testEmail, testPassword, testUsername);
        if (!okUp)
        {
            Debug.LogError($"[SupabaseAuthManager] Dev test sign-up failed: {errUp}");
            return;
        }

        Debug.Log("[SupabaseAuthManager] Dev test sign-up OK.");
    }

    /// <summary>
    /// Ensure artist profile exists + record a "session start" row.
    /// Safe to call multiple times (it will just insert another session row).
    /// </summary>
    private async Task SendArtistInfoAtSessionStartAsync()
    {
        if (!IsReady) await InitializeAsync();

        // No authenticated session => nothing to send
        if (_session?.User == null)
        {
            Debug.Log("[SupabaseAuthManager] No session on start; skipping session-start upload.");
            return;
        }

        var userId = _session.User.Id;

        // 1) Ensure an artist profile exists for this user (your existing repo method)
        await _artistsRepo.EnsureProfileExistsAsync(userId);

        // 2) Insert session-start telemetry row (requires a table in Supabase)
        var row = new ArtistSessionRow
        {
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            Platform = Application.platform.ToString(),
            AppVersion = Application.version,
            DeviceId = SystemInfo.deviceUniqueIdentifier
        };

        try
        {
            await SupabaseClientManager.Instance
                .From<ArtistSessionRow>()
                .Insert(row);

            Debug.Log("[SupabaseAuthManager] Session-start row inserted.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SupabaseAuthManager] Session-start insert failed (table/RLS?): {e.Message}");
        }
    }

    public async Task<(bool ok, string error)> SignUpAsync(string email, string password, string username)
    {
        if (!IsReady) await InitializeAsync();

        try
        {
            var session = await SupabaseClientManager.Instance.Auth.SignUp(email, password);
            if (session == null || session.User == null)
                return (false, "SignUp returned no session/user. (Email confirm may be ON.)");

            _session = session;

            await _artistsRepo.EnsureProfileExistsAsync(session.User.Id);

            if (!string.IsNullOrEmpty(username))
                await _artistsRepo.SetUsernameAsync(session.User.Id, username);

            return (true, null);
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
    }

    public async Task<(bool ok, string error)> SignInAsync(string email, string password)
    {
        if (!IsReady) await InitializeAsync();

        try
        {
            var session = await SupabaseClientManager.Instance.Auth.SignIn(email, password);
            if (session == null || session.User == null)
                return (false, "Invalid credentials or provider disabled.");

            _session = session;

            await _artistsRepo.EnsureProfileExistsAsync(session.User.Id);
            return (true, null);
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
    }

    public async Task SignOutAsync()
    {
        if (!IsReady) await InitializeAsync();
        await SupabaseClientManager.Instance.Auth.SignOut();
        _session = null;
    }

    // ---------------------------------------
    // Supabase table model for session logging
    // ---------------------------------------
    [Table("artist_sessions")]
    private class ArtistSessionRow : BaseModel
    {
        // You can omit Id if your table auto-generates it; leaving it out is fine.
        // If your schema has an "id uuid primary key default gen_random_uuid()",
        // you don't need to send it from Unity.
        // [PrimaryKey("id", false)]
        // public Guid Id { get; set; }

        [Column("user_id")] public string UserId { get; set; }
        [Column("started_at")] public DateTime StartedAt { get; set; }
        [Column("platform")] public string Platform { get; set; }
        [Column("app_version")] public string AppVersion { get; set; }
        [Column("device_id")] public string DeviceId { get; set; }
    }
}
