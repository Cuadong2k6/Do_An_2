namespace Model
{
    /// <summary>
    /// Thẻ bạn đọc
    /// trangthai: 0 = Hoạt động, 1 = Hết hạn, 2 = Bị khoá
    /// </summary>
    public class ReaderModel
    {
        public Guid reader_id { get; set; }
        public string hoten { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string matkhau { get; set; } = string.Empty;
        public string sodienthoai { get; set; } = string.Empty;
        public string diachi { get; set; } = string.Empty;
        public string so_the { get; set; } = string.Empty;
        public DateTime? ngaycap { get; set; }
        public DateTime? ngayhethan { get; set; }
        public int trangthai { get; set; }
        public int somughin { get; set; }
    }
}
