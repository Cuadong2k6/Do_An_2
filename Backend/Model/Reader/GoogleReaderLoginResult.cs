namespace Model.Reader
{
    /// <summary>
    /// Kết quả từ stored procedure sp_google_reader_login.
    /// Dùng riêng cho luồng đăng nhập qua Google OAuth.
    /// </summary>
    public class GoogleReaderLoginResult
    {
        public Guid reader_id { get; set; }
        public string hoten { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string so_the { get; set; } = string.Empty;
        public int trangthai { get; set; }
        public int trangthai_onboarding { get; set; }
        public bool is_new { get; set; }
        public int status_code { get; set; }
    }
}
