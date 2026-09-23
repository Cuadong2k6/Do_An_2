using DAL;
using Microsoft.Extensions.Logging;
using Model;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace BLL
{
    public class ReaderService
    {
        private readonly ReaderRepository _readerRepo;
        private readonly ILogger<ReaderService> _logger;

        public ReaderService(ReaderRepository readerRepo, ILogger<ReaderService> logger)
        {
            _readerRepo = readerRepo;
            _logger     = logger;
        }

        public async Task<ResponseModel> danhsachbandoc(string? keyword, int? trangthai, int page, int pageSize)
        {
            var (items, total) = await _readerRepo.danhsachbandoc(keyword, trangthai, page, pageSize);

            // Ẩn email / số điện thoại trong DANH SÁCH (riêng tư).
            // Chi tiết đầy đủ vẫn trả qua GET /api/Reader/{id} → modal Sửa thấy thông tin thật.
            var ds = items.ToList();
            foreach (var r in ds)
            {
                if (!string.IsNullOrWhiteSpace(r.email))
                {
                    var viTriCham = r.email.IndexOf('@');
                    r.email = viTriCham > 0
                        ? r.email[..1] + "***" + r.email[viTriCham..]
                        : "***";
                }
                if (!string.IsNullOrWhiteSpace(r.sodienthoai) && r.sodienthoai.Length > 4)
                    r.sodienthoai = r.sodienthoai[..3] + "****" + r.sodienthoai[^2..];
            }

            return ResponseModel.Ok(ds, totalItems: total, page: page, pageSize: pageSize);
        }

        public async Task<ResponseModel> chitietchibandoc(Guid readerId)
        {
            var reader = await _readerRepo.laychitietchibandoc(readerId);
            if (reader == null) return ResponseModel.Fail("Không tìm thấy độc giả.");
            return ResponseModel.Ok(reader);
        }

        public async Task<ResponseModel> dangkymoi(ReaderModel model)
        {
            if (string.IsNullOrWhiteSpace(model.hoten))   return ResponseModel.Fail("Họ tên không được để trống.");
            if (string.IsNullOrWhiteSpace(model.email))   return ResponseModel.Fail("Email không được để trống.");
            if (string.IsNullOrWhiteSpace(model.so_the))  return ResponseModel.Fail("Số thẻ không được để trống.");

            model.reader_id  = Guid.NewGuid();
            model.ngaycap    = DateTime.Now;
            model.ngayhethan = (model.ngayhethan == null || model.ngayhethan <= DateTime.Now)
                ? DateTime.Now.AddYears(1)
                : model.ngayhethan;
            model.trangthai  = 0;
            if (model.somughin <= 0) model.somughin = 3;
            model.matkhau = string.IsNullOrWhiteSpace(model.matkhau)
                ? hashmatkhau(model.email)
                : hashmatkhau(model.matkhau);

            try
            {
                await _readerRepo.dangkythebandoc(model);
                _logger.LogInformation("Đăng ký thẻ độc giả mới: {ReaderId} - {HoTen}", model.reader_id, model.hoten);
                return ResponseModel.Ok(model.reader_id, "Đăng ký thẻ độc giả thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đăng ký độc giả {Email}", model.email);
                return ResponseModel.Fail(ex.Message);
            }
        }

        public async Task<ResponseModel> capnhatthongtin(ReaderModel model)
        {
            if (string.IsNullOrWhiteSpace(model.hoten)) return ResponseModel.Fail("Họ tên không được để trống.");
            if (model.somughin <= 0)                    return ResponseModel.Fail("Số sách mượn tối đa phải lớn hơn 0.");

            try
            {
                await _readerRepo.capnhatthongtinbandoc(model);
                _logger.LogInformation("Cập nhật độc giả: {ReaderId}", model.reader_id);
                return ResponseModel.Ok(null, "Cập nhật thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật độc giả {ReaderId}", model.reader_id);
                return ResponseModel.Fail(ex.Message);
            }
        }

        /// <summary>Xoá độc giả (SP chặn khi còn lịch sử mượn / đặt chỗ)</summary>
        public async Task<ResponseModel> xoadocgia(Guid readerId)
        {
            try
            {
                await _readerRepo.xoadocgia(readerId);
                _logger.LogInformation("Xoá độc giả: {ReaderId}", readerId);
                return ResponseModel.Ok(null, "Xoá độc giả thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xoá độc giả {ReaderId}", readerId);
                return ResponseModel.Fail(ex.Message);
            }
        }

        /// <summary>Gia hạn thẻ bạn đọc (kích hoạt lại thẻ nếu đang hết hạn)</summary>
        public async Task<ResponseModel> giathanthu(Guid readerId, DateTime ngayhethan)
        {
            if (ngayhethan <= DateTime.Now)
                return ResponseModel.Fail("Ngày hết hạn mới phải lớn hơn ngày hiện tại.");

            try
            {
                await _readerRepo.giathanthu(readerId, ngayhethan);
                _logger.LogInformation("Gia hạn thẻ {ReaderId} đến {NgayHetHan}", readerId, ngayhethan);
                return ResponseModel.Ok(null, "Gia hạn thẻ độc giả thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gia hạn thẻ {ReaderId}", readerId);
                return ResponseModel.Fail(ex.Message);
            }
        }

        /// <summary>Xuất thẻ bạn đọc ra file PDF. Trả null nếu không tìm thấy.</summary>
        public async Task<byte[]?> xuatthebandoc(Guid readerId)
        {
            var reader = await _readerRepo.laychitietchibandoc(readerId);
            if (reader == null) return null;

            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .AlignCenter()
                        .Text("THẺ THƯ VIỆN")
                        .FontSize(20)
                        .Bold();

                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Text(t => { t.Span("Số thẻ: ").Bold();        t.Span(reader.so_the); });
                        col.Item().Text(t => { t.Span("Họ tên: ").Bold();        t.Span(reader.hoten); });
                        col.Item().Text(t => { t.Span("Email: ").Bold();         t.Span(reader.email); });
                        col.Item().Text(t => { t.Span("Số điện thoại: ").Bold(); t.Span(reader.sodienthoai); });
                        col.Item().Text(t => { t.Span("Địa chỉ: ").Bold();       t.Span(reader.diachi); });
                        col.Item().Text(t => { t.Span("Ngày cấp: ").Bold();      t.Span(reader.ngaycap?.ToString("dd/MM/yyyy") ?? "-"); });
                        col.Item().Text(t => { t.Span("Ngày hết hạn: ").Bold();  t.Span(reader.ngayhethan?.ToString("dd/MM/yyyy") ?? "-"); });
                        col.Item().Text(t => { t.Span("Giới hạn mượn: ").Bold(); t.Span($"{reader.somughin} cuốn/lượt"); });
                        col.Item().PaddingTop(10)
                             .Text("Thẻ có giá trị theo quy định của thư viện.")
                             .FontSize(9)
                             .Italic();
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(9);
                });
            }).GeneratePdf();

            _logger.LogInformation("Xuất thẻ PDF {ReaderId} - {SoThe}", readerId, reader.so_the);
            return pdf;
        }

        private string hashmatkhau(string matkhau)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(matkhau));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
