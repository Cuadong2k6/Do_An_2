// Các services cho tính năng mới (OAuth2, QR code, Cache, Notifications)
namespace LibraryManagement.API.Services
{
    public interface IQRCodeGenerator
    {
        string GenerateQRCode(string content, int size = 300, int margin = 2);
        byte[] GenerateQRCodeAsBytes(string content, int size = 300, int margin = 2);
    }

    public class QRCodeGenerator : IQRCodeGenerator
    {
        private readonly ILogger<QRCodeGenerator> _logger;

        public QRCodeGenerator(ILogger<QRCodeGenerator> logger)
        {
            _logger = logger;
        }

        public string GenerateQRCode(string content, int size = 300, int margin = 2)
        {
            try
            {
                using (var bitmap = new ZXing.BarcodeWriter()
                {
                    Format = ZXing.BarcodeFormat.QR_CODE,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Width = size,
                        Height = size,
                        Margin = margin
                    }
                }.Write(content))
                {
                    using (var ms = new MemoryStream())
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var bytes = ms.ToArray();
                        return Convert.ToBase64String(bytes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo QR code cho nội dung: {Content}", content);
                throw new InvalidOperationException("Không thể tạo QR code", ex);
            }
        }

        public byte[] GenerateQRCodeAsBytes(string content, int size = 300, int margin = 2)
        {
            try
            {
                using (var bitmap = new ZXing.BarcodeWriter()
                {
                    Format = ZXing.BarcodeFormat.QR_CODE,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Width = size,
                        Height = size,
                        Margin = margin
                    }
                }.Write(content))
                {
                    using (var ms = new MemoryStream())
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo QR code cho nội dung: {Content}", content);
                throw new InvalidOperationException("Không thể tạo QR code", ex);
            }
        }
    }
}