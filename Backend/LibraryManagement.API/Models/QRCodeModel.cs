// QR Code model classes
namespace LibraryManagement.API.Models
{
    public class QRCodeModel
    {
        public string Content { get; set; }
        public int Size { get; set; } = 300;
        public int Margin { get; set; } = 2;
        public string Format { get; set; } = "PNG";
        public byte[] GeneratedImage { get; set; }
        public string Base64Image => GeneratedImage != null ? Convert.ToBase64String(GeneratedImage) : null;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(60);
    }
}
