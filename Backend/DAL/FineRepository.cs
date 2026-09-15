using DAL.Helper;
using Model;

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

        public async Task thutienphattrehan(PaymentModel model)
        {
            await _db.ExecuteAsync("sp_fine_payment", new
            {
                fine_id     = model.fine_id,
                sotien      = model.sotien,
                phuongthuc  = model.phuongthuc,
                ghichu      = model.ghichu
            });
        }
    }
}
