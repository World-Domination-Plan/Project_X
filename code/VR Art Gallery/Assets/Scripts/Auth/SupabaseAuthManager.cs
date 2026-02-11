using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseAuthManager : MonoBehaviour
{

    [Header("Supabase Project (loaded from StreamingAssets/supabase.json)")]
    [SerializeField] private string configFileName = "supabase.json";
    private string supabaseUrl;
    private string supabaseAnonKey;
    [SerializeField] private string artistsTable = "artists";

    [Header("Dev Test (optional)")]
    [SerializeField] private bool runDevTestOnStart = false;
    [SerializeField] private string testEmail = "test@example.com";
    [SerializeField] private string testPassword = "password123";
    [SerializeField] private string testUsername = "user12345";

    [Header("Session Restore (optional)")]
    [SerializeField] private bool restoreSessionOnStart = true;

    public SupabaseSession CurrentSession => _session;
    public bool IsSignedIn => _session != null && !string.IsNullOrEmpty(_session.access_token);

    private SupabaseSession _session;

    private const string PREF_SB_ACCESS = "SB_ACCESS_TOKEN";
    private const string PREF_SB_REFRESH = "SB_REFRESH_TOKEN";

    // ---------- DTOs for JsonUtility ----------
    [Serializable] private class EmailPasswordBody { public string email; public string password; }
    [Serializable] private class RefreshTokenBody { public string refresh_token; }

    [Serializable] public class SupabaseUser
    {
        public string id;    // UUID string
        public string email;
    }

    [Serializable] public class SupabaseSession
    {
        public string access_token;
        public string refresh_token;
        public int expires_in;
        public SupabaseUser user;
    }

    [Serializable]
    private class SupabaseConfigFile
    {
        public string url;
        public string anonKey;
    }

    private IEnumerator LoadSupabaseConfig()
    {
        // Build a URI that works for Editor + Android (Quest)
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, configFileName);
        string uri = path;

    #if !UNITY_ANDROID || UNITY_EDITOR
        // Editor/Standalone needs file://
        uri = "file://" + path;
    #endif

        using var req = UnityWebRequest.Get(uri);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[SupabaseAuthManager] Failed to load config at {uri}\n{req.error}");
            yield break;
        }

        var cfg = JsonUtility.FromJson<SupabaseConfigFile>(req.downloadHandler.text);
        if (cfg == null || string.IsNullOrEmpty(cfg.url) || string.IsNullOrEmpty(cfg.anonKey))
        {
            Debug.LogError("[SupabaseAuthManager] supabase.json is missing url/anonKey.");
            yield break;
        }

        supabaseUrl = cfg.url.TrimEnd('/');
        supabaseAnonKey = cfg.anonKey;

        Debug.Log("[SupabaseAuthManager] Supabase config loaded from StreamingAssets.");
    }


    private void Awake()
    {
        supabaseUrl = (supabaseUrl ?? "").TrimEnd('/');
    }

    private IEnumerator Start()
    {
        yield return LoadSupabaseConfig();

        // If config failed, stop early
        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseAnonKey))
            yield break;

        if (restoreSessionOnStart)
            yield return RestoreSession();

        if (runDevTestOnStart)
            yield return RegisterAndCreateProfile(testEmail, testPassword, testUsername);
    }


    // ===================== PUBLIC API =====================

    /// <summary>
    /// Create Supabase Auth account (email/password), then create/update artists row.
    /// </summary>
    public void Register(string email, string password, string username)
    {
        StartCoroutine(RegisterAndCreateProfile(email, password, username));
    }

    /// <summary>
    /// Login with Supabase Auth (email/password), then ensure artists row exists.
    /// </summary>
    public void Login(string email, string password)
    {
        StartCoroutine(LoginAndEnsureProfile(email, password));
    }

    /// <summary>
    /// Clear local tokens. (You can also call Supabase logout endpoint later if you want.)
    /// </summary>
    public void LogoutLocal()
    {
        _session = null;
        PlayerPrefs.DeleteKey(PREF_SB_ACCESS);
        PlayerPrefs.DeleteKey(PREF_SB_REFRESH);
        PlayerPrefs.Save();
        Debug.Log("[SupabaseAuthManager] Logged out locally (tokens cleared).");
    }

    /// <summary>
    /// Restore session using refresh token if present.
    /// </summary>
    public IEnumerator RestoreSession()
    {
        string refresh = PlayerPrefs.GetString(PREF_SB_REFRESH, "");
        if (string.IsNullOrEmpty(refresh))
            yield break;

        string endpoint = $"{supabaseUrl}/auth/v1/token?grant_type=refresh_token";
        string bodyJson = JsonUtility.ToJson(new RefreshTokenBody { refresh_token = refresh });

        yield return PostJson(
            endpoint,
            bodyJson,
            extraHeaders: null,
            onSuccess: json =>
            {
                var s = JsonUtility.FromJson<SupabaseSession>(json);
                if (s != null && !string.IsNullOrEmpty(s.access_token))
                {
                    SetSession(s);
                    Debug.Log($"[SupabaseAuthManager] Session restored: {s.user?.email} ({s.user?.id})");
                }
                else
                {
                    Debug.LogWarning("[SupabaseAuthManager] Refresh returned no access_token. Clearing tokens.");
                    LogoutLocal();
                }
            },
            onError: (code, body) =>
            {
                Debug.LogWarning($"[SupabaseAuthManager] Refresh failed HTTP {code}: {body}");
                LogoutLocal();
            }
        );
    }

    // ===================== FLOWS =====================

    private IEnumerator RegisterAndCreateProfile(string email, string password, string username)
    {
        // 1) SIGN UP -> creates Supabase Auth user account
        string endpoint = $"{supabaseUrl}/auth/v1/signup";
        string bodyJson = JsonUtility.ToJson(new EmailPasswordBody { email = email, password = password });

        SupabaseSession session = null;

        yield return PostJson(
            endpoint,
            bodyJson,
            extraHeaders: null,
            onSuccess: json =>
            {
                session = JsonUtility.FromJson<SupabaseSession>(json);

                // If email confirmations are ON, Supabase may create the user but NOT return a session.
                if (session == null || string.IsNullOrEmpty(session.access_token))
                {
                    Debug.LogWarning(
                        "[SupabaseAuthManager] SignUp succeeded but no session returned.\n" +
                        "If Email Confirmations are enabled, the user must confirm email first, then call Login().");
                }
            },
            onError: (code, body) =>
            {
                Debug.LogError($"[SupabaseAuthManager] SignUp failed HTTP {code}: {body}");
            }
        );

        if (session == null || string.IsNullOrEmpty(session.access_token))
            yield break;

        SetSession(session);

        // 2) Create/Upsert artists row (set username + initialise arrays)
        yield return UpsertArtistsRow(username, includeCreatedAt: true);
    }

    private IEnumerator LoginAndEnsureProfile(string email, string password)
    {
        // 1) SIGN IN -> returns session
        string endpoint = $"{supabaseUrl}/auth/v1/token?grant_type=password";
        string bodyJson = JsonUtility.ToJson(new EmailPasswordBody { email = email, password = password });

        SupabaseSession session = null;

        yield return PostJson(
            endpoint,
            bodyJson,
            extraHeaders: null,
            onSuccess: json =>
            {
                session = JsonUtility.FromJson<SupabaseSession>(json);
            },
            onError: (code, body) =>
            {
                Debug.LogError($"[SupabaseAuthManager] SignIn failed HTTP {code}: {body}");
            }
        );

        if (session == null || string.IsNullOrEmpty(session.access_token))
            yield break;

        SetSession(session);

        // 2) Ensure artists row exists (DON'T overwrite created_at on login)
        yield return UpsertArtistsRow(username: null, includeCreatedAt: false);
    }

    // ===================== DATABASE (artists table) =====================

    private IEnumerator UpsertArtistsRow(string username, bool includeCreatedAt)
    {
        if (!IsSignedIn || _session.user == null || string.IsNullOrEmpty(_session.user.id))
        {
            Debug.LogError("[SupabaseAuthManager] No valid session; cannot upsert artists row.");
            yield break;
        }

        // Your table columns (from screenshot):
        // user_id (text PK), created_at (timestamptz), managed_gallary (json[]), gallary_access (json[]), username (text)

        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append($"\"user_id\":\"{Escape(_session.user.id)}\"");

        if (includeCreatedAt)
        {
            // Only set this on first creation; avoid overwriting on later logins.
            string nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            sb.Append($",\"created_at\":\"{nowIso}\"");
        }

        // IMPORTANT: json[] must be real JSON arrays, not strings
        sb.Append(",\"managed_gallary\":[]");
        sb.Append(",\"gallary_access\":[]");

        if (!string.IsNullOrEmpty(username))
            sb.Append($",\"username\":\"{Escape(username)}\"");

        sb.Append("}");

        // UPSERT by primary key user_id
        string endpoint = $"{supabaseUrl}/rest/v1/{artistsTable}?on_conflict=user_id";

        var headers = new (string key, string value)[]
        {
            ("Authorization", $"Bearer {_session.access_token}"),
            ("Prefer", "resolution=merge-duplicates, return=representation")
        };

        yield return PostJson(
            endpoint,
            sb.ToString(),
            extraHeaders: headers,
            onSuccess: json =>
            {
                Debug.Log("[SupabaseAuthManager] Upsert artists OK.");
                Debug.Log($"[SupabaseAuthManager] Response: {json}");
            },
            onError: (code, body) =>
            {
                Debug.LogError($"[SupabaseAuthManager] Upsert artists failed HTTP {code}: {body}");
            }
        );
    }

    // ===================== HTTP HELPER =====================

    private IEnumerator PostJson(
        string endpoint,
        string bodyJson,
        (string key, string value)[] extraHeaders,
        Action<string> onSuccess,
        Action<long, string> onError)
    {
        using var req = new UnityWebRequest(endpoint, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson));
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("apikey", supabaseAnonKey);
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept", "application/json");

        if (extraHeaders != null)
        {
            foreach (var h in extraHeaders)
                req.SetRequestHeader(h.key, h.value);
        }

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            onSuccess?.Invoke(req.downloadHandler.text);
        else
            onError?.Invoke(req.responseCode, req.downloadHandler.text);
    }

    // ===================== SESSION STORAGE =====================

    private void SetSession(SupabaseSession s)
    {
        _session = s;
        PlayerPrefs.SetString(PREF_SB_ACCESS, s.access_token ?? "");
        PlayerPrefs.SetString(PREF_SB_REFRESH, s.refresh_token ?? "");
        PlayerPrefs.Save();
    }

    // ===================== JSON ESCAPE =====================

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
