namespace Model
{
    /// <summary>
    /// Đặt chỗ sách
    /// trangthai: 0 = Đang chờ, 1 = Đã nhận, 2 = Đã huỷ, 3 = Hết hạn
    /// </summary>
    public class ReservationModel
    {
        public Guid res_id { get; set; }
        public Guid book_id { get; set; }
        public Guid reader_id { get; set; }
        public DateTime res_date { get; set; }
        public DateTime expiry_date { get; set; }
        public int trangthai { get; set; }
        // Join fields
        public string book_title { get; set; }
        public string reader_hoten { get; set; }
    }
}
