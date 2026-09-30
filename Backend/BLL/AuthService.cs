using DAL.Helper;
using DAL;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Model.Reader;
using Model.Shared;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BLL
{
    public class AuthService
    {
        private readonly IDatabaseHelper _db;
        private readonly IConfiguration  _config;

        public AuthService(IDatabaseHelper db, IConfiguration config)
        {
            _db     = db;
            _config = config;
        }

        /// <summary>
        /// Đăng nhập Admin / Thủ thư
        /// </summary>
        public async Task<ResponseModel> dangnhapquantri(string taikhoan, string matkhau)
        {
            var user = await _db.QueryFirstOrDefaultAsync<UserModel>("sp_user_login", new { taikhoan });

            if (user == null) return ResponseModel.Fail("Sai tài khoản hoặc mật khẩu.");
            if (!kiemtramatkhau(matkhau, user.matkhau)) return ResponseModel.Fail("Sai tài khoản hoặc mật khẩu.");

            user.token = taojwttoken(user.user_id.ToString(), user.hoten, user.role);
            return ResponseModel.Ok(user, "Đăng nhập thành công.");
        }

        /// <summary>
        /// Đăng nhập Bạn đọc
        /// </summary>
        public async Task<ResponseModel> dangnhapbandoc(string email, string matkhau)
        {
            using var conn = _db.GetConnection();
            var reader = await conn.QueryFirstOrDefaultAsync<ReaderModel>("sp_reader_login",
                new { email = email },
                commandType: CommandType.StoredProcedure);

            if (reader == null) return ResponseModel.Fail("Sai email hoặc mật khẩu.");
            if (reader.trangthai != 0) return ResponseModel.Fail("Tài khoản đã bị khoá hoặc hết hạn.");
            if (!kiemtramatkhau(matkhau, reader.matkhau)) return ResponseModel.Fail("Sai email hoặc mật khẩu.");

            var token = taojwttoken(reader.reader_id.ToString(), reader.hoten, "BanDoc");
            return ResponseModel.Ok(new { reader, token }, "Đăng nhập thành công.");
        }

        private string taojwttoken(string userId, string hoten, string role)
        {
            var secret   = _config["JwtSettings:SecretKey"]!;
            var issuer   = _config["JwtSettings:Issuer"]!;
            var audience = _config["JwtSettings:Audience"]!;
            int expHours = int.Parse(_config["JwtSettings:ExpireHours"] ?? "8");

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name,           hoten),
                new Claim(ClaimTypes.Role,           role.Trim())
            };

            var token = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                expires:            DateTime.Now.AddHours(expHours),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string hashmatkhau(string matkhau)
        {
            return BCrypt.Net.BCrypt.HashPassword(matkhau, workFactor: 12);
        }

        private bool kiemtramatkhau(string matkhau, string matkhauHash)
        {
            // BCrypt hashes start with $2a$, $2b$, $2y$
            if (matkhauHash.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(matkhau, matkhauHash);
            }
            
            // Legacy MD5 hash (32 hex chars)
            if (matkhauHash.Length == 32 && System.Text.RegularExpressions.Regex.IsMatch(matkhauHash, @"^[a-f0-9]{32}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                // Verify with MD5
                using var md5 = System.Security.Cryptography.MD5.Create();
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(matkhau));
                var md5Hash = Convert.ToHexString(bytes).ToLower();
                return md5Hash == matkhauHash;
            }
            
            // Plaintext fallback (should not happen in production)
            return matkhau == matkhauHash;
        }
    }
}
