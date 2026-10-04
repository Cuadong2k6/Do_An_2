using System.Text.Json;
using FluentAssertions;
using Model.Reader;
using Xunit;

namespace LibraryManagement.Tests
{
    /// <summary>
    /// Kiểm thử rò rỉ mật khẩu qua JSON (Unit 2).
    /// Mọi model trả về cho client đều phải có [JsonIgnore] trên matkhau.
    /// </summary>
    public class ModelSerializationTests
    {
        private static readonly JsonSerializerOptions Bo =
            new(JsonSerializerDefaults.Web);

        [Fact]
        public void UserModel_khong_duoc_serialize_matkhau()
        {
            var user = new UserModel
            {
                taikhoan = "admin",
                hoten    = "Quản Trị Viên",
                role     = "Admin",
                matkhau  = "e10adc3949ba59abbe56e057f20f883e",
            };

            var json = JsonSerializer.Serialize(user, Bo);

            json.Should().NotContain("matkhau");
            json.Should().NotContain("e10adc3949ba59abbe56e057f20f883e");
            json.Should().Contain("taikhoan");
        }

        [Fact]
        public void ReaderModel_khong_duoc_serialize_matkhau()
        {
            var reader = new ReaderModel
            {
                hoten   = "Bạn Đọc",
                email   = "bandoc1@gmail.com",
                so_the  = "BD001",
                matkhau = "$2a$12$abcdefghijklmnopqrstuv",
            };

            var json = JsonSerializer.Serialize(reader, Bo);

            json.Should().NotContain("matkhau");
            json.Should().NotContain("$2a$12$");
            json.Should().Contain("so_the");
        }

        [Fact]
        public void ReaderModel_matkhau_van_doc_duoc_noi_bo()
        {
            // [JsonIgnore] chỉ chặn serialize ra, KHÔNG ảnh hưởng Dapper mapping qua reflection
            var reader = new ReaderModel { matkhau = "hash-noi-bo" };

            reader.matkhau.Should().Be("hash-noi-bo");
        }
    }
}