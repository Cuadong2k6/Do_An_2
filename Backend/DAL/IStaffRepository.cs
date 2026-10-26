using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Model.Staff;

namespace DAL
{
    /// <summary>
    /// Interface tối thiểu cho StaffRepository, phục vụ mock trong unit test.
    /// Chỉ khai báo method cần thiết — mở rộng khi có nhu cầu test thêm.
    /// </summary>
    public interface IStaffRepository
    {
        Task<(IEnumerable<StaffDto> items, long total)> DanhSachThuThuAsync(int pageIndex, int pageSize);
        Task<StaffDto?> ChiTietThuThuAsync(Guid userId);
        Task<Guid> TaoThuThuAsync(string hoten, string email, string taikhoan, string matkhauHash, string? sodienthoai, string? imageUrl);
        Task CapNhatThuThuAsync(Guid userId, string? hoten, string? email, string? taikhoan, string? matkhauHash, string? sodienthoai, string? imageUrl);
        Task XoaThuThuAsync(Guid userId);
        Task KhoaThuThuAsync(Guid userId);
        Task MoKhoaThuThuAsync(Guid userId);
    }
}
