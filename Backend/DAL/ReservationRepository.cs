using Dapper;
using DAL.Helper;
using Model;
using System.Data;

namespace DAL
{
    public class ReservationRepository
    {
        private readonly IDatabaseHelper _db;

        public ReservationRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        public async Task taodatcho(ReservationModel model)
        {
            await _db.ExecuteAsync("sp_reservation_create", new
            {
                res_id      = model.res_id,
                book_id     = model.book_id,
                reader_id   = model.reader_id,
                expiry_date = model.expiry_date
            });
        }

        public async Task<(IEnumerable<ReservationModel> items, long total)> danhsachdatcho(
            int? trangthai, int pageIndex = 1, int pageSize = 10)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@trangthai", trangthai, DbType.Int32);
            p.Add("@page_index", pageIndex, DbType.Int32);
            p.Add("@page_size",  pageSize,  DbType.Int32);
            p.Add("@total",      dbType: DbType.Int64, direction: ParameterDirection.Output);

            var items = await conn.QueryAsync<ReservationModel>("sp_reservation_getlist", p, commandType: CommandType.StoredProcedure);
            long total = p.Get<long>("@total");
            return (items, total);
        }

        public async Task<(IEnumerable<ReservationModel> items, long total)> danhsachdatchocuabandoc(
            Guid readerId, int pageIndex = 1, int pageSize = 10)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@reader_id",  readerId,  DbType.Guid);
            p.Add("@page_index", pageIndex, DbType.Int32);
            p.Add("@page_size",  pageSize,  DbType.Int32);
            p.Add("@total",      dbType: DbType.Int64, direction: ParameterDirection.Output);

            var items = await conn.QueryAsync<ReservationModel>("sp_reservation_get_by_reader", p, commandType: CommandType.StoredProcedure);
            long total = p.Get<long>("@total");
            return (items, total);
        }

        public async Task nhancho(Guid resId)
        {
            await _db.ExecuteAsync("sp_reservation_receive", new { res_id = resId });
        }

        public async Task huycho(Guid resId)
        {
            await _db.ExecuteAsync("sp_reservation_cancel", new { res_id = resId });
        }

        public async Task capnhatchohethan()
        {
            await _db.ExecuteAsync("sp_reservation_expire");
        }
    }
}
