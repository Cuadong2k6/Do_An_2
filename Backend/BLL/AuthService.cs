using DAL.Helper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Model;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
            // Hash mật khẩu (MD5 / BCrypt tuỳ cấu hình)
            string matkhauHash = hashmatkhau(matkhau);

            var user = await _db.QueryFirstOrDefaultAsync<UserModel>("sp_user_login", new
            {
                taikhoan = taikhoan,
                matkhau  = matkhauHash
            });

            if (user == null) return ResponseModel.Fail("Sai tài khoản hoặc mật khẩu.");

            user.token = taojwttoken(user.user_id, user.hoten, user.role);
            return ResponseModel.Ok(user, "Đăng nhập thành công.");
        }

        /// <summary>
        /// Đăng nhập Bạn đọc
        /// </summary>
        public async Task<ResponseModel> dangnhapbandoc(string email, string matkhau)
        {
            string matkhauHash = hashmatkhau(matkhau);

            using var conn = _db.GetConnection();
            var reader = await conn.QueryFirstOrDefaultAsync<ReaderModel>("sp_reader_login",
                new { email = email, matkhau = matkhauHash },
                commandType: CommandType.StoredProcedure);

            if (reader == null) return ResponseModel.Fail("Sai email hoặc mật khẩu.");
            if (reader.trangthai != 0) return ResponseModel.Fail("Tài khoản đã bị khoá hoặc hết hạn.");

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
                new Claim(ClaimTypes.Role,           role)
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
            using var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(matkhau));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
