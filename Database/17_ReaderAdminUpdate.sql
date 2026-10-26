-- ============================================================
-- Script 17: Admin cập nhật độc giả — bổ sung @ngaycap
-- Database: DoAn2
--
-- Sao chép từ sp_reader_update trong 02_StoredProcedures.sql.
-- Giữ nguyên kiểu và validation hiện có, chỉ thêm @ngaycap.
-- Không sửa script 02. Không tự apply.
-- ============================================================
USE DoAn2;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Cập nhật thông tin độc giả (kèm hạn thẻ / số sách mượn tối đa / ngày cấp)
CREATE OR ALTER PROCEDURE sp_reader_update
    @reader_id   UNIQUEIDENTIFIER,
    @hoten       NVARCHAR(200),
    @sodienthoai VARCHAR(15)   = NULL,
    @diachi      NVARCHAR(300) = NULL,
    @trangthai   INT,
    @ngaycap     DATETIME = NULL,
    @ngayhethan  DATETIME = NULL,
    @somughin    INT      = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM readers WHERE reader_id = @reader_id)
        THROW 50107, N'Không tìm thấy độc giả.', 1;

    DECLARE @hancu DATETIME = (SELECT ngayhethan FROM readers WHERE reader_id = @reader_id);

    -- Chỉ kiểm tra khi THAY ĐỔI ngày hết hạn (cùng ngày = giữ nguyên → bỏ qua, không chặn oan)
    IF @ngayhethan IS NOT NULL
       AND CONVERT(DATE, @ngayhethan) <> CONVERT(DATE, @hancu)
       AND @ngayhethan <= GETDATE()
        THROW 50108, N'Ngày hết hạn mới phải lớn hơn ngày hiện tại.', 1;

    IF @somughin IS NOT NULL AND @somughin <= 0
        THROW 50109, N'Số sách mượn tối đa phải lớn hơn 0.', 1;

    -- Logic thẻ: hạn mới hợp lệ → tự kích hoạt lại thẻ đang "Hết hạn" (thẻ bị Khoá giữ nguyên)
    DECLARE @tt INT = @trangthai;
    IF @ngayhethan IS NOT NULL AND @ngayhethan > GETDATE() AND @tt = 1
        SET @tt = 0;

    UPDATE readers
    SET hoten       = @hoten,
        sodienthoai = @sodienthoai,
        diachi      = @diachi,
        trangthai   = @tt,
        ngaycap     = COALESCE(@ngaycap, ngaycap),
        ngayhethan  = COALESCE(@ngayhethan, ngayhethan),
        somughin    = COALESCE(@somughin, somughin)
    WHERE reader_id = @reader_id;
END;
GO
