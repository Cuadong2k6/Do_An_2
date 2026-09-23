using Dapper;
using DAL.Helper;
using Model;
using System.Data;

namespace DAL
{
    public class ShelfRepository
    {
        private readonly IDatabaseHelper _db;

        public ShelfRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<ShelfModel> items, long total)> danhsachke(
            string? keyword, int pageIndex = 1, int pageSize = 10)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@keyword",    keyword,   DbType.String);
            p.Add("@page_index", pageIndex, DbType.Int32);
            p.Add("@page_size",  pageSize,  DbType.Int32);
            p.Add("@total",      dbType: DbType.Int64, direction: ParameterDirection.Output);

            var items = await conn.QueryAsync<ShelfModel>("sp_shelf_getlist", p, commandType: CommandType.StoredProcedure);
            long total = p.Get<long>("@total");
            return (items, total);
        }

        public async Task themke(ShelfModel model)
        {
            await _db.ExecuteAsync("sp_shelf_create", new
            {
                location_code = model.location_code,
                mota          = model.mota
            });
        }

        public async Task capnhatke(int shelfId, ShelfModel model)
        {
            await _db.ExecuteAsync("sp_shelf_update", new
            {
                shelf_id      = shelfId,
                location_code = model.location_code,
                mota          = model.mota
            });
        }
    }
}
