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
    /// Kiểm thử LoanService.taophieumuon — các quy tắc validate phía BLL (trước khi gọi SP).
    /// Repository là class thật, chỉ mock IDatabaseHelper bên dưới → không cần DB.
    /// </summary>
    public class LoanServiceTests
    {
        private static (LoanService svc, Mock<IDatabaseHelper> db) TaoService(ReaderModel? reader)
        {
            var db = new Mock<IDatabaseHelper>();
            db.Setup(x => x.QueryFirstOrDefaultAsync<ReaderModel>("sp_reader_getbyid", It.IsAny<object?>()))
              .ReturnsAsync(reader);
            db.Setup(x => x.ExecuteAsync("sp_loan_create", It.IsAny<object?>()))
              .ReturnsAsync(1);
            db.Setup(x => x.ExecuteAsync("sp_loan_create_auto", It.IsAny<object?>()))
              .ReturnsAsync(1);

            var svc = new LoanService(
                new LoanRepository(db.Object),
                new FineRepository(db.Object),
                new ReaderRepository(db.Object),
                NullLogger<LoanService>.Instance);

            return (svc, db);
        }

        private static ReaderModel BanDocHopLe(int somughin = 3) => new()
        {
            reader_id  = TestHelper.READER_ID,
            hoten      = "Bạn Đọc Test",
            so_the     = "BD001",
            trangthai  = 0,
            somughin   = somughin,
            ngayhethan = DateTime.Now.AddMonths(6),
        };

        private static List<Guid> HaiBanSao() => new() { TestHelper.COPY_1, TestHelper.COPY_2 };

        private static List<Guid> HaiSach() => new() { TestHelper.BOOK_1, TestHelper.BOOK_2 };

        // ---------- Ràng buộc đầu vào ----------

        [Fact]
        public async Task Taophieumuon_danh_sach_rong_thi_that_bai()
        {
            var (svc, db) = TaoService(BanDocHopLe());

            var res = await svc.taophieumuon(TestHelper.READER_ID, new List<Guid>(), DateTime.Now.AddDays(7));

            res.success.Should().BeFalse();
            res.message.Should().Be("Danh sách sách mượn không được rỗng.");
            db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>()), Times.Never,
                "không được gọi SP khi dữ liệu đầu vào không hợp lệ");
        }

        [Fact]
        public async Task Taophieumuon_danh_sach_null_thi_that_bai()
        {
            var (svc, _) = TaoService(BanDocHopLe());

            var res = await svc.taophieumuon(TestHelper.READER_ID, null!, DateTime.Now.AddDays(7));

            res.success.Should().BeFalse();
            res.message.Should().Be("Danh sách sách mượn không được rỗng.");
        }

        // ---------- Ràng buộc bạn đọc ----------

        [Fact]
        public async Task Taophieumuon_khong_tim_thay_ban_doc_thi_that_bai()
        {
            var (svc, _) = TaoService(reader: null);

            var res = await svc.taophieumuon(Guid.NewGuid(), HaiBanSao(), DateTime.Now.AddDays(7));

            res.success.Should().BeFalse();
            res.message.Should().Be("Không tìm thấy thẻ bạn đọc.");
        }

        [Theory]
        [InlineData(1)] // Hết hạn
        [InlineData(2)] // Bị khoá
        public async Task Taophieumuon_the_ban_doc_khong_con_hieu_luc_thi_that_bai(int trangthai)
        {
            var reader = BanDocHopLe();
            reader.trangthai = trangthai;
            var (svc, _) = TaoService(reader);

            var res = await svc.taophieumuon(TestHelper.READER_ID, HaiBanSao(), DateTime.Now.AddDays(7));

            res.success.Should().BeFalse();
            res.message.Should().Be("Thẻ bạn đọc không còn hiệu lực.");
        }

        [Fact]
        public async Task Taophieumuon_the_ban_doc_da_het_han_thi_that_bai()
        {
            var reader = BanDocHopLe();
            reader.ngayhethan = DateTime.Now.AddDays(-1);
            var (svc, _) = TaoService(reader);

            var res = await svc.taophieumuon(TestHelper.READER_ID, HaiBanSao(), DateTime.Now.AddDays(7));

            res.success.Should().BeFalse();
            res.message.Should().Be("Thẻ bạn đọc đã hết hạn.");
        }

        [Fact]
        public async Task Taophieumuon_vuot_gioi_han_so_sach_muon_thi_that_bai()
        {
            var (svc, _) = TaoService(BanDocHopLe(somughin: 1));

            var res = await svc.taophieumuon(TestHelper.READER_ID, HaiBanSao(), DateTime.Now.AddDays(7));

            res.success.Should().BeFalse();
            res.message.Should().Be("Vượt giới hạn số sách mượn. Tối đa: 1 cuốn.");
        }

        [Fact]
        public async Task Taophieumuon_dung_gioi_han_thi_duoc_phep()
        {
            var (svc, db) = TaoService(BanDocHopLe(somughin: 2));

            var res = await svc.taophieumuon(TestHelper.READER_ID, HaiBanSao(), DateTime.Now.AddDays(7));

            res.success.Should().BeTrue();
            db.Verify(x => x.ExecuteAsync("sp_loan_create", It.IsAny<object?>()), Times.Once);
        }

        // ---------- Đóng gói JSON gửi xuống SP ----------

        [Fact]
        public async Task Taophieumuon_goi_sp_voi_danh_sach_copy_id_dung_dinh_dang()
        {
            var (svc, db) = TaoService(BanDocHopLe());
            object? pThamSo = null;
            db.Setup(x => x.ExecuteAsync("sp_loan_create", It.IsAny<object?>()))
              .Callback<string, object?>((_, p) => pThamSo = p)
              .ReturnsAsync(1);

            var res = await svc.taophieumuon(TestHelper.READER_ID, HaiBanSao(), DateTime.Now.AddDays(7));

            res.success.Should().BeTrue();

            // LoanRepository truyền anonymous object xuống IDatabaseHelper → đọc bằng reflection
            pThamSo.Should().NotBeNull();
            var thamSo = pThamSo!;
            var listjson = thamSo.GetType()
                                  .GetProperty("listjson_chitiet")!.GetValue(thamSo)?.ToString();

            listjson.Should().Be($"[{{\"copy_id\":\"{TestHelper.COPY_1}\"}},{{\"copy_id\":\"{TestHelper.COPY_2}\"}}]");
        }

        [Fact]
        public async Task Taophieumuon_that_bai_thi_tra_loi_tu_SP()
        {
            var (svc, db) = TaoService(BanDocHopLe());
            db.Setup(x => x.ExecuteAsync("sp_loan_create", It.IsAny<object?>()))
              .ThrowsAsync(new InvalidOperationException("Thẻ bạn đọc không hợp lệ hoặc đã hết hạn."));

            var res = await svc.taophieumuon(TestHelper.READER_ID, HaiBanSao(), DateTime.Now.AddDays(7));

            res.success.Should().BeFalse();
            res.message.Should().Be("Thẻ bạn đọc không hợp lệ hoặc đã hết hạn.");
        }

        // ==============================================================
        // Giỏ hàng bạn đọc — LoanService.taophieumuongio
        // Nhận danh sách book_id, SP tự gán bản sao đang rảnh.
        // ==============================================================

        [Fact]
        public async Task Taophieumuongio_gio_rong_thi_that_bai()
        {
            var (svc, db) = TaoService(BanDocHopLe());

            var res = await svc.taophieumuongio(TestHelper.READER_ID, new List<Guid>(), DateTime.Now.AddDays(14));

            res.success.Should().BeFalse();
            res.message.Should().Be("Giỏ hàng không có sách nào để mượn.");
            db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>()), Times.Never,
                "không được gọi SP khi giỏ hàng rỗng");
        }

        [Fact]
        public async Task Taophieumuongio_gio_null_thi_that_bai()
        {
            var (svc, _) = TaoService(BanDocHopLe());

            var res = await svc.taophieumuongio(TestHelper.READER_ID, null!, DateTime.Now.AddDays(14));

            res.success.Should().BeFalse();
            res.message.Should().Be("Giỏ hàng không có sách nào để mượn.");
        }

        [Fact]
        public async Task Taophieumuongio_khong_tim_thay_ban_doc_thi_that_bai()
        {
            var (svc, _) = TaoService(reader: null);

            var res = await svc.taophieumuongio(Guid.NewGuid(), HaiSach(), DateTime.Now.AddDays(14));

            res.success.Should().BeFalse();
            res.message.Should().Be("Không tìm thấy thẻ bạn đọc.");
        }

        [Theory]
        [InlineData(1)] // Hết hạn
        [InlineData(2)] // Bị khoá
        public async Task Taophieumuongio_the_ban_doc_khong_con_hieu_luc_thi_that_bai(int trangthai)
        {
            var reader = BanDocHopLe();
            reader.trangthai = trangthai;
            var (svc, _) = TaoService(reader);

            var res = await svc.taophieumuongio(TestHelper.READER_ID, HaiSach(), DateTime.Now.AddDays(14));

            res.success.Should().BeFalse();
            res.message.Should().Be("Thẻ bạn đọc không còn hiệu lực.");
        }

        [Fact]
        public async Task Taophieumuongio_the_ban_doc_da_het_han_thi_that_bai()
        {
            var reader = BanDocHopLe();
            reader.ngayhethan = DateTime.Now.AddDays(-1);
            var (svc, _) = TaoService(reader);

            var res = await svc.taophieumuongio(TestHelper.READER_ID, HaiSach(), DateTime.Now.AddDays(14));

            res.success.Should().BeFalse();
            res.message.Should().Be("Thẻ bạn đọc đã hết hạn.");
        }

        [Fact]
        public async Task Taophieumuongio_vuot_gioi_han_so_sach_muon_thi_that_bai()
        {
            var (svc, db) = TaoService(BanDocHopLe(somughin: 1));

            var res = await svc.taophieumuongio(TestHelper.READER_ID, HaiSach(), DateTime.Now.AddDays(14));

            res.success.Should().BeFalse();
            res.message.Should().Be("Vượt giới hạn mượn của thẻ. Bạn đang mượn 0/1 cuốn, chỉ mượn thêm tối đa 1 cuốn.");
            db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>()), Times.Never);
        }

        // ---------- Giới hạn thẻ tính CẢ sách đang mượn ----------

        [Fact]
        public async Task Taophieumuongio_con_du_dia_chi_muan_them_duoc_phan_con_lai()
        {
            // Thẻ mượn tối đa 5 cuốn, đang giữ 3 cuốn → còn được mượn thêm 2
            var (svc, db) = TaoService(BanDocHopLe(somughin: 5));
            db.Setup(x => x.ExecuteScalarAsync<int>("sp_reader_sodangmuon", It.IsAny<object?>()))
              .ReturnsAsync(3);
            db.Setup(x => x.ExecuteAsync("sp_loan_create_auto", It.IsAny<object?>()))
              .ReturnsAsync(1);

            var res = await svc.taophieumuongio(TestHelper.READER_ID, HaiSach(), DateTime.Now.AddDays(14));

            res.success.Should().BeTrue();
            db.Verify(x => x.ExecuteAsync("sp_loan_create_auto", It.IsAny<object?>()), Times.Once);
        }

        [Fact]
        public async Task Taophieumuongio_dang_muon_con_som_moi_thi_that_bai()
        {
            // Thẻ mượn tối đa 3 cuốn, đang giữ 2 cuốn → giỏ 2 cuốn là 2 + 2 = 4 > 3
            var (svc, db) = TaoService(BanDocHopLe(somughin: 3));
            db.Setup(x => x.ExecuteScalarAsync<int>("sp_reader_sodangmuon", It.IsAny<object?>()))
              .ReturnsAsync(2);

            var res = await svc.taophieumuongio(TestHelper.READER_ID, HaiSach(), DateTime.Now.AddDays(14));

            res.success.Should().BeFalse();
            res.message.Should().Be("Vượt giới hạn mượn của thẻ. Bạn đang mượn 2/3 cuốn, chỉ mượn thêm tối đa 1 cuốn.");
            db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>()), Times.Never);
        }

        [Fact]
        public async Task Taophieumuongio_da_dat_han_thi_that_bai()
        {
            // Thẻ mượn tối đa 3 cuốn, đang giữ đúng 3 cuốn → không được mượn thêm
            var (svc, db) = TaoService(BanDocHopLe(somughin: 3));
            db.Setup(x => x.ExecuteScalarAsync<int>("sp_reader_sodangmuon", It.IsAny<object?>()))
              .ReturnsAsync(3);

            var res = await svc.taophieumuongio(TestHelper.READER_ID, HaiSach(), DateTime.Now.AddDays(14));

            res.success.Should().BeFalse();
            res.message.Should().Be("Bạn đang mượn 3 cuốn, đã đạt giới hạn 3 cuốn của thẻ. Hãy trả sách trước khi mượn tiếp.");
            db.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>()), Times.Never);
        }

        [Fact]
        public async Task Taophieumuongio_goi_sp_voi_danh_sach_book_id_dung_dinh_dang()
        {
            var (svc, db) = TaoService(BanDocHopLe());
            object? pThamSo = null;
            db.Setup(x => x.ExecuteAsync("sp_loan_create_auto", It.IsAny<object?>()))
              .Callback<string, object?>((_, p) => pThamSo = p)
              .ReturnsAsync(1);

            var res = await svc.taophieumuongio(TestHelper.READER_ID, HaiSach(), DateTime.Now.AddDays(14));

            res.success.Should().BeTrue();
            db.Verify(x => x.ExecuteAsync("sp_loan_create_auto", It.IsAny<object?>()), Times.Once);
            db.Verify(x => x.ExecuteAsync("sp_loan_create", It.IsAny<object?>()), Times.Never,
                "giỏ hàng phải dùng SP tự gán bản sao, không dùng SP của thủ thư");

            pThamSo.Should().NotBeNull();
            var thamSo = pThamSo!;
            var listjson = thamSo.GetType()
                                  .GetProperty("listjson_chitiet")!.GetValue(thamSo)?.ToString();

            listjson.Should().Be($"[{{\"book_id\":\"{TestHelper.BOOK_1}\"}},{{\"book_id\":\"{TestHelper.BOOK_2}\"}}]");
        }

        [Fact]
        public async Task Taophieumuongio_that_bai_thi_tra_loi_tu_SP()
        {
            var (svc, db) = TaoService(BanDocHopLe());
            db.Setup(x => x.ExecuteAsync("sp_loan_create_auto", It.IsAny<object?>()))
              .ThrowsAsync(new InvalidOperationException("Không còn bản sao rảnh cho: Lập Trình C#. Vui lòng bỏ khỏi giỏ và thử lại."));

            var res = await svc.taophieumuongio(TestHelper.READER_ID, HaiSach(), DateTime.Now.AddDays(14));

            res.success.Should().BeFalse();
            res.message.Should().Be("Không còn bản sao rảnh cho: Lập Trình C#. Vui lòng bỏ khỏi giỏ và thử lại.");
        }
    }
}