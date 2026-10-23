namespace BLL
{
    public class ThongBaoEmail
    {
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string NguoiNhan { get; set; } = string.Empty;
    }

    /// <summary>Hàng đợi thông báo — BLL không phụ thuộc project API.</summary>
    public interface IHangDoiThongBao
    {
        void ThemEmail(ThongBaoEmail thongBao);
    }
}
