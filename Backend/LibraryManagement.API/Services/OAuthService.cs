// Các service cho OAuth2 authentication
namespace LibraryManagement.API.Services
{
    public interface IOAuthService
    {
        Task<AuthenticationResult> AuthenticateWithGoogleAsync(string code, string returnUrl = null);
        Task<AuthenticationResult> AuthenticateWithFacebookAsync(string code, string returnUrl = null);
        Task LogoutAsync(string returnUrl = null);
        Task<Dictionary<string, object>> GetUserInfoFromGoogleAsync(string accessToken);
        Task<Dictionary<string, object>> GetUserInfoFromFacebookAsync(string accessToken);
    }

    public class OAuthService : IOAuthService
    {
        private readonly ILogger<OAuthService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public OAuthService(
            ILogger<OAuthService> logger,
            IHttpClientFactory httpClientFactory,
            IUserService userService,
            IJwtService jwtService,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        public async Task<AuthenticationResult> AuthenticateWithGoogleAsync(string code, string returnUrl = null)
        {
            try
            {
                var googleConfig = _configuration.GetSection("Authentication:Google");
                var clientId = googleConfig["ClientId"];
                var clientSecret = googleConfig["ClientSecret"];
                var redirectUri = string.IsNullOrEmpty(returnUrl) 
                    ? $"{_configuration["AppUrl"]}/signin-google" 
                    : returnUrl;

                using var client = _httpClientFactory.CreateClient();
                var tokenRequestParameters = new Dictionary<string, string>
                {
                    { "code", code },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "redirect_uri", redirectUri },
                    { "grant_type", "authorization_code" },
                    { "scope", "openid email profile" }
                };

                var tokenContent = new FormUrlEncodedContent(tokenRequestParameters);
                var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token", tokenContent);
                tokenResponse.EnsureSuccessStatusCode();

                var tokenResult = JsonSerializer.Deserialize<GoogleTokenResponse>(await tokenResponse.Content.ReadAsStringAsync());
                var userInfo = await GetUserInfoFromGoogleAsync(tokenResult.access_token);

                return await ProcessOAuthUserInfo(userInfo, "google", tokenResult.access_token, tokenResult.refresh_token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xác thực Google OAuth");
                return new AuthenticationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<AuthenticationResult> AuthenticateWithFacebookAsync(string code, string returnUrl = null)
        {
            try
            {
                var facebookConfig = _configuration.GetSection("Authentication:Facebook");
                var appId = facebookConfig["AppId"];
                var appSecret = facebookConfig["AppSecret"];
                var redirectUri = string.IsNullOrEmpty(returnUrl)
                    ? $"{_configuration["AppUrl"]}/signin-facebook"
                    : returnUrl;

                using var client = _httpClientFactory.CreateClient();
                var tokenRequestParameters = new Dictionary<string, string>
                {
                    { "code", code },
                    { "client_id", appId },
                    { "client_secret", appSecret },
                    { "redirect_uri", redirectUri }
                };

                var tokenContent = new FormUrlEncodedContent(tokenRequestParameters);
                var tokenResponse = await client.PostAsync($"https://graph.facebook.com/v18.0/oauth/access_token?client_id={appId}&redirect_uri={redirectUri}&client_secret={appSecret}&code={code}", tokenContent);
                tokenResponse.EnsureSuccessStatusCode();

                var tokenResult = JsonSerializer.Deserialize<FacebookTokenResponse>(await tokenResponse.Content.ReadAsStringAsync());
                var userInfo = await GetUserInfoFromFacebookAsync(tokenResult.access_token);

                return await ProcessOAuthUserInfo(userInfo, "facebook", tokenResult.access_token, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xác thực Facebook OAuth");
                return new AuthenticationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task LogoutAsync(string returnUrl = null)
        {
            try
            {
                // Xóa session cookie và JWT token
                _logger.LogInformation("Người dùng đã đăng xuất");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng xuất");
                throw;
            }
        }
    }
}