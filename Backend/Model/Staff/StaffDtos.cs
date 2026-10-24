using System;
using System.ComponentModel.DataAnnotations;

namespace Model.Staff
{
    /// <summary>
    /// DTO trả về thông tin ThuThu — không bao gồm mật khẩu hash.
    /// Khớp với output của sp_user_list và sp_user_detail.
    /// </summary>
    public class StaffDto
    {
        public Guid user_id { get; set; }
        public string hoten { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string taikhoan { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string? image_url { get; set; }
        public DateTime? ngaytao { get; set; }
        public bool is_locked { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu tạo tài khoản ThuThu mới.
    /// </summary>
    public class TaoThuThuRequest
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc.")]
        [MaxLength(200)]
        public string hoten { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(200)]
        public string email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tài khoản là bắt buộc.")]
        [MaxLength(100)]
        public string taikhoan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
        public string matkhau { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? image_url { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu cập nhật thông tin ThuThu.
    /// Các trường null = không thay đổi. matkhau chỉ hash khi có giá trị.
    /// </summary>
    public class CapNhatThuThuRequest
    {
        [MaxLength(200)]
        public string? hoten { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(200)]
        public string? email { get; set; }

        [MaxLength(100)]
        public string? taikhoan { get; set; }

        /// <summary>Nếu null = giữ nguyên mật khẩu cũ. Nếu có = hash BCrypt mới.</summary>
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
        public string? matkhau { get; set; }

        [MaxLength(500)]
        public string? image_url { get; set; }
    }
}