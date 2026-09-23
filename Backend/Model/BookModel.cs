namespace Model
{
    public class BookModel
    {
        public Guid book_id { get; set; }
        public string title { get; set; } = string.Empty;
        public string isbn { get; set; } = string.Empty;
        public string tacgia { get; set; } = string.Empty;
        public string theloai { get; set; } = string.Empty;
        public string nxb { get; set; } = string.Empty;
        public int? namxuatban { get; set; }
        public string mota { get; set; } = string.Empty;
        public string image_url { get; set; } = string.Empty;
        public int? tongsobancao { get; set; }   // NULL = không đổi số bản sao
        public int sobancaosangio { get; set; }
    }
}
