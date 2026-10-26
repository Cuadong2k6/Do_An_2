using DAL;
using Microsoft.Extensions.Logging;
using Model.Shared;
using Model.Staff;

namespace BLL
{
    /// <summary>
    /// Service quản lý tài khoản ThuThu. Chỉ Admin mới được gọi.
    /// BCrypt hash mật khẩu tại C# trước khi truyền xuống SP.
    /// </summary>
    public class StaffService
    {
        private readonly IStaffRepository _repo;
        private readonly ILogger<StaffService> _logger;

        public StaffService(IStaffRepository repo, ILogger<StaffService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// <summary>Danh sách ThuThu có phân trang.</summary>
        public async Task<ResponseModel> DanhSachThuThuAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var (items, total) = await _repo.DanhSachThuThuAsync(page, pageSize);
            return ResponseModel.Ok(items, "Thành công", total, page, pageSize);
        }

        /// <summary>Chi tiết 1 ThuThu theo user_id.</summary>
        public async Task<ResponseModel> ChiTietThuThuAsync(Guid userId)
        {
            var staff = await _repo.ChiTietThuThuAsync(userId);
            if (staff == null)
                return ResponseModel.Fail("Không tìm thấy thủ thư.");
            return ResponseModel.Ok(staff);
        }

        /// <summary>Tạo tài khoản ThuThu mới. Hash mật khẩu bằng BCrypt.</summary>
        public async Task<ResponseModel> TaoThuThuAsync(TaoThuThuRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.hoten) || string.IsNullOrWhiteSpace(req.taikhoan))
                return ResponseModel.Fail("Họ tên và tài khoản là bắt buộc.");

            // Hash BCrypt — workFactor 12 theo chuẩn project (giống AuthService)
            var hashMatKhau = BCrypt.Net.BCrypt.HashPassword(req.matkhau, workFactor: 12);

            try
            {
                // Repository tự sinh ID từ SQL output, trả về Guid
                var userId = await _repo.TaoThuThuAsync(req.hoten, req.email, req.taikhoan, hashMatKhau, req.sodienthoai, req.image_url);
                _logger.LogInformation("Đã tạo thủ thư {UserId}, tài khoản {TaiKhoan}", userId, req.taikhoan);
                return ResponseModel.Ok(new { user_id = userId }, "Tạo thủ thư thành công.");
            }
            catch (Exception ex) when (ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                                       || ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                return ResponseModel.Fail("Tài khoản hoặc email đã tồn tại.");
            }
        }

        /// <summary>Cập nhật thông tin ThuThu. Nếu có matkhau mới thì hash BCrypt ghi đè.</summary>
        public async Task<ResponseModel> CapNhatThuThuAsync(Guid userId, CapNhatThuThuRequest req)
        {
            var existing = await _repo.ChiTietThuThuAsync(userId);
            if (existing == null)
                return ResponseModel.Fail("Không tìm thấy thủ thư.");

            // Chỉ hash nếu client gửi mật khẩu mới
            string? hashMatKhau = null;
            if (!string.IsNullOrWhiteSpace(req.matkhau))
            {
                hashMatKhau = BCrypt.Net.BCrypt.HashPassword(req.matkhau, workFactor: 12);
            }

            try
            {
                await _repo.CapNhatThuThuAsync(
                    userId,
                    req.hoten ?? existing.hoten,
                    req.email ?? existing.email,
                    req.taikhoan ?? existing.taikhoan,
                    hashMatKhau,
                    req.sodienthoai, // null = giữ nguyên, "" = xoá (xử lý trong SP)
                    req.image_url ?? existing.image_url
                );
                _logger.LogInformation("Đã cập nhật thủ thư {UserId}", userId);
                return ResponseModel.Ok(null, "Cập nhật thành công.");
            }
            catch (Exception ex) when (ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                                       || ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                return ResponseModel.Fail("Tài khoản hoặc email đã tồn tại.");
            }
        }

        /// <summary>Xóa cứng ThuThu. Không xóa Admin.</summary>
        public async Task<ResponseModel> XoaThuThuAsync(Guid userId)
        {
            var existing = await _repo.ChiTietThuThuAsync(userId);
            if (existing == null)
                return ResponseModel.Fail("Không tìm thấy thủ thư.");

            await _repo.XoaThuThuAsync(userId);
            _logger.LogInformation("Đã xóa thủ thư {UserId}", userId);
            return ResponseModel.Ok(null, "Xóa thành công.");
        }

        /// <summary>Khóa tài khoản ThuThu.</summary>
        public async Task<ResponseModel> KhoaThuThuAsync(Guid userId)
        {
            await _repo.KhoaThuThuAsync(userId);
            _logger.LogInformation("Đã khóa thủ thư {UserId}", userId);
            return ResponseModel.Ok(null, "Khóa tài khoản thành công.");
        }

        /// <summary>Mở khóa tài khoản ThuThu.</summary>
        public async Task<ResponseModel> MoKhoaThuThuAsync(Guid userId)
        {
            await _repo.MoKhoaThuThuAsync(userId);
            _logger.LogInformation("Đã mở khóa thủ thư {UserId}", userId);
            return ResponseModel.Ok(null, "Mở khóa tài khoản thành công.");
        }
    }
}