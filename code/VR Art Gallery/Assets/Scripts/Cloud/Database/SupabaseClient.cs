using System;
using System.IO;
using System.Threading.Tasks;
using dotenv.net;
using Supabase;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

namespace VRGallery.Cloud
{
    /// <summary>
    /// Singleton for managing Supabase client initialization and access
    /// Ensures a single, initialized client instance is shared across the application
    /// </summary>
    public static class SupabaseClientManager
    {
        private static Client _instance;
        private static bool _isInitializing = false;
        private static TaskCompletionSource<bool> _initializationTask;

        public static Client Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[SupabaseClient] Client not initialized. Call InitializeAsync() first.");
                }
                return _instance;
            }
        }

        public static bool IsInitialized => _instance != null;

        public static Task InitializeAsync()
            => InitializeAsync("supabase.json");

        [Serializable]
        private class SupabaseConfigFile
        {
            public string url;
            public string anonKey;
        }

        /// <summary>
        /// Initialize the Supabase client from environment variables
        /// Safe to call multiple times - subsequent calls will await the first initialization
        /// </summary>
        public static async Task InitializeAsync(string configFileName)
        {
            // If already initialized, return immediately
            if (_instance != null)
            {
                return;
            }

            // If already initializing, await the ongoing initialization
            if (_isInitializing)
            {
                if (_initializationTask != null)
                {
                    await _initializationTask.Task;
                }
                return;
            }

            _isInitializing = true;
            _initializationTask = new TaskCompletionSource<bool>();

            try
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                var codeDir = Directory.GetParent(projectRoot)?.FullName;
                var repoRoot = Directory.GetParent(codeDir)?.FullName
                                  ?? Directory.GetCurrentDirectory();

                var envPath = Path.Combine(repoRoot, ".env");

                if (File.Exists(envPath))
                    DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { envPath }));
                else
                    Debug.Log($"[SupabaseClient] No .env file found at {envPath}, using system environment variables.");

                var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");

                // 2) Fallback to StreamingAssets/supabase.json for builds (Quest/Android)
                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
                    throw new InvalidOperationException("SUPABASE_URL and SUPABASE_KEY environment variables are not set");

                var options = new SupabaseOptions { AutoConnectRealtime = false };

                _instance = new Client(supabaseUrl.TrimEnd('/'), supabaseKey, options);
                await _instance.InitializeAsync();

                Debug.Log("[SupabaseClient] Supabase client initialized successfully");
                _initializationTask.SetResult(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupabaseClient] Failed to initialize Supabase client: {ex.Message}");
                _initializationTask.SetException(ex);
                throw;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private static async Task<SupabaseConfigFile> LoadConfigFromStreamingAssets(string configFileName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, configFileName);

#if UNITY_ANDROID && !UNITY_EDITOR
            using var req = UnityWebRequest.Get(path);
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"Failed to load {configFileName} from StreamingAssets: {req.error}");

            return JsonUtility.FromJson<SupabaseConfigFile>(req.downloadHandler.text);
#else
            if (!File.Exists(path))
                throw new FileNotFoundException($"Missing {configFileName} in StreamingAssets: {path}");

            var json = await File.ReadAllTextAsync(path);
            return JsonUtility.FromJson<SupabaseConfigFile>(json);
#endif
        }

        /// <summary>
        /// Get the initialized client, or null if not yet initialized
        /// </summary>
        public static Client GetClient()
        {
            return _instance;
        }
    }
}
