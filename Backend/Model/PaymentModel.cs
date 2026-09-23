namespace Model
{
    public class PaymentModel
    {
        public Guid pay_id { get; set; }
        public Guid fine_id { get; set; }
        public decimal sotien { get; set; }
        public DateTime payment_date { get; set; }
        public string phuongthuc { get; set; } = string.Empty;
        public string ghichu { get; set; } = string.Empty;
    }
}
