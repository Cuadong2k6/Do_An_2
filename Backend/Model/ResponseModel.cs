namespace Model
{
    /// <summary>
    /// Chuẩn hóa phản hồi API có hỗ trợ phân trang
    /// </summary>
    public class ResponseModel
    {
        public bool success { get; set; }
        public string message { get; set; }
        public long totalItems { get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
        public dynamic data { get; set; }

        public static ResponseModel Ok(dynamic data, string message = "Thành công", long totalItems = 0, int page = 1, int pageSize = 10)
            => new ResponseModel { success = true, message = message, data = data, totalItems = totalItems, page = page, pageSize = pageSize };

        public static ResponseModel Fail(string message)
            => new ResponseModel { success = false, message = message };
    }
}
