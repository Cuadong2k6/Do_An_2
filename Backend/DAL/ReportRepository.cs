using DAL.Helper;
using Dapper;
using Model.Report;
using System.Data;

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

        public async Task<DashboardStatsModel> laythongkedashboard()
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var stats = new DashboardStatsModel();

            // Tổng bản sao sách
            stats.tong_sach = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM copies");

            // Độc giả đang hoạt động
            stats.doc_gia_dang_ky = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM readers WHERE trangthai = 0");

            // Quá hạn chưa trả (trangthai = 0, due_date < GETDATE())
            stats.qua_han_tra = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM loans WHERE trangthai = 0 AND due_date < GETDATE()");

            // 10 giao dịch gần nhất
            var sql = @"SELECT TOP 10
                             CAST(l.loan_id AS NVARCHAR(36)) AS ma_giao_dich,
                             r.hoten AS doc_gia,
                             l.loan_date AS ngay_muon,
                             CASE
                                 WHEN l.trangthai = 1 THEN N'Đã trả'
                                 WHEN l.trangthai = 0 AND l.due_date < GETDATE() THEN N'Quá hạn'
                                 ELSE N'Đang mượn'
                             END AS trang_thai
                         FROM loans l
                         INNER JOIN readers r ON l.reader_id = r.reader_id
                         ORDER BY l.loan_date DESC";
            stats.giao_dich_gan_day = await conn.QueryAsync<RecentLoanModel>(sql);

            return stats;
        }
    }
}