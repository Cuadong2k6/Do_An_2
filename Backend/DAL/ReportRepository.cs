using DAL.Helper;
using Model;

namespace DAL
{
    public class ReportRepository
    {
        private readonly IDatabaseHelper _db;

        public ReportRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<TopBorrowedBookModel>> laysachmuonnhieu(int topN = 10)
        {
            return await _db.QueryAsync<TopBorrowedBookModel>("sp_report_sachmuonnhieu", new { topN });
        }

        public async Task<IEnumerable<TonKhoTheoTheLoaiModel>> laytonkhotheoloai()
        {
            return await _db.QueryAsync<TonKhoTheoTheLoaiModel>("sp_report_tonkho");
        }

        public async Task<IEnumerable<QuaHanModel>> laybaocaoquahan()
        {
            return await _db.QueryAsync<QuaHanModel>("sp_report_quahan");
        }
    }
}
