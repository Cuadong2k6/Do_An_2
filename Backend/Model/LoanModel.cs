namespace Model
{
    /// <summary>
    /// Phiếu mượn sách
    /// trangthai: 0 = Đang mượn, 1 = Đã trả, 2 = Quá hạn
    /// </summary>
    public class LoanModel
    {
        public Guid loan_id { get; set; }
        public Guid reader_id { get; set; }
        public DateTime loan_date { get; set; }
        public DateTime due_date { get; set; }
        public DateTime? return_date { get; set; }
        public int trangthai { get; set; }
        public string ghichu { get; set; } = string.Empty;
        // JSON danh sách copy_id mượn (truyền xuống SP)
        public string listjson_chitiet { get; set; } = string.Empty;
        // Join fields
        public string reader_hoten { get; set; } = string.Empty;
        public string reader_so_the { get; set; } = string.Empty;
    }
}
