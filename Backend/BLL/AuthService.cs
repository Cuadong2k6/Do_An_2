using DAL.Helper;
using DAL;
using Microsoft.Data.SqlClient;
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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BLL
{
    public class AuthService
    {
        public const string ThongDiepYeuCauDatLai =
            "Nếu email tồn tại, hệ thống đã gửi hướng dẫn đặt lại mật khẩu.";
        public const string ThongDiepDatLaiThanhCong = "Đặt lại mật khẩu thành công.";
        public const string ThongDiepDatLaiThatBai =
            "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";

        private readonly IDatabaseHelper        _db;
        private readonly IConfiguration         _config;
        private readonly ILogger<AuthService>  _logger;
        private readonly IPasswordResetRepository? _pwReset;
        private readonly IHangDoiThongBao? _hangDoi;
        private readonly IGoogleReaderRepository? _readerRepo;

        public AuthService(IDatabaseHelper db, IConfiguration config, ILogger<AuthService> logger)
            : this(db, config, logger, null, null)
        {
        }

        public AuthService(
            IDatabaseHelper db,
            IConfiguration config,
            ILogger<AuthService> logger,
            IPasswordResetRepository? pwReset,
            IHangDoiThongBao? hangDoi,
            IGoogleReaderRepository? readerRepo = null)
        {
            _db      = db;
            _config  = config;
            _logger  = logger;
            _pwReset = pwReset;
            _hangDoi = hangDoi;
            _readerRepo = readerRepo;
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
                // Staff bị khóa: không kiểm tra mật khẩu, không phát JWT
                if (user.is_locked)
                    return ResponseModel.Fail("Tài khoản nhân viên đã bị khóa. Vui lòng liên hệ quản trị viên.");

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

            // Reader chỉ đăng nhập bằng Google; không dùng password fallback.
            var reader = await _db.QueryFirstOrDefaultAsync<ReaderModel>("sp_reader_login", new { email = tendangnhap });
            return reader == null
                ? ResponseModel.Fail("Sai tài khoản hoặc mật khẩu.")
                : ResponseModel.Fail("Bạn đọc vui lòng đăng nhập bằng Google.");
        }

        public async Task<ResponseModel> DangNhapGoogleAsync(string googleSub, string email, string hoten)
        {
            if (_readerRepo == null)
                return ResponseModel.Fail("Dịch vụ đăng nhập Google chưa sẵn sàng.");

            Model.Reader.GoogleReaderLoginResult? reader = null;
            for (var lanThu = 0; lanThu < 3; lanThu++)
            {
                var soThe = $"RD-{Guid.NewGuid():N}"[..15].ToUpperInvariant();
                reader = await _readerRepo.DangNhapBangGoogleAsync(googleSub, email, hoten, soThe);
                if (reader?.status_code != 50306)
                    break;
            }

            if (reader == null || reader.status_code != 0)
                return ResponseModel.Fail("Không thể đăng nhập Google hoặc liên kết tài khoản. Vui lòng liên hệ thư viện.");

            if (reader.trangthai != 0)
                return ResponseModel.Fail("Tài khoản bạn đọc đã bị khóa hoặc hết hạn.");

            var token = taojwttoken(reader.reader_id.ToString(), reader.hoten, "BanDoc", reader.trangthai_onboarding);
            return ResponseModel.Ok(new
            {
                token,
                user_id = reader.reader_id,
                hoten = reader.hoten,
                role = "BanDoc",
                onboarding_status = reader.trangthai_onboarding,
                is_new = reader.is_new
            }, reader.trangthai_onboarding == 1
                ? "Đăng nhập thành công. Tài khoản đang chờ thanh toán."
                : "Đăng nhập thành công.");
        }

        private string taojwttoken(string userId, string hoten, string role, int? onboardingStatus = null)
        {
            var secret   = _config["JwtSettings:SecretKey"]!;
            var issuer   = _config["JwtSettings:Issuer"]!;
            var audience = _config["JwtSettings:Audience"]!;
            int expHours = int.Parse(_config["JwtSettings:ExpireHours"] ?? "8");

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name,           hoten),
                new Claim(ClaimTypes.Role,           role.Trim())
            };
            if (onboardingStatus.HasValue)
                claims.Add(new Claim("onboarding_status", onboardingStatus.Value.ToString()));

            var token = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                expires:            DateTime.Now.AddHours(expHours),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Yêu cầu đặt lại mật khẩu. Luôn trả cùng kết quả generic, kể cả email null,
        /// email sai định dạng, email không tồn tại, email mơ hồ, lỗi DB hoặc lỗi hàng đợi.
        /// Email không hợp lệ không ghi DB/token. Không ghi log raw token.
        /// </summary>
        public async Task<ResponseModel> yeucaudatlaimatkhau(string? email)
        {
            if (!emailhople(email))
            {
                _logger.LogWarning("Yêu cầu đặt lại mật khẩu bị bỏ qua: email null hoặc không đúng định dạng.");
                return ResponseModel.Ok(null, ThongDiepYeuCauDatLai);
            }

            if (_pwReset == null || _hangDoi == null)
            {
                _logger.LogError("Thiếu PasswordResetRepository hoặc hàng đợi thông báo.");
                return ResponseModel.Ok(null, ThongDiepYeuCauDatLai);
            }

            var raw = taotoken();
            var hash = bamtoken(raw);
            string? loaiTaiKhoan;
            try
            {
                loaiTaiKhoan = await _pwReset.TaoTokenAsync(email!.Trim(), hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo token đặt lại mật khẩu. SqlNumber={SqlNumber}", sqlnumber(ex));
                return ResponseModel.Ok(null, ThongDiepYeuCauDatLai);
            }

            if (loaiTaiKhoan == null)
                return ResponseModel.Ok(null, ThongDiepYeuCauDatLai);

            var baseUrl = _config["Frontend:BaseUrl"];
            if (!baseurlhople(baseUrl))
            {
                _logger.LogError("Thiếu hoặc sai Frontend:BaseUrl. Không gửi email đặt lại mật khẩu.");
                return ResponseModel.Ok(null, ThongDiepYeuCauDatLai);
            }

            try
            {
                var link = $"{baseUrl!.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(raw)}";
                _hangDoi.ThemEmail(new ThongBaoEmail
                {
                    TieuDe = "Đặt lại mật khẩu thư viện",
                    NoiDung =
                        "<p>Bạn nhận được yêu cầu đặt lại mật khẩu.</p>" +
                        $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Đặt lại mật khẩu</a></p>" +
                        "<p>Liên kết có hiệu lực 30 phút và chỉ dùng một lần. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>",
                    NguoiNhan = email!.Trim()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không đưa được email đặt lại mật khẩu vào hàng đợi.");
            }

            return ResponseModel.Ok(null, ThongDiepYeuCauDatLai);
        }

        /// <summary>
        /// Hoàn tất đặt lại. Token sai/hết hạn và lỗi DB đều trả cùng thông điệp generic.
        /// </summary>
        public async Task<ResponseModel> hoanthanhdatlaimatkhau(string? token, string? matkhau)
        {
            if (!tokenhople(token))
                return ResponseModel.Fail(ThongDiepDatLaiThatBai);
            if (!matkhauhople(matkhau))
                return ResponseModel.Fail("Mật khẩu không hợp lệ.");

            if (_pwReset == null)
            {
                _logger.LogError("Thiếu PasswordResetRepository.");
                return ResponseModel.Fail(ThongDiepDatLaiThatBai);
            }

            try
            {
                await _pwReset.TieuThuTokenAsync(bamtoken(token!), hashmatkhau(matkhau!));
                return ResponseModel.Ok(null, ThongDiepDatLaiThanhCong);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đặt lại mật khẩu thất bại. SqlNumber={SqlNumber}", sqlnumber(ex));
                return ResponseModel.Fail(ThongDiepDatLaiThatBai);
            }
        }

        internal static string taotoken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(bytes);
        }

        internal static string bamtoken(string rawToken)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool emailhople(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var e = email.Trim();
            if (e.Length > 255) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(e);
                return addr.Address == e && e.Contains('.') && !e.Contains(' ');
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool tokenhople(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var t = token.Trim();
            if (t.Length < 43 || t.Length > 64) return false;
            foreach (var c in t)
            {
                var ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')
                    || (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!ok) return false;
            }
            return true;
        }

        /// <summary>Không siết policy mới: chỉ chặn rỗng và hash vượt NVARCHAR(255).</summary>
        private static bool matkhauhople(string? matkhau)
        {
            if (string.IsNullOrEmpty(matkhau) || matkhau.Length > 72) return false;
            return hashmatkhau(matkhau).Length <= 255;
        }

        private static bool baseurlhople(string? baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) return false;
            if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private static int? sqlnumber(Exception ex)
        {
            for (Exception? cur = ex; cur != null; cur = cur.InnerException)
            {
                if (cur is SqlException sql) return sql.Number;
            }
            return null;
        }

        private static string hashmatkhau(string matkhau)
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
