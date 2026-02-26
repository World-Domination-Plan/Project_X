using System;
using System.Threading.Tasks;
using Supabase.Gotrue;

namespace VRGallery.Cloud
{
    /// <summary>
    /// Interface for Supabase client to enable mocking in tests
    /// </summary>
    public interface ISupabaseClient
    {
        bool IsAuthenticated { get; }
        string CurrentUserId { get; }
        string CurrentUserEmail { get; }
        User CurrentUser { get; }

        Task<Session> SignUpAsync(string email, string password);
        Task<Session> SignInAsync(string email, string password);
        Task SignOutAsync();
        Task<Session> RefreshSessionAsync();

        void AddAuthStateChangedListener(System.EventHandler<Supabase.Gotrue.Constants.AuthState> handler);
        void RemoveAuthStateChangedListener(System.EventHandler<Supabase.Gotrue.Constants.AuthState> handler);

        Supabase.Client GetClient();
    }
}