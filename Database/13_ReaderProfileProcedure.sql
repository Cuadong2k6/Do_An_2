-- Cập nhật procedure self-service hồ sơ reader.
-- Chỉ cập nhật họ tên, điện thoại, địa chỉ; không thay email/thẻ/trạng thái.
USE DoAn2;
GO

CREATE OR ALTER PROCEDURE dbo.sp_reader_update_profile
    @reader_id UNIQUEIDENTIFIER,
    @hoten NVARCHAR(200),
    @sodienthoai VARCHAR(15) = NULL,
    @diachi NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.readers WHERE reader_id = @reader_id)
        THROW 50107, N'Khong tim thay reader.', 1;

    UPDATE dbo.readers
    SET hoten = @hoten,
        sodienthoai = @sodienthoai,
        diachi = @diachi
    WHERE reader_id = @reader_id;
END;
GO
