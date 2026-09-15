namespace Model
{
    /// <summary>
    /// Thẻ bạn đọc
    /// trangthai: 0 = Hoạt động, 1 = Hết hạn, 2 = Bị khoá
    /// </summary>
    public class ReaderModel
    {
        public Guid reader_id { get; set; }
        public string hoten { get; set; }
        public string email { get; set; }
        public string sodienthoai { get; set; }
        public string diachi { get; set; }
        public string so_the { get; set; }
        public DateTime? ngaycap { get; set; }
        public DateTime? ngayhethan { get; set; }
        public int trangthai { get; set; }
        public int somughin { get; set; }
    }
}
