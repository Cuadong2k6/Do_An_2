using BLL;

namespace LibraryManagement.API.BackgroundServices
{
    /// <summary>Đưa email BLL vào NotificationQueue hiện có. Type = email.</summary>
    public class HangDoiThongBaoAdapter : IHangDoiThongBao
    {
        private readonly INotificationQueue _queue;

        public HangDoiThongBaoAdapter(INotificationQueue queue)
        {
            _queue = queue;
        }

        public void ThemEmail(ThongBaoEmail thongBao)
        {
            _queue.EnqueueNotification(new NotificationMessage
            {
                Type = "email",
                Subject = thongBao.TieuDe,
                Body = thongBao.NoiDung,
                Recipient = thongBao.NguoiNhan
            });
        }
    }
}
