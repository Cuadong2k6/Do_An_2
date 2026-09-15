namespace Model
{
    public class BookModel
    {
        public Guid book_id { get; set; }
        public string title { get; set; }
        public string isbn { get; set; }
        public string tacgia { get; set; }
        public string theloai { get; set; }
        public string nxb { get; set; }
        public int? namxuatban { get; set; }
        public string mota { get; set; }
        public string image_url { get; set; }
        public int tongsobancao { get; set; }
        public int sobancaosangio { get; set; }
    }
}
