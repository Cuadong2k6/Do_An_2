namespace Model
{
    /// <summary>
    /// Bản sao vật lý của sách
    /// status: 0 = Sẵn có, 1 = Đã mượn, 2 = Hỏng
    /// </summary>
    public class CopyModel
    {
        public Guid copy_id { get; set; }
        public Guid book_id { get; set; }
        public int? shelf_id { get; set; }
        public int status { get; set; }
        public string mabancao { get; set; }
        public DateTime? ngaynhap { get; set; }
        // Join fields
        public string book_title { get; set; }
        public string shelf_location { get; set; }
    }
}
