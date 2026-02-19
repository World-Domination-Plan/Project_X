using System;
using System.IO;
using System.Threading.Tasks;
using dotenv.net;
// using DotNetEnv.Core; // Using DotNetEnv.Core for more flexible .env loading in Unity
using Supabase;
using UnityEngine;

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

        /// <summary>
        /// Initialize the Supabase client from environment variables
        /// Safe to call multiple times - subsequent calls will await the first initialization
        /// </summary>
        public static async Task InitializeAsync()
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

                if (!File.Exists(envPath))
                    Debug.LogWarning($"[SupabaseClient] .env not found at: {envPath}");

                DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { envPath }));

                var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
                    throw new InvalidOperationException("SUPABASE_URL and SUPABASE_KEY environment variables are not set");

                var options = new SupabaseOptions { AutoConnectRealtime = false };

                _instance = new Client(supabaseUrl, supabaseKey, options);
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

        /// <summary>
        /// Get the initialized client, or null if not yet initialized
        /// </summary>
        public static Client GetClient()
        {
            return _instance;
        }
    }
}
