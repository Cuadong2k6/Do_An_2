using Google.Apis.Auth;
using LibraryManagement.API.Services;
using Microsoft.Extensions.Configuration;

namespace LibraryManagement.Tests;

public class GoogleTokenValidatorTests
{
    [Fact]
    public async Task XacThucAsync_DungAudienceVaTraPayloadDaXacMinh()
    {
        var configuration = CauHinh("reader-client-id");
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "google-sub",
            Email = "reader@example.com",
            EmailVerified = true
        };
        var validator = new GoogleTokenValidator(configuration, (token, settings) =>
        {
            Assert.Equal("google-id-token", token);
            Assert.Contains("reader-client-id", settings.Audience);
            return Task.FromResult(payload);
        });

        var result = await validator.XacThucAsync("google-id-token");

        Assert.Same(payload, result);
    }

    [Fact]
    public async Task XacThucAsync_TuChoiEmailChuaXacMinh()
    {
        var validator = new GoogleTokenValidator(CauHinh("reader-client-id"), (_, _) => Task.FromResult(
            new GoogleJsonWebSignature.Payload
            {
                Subject = "google-sub",
                Email = "reader@example.com",
                EmailVerified = false
            }));

        await Assert.ThrowsAsync<InvalidJwtException>(() => validator.XacThucAsync("google-id-token"));
    }

    [Fact]
    public async Task XacThucAsync_ThieuAudienceThiTuChoiTruocKhiGoiVerifier()
    {
        var validator = new GoogleTokenValidator(CauHinh("YOUR_GOOGLE_CLIENT_ID"), (_, _) =>
            throw new Xunit.Sdk.XunitException("Không được gọi verifier nếu chưa cấu hình audience."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => validator.XacThucAsync("google-id-token"));
    }

    [Fact]
    public async Task XacThucAsync_PropagateTokenInvalid()
    {
        var validator = new GoogleTokenValidator(CauHinh("reader-client-id"), (_, _) =>
            throw new InvalidJwtException("invalid token"));

        await Assert.ThrowsAsync<InvalidJwtException>(() => validator.XacThucAsync("bad-token"));
    }

    private static IConfiguration CauHinh(string clientId) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Google:ClientId"] = clientId
        })
        .Build();
}
