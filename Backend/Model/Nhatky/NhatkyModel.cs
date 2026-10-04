namespace Model.Nhatky
{
    /// <summary>
    /// Một dòng nhật ký thay đổi — 1 dòng tương ứng với 1 TRƯỜNG bị sửa
    /// (hoặc 1 lần xoá). Nhờ vậy đọc nhật ký là biết ngay sửa nhầm chỗ nào.
    /// </summary>
    public class NhatkyModel
    {
        public long    nhatky_id { get; set; }
        public DateTime thoigian  { get; set; }

        /// <summary>Người thực hiện — hiện ghi 'ADMIN/ThuThu' vì cả 2 bảng này
        /// đều chỉ nhân viên được sửa (xem ReaderController / BookController)</summary>
        public string  nguoithuc { get; set; } = string.Empty;

        /// <summary>'books' | 'readers'</summary>
        public string  bang     { get; set; } = string.Empty;

        /// <summary>books → ISBN, readers → số thẻ</summary>
        public string  doituong { get; set; } = string.Empty;

        /// <summary>'SUA' (sửa) | 'XOA' (xoá)</summary>
        public string  hanhdong { get; set; } = string.Empty;

        /// <summary>Tên cột bị thay đổi (chỉ có khi hành động = sửa)</summary>
        public string? truong   { get; set; }

        /// <summary>Giá trị cũ (chỉ có khi hành động = sửa)</summary>
        public string? truoc    { get; set; }

        /// <summary>Giá trị mới (chỉ có khi hành động = sửa)</summary>
        public string? sau      { get; set; }
    }
}