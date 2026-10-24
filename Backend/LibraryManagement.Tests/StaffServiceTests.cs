using BLL;
using DAL;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Model.Staff;
using Moq;
using Xunit;

namespace LibraryManagement.Tests
{
    /// <summary>
    /// Kiểm thử StaffService — quản lý tài khoản ThuThu.
    /// Mock IStaffRepository trực tiếp, không phụ thuộc database hay IDatabaseHelper.
    /// </summary>
    public class StaffServiceTests
    {
        private static readonly Guid THU_THU_ID = new("55555555-5555-5555-5555-555555555555");

        private static StaffService TaoService(IStaffRepository repo)
        {
            return new StaffService(repo, NullLogger<StaffService>.Instance);
        }

        [Fact]
        public async Task DanhSachThuThu_TraVePhanTrang()
        {
            var mockRepo = new Mock<IStaffRepository>();
            mockRepo.Setup(x => x.DanhSachThuThuAsync(1, 10))
                .ReturnsAsync((Enumerable.Empty<StaffDto>(), 0L));

            var svc = TaoService(mockRepo.Object);
            var result = await svc.DanhSachThuThuAsync(1, 10);

            result.success.Should().BeTrue();
            mockRepo.Verify(x => x.DanhSachThuThuAsync(1, 10), Times.Once);
        }

        [Fact]
        public async Task TaoThuThu_HashMatKhau_BCrypt()
        {
            // Arrange
            var mockRepo = new Mock<IStaffRepository>();
            string? capturedHash = null;

            mockRepo.Setup(x => x.TaoThuThuAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>()))
                .Callback<string, string, string, string, string?>((hoten, email, taikhoan, matkhauHash, imageUrl) =>
                {
                    capturedHash = matkhauHash;
                })
                .ReturnsAsync(THU_THU_ID);

            var svc = TaoService(mockRepo.Object);
            var req = new TaoThuThuRequest
            {
                hoten = "Test User",
                email = "test@example.com",
                taikhoan = "testuser",
                matkhau = "Password123!"
            };

            // Act
            var result = await svc.TaoThuThuAsync(req);

            // Assert — không try/catch swallow, để exception tự fail test
            result.success.Should().BeTrue("TaoThuThuAsync phải trả về success=true khi hợp lệ");
            capturedHash.Should().NotBeNullOrWhiteSpace("hash mật khẩu phải được truyền xuống repository");

            // Verify BCrypt: prefix $2a$ hoặc $2b$, work factor 12
            (capturedHash!.StartsWith("$2a$12$") || capturedHash.StartsWith("$2b$12$"))
                .Should().BeTrue("BCrypt hash phải dùng prefix $2a$ hoặc $2b$ và work factor 12");

            // Verify hash khớp với plaintext gốc
            var verified = BCrypt.Net.BCrypt.Verify(req.matkhau, capturedHash);
            verified.Should().BeTrue("BCrypt.Verify phải xác thực đúng plaintext với hash đã tạo");

            mockRepo.Verify(x => x.TaoThuThuAsync(
                req.hoten,
                req.email,
                req.taikhoan,
                It.IsAny<string>(),
                req.image_url), Times.Once);
        }
    }
}