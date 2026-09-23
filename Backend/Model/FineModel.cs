namespace Model
{
    /// <summary>
    /// Phiếu phạt trễ hạn
    /// </summary>
    public class FineModel
    {
        public Guid fine_id { get; set; }
        public Guid loan_id { get; set; }
        public int songaytre { get; set; }
        public decimal amount { get; set; }
        public bool is_paid { get; set; }
        public DateTime? ngaytao { get; set; }
        // Join fields
        public string reader_hoten { get; set; } = string.Empty;
        public string reader_so_the { get; set; } = string.Empty;
    }
}
