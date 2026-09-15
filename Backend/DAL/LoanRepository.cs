using Dapper;
using DAL.Helper;
using Model;
using System.Data;

namespace DAL
{
    public class LoanRepository
    {
        private readonly IDatabaseHelper _db;

        public LoanRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        public async Task taomoimuontra(LoanModel model)
        {
            await _db.ExecuteAsync("sp_loan_create", new
            {
                loan_id          = model.loan_id,
                reader_id        = model.reader_id,
                due_date         = model.due_date,
                listjson_chitiet = model.listjson_chitiet
            });
        }

        public async Task trasach(Guid loanId)
        {
            await _db.ExecuteAsync("sp_loan_return", new
            {
                loan_id     = loanId,
                return_date = DateTime.Now
            });
        }

        public async Task<IEnumerable<LoanModel>> laydanhsachsachquahan()
        {
            return await _db.QueryAsync<LoanModel>("sp_loan_get_overdue", new { current_date = DateTime.Now });
        }

        public async Task<(IEnumerable<LoanModel> items, long total)> laydanhsachmuontratheobandoc(
            Guid readerId, int pageIndex = 1, int pageSize = 10)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@reader_id",  readerId,  DbType.Guid);
            p.Add("@page_index", pageIndex, DbType.Int32);
            p.Add("@page_size",  pageSize,  DbType.Int32);
            p.Add("@total",      dbType: DbType.Int64, direction: ParameterDirection.Output);

            var items = await conn.QueryAsync<LoanModel>("sp_loan_get_by_reader", p, commandType: CommandType.StoredProcedure);
            long total = p.Get<long>("@total");
            return (items, total);
        }
    }
}
