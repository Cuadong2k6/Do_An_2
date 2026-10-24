using DAL.Helper;
using Dapper;
using Model.Staff;
using System.Data;

namespace DAL
{
    /// <summary>
    /// Repository gọi các stored procedure quản lý staff (ThuThu) từ script 14_UserAccountManagement.sql.
    /// Không thao tác trực tiếp với bảng users — mọi truy vấn đều qua SP.
    /// </summary>
    public class StaffRepository : IStaffRepository
    {
        private readonly IDatabaseHelper _db;

        public StaffRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        /// <summary>
        /// Danh sách ThuThu có phân trang. Khớp sp_user_list.
        /// Trả về (items, totalItems).
        /// </summary>
        public async Task<(IEnumerable<StaffDto> items, long total)> DanhSachThuThuAsync(int pageIndex, int pageSize)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@page_index", pageIndex, DbType.Int32);
            p.Add("@page_size", pageSize, DbType.Int32);
            p.Add("@total", dbType: DbType.Int64, direction: ParameterDirection.Output);

            var items = await conn.QueryAsync<StaffDto>("sp_user_list", p, commandType: CommandType.StoredProcedure);
            long total = p.Get<long>("@total");
            return (items, total);
        }

        /// <summary>
        /// Chi tiết 1 ThuThu theo user_id. Khớp sp_user_detail.
        /// Trả về null nếu không tìm thấy hoặc không phải ThuThu.
        /// </summary>
        public async Task<StaffDto?> ChiTietThuThuAsync(Guid userId)
        {
            try
            {
                return await _db.QueryFirstOrDefaultAsync<StaffDto>("sp_user_detail", new { user_id = userId });
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 50404 || ex.Number == 50403)
            {
                // 50404 = không tìm thấy, 50403 = không phải ThuThu
                return null;
            }
        }

        /// <summary>
        /// Tạo tài khoản ThuThu mới. Khớp sp_user_create_thuthu.
        /// matkhauHash là BCrypt hash do C# tạo sẵn.
        /// Trả về user_id mới được tạo.
        /// </summary>
        public async Task<Guid> TaoThuThuAsync(string hoten, string email, string taikhoan, string matkhauHash, string? imageUrl)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@hoten", hoten, DbType.String);
            p.Add("@email", email, DbType.String);
            p.Add("@taikhoan", taikhoan, DbType.String);
            p.Add("@matkhau_hash", matkhauHash, DbType.String);
            p.Add("@image_url", imageUrl, DbType.String);
            p.Add("@new_user_id", dbType: DbType.Guid, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("sp_user_create_thuthu", p, commandType: CommandType.StoredProcedure);
            return p.Get<Guid>("@new_user_id");
        }

        /// <summary>
        /// Cập nhật thông tin ThuThu. Khớp sp_user_update_thuthu.
        /// matkhauHash = null → giữ nguyên mật khẩu cũ.
        /// </summary>
        public async Task CapNhatThuThuAsync(Guid userId, string? hoten, string? email, string? taikhoan, string? matkhauHash, string? imageUrl)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@user_id", userId, DbType.Guid);
            p.Add("@hoten", hoten, DbType.String);
            p.Add("@email", email, DbType.String);
            p.Add("@taikhoan", taikhoan, DbType.String);
            p.Add("@matkhau_hash", matkhauHash, DbType.String);
            p.Add("@image_url", imageUrl, DbType.String);

            await conn.ExecuteAsync("sp_user_update_thuthu", p, commandType: CommandType.StoredProcedure);
        }

        /// <summary>Xóa cứng tài khoản ThuThu. Khớp sp_user_delete_thuthu.</summary>
        public async Task XoaThuThuAsync(Guid userId)
        {
            await _db.ExecuteAsync("sp_user_delete_thuthu", new { user_id = userId });
        }

        /// <summary>Khóa tài khoản ThuThu. Khớp sp_user_lock.</summary>
        public async Task KhoaThuThuAsync(Guid userId)
        {
            await _db.ExecuteAsync("sp_user_lock", new { user_id = userId });
        }

        /// <summary>Mở khóa tài khoản ThuThu. Khớp sp_user_unlock.</summary>
        public async Task MoKhoaThuThuAsync(Guid userId)
        {
            await _db.ExecuteAsync("sp_user_unlock", new { user_id = userId });
        }
    }
}