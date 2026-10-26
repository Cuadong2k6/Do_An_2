using BLL;
using DAL;
using DAL.Helper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LibraryManagement.Tests
{
    /// <summary>
    /// Kiểm thử luồng activation ở lớp ReaderService:
    /// chỉ xác nhận service gọi đúng SP qua repository và trả thông điệp kỳ vọng.
    /// Không cần DB thật.
    /// </summary>
    public class ReaderActivationFlowTests
    {
        private static (ReaderService svc, Mock<IDatabaseHelper> db) TaoService()
        {
            var db = new Mock<IDatabaseHelper>();
            db.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>()))
              .ReturnsAsync(1);

            var repo = new ReaderRepository(db.Object);
            var svc = new ReaderService(repo, NullLogger<ReaderService>.Instance);
            return (svc, db);
        }

        [Fact]
        public async Task YeuCauKichHoat_GoiDungStoredProcedure()
        {
            var (svc, db) = TaoService();
            var readerId = Guid.NewGuid();

            var res = await svc.yeucaukichhoat(readerId);

            res.success.Should().BeTrue();
            db.Verify(x => x.ExecuteAsync(
                    "sp_reader_activation_request",
                    It.Is<object?>(p => LayGuid(p, "reader_id") == readerId)),
                Times.Once);
        }

        [Fact]
        public async Task DuyetKichHoat_GoiDungStoredProcedure()
        {
            var (svc, db) = TaoService();
            var readerId = Guid.NewGuid();
            var staffId = Guid.NewGuid();

            var res = await svc.duyetkichhoat(readerId, staffId);

            res.success.Should().BeTrue();
            db.Verify(x => x.ExecuteAsync(
                    "sp_reader_activation_approve",
                    It.Is<object?>(p =>
                        LayGuid(p, "reader_id") == readerId &&
                        LayGuid(p, "staff_id") == staffId)),
                Times.Once);
        }

        [Fact]
        public async Task TuChoiKichHoat_GoiDungStoredProcedure()
        {
            var (svc, db) = TaoService();
            var readerId = Guid.NewGuid();
            var staffId = Guid.NewGuid();

            var res = await svc.tuchoikichhoat(readerId, staffId);

            res.success.Should().BeTrue();
            db.Verify(x => x.ExecuteAsync(
                    "sp_reader_activation_reject",
                    It.Is<object?>(p =>
                        LayGuid(p, "reader_id") == readerId &&
                        LayGuid(p, "staff_id") == staffId)),
                Times.Once);
        }

        private static Guid? LayGuid(object? anon, string tenThuocTinh)
        {
            if (anon == null) return null;
            var prop = anon.GetType().GetProperty(tenThuocTinh);
            var value = prop?.GetValue(anon);
            if (value is Guid g) return g;
            return null;
        }
    }
}
