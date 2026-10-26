using DAL.Helper;
using Dapper;
using Model.Reader;
using System.Data;

namespace DAL
{
    public interface IGoogleReaderRepository
    {
        Task<GoogleReaderLoginResult?> DangNhapBangGoogleAsync(string googleSub, string email, string hoten, string? soThe);
    }

    public class ReaderRepository : IGoogleReaderRepository
    {
        private readonly IDatabaseHelper _db;

        public ReaderRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<ReaderModel> items, long total)> danhsachbandoc(
            string? keyword, int? trangthai, int pageIndex = 1, int pageSize = 10)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@keyword",    keyword,   DbType.String);
            p.Add("@trangthai",  trangthai, DbType.Int32);
            p.Add("@page_index", pageIndex, DbType.Int32);
            p.Add("@page_size",  pageSize,  DbType.Int32);
            p.Add("@total",      dbType: DbType.Int64, direction: ParameterDirection.Output);

            var items = await conn.QueryAsync<ReaderModel>("sp_reader_getlist", p, commandType: CommandType.StoredProcedure);
            long total = p.Get<long>("@total");
            return (items, total);
        }

        public async Task<ReaderModel?> laychitietchibandoc(Guid readerId)
        {
            return await _db.QueryFirstOrDefaultAsync<ReaderModel>("sp_reader_getbyid", new { reader_id = readerId });
        }

        public async Task CapNhatHoSoAsync(Guid readerId, string hoten, string? sodienthoai, string? diachi)
        {
            await _db.ExecuteAsync("sp_reader_update_profile", new
            {
                reader_id = readerId,
                hoten,
                sodienthoai,
                diachi
            });
        }

        public async Task YeuCauKichHoatAsync(Guid readerId)
        {
            await _db.ExecuteAsync("sp_reader_activation_request", new { reader_id = readerId });
        }

        public async Task<IEnumerable<dynamic>> LayDanhSachKichHoatChoDuyetAsync()
        {
            using var conn = _db.GetConnection();
            conn.Open();

            return await conn.QueryAsync(
                "sp_reader_activation_pending",
                commandType: CommandType.StoredProcedure);
        }

        public async Task DuyetYeuCauKichHoatAsync(Guid readerId, Guid staffId)
        {
            await _db.ExecuteAsync("sp_reader_activation_approve", new
            {
                reader_id = readerId,
                staff_id = staffId
            });
        }

        public async Task TuChoiYeuCauKichHoatAsync(Guid readerId, Guid staffId)
        {
            await _db.ExecuteAsync("sp_reader_activation_reject", new
            {
                reader_id = readerId,
                staff_id = staffId
            });
        }

        /// <summary>Số cuốn bạn đọc đang mượn (chưa trả, tính cả phiếu quá hạn)</summary>
        public async Task<int> sodangmuon(Guid readerId)
        {
            return await _db.ExecuteScalarAsync<int>("sp_reader_sodangmuon", new { reader_id = readerId });
        }

        public async Task dangkythebandoc(ReaderModel model)
        {
            await _db.ExecuteAsync("sp_reader_create", new
            {
                reader_id   = model.reader_id,
                hoten       = model.hoten,
                email       = model.email,
                sodienthoai = model.sodienthoai,
                diachi      = model.diachi,
                so_the      = model.so_the,
                matkhau     = model.matkhau,
                ngayhethan  = model.ngayhethan,
                somughin    = model.somughin
            });
        }

        public async Task capnhatthongtinbandoc(ReaderModel model)
        {
            await _db.ExecuteAsync("sp_reader_update", new
            {
                reader_id   = model.reader_id,
                hoten       = model.hoten,
                sodienthoai = model.sodienthoai,
                diachi      = model.diachi,
                trangthai   = model.trangthai,
                ngaycap     = model.ngaycap,
                ngayhethan  = model.ngayhethan,
                somughin    = model.somughin
            });
        }

        public async Task giathanthu(Guid readerId, DateTime ngayhethan)
        {
            await _db.ExecuteAsync("sp_reader_renew", new
            {
                reader_id  = readerId,
                ngayhethan = ngayhethan
            });
        }

        public async Task xoadocgia(Guid readerId)
        {
            await _db.ExecuteAsync("sp_reader_delete", new { reader_id = readerId });
        }

        /// <summary>Khoá hoặc mở khoá bạn đọc (chỉ cập nhật trangthai)</summary>
        public async Task KhoaMoKhoaBandocAsync(ReaderModel model)
        {
            // Tái sử dụng sp_reader_update một cách an toàn:
            // Truyền nguyên vẹn các trường hiện tại, chỉ thay đổi trangthai.
            await _db.ExecuteAsync("sp_reader_update", new
            {
                reader_id   = model.reader_id,
                hoten       = model.hoten,
                sodienthoai = model.sodienthoai,
                diachi      = model.diachi,
                trangthai   = model.trangthai,
                ngayhethan  = model.ngayhethan,
                somughin    = model.somughin
            });
        }

        /// <summary>Đăng nhập bạn đọc qua Google OAuth</summary>
        public async Task<GoogleReaderLoginResult?> DangNhapBangGoogleAsync(
            string googleSub, string email, string hoten, string? soThe)
        {
            return await _db.QueryFirstOrDefaultAsync<GoogleReaderLoginResult>(
                "sp_google_reader_login",
                new
                {
                    google_sub = googleSub,
                    email = email,
                    hoten = hoten,
                    so_the = soThe
                });
        }
    }
}
