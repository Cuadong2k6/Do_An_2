using Google.Apis.Auth;

namespace LibraryManagement.API.Services;

public sealed class GoogleTokenValidator
{
    private readonly string _clientId;
    private readonly Func<string, GoogleJsonWebSignature.ValidationSettings, Task<GoogleJsonWebSignature.Payload>> _xacThucToken;

    public GoogleTokenValidator(IConfiguration configuration)
        : this(configuration, GoogleJsonWebSignature.ValidateAsync)
    {
    }

    public GoogleTokenValidator(
        IConfiguration configuration,
        Func<string, GoogleJsonWebSignature.ValidationSettings, Task<GoogleJsonWebSignature.Payload>> xacThucToken)
    {
        _clientId = configuration["Authentication:Google:ClientId"] ?? string.Empty;
        _xacThucToken = xacThucToken;
    }

    public async Task<GoogleJsonWebSignature.Payload> XacThucAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(_clientId) || _clientId == "YOUR_GOOGLE_CLIENT_ID")
            throw new InvalidOperationException("Chưa cấu hình Authentication:Google:ClientId.");

        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException("Google ID token không được để trống.", nameof(idToken));

        var payload = await _xacThucToken(
            idToken,
            new GoogleJsonWebSignature.ValidationSettings { Audience = [_clientId] });

        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Subject))
            throw new InvalidJwtException("Google account chưa xác minh email hoặc thiếu thông tin bắt buộc.");

        return payload;
    }
}
