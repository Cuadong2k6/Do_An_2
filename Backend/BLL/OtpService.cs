using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using DAL;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Model.Shared;

namespace BLL
{
    public interface IOtpService
    {
        Task<ResponseModel> YeuCauOtpAsync(string? email);
        Task<ResponseModel> XacThucOtpAsync(string? email, string? otp);
    }

    public sealed class OtpService : IOtpService
    {
        private readonly IOtpRepository _repository;
        private readonly IHangDoiThongBao _hangDoi;
        private readonly byte[] _hmacKey;
        private readonly ILogger<OtpService> _logger;

        public OtpService(IOtpRepository repository, IConfiguration configuration,
            IHangDoiThongBao hangDoi, ILogger<OtpService> logger)
        {
            _repository = repository;
            _hangDoi = hangDoi;
            _logger = logger;
            var secret = configuration["Security:OtpHmacKey"];
            if (string.IsNullOrEmpty(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
                throw new InvalidOperationException("Security:OtpHmacKey phải có ít nhất 32 byte.");
            _hmacKey = Encoding.UTF8.GetBytes(secret);
        }

        public async Task<ResponseModel> YeuCauOtpAsync(string? email)
        {
            if (!EmailHopLe(email)) return ResponseModel.Fail("Email không hợp lệ.");

            var emailNorm = email!.Trim().ToLowerInvariant();
            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            try
            {
                await _repository.TaoOtpAsync(emailNorm, BamOtp(otp));
                _hangDoi.ThemEmail(new ThongBaoEmail
                {
                    NguoiNhan = emailNorm,
                    TieuDe = "Mã xác thực email",
                    NoiDung = $"Mã OTP của bạn là {otp}. Mã có hiệu lực trong 5 phút."
                });
                return ResponseModel.Ok(null, "Đã gửi mã xác thực qua email.");
            }
            catch (SqlException ex) when (ex.Number == OtpRepository.LoiCooldown)
            {
                return ResponseModel.Fail("Bạn vừa yêu cầu mã OTP. Vui lòng đợi 60 giây.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể yêu cầu OTP email.");
                return ResponseModel.Fail("Không thể gửi mã xác thực.");
            }
        }

        public async Task<ResponseModel> XacThucOtpAsync(string? email, string? otp)
        {
            if (!EmailHopLe(email) || otp is null || otp.Length != 6 || otp.Any(c => c < '0' || c > '9'))
                return ResponseModel.Fail("Email hoặc mã OTP không hợp lệ.");

            var emailNorm = email!.Trim().ToLowerInvariant();
            var proof = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            try
            {
                await _repository.XacThucOtpAsync(emailNorm, BamOtp(otp), Sha256(proof));
                return ResponseModel.Ok(new { proof }, "Xác thực OTP thành công.");
            }
            catch (SqlException ex) when (ex.Number is >= OtpRepository.LoiSaiMa and <= OtpRepository.LoiBiKhoa)
            {
                var message = ex.Number switch
                {
                    OtpRepository.LoiSaiMa => "Mã OTP không đúng.",
                    OtpRepository.LoiDaDung => "Mã OTP đã được sử dụng.",
                    OtpRepository.LoiHetHan => "Mã OTP đã hết hạn.",
                    _ => "Mã OTP đã bị khóa. Vui lòng yêu cầu mã mới."
                };
                return ResponseModel.Fail(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xác thực OTP email.");
                return ResponseModel.Fail("Không thể xác thực mã OTP.");
            }
        }

        private string BamOtp(string otp)
            => Convert.ToHexString(HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(otp))).ToLowerInvariant();

        private static string Sha256(string value)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

        private static bool EmailHopLe(string? email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 200) return false;
            try
            {
                var trimmed = email.Trim();
                var address = new MailAddress(trimmed);
                return address.Address == trimmed && trimmed.Contains('.') && !trimmed.Contains(' ');
            }
            catch (FormatException) { return false; }
        }
    }
}
