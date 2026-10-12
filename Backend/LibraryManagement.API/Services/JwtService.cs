// Các service cho JWT and authentication
namespace LibraryManagement.API.Services
{
    public interface IJwtService
    {
        string GenerateToken(string userId, string email, string role, IEnumerable<string> permissions = null);
        string GenerateRefreshToken();
        bool ValidateToken(string token);
        string GetUserIdFromToken(string token);
        string GetEmailFromToken(string token);
        IEnumerable<string> GetRolesFromToken(string token);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateToken(string userId, string email, string role, IEnumerable<string> permissions = null)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"];
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    throw new InvalidOperationException("JWT SecretKey chưa được cấu hình");
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, role),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };

                if (permissions != null)
                {
                    foreach (var permission in permissions)
                    {
                        claims.Add(new Claim("permission", permission));
                    }
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpireHours"])),
                    Issuer = jwtSettings["Issuer"],
                    Audience = jwtSettings["Audience"],
                    SigningCredentials = credentials,
                    NotBefore = DateTime.UtcNow
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo JWT token");
                throw;
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public bool ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = GetValidationParameters();

                tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return false;
            }
        }

        public string GetUserIdFromToken(string token)
        {
            var validationParameters = GetValidationParameters();
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi trích xuất UserId từ token");
                return null;
            }
        }

        public string GetEmailFromToken(string token)
        {
            var validationParameters = GetValidationParameters();
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                return email;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi trích xuất Email từ token");
                return null;
            }
        }

        public IEnumerable<string> GetRolesFromToken(string token)
        {
            var validationParameters = GetValidationParameters();
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var roles = principal.FindAll(ClaimTypes.Role)?.Select(c => c.Value);
                return roles ?? Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi trích xuất Roles từ token");
                return Enumerable.Empty<string>();
            }
        }

        private TokenValidationParameters GetValidationParameters()
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey chưa được cấu hình");
            }

            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true
            };
        }
    }
}