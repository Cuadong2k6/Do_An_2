using Xunit;

namespace LibraryManagement.Tests
{
    /// <summary>Các test dùng chung cho toàn bộ project test.</summary>
    public class TestHelper
    {
        /// <summary>
        /// Chuỗi hash MD5 của mật khẩu "123456" — dùng để mô phỏng dữ liệu seed cũ.
        /// </summary>
        public const string HASH_MD5_123456 = "e10adc3949ba59abbe56e057f20f883e";

        public const string SECRET_KEY = "Day-la-khoa-bi-mat-cho-unit-test-01-it-nhat-32-ky-tu!!";

        public static readonly Guid READER_ID = new("11111111-1111-1111-1111-111111111111");
        public static readonly Guid USER_ID   = new("22222222-2222-2222-2222-222222222222");
        public static readonly Guid COPY_1    = new("33333333-3333-3333-3333-333333333331");
        public static readonly Guid COPY_2    = new("33333333-3333-3333-3333-333333333332");
        public static readonly Guid BOOK_1    = new("44444444-4444-4444-4444-444444444441");
        public static readonly Guid BOOK_2    = new("44444444-4444-4444-4444-444444444442");

        /// <summary>Tạo hash BCrypt. workFactor thấp để test chạy nhanh.</summary>
        public static string Bcrypt(string matkhau, int workFactor = 4)
            => BCrypt.Net.BCrypt.HashPassword(matkhau, workFactor);
    }

    /// <summary>
    /// ResponseModel.data kiểu dynamic? — dùng extension này để ép về object?
    /// tránh cảnh báo CS8602 khi test gọi FluentAssertions.
    /// </summary>
    public static class ResponseModelExtensions
    {
        public static object? DuLieu(this Model.Shared.ResponseModel res) => (object?)res.data;
    }
}