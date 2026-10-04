using DAL.Helper;
using Dapper;
using Model.Nhatky;
using System.Data;

namespace DAL
{
    /// <summary>Truy vấn nhật ký thay đổi. Bảng nhatky do trigger ghi tự động,
    /// không có SP nào ghi tay vào bảng này.</summary>
    public class NhatkyRepository
    {
        private readonly IDatabaseHelper _db;

        public NhatkyRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<NhatkyModel> items, long total)> danhsachnhatky(
            string? keyword, string? bang, string? hanhdong, int pageIndex = 1, int pageSize = 10)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@keyword",    keyword,   DbType.String);
            p.Add("@bang",       bang,      DbType.String);
            p.Add("@hanhdong",   hanhdong,  DbType.String);
            p.Add("@page_index", pageIndex, DbType.Int32);
            p.Add("@page_size",  pageSize,  DbType.Int32);
            p.Add("@total",      dbType: DbType.Int64, direction: ParameterDirection.Output);

            var items = await conn.QueryAsync<NhatkyModel>("sp_nhatky_getlist", p, commandType: CommandType.StoredProcedure);
            long total = p.Get<long>("@total");
            return (items, total);
        }
    }
}