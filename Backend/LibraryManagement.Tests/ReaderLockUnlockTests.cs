using BLL;
using DAL;
using DAL.Helper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Model.Reader;
using Moq;
using Xunit;

namespace LibraryManagement.Tests
{
    /// <summary>
    /// Kiểm thử ReaderService.KhoaBandocAsync / MoKhoaBandocAsync.
    /// Mock IDatabaseHelper → không cần DB thật.
    /// </summary>
    public class ReaderLockUnlockTests
    {
        private static (ReaderService svc, Mock<IDatabaseHelper> db) TaoService(ReaderModel? reader)
        {
            var db = new Mock<IDatabaseHelper>();

            // Mock sp_reader_getbyid (dùng bởi laychitietchibandoc)
            db.Setup(x => x.QueryFirstOrDefaultAsync<ReaderModel>("sp_reader_getbyid", It.IsAny<object?>()))
              .ReturnsAsync(reader);

            // Mock sp_reader_update (dùng bởi KhoaMoKhoaBandocAsync)
            db.Setup(x => x.ExecuteAsync("sp_reader_update", It.IsAny<object?>()))
              .ReturnsAsync(1);

            var repo = new ReaderRepository(db.Object);
            var svc = new ReaderService(repo, NullLogger<ReaderService>.Instance);

            return (svc, db);
        }

        private static ReaderModel BanDocDangHoatDong() => new()
        {
            reader_id   = TestHelper.READER_ID,
            hoten       = "Nguyen Van A",
            email       = "a@test.com",
            sodienthoai = "0901234567",
            diachi      = "Ha Noi",
            so_the      = "BD001",
            trangthai   = 0,
            ngayhethan  = DateTime.Now.AddMonths(6),
            somughin    = 3
        };

        private static ReaderModel BanDocDangBiKhoa() => new()
        {
            reader_id   = TestHelper.READER_ID,
            hoten       = "Nguyen Van A",
            email       = "a@test.com",
            sodienthoai = "0901234567",
            diachi      = "Ha Noi",
            so_the      = "BD001",
            trangthai   = 2,
            ngayhethan  = DateTime.Now.AddMonths(6),
            somughin    = 3
        };

        [Fact]
        public async Task KhoaBandoc_BanDocTonTai_ThanhCong()
        {
            var reader = BanDocDangHoatDong();
            var (svc, db) = TaoService(reader);

            var result = await svc.KhoaBandocAsync(TestHelper.READER_ID);

            result.success.Should().BeTrue();
            result.message.Should().Contain("Khoá");

            // Xác nhận sp_reader_update được gọi với trangthai = 2
            db.Verify(x => x.ExecuteAsync("sp_reader_update", It.Is<object>(o =>
                o.GetType().GetProperty("trangthai")!.GetValue(o)!.Equals(2)
            )), Times.Once);
        }

        [Fact]
        public async Task KhoaBandoc_DaBiKhoa_TraVeDaOKhongGoiSP()
        {
            var reader = BanDocDangBiKhoa();
            var (svc, db) = TaoService(reader);

            var result = await svc.KhoaBandocAsync(TestHelper.READER_ID);

            result.success.Should().BeTrue();
            result.message.Should().Contain("đã ở trạng thái bị khoá");

            // Không gọi update vì đã ở trạng thái mong muốn
            db.Verify(x => x.ExecuteAsync("sp_reader_update", It.IsAny<object?>()), Times.Never);
        }

        [Fact]
        public async Task MoKhoaBandoc_BanDocDangBiKhoa_ThanhCong()
        {
            var reader = BanDocDangBiKhoa();
            var (svc, db) = TaoService(reader);

            var result = await svc.MoKhoaBandocAsync(TestHelper.READER_ID);

            result.success.Should().BeTrue();
            result.message.Should().Contain("Mở khoá");

            // Xác nhận sp_reader_update được gọi với trangthai = 0
            db.Verify(x => x.ExecuteAsync("sp_reader_update", It.Is<object>(o =>
                o.GetType().GetProperty("trangthai")!.GetValue(o)!.Equals(0)
            )), Times.Once);
        }

        [Fact]
        public async Task MoKhoaBandoc_DangHoatDong_TraVeDaOKhongGoiSP()
        {
            var reader = BanDocDangHoatDong();
            var (svc, db) = TaoService(reader);

            var result = await svc.MoKhoaBandocAsync(TestHelper.READER_ID);

            result.success.Should().BeTrue();
            result.message.Should().Contain("đã ở trạng thái hoạt động");

            db.Verify(x => x.ExecuteAsync("sp_reader_update", It.IsAny<object?>()), Times.Never);
        }

        [Fact]
        public async Task KhoaBandoc_KhongTimThay_Fail()
        {
            var (svc, _) = TaoService(null);

            var result = await svc.KhoaBandocAsync(Guid.NewGuid());

            result.success.Should().BeFalse();
            result.message.Should().Contain("Không tìm thấy");
        }

        [Fact]
        public async Task MoKhoaBandoc_KhongTimThay_Fail()
        {
            var (svc, _) = TaoService(null);

            var result = await svc.MoKhoaBandocAsync(Guid.NewGuid());

            result.success.Should().BeFalse();
            result.message.Should().Contain("Không tìm thấy");
        }

        [Fact]
        public async Task KhoaBandoc_ChiThayDoiTrangThai_GiuNguyenCacTruongKhac()
        {
            var reader = BanDocDangHoatDong();
            var (svc, db) = TaoService(reader);

            await svc.KhoaBandocAsync(TestHelper.READER_ID);

            // Xác nhận các trường khác được truyền nguyên vẹn qua SP
            db.Verify(x => x.ExecuteAsync("sp_reader_update", It.Is<object>(o =>
                o.GetType().GetProperty("hoten")!.GetValue(o)!.Equals("Nguyen Van A") &&
                o.GetType().GetProperty("sodienthoai")!.GetValue(o)!.Equals("0901234567") &&
                o.GetType().GetProperty("diachi")!.GetValue(o)!.Equals("Ha Noi")
            )), Times.Once);
        }
    }
}
