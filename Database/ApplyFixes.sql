/* ======================================================================
   Database/ApplyFixes.sql
   Mục tiêu: vá nhanh các stored procedure cần thiết cho backend mới nhất
   mà không cần chạy toàn bộ 02_StoredProcedures.sql.

   Chạy file này trên DB DoAn2 bằng SSMS/Azure Data Studio/sqlcmd.
   ====================================================================== */

-- ==============================================
-- sp_copy_create
-- Thêm bản sao: hỗ trợ status + ngaynhap
-- ==============================================
CREATE OR ALTER PROCEDURE sp_copy_create
    @copy_id  UNIQUEIDENTIFIER,
    @book_id  UNIQUEIDENTIFIER,
    @mabancao NVARCHAR(50),
    @shelf_id INT = NULL,
    @status   INT = 0,
    @ngaynhap DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM books WHERE book_id = @book_id)
        THROW 50018, N'Đầu sách không tồn tại.', 1;

    IF @shelf_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM shelves WHERE shelf_id = @shelf_id)
        THROW 50019, N'Kệ sách không tồn tại.', 1;

    IF EXISTS (SELECT 1 FROM copies WHERE mabancao = @mabancao)
        THROW 50020, N'Mã bản sao đã tồn tại.', 1;

    INSERT INTO copies (copy_id, book_id, shelf_id, mabancao, status, ngaynhap)
    VALUES (@copy_id, @book_id, @shelf_id, @mabancao, @status, ISNULL(@ngaynhap, GETDATE()));
END;
GO

-- ==============================================
-- sp_fine_payment
-- Thanh toán phạt: trả pay_id thực tế qua OUTPUT
-- ==============================================
CREATE OR ALTER PROCEDURE sp_fine_payment
    @fine_id    UNIQUEIDENTIFIER,
    @sotien     DECIMAL(18,2),
    @phuongthuc NVARCHAR(50) = N'Tiền mặt',
    @ghichu     NVARCHAR(300) = NULL,
    @pay_id     UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM fines WHERE fine_id = @fine_id)
            THROW 50003, N'Phiếu phạt không tồn tại.', 1;

        IF EXISTS (SELECT 1 FROM fines WHERE fine_id = @fine_id AND is_paid = 1)
            THROW 50004, N'Phiếu phạt đã được thanh toán.', 1;

        SET @pay_id = NEWID();

        INSERT INTO payments (pay_id, fine_id, sotien, phuongthuc, ghichu)
        VALUES (@pay_id, @fine_id, @sotien, @phuongthuc, @ghichu);

        UPDATE fines
        SET is_paid = 1
        WHERE fine_id = @fine_id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ==============================================
-- Kiểm tra nhanh sau khi chạy (tùy chọn)
-- ==============================================
-- SELECT COUNT(*) AS sp_copy_create_param_count
-- FROM sys.parameters
-- WHERE object_id = OBJECT_ID(N'dbo.sp_copy_create');
-- -- Kỳ vọng: 6
