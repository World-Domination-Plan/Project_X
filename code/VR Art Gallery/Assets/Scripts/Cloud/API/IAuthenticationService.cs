using System;
using System.Threading.Tasks;

namespace VRGallery.Cloud
{
    public interface IAuthenticationService
    {
        bool IsAuthenticated { get; }
        string CurrentUserId { get; }
        string CurrentUserEmail { get; }
        
        event Action OnLoginSuccess;
        event Action OnLogoutSuccess;
        event Action<string> OnAuthError;
        
        Task<bool> SignUpAsync(string email, string password, string username);
        Task<bool> SignInAsync(string email, string password);
        Task<bool> SignOutAsync();
        Task<bool> RefreshSessionAsync();
    }
}