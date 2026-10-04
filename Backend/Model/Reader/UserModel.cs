using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Model.Reader
{
    public class UserModel
    {
        public Guid user_id { get; set; }
        public string hoten { get; set; } = string.Empty;
        public DateTime? ngaysinh { get; set; }
        public string diachi { get; set; } = string.Empty;
        public string gioitinh { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string taikhoan { get; set; } = string.Empty;
        /// <summary>Hash mật khẩu — chỉ dùng nội bộ để xác thực, KHÔNG serialize ra client.</summary>
        [JsonIgnore]
        public string matkhau { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string token { get; set; } = string.Empty;
        public string image_url { get; set; } = string.Empty;
    }
}
