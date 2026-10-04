using DAL.Helper;
using DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Model.Reader;
using Model.Shared;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BLL
{
    public class AuthService
    {
        private readonly IDatabaseHelper        _db;
        private readonly IConfiguration         _config;
        private readonly ILogger<AuthService>  _logger;

        public AuthService(IDatabaseHelper db, IConfiguration config, ILogger<AuthService> logger)
        {
            _db     = db;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Đăng nhập thống nhất cho mọi loại tài khoản.
        /// Tra bảng users (Admin / Thủ thư) trước; không có thì tra bảng readers (Bạn đọc) theo email.
        /// Cả 2 loại đều trả về cùng một cấu trúc: { token, user_id, hoten, role }.
        /// </summary>
        public async Task<ResponseModel> dangnhap(string tendangnhap, string matkhau)
        {
            if (string.IsNullOrWhiteSpace(tendangnhap) || string.IsNullOrWhiteSpace(matkhau))
                return ResponseModel.Fail("Sai tài khoản hoặc mật khẩu.");

            // 1. Nhân viên: Admin / Thủ thư — khóa đăng nhập là tên tài khoản
            var user = await _db.QueryFirstOrDefaultAsync<UserModel>("sp_user_login", new { taikhoan = tendangnhap });

            if (user != null)
            {
                if (!kiemtramatkhau(matkhau, user.matkhau))
                    return ResponseModel.Fail("Sai tài khoản hoặc mật khẩu.");

                return ResponseModel.Ok(new
                {
                    token   = taojwttoken(user.user_id.ToString(), user.hoten, user.role),
                    user_id = user.user_id,
                    hoten   = user.hoten,
                    role    = user.role
                }, "Đăng nhập thành công.");
            }

            // 2. Bạn đọc — khóa đăng nhập là email
            var reader = await _db.QueryFirstOrDefaultAsync<ReaderModel>("sp_reader_login", new { email = tendangnhap });

            if (reader == null || !kiemtramatkhau(matkhau, reader.matkhau))
                return ResponseModel.Fail("Sai tài khoản hoặc mật khẩu.");

            // Chỉ báo "bị khoá / hết hạn" SAU khi mật khẩu đúng, tránh lộ trạng thái tài khoản cho người dò
            if (reader.trangthai != 0)
                return ResponseModel.Fail("Tài khoản đã bị khoá hoặc hết hạn.");

            return ResponseModel.Ok(new
            {
                token   = taojwttoken(reader.reader_id.ToString(), reader.hoten, "BanDoc"),
                user_id = reader.reader_id,
                hoten   = reader.hoten,
                role    = "BanDoc"
            }, "Đăng nhập thành công.");
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
            // Định dạng lưu trữ duy nhất là BCrypt ($2a$ / $2b$ / $2y$)
            if (matkhauHash.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(matkhau, matkhauHash);
            }

            // Định dạng lưu trữ không hợp lệ (MD5 cũ, plaintext, ...) → từ chối đăng nhập.
            // Tuyệt đối KHÔNG so sánh plaintext.
            _logger.LogWarning("Tài khoản có định dạng mật khẩu không hợp lệ ({DoDaiKyTu} ký tự). Từ chối đăng nhập.", matkhauHash.Length);
            return false;
        }
    }
}
