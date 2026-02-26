using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace VRGallery.Cloud
{
    /// <summary>
    /// Wrapper around Supabase.Client that implements ISupabaseClient
    /// Allows for dependency injection and mocking in tests
    /// </summary>
    public class SupabaseClientWrapper : ISupabaseClient
    {
        private readonly Supabase.Client client;

        // Map interface handlers -> Supabase handlers so Remove works
        private readonly Dictionary<
            EventHandler<Supabase.Gotrue.Constants.AuthState>,
            IGotrueClient<User, Session>.AuthEventHandler
        > _authHandlerMap = new();

        public bool IsAuthenticated => client?.Auth?.CurrentUser != null;

        public string CurrentUserId => client?.Auth?.CurrentUser?.Id ?? "";

        public string CurrentUserEmail => client?.Auth?.CurrentUser?.Email ?? "";

        public User CurrentUser => client?.Auth?.CurrentUser;

        /// <summary>
        /// Wraps an existing Supabase Client
        /// </summary>
        public SupabaseClientWrapper(Supabase.Client client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<Session> SignUpAsync(string email, string password)
        {
            return await client.Auth.SignUp(email, password);
        }

        public async Task<Session> SignInAsync(string email, string password)
        {
            return await client.Auth.SignIn(email, password);
        }

        public async Task SignOutAsync()
        {
            await client.Auth.SignOut();
        }

        public async Task<Session> RefreshSessionAsync()
        {
            return await client.Auth.RefreshSession();
        }

        public void AddAuthStateChangedListener(EventHandler<Supabase.Gotrue.Constants.AuthState> handler)
        {
            if (handler == null) return;

            // Don’t double-add the same handler
            if (_authHandlerMap.ContainsKey(handler))
                return;

            IGotrueClient<User, Session>.AuthEventHandler supabaseHandler =
                (sender, state) =>
                {
                    // Your interface expects (object sender, AuthState state)
                    handler.Invoke(this, state);
                };

            _authHandlerMap[handler] = supabaseHandler;
            client.Auth.AddStateChangedListener(supabaseHandler);
        }

        public void RemoveAuthStateChangedListener(EventHandler<Supabase.Gotrue.Constants.AuthState> handler)
        {
            if (handler == null) return;

            if (_authHandlerMap.TryGetValue(handler, out var supabaseHandler))
            {
                client.Auth.RemoveStateChangedListener(supabaseHandler);
                _authHandlerMap.Remove(handler);
            }
        }

        // Expose the underlying client for repository use
        public Supabase.Client GetClient() => client;
    }
}
