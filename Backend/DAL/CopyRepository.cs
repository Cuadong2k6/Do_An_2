using Dapper;
using DAL.Helper;
using Model;
using System.Data;

namespace DAL
{
    public class CopyRepository
    {
        private readonly IDatabaseHelper _db;

        public CopyRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<CopyModel> items, long total)> danhsachbansao(
            Guid? bookId, int? status, int pageIndex = 1, int pageSize = 10)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@book_id",    bookId,    DbType.Guid);
            p.Add("@status",     status,    DbType.Int32);
            p.Add("@page_index", pageIndex, DbType.Int32);
            p.Add("@page_size",  pageSize,  DbType.Int32);
            p.Add("@total",      dbType: DbType.Int64, direction: ParameterDirection.Output);

            var items = await conn.QueryAsync<CopyModel>("sp_copy_getlist", p, commandType: CommandType.StoredProcedure);
            long total = p.Get<long>("@total");
            return (items, total);
        }

        public async Task<CopyModel?> laychitietbansao(Guid copyId)
        {
            return await _db.QueryFirstOrDefaultAsync<CopyModel>("sp_copy_getbyid", new { copy_id = copyId });
        }

        public async Task thembansao(CopyModel model)
        {
            await _db.ExecuteAsync("sp_copy_create", new
            {
                copy_id  = model.copy_id,
                book_id  = model.book_id,
                mabancao = model.mabancao,
                shelf_id = model.shelf_id
            });
        }

        public async Task capnhatbansao(CopyModel model)
        {
            await _db.ExecuteAsync("sp_copy_update", new
            {
                copy_id  = model.copy_id,
                shelf_id = model.shelf_id,
                status   = model.status
            });
        }
    }
}
