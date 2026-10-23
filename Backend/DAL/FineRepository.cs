using DAL.Helper;
using Dapper;
using Model.Loan;
using System.Data;

namespace DAL
{
    public class FineRepository
    {
        private readonly IDatabaseHelper _db;

        public FineRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        public async Task tinhtoantienphat(Guid loanId, decimal tilephattrehan = 5000)
        {
            await _db.ExecuteAsync("sp_fine_calculate", new
            {
                loan_id          = loanId,
                tilephattrehan   = tilephattrehan
            });
        }

        public async Task<FineModel?> laytienphattheomuon(Guid loanId)
        {
            return await _db.QueryFirstOrDefaultAsync<FineModel>("sp_fine_get_by_loan", new { loan_id = loanId });
        }

        public async Task<Guid> thutienphattrehan(PaymentModel model)
        {
            using var conn = _db.GetConnection();

            var p = new DynamicParameters();
            p.Add("@fine_id", model.fine_id, DbType.Guid);
            p.Add("@sotien", model.sotien, DbType.Decimal);
            p.Add("@phuongthuc", model.phuongthuc, DbType.String);
            p.Add("@ghichu", model.ghichu, DbType.String);
            p.Add("@pay_id", dbType: DbType.Guid, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("sp_fine_payment", p, commandType: CommandType.StoredProcedure);
            return p.Get<Guid>("@pay_id");
        }
    }
}
