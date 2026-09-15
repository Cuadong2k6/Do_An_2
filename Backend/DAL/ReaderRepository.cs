using Dapper;
using DAL.Helper;
using Model;
using System.Data;

namespace DAL
{
    public class ReaderRepository
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
            p.Add("@keyword",    keyword,    DbType.String);
            p.Add("@trangthai",  trangthai,  DbType.Int32);
            p.Add("@page_index", pageIndex,  DbType.Int32);
            p.Add("@page_size",  pageSize,   DbType.Int32);
            p.Add("@total",      dbType: DbType.Int64, direction: ParameterDirection.Output);

            var items = await conn.QueryAsync<ReaderModel>("sp_reader_getlist", p, commandType: CommandType.StoredProcedure);
            long total = p.Get<long>("@total");
            return (items, total);
        }

        public async Task<ReaderModel?> laychitietchibandoc(Guid readerId)
        {
            return await _db.QueryFirstOrDefaultAsync<ReaderModel>("sp_reader_getbyid", new { reader_id = readerId });
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
                matkhau     = model.email, // Mặc định = email (hash ở BLL)
                ngayhethan  = model.ngayhethan
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
                trangthai   = model.trangthai
            });
        }
    }
}
