namespace Model.Report {
    /// <summary>Báo cáo: sách mượn nhiều nhất</summary>
    public class TopBorrowedBookModel
    {
        public Guid book_id { get; set; }
        public string title { get; set; } = string.Empty;
        public string tacgia { get; set; } = string.Empty;
        public string theloai { get; set; } = string.Empty;
        public int sotluongmuon { get; set; }
    }

    /// <summary>Báo cáo: tồn kho theo thể loại</summary>
    public class TonKhoTheoTheLoaiModel
    {
        public string theloai { get; set; } = string.Empty;
        public int sodausach { get; set; }
        public int tongbancao { get; set; }
        public int bansangio { get; set; }
        public int danmuon { get; set; }
    }

    /// <summary>Báo cáo: sách quá hạn chưa trả</summary>
    public class QuaHanModel
    {
        public Guid loan_id { get; set; }
        public DateTime loan_date { get; set; }
        public DateTime due_date { get; set; }
        public string reader_hoten { get; set; } = string.Empty;
        public string reader_so_the { get; set; } = string.Empty;
        public string sodienthoai { get; set; } = string.Empty;
        public int songaytre { get; set; }
        public decimal sotienphat { get; set; }
        public bool is_paid { get; set; }
    }

    /// <summary>Thống kê dashboard</summary>
    public class DashboardStatsModel
    {
        public int tong_sach { get; set; }
        public int doc_gia_dang_ky { get; set; }
        public int qua_han_tra { get; set; }
        public IEnumerable<RecentLoanModel> giao_dich_gan_day { get; set; } = Array.Empty<RecentLoanModel>();
    }

    /// <summary>Giao dịch gần đây cho dashboard</summary>
    public class RecentLoanModel
    {
        public string ma_giao_dich { get; set; } = string.Empty;
        public string doc_gia { get; set; } = string.Empty;
        public DateTime ngay_muon { get; set; }
        public string trang_thai { get; set; } = string.Empty;
    }
}