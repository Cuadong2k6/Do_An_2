using FluentAssertions;
using Model.Shared;
using Xunit;

namespace LibraryManagement.Tests
{
    /// <summary>Kiểm thử ResponseModel — lớp chuẩn hoá mọi phản hồi API.</summary>
    public class ResponseModelTests
    {
        [Fact]
        public void Ok_phai_dat_day_bo_field()
        {
            var res = ResponseModel.Ok("du-lieu", "Thành công", totalItems: 42, page: 3, pageSize: 7);

            res.success.Should().BeTrue();
            res.message.Should().Be("Thành công");
            res.DuLieu().Should().Be("du-lieu");
            res.totalItems.Should().Be(42);
            res.page.Should().Be(3);
            res.pageSize.Should().Be(7);
        }

        [Fact]
        public void Ok_mac_dinh_phai_la_phan_trang_trang_1()
        {
            var res = ResponseModel.Ok(null);

            res.success.Should().BeTrue();
            res.message.Should().Be("Thành công");
            res.totalItems.Should().Be(0);
            res.page.Should().Be(1);
            res.pageSize.Should().Be(10);
        }

        [Fact]
        public void Fail_phat_su_that_bai()
        {
            var res = ResponseModel.Fail("Có lỗi xảy ra");

            res.success.Should().BeFalse();
            res.message.Should().Be("Có lỗi xảy ra");
            res.DuLieu().Should().BeNull();
        }

        [Fact]
        public void Fail_khong_duoc_co_totalItems()
        {
            // Fail chỉ tạo response rỗng → không có thông tin phân trang (giữ nguyên 0)
            var res = ResponseModel.Fail("Lỗi");

            res.totalItems.Should().Be(0);
            res.page.Should().Be(0);
            res.pageSize.Should().Be(0);
        }
    }
}