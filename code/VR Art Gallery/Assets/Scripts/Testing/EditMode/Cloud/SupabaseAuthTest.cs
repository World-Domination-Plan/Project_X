using NUnit.Framework;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;
using VRGallery.Cloud;
using System;
using Supabase.Gotrue;

namespace VRGallery.Cloud.Tests
{
    public class SupabaseAuthServiceTests
    {
        private IAuthenticationService authService;
        private MockSupabaseClient mockClient;
        private MockArtistRepository mockArtistRepo;

        [SetUp]
        public void SetUp()
        {
            mockClient = new MockSupabaseClient();
            mockArtistRepo = new MockArtistRepository();

            authService = new SupabaseAuthenticationService(mockClient, mockArtistRepo);
        }

        [TearDown]
        public void TearDown()
        {
            authService = null;
            mockClient = null;
            mockArtistRepo = null;
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithNullClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new SupabaseAuthenticationService(null, mockArtistRepo);
            });
        }

        [Test]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new SupabaseAuthenticationService(mockClient, null);
            });
        }

        [Test]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var service = new SupabaseAuthenticationService(mockClient, mockArtistRepo);
            Assert.NotNull(service);
        }

        #endregion

        #region IsAuthenticated Tests

        [Test]
        public void IsAuthenticated_WhenNoUser_ReturnsFalse()
        {
            mockClient.SetCurrentUser(null);
            Assert.IsFalse(authService.IsAuthenticated);
        }

        [Test]
        public void IsAuthenticated_WhenUserExists_ReturnsTrue()
        {
            mockClient.SetCurrentUser(new User { Id = "user123", Email = "test@example.com" });
            Assert.IsTrue(authService.IsAuthenticated);
        }

        #endregion

        #region CurrentUserId Tests

        [Test]
        public void CurrentUserId_WhenNoUser_ReturnsEmptyString()
        {
            mockClient.SetCurrentUser(null);
            Assert.AreEqual("", authService.CurrentUserId);
        }

        [Test]
        public void CurrentUserId_WhenUserExists_ReturnsUserId()
        {
            mockClient.SetCurrentUser(new User { Id = "user123", Email = "test@example.com" });
            Assert.AreEqual("user123", authService.CurrentUserId);
        }

        #endregion

        #region CurrentUserEmail Tests

        [Test]
        public void CurrentUserEmail_WhenNoUser_ReturnsEmptyString()
        {
            mockClient.SetCurrentUser(null);
            Assert.AreEqual("", authService.CurrentUserEmail);
        }

        [Test]
        public void CurrentUserEmail_WhenUserExists_ReturnsEmail()
        {
            mockClient.SetCurrentUser(new User { Id = "user123", Email = "test@example.com" });
            Assert.AreEqual("test@example.com", authService.CurrentUserEmail);
        }

        #endregion

        #region SignUpAsync Tests

        [Test]
        public async Task SignUpAsync_WithValidCredentials_ReturnsTrue()
        {
            mockClient.SetSignUpResult(true, "newuser123", "new@example.com");
            mockArtistRepo.SetCreateProfileResult(true);

            bool result = await authService.SignUpAsync("new@example.com", "password123", "NewUser");

            Assert.IsTrue(result);
            Assert.AreEqual(1, mockClient.SignUpCallCount);
            Assert.AreEqual(1, mockArtistRepo.CreateProfileCallCount);
        }

        [Test]
        public async Task SignUpAsync_WithInvalidCredentials_ReturnsFalse()
        {
            mockClient.SetSignUpResult(false, null, null);

            bool result = await authService.SignUpAsync("invalid@example.com", "weak", "User");

            Assert.IsFalse(result);
            Assert.AreEqual(1, mockClient.SignUpCallCount);
            Assert.AreEqual(0, mockArtistRepo.CreateProfileCallCount); // Profile not created on signup failure
        }

        [Test]
        public async Task SignUpAsync_WhenProfileCreationFails_StillReturnsTrue()
        {
            // User is created in auth system even if profile fails
            mockClient.SetSignUpResult(true, "newuser123", "new@example.com");
            mockArtistRepo.SetCreateProfileResult(false);

            bool result = await authService.SignUpAsync("new@example.com", "password123", "NewUser");

            Assert.IsTrue(result); // Auth succeeded
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("profile creation failed"));
        }

        [Test]
        public async Task SignUpAsync_FiresOnLoginSuccessEvent()
        {
            mockClient.SetSignUpResult(true, "newuser123", "new@example.com");
            mockArtistRepo.SetCreateProfileResult(true);

            bool eventFired = false;
            authService.OnLoginSuccess += () => eventFired = true;

            await authService.SignUpAsync("new@example.com", "password123", "NewUser");

            Assert.IsTrue(eventFired);
        }

        [Test]
        public async Task SignUpAsync_OnFailure_FiresOnAuthErrorEvent()
        {
            mockClient.SetSignUpResult(false, null, null);

            string errorMessage = null;
            authService.OnAuthError += (msg) => errorMessage = msg;

            await authService.SignUpAsync("invalid@example.com", "weak", "User");

            Assert.IsNotNull(errorMessage);
            Assert.IsTrue(errorMessage.Contains("Sign up failed"));
        }

        [Test]
        public async Task SignUpAsync_WithEmptyEmail_ReturnsFalse()
        {
            bool result = await authService.SignUpAsync("", "password123", "User");
            Assert.IsFalse(result);
        }

        [Test]
        public async Task SignUpAsync_WithEmptyPassword_ReturnsFalse()
        {
            bool result = await authService.SignUpAsync("test@example.com", "", "User");
            Assert.IsFalse(result);
        }

        [Test]
        public async Task SignUpAsync_WithEmptyUsername_ReturnsFalse()
        {
            bool result = await authService.SignUpAsync("test@example.com", "password123", "");
            Assert.IsFalse(result);
        }

        #endregion

        #region SignInAsync Tests

        [Test]
        public async Task SignInAsync_WithValidCredentials_ReturnsTrue()
        {
            mockClient.SetSignInResult(true, "user123", "test@example.com");

            bool result = await authService.SignInAsync("test@example.com", "password123");

            Assert.IsTrue(result);
            Assert.AreEqual(1, mockClient.SignInCallCount);
        }

        [Test]
        public async Task SignInAsync_WithInvalidCredentials_ReturnsFalse()
        {
            mockClient.SetSignInResult(false, null, null);

            bool result = await authService.SignInAsync("wrong@example.com", "wrongpass");

            Assert.IsFalse(result);
        }

        [Test]
        public async Task SignInAsync_FiresOnLoginSuccessEvent()
        {
            mockClient.SetSignInResult(true, "user122", "test@example.com");

            bool eventFired = false;
            authService.OnLoginSuccess += () => eventFired = true;

            await authService.SignInAsync("test@example.com", "password123");

            Assert.IsTrue(eventFired);
        }

        [Test]
        public async Task SignInAsync_OnFailure_FiresOnAuthErrorEvent()
        {
            mockClient.SetSignInResult(false, null, null);

            string errorMessage = null;
            authService.OnAuthError += (msg) => errorMessage = msg;

            await authService.SignInAsync("wrong@example.com", "wrongpass");

            Assert.IsNotNull(errorMessage);
        }

        [Test]
        public async Task SignInAsync_WithEmptyEmail_ReturnsFalse()
        {
            bool result = await authService.SignInAsync("", "password123");
            Assert.IsFalse(result);
        }

        [Test]
        public async Task SignInAsync_WithEmptyPassword_ReturnsFalse()
        {
            bool result = await authService.SignInAsync("test@example.com", "");
            Assert.IsFalse(result);
        }

        #endregion

        #region SignOutAsync Tests

        [Test]
        public async Task SignOutAsync_WhenAuthenticated_ReturnsTrue()
        {
            mockClient.SetCurrentUser(new User { Id = "user123", Email = "test@example.com" });
            mockClient.SetSignOutResult(true);

            bool result = await authService.SignOutAsync();

            Assert.IsTrue(result);
            Assert.AreEqual(1, mockClient.SignOutCallCount);
        }

        [Test]
        public async Task SignOutAsync_FiresOnLogoutSuccessEvent()
        {
            mockClient.SetCurrentUser(new User { Id = "user123", Email = "test@example.com" });
            mockClient.SetSignOutResult(true);

            bool eventFired = false;
            authService.OnLogoutSuccess += () => eventFired = true;

            await authService.SignOutAsync();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public async Task SignOutAsync_OnFailure_ReturnsFalse()
        {
            mockClient.SetSignOutResult(false);

            bool result = await authService.SignOutAsync();

            Assert.IsFalse(result);
        }

        #endregion

        #region RefreshSessionAsync Tests

        [Test]
        public async Task RefreshSessionAsync_WhenSuccessful_ReturnsTrue()
        {
            mockClient.SetRefreshResult(true);

            bool result = await authService.RefreshSessionAsync();

            Assert.IsTrue(result);
            Assert.AreEqual(1, mockClient.RefreshCallCount);
        }

        [Test]
        public async Task RefreshSessionAsync_WhenFailed_ReturnsFalse()
        {
            mockClient.SetRefreshResult(false);

            bool result = await authService.RefreshSessionAsync();

            Assert.IsFalse(result);
        }

        [Test]
        public async Task RefreshSessionAsync_OnSuccess_FiresOnLoginSuccessEvent()
        {
            mockClient.SetRefreshResult(true);

            bool eventFired = false;
            authService.OnLoginSuccess += () => eventFired = true;

            await authService.RefreshSessionAsync();

            Assert.IsTrue(eventFired);
        }

        #endregion

        #region Event Tests

        [Test]
        public void Events_CanSubscribeAndUnsubscribe()
        {
            Action loginHandler = () => { };
            Action logoutHandler = () => { };
            Action<string> errorHandler = (msg) => { };

            // Subscribe
            authService.OnLoginSuccess += loginHandler;
            authService.OnLogoutSuccess += logoutHandler;
            authService.OnAuthError += errorHandler;

            // Unsubscribe
            authService.OnLoginSuccess -= loginHandler;
            authService.OnLogoutSuccess -= logoutHandler;
            authService.OnAuthError -= errorHandler;

            // No exception = pass
            Assert.Pass();
        }

        #endregion

        #region Integration Tests (Optional - requires real Supabase)

        [Test]
        [Category("Integration")]
        public async Task Integration_SignUp_SignIn_SignOut_Flow()
        {
            // This test requires real Supabase connection
            // Skip in CI/CD, run manually for integration testing

            string testEmail = $"test_{Guid.NewGuid()}@example.com";
            string testPassword = "TestPassword123!";
            string testUsername = "TestUser";

            // Sign up
            bool signUpResult = await authService.SignUpAsync(testEmail, testPassword, testUsername);
            Assert.IsTrue(signUpResult, "Sign up should succeed");

            // TODO: Verify email in test environment or use test mode

            // Sign in
            bool signInResult = await authService.SignInAsync(testEmail, testPassword);
            Assert.IsTrue(signInResult, "Sign in should succeed");
            Assert.IsTrue(authService.IsAuthenticated);

            // Sign out
            bool signOutResult = await authService.SignOutAsync();
            Assert.IsTrue(signOutResult, "Sign out should succeed");
            Assert.IsFalse(authService.IsAuthenticated);
        }

        #endregion
    }

    #region Mock Classes

    public class MockSupabaseClient : ISupabaseClient
    {
        private User currentUser;
        private bool signUpSuccess;
        private bool signInSuccess;
        private bool signOutSuccess;
        private bool refreshSuccess;
        private string newUserId;
        private string newUserEmail;

        public int SignUpCallCount { get; private set; }
        public int SignInCallCount { get; private set; }
        public int SignOutCallCount { get; private set; }
        public int RefreshCallCount { get; private set; }

        public bool IsAuthenticated => currentUser != null;
        public string CurrentUserId => currentUser?.Id ?? "";
        public string CurrentUserEmail => currentUser?.Email ?? "";
        public User CurrentUser => currentUser;

        private event EventHandler<Supabase.Gotrue.Constants.AuthState> authStateChanged;

        public void SetCurrentUser(User user)
        {
            currentUser = user;
        }

        public void SetSignUpResult(bool success, string userId, string email)
        {
            signUpSuccess = success;
            newUserId = userId;
            newUserEmail = email;
        }

        public void SetSignInResult(bool success, string userId, string email)
        {
            signInSuccess = success;
            if (success)
            {
                currentUser = new User { Id = userId, Email = email };
            }
        }

        public void SetSignOutResult(bool success)
        {
            signOutSuccess = success;
            if (success)
            {
                currentUser = null;
            }
        }

        public void SetRefreshResult(bool success)
        {
            refreshSuccess = success;
        }

        public Task<Session> SignUpAsync(string email, string password)
        {
            SignUpCallCount++;
            if (signUpSuccess)
            {
                currentUser = new User { Id = newUserId, Email = newUserEmail };
                var session = new Session { User = currentUser };
                authStateChanged?.Invoke(this, Supabase.Gotrue.Constants.AuthState.SignedIn);
                return Task.FromResult(session);
            }
            return Task.FromResult<Session>(null);
        }

        public Task<Session> SignInAsync(string email, string password)
        {
            SignInCallCount++;
            if (signInSuccess)
            {
                var session = new Session { User = currentUser };
                authStateChanged?.Invoke(this, Supabase.Gotrue.Constants.AuthState.SignedIn);
                return Task.FromResult(session);
            }
            return Task.FromResult<Session>(null);
        }

        public Task SignOutAsync()
        {
            SignOutCallCount++;
            if (signOutSuccess)
            {
                currentUser = null;
                authStateChanged?.Invoke(this, Supabase.Gotrue.Constants.AuthState.SignedOut);
            }
            return Task.CompletedTask;
        }

        public Task<Session> RefreshSessionAsync()
        {
            RefreshCallCount++;
            if (refreshSuccess)
            {
                var session = new Session { User = currentUser };
                authStateChanged?.Invoke(this, Supabase.Gotrue.Constants.AuthState.SignedIn);
                return Task.FromResult(session);
            }
            return Task.FromResult<Session>(null);
        }

        public void AddAuthStateChangedListener(EventHandler<Supabase.Gotrue.Constants.AuthState> handler)
        {
            authStateChanged += handler;
        }

        public void RemoveAuthStateChangedListener(EventHandler<Supabase.Gotrue.Constants.AuthState> handler)
        {
            authStateChanged -= handler;
        }

        public Supabase.Client GetClient()
        {
            // Return null in mock - tests shouldn't need the actual client
            // If a test needs it, you can make this configurable
            return null;
        }
    }

    public class MockUser
    {
        public string Id { get; set; }
        public string Email { get; set; }
    }

    public class MockSession
    {
        public MockUser User { get; set; }
    }

    public class MockArtistRepository : IArtistRepository
    {
        private bool createProfileSuccess;

        public int CreateProfileCallCount { get; private set; }

        public void SetCreateProfileResult(bool success)
        {
            createProfileSuccess = success;
        }

        public Task<bool> CreateArtistProfileAsync(string userId, string username)
        {
            CreateProfileCallCount++;
            return Task.FromResult(createProfileSuccess);
        }

        public Task<ArtistProfile> GetArtistProfileAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateArtistProfileAsync(ArtistProfile profile)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}