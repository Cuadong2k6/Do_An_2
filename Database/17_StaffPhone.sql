-- ============================================================
-- Script 17: Bổ sung số điện thoại cho tài khoản Thủ thư
-- Database: DoAn2
--
-- Phạm vi:
--   1. Thêm cột sodienthoai vào bảng users (idempotent).
--   2. Cập nhật sp_user_detail / sp_user_create_thuthu / sp_user_update_thuthu
--      để đọc/ghi số điện thoại.
--   3. Không thay đổi bảng readers (đã có sodienthoai từ trước).
--
-- LƯU Ý:
--   - Chạy SAU script 14. Nếu chạy lại script 14 thì phải chạy lại script này
--     vì 14 dùng CREATE OR ALTER cho các SP trên.
--   - Không tự chạy SQL. Chỉ tạo script để DBA/review trước khi apply.
-- ============================================================
USE DoAn2;
GO

-- ============================================================
-- 1. ALTER TABLE: thêm cột sodienthoai nếu chưa tồn tại
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.users')
      AND name = N'sodienthoai'
)
BEGIN
    ALTER TABLE dbo.users ADD sodienthoai VARCHAR(15) NULL;
    PRINT N'OK: 17 - Đã thêm cột sodienthoai vào bảng users.';
END
ELSE
BEGIN
    PRINT N'SKIP: 17 - Cột sodienthoai đã tồn tại.';
END;
GO

-- ============================================================
-- 2. sp_user_detail — bổ sung sodienthoai
-- ============================================================
CREATE OR ALTER PROCEDURE sp_user_detail
    @user_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id)
    BEGIN
        THROW 50404, N'Không tìm thấy tài khoản.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id AND role = N'ThuThu')
    BEGIN
        THROW 50403, N'Tài khoản không phải ThuThu, không thể xem qua SP này.', 1;
    END

    SELECT user_id, hoten, email, sodienthoai, taikhoan, role, image_url, ngaytao, is_locked
    FROM users
    WHERE user_id = @user_id AND role = N'ThuThu';
END;
GO

-- ============================================================
-- 3. sp_user_create_thuthu — nhận thêm @sodienthoai
-- ============================================================
CREATE OR ALTER PROCEDURE sp_user_create_thuthu
    @hoten        NVARCHAR(200),
    @email        NVARCHAR(200),
    @taikhoan     NVARCHAR(100),
    @matkhau_hash NVARCHAR(255),
    @sodienthoai  VARCHAR(15) = NULL,
    @image_url    NVARCHAR(500) = NULL,
    @new_user_id  UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM users WHERE taikhoan = @taikhoan)
    BEGIN
        THROW 50401, N'Tài khoản đã tồn tại.', 1;
    END

    IF EXISTS (SELECT 1 FROM users WHERE email = @email)
    BEGIN
        THROW 50402, N'Email đã được sử dụng.', 1;
    END

    SET @new_user_id = NEWID();

    BEGIN TRY
        INSERT INTO users (user_id, hoten, email, taikhoan, matkhau, role, sodienthoai, image_url, ngaytao, is_locked)
        VALUES (@new_user_id, @hoten, @email, @taikhoan, @matkhau_hash, N'ThuThu', @sodienthoai, @image_url, GETDATE(), 0);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            THROW 50409, N'Vi phạm ràng buộc duy nhất: tài khoản hoặc email đã tồn tại.', 1;
        END;
        THROW;
    END CATCH
END;
GO

-- ============================================================
-- 4. sp_user_update_thuthu — nhận thêm @sodienthoai
--    @sodienthoai NULL   = giữ nguyên
--    @sodienthoai ''     = xoá số điện thoại
--    @sodienthoai khác   = cập nhật giá trị mới
-- ============================================================
CREATE OR ALTER PROCEDURE sp_user_update_thuthu
    @user_id      UNIQUEIDENTIFIER,
    @hoten        NVARCHAR(200) = NULL,
    @email        NVARCHAR(200) = NULL,
    @taikhoan     NVARCHAR(100) = NULL,
    @matkhau_hash NVARCHAR(255) = NULL,
    @sodienthoai  VARCHAR(15) = NULL,
    @image_url    NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id)
    BEGIN
        THROW 50404, N'Không tìm thấy tài khoản.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id AND role = N'ThuThu')
    BEGIN
        THROW 50403, N'Tài khoản không phải ThuThu, không thể cập nhật qua SP này.', 1;
    END

    IF @taikhoan IS NOT NULL
       AND EXISTS (SELECT 1 FROM users WHERE taikhoan = @taikhoan AND user_id <> @user_id)
    BEGIN
        THROW 50401, N'Tài khoản đã tồn tại.', 1;
    END

    IF @email IS NOT NULL
       AND EXISTS (SELECT 1 FROM users WHERE email = @email AND user_id <> @user_id)
    BEGIN
        THROW 50402, N'Email đã được sử dụng.', 1;
    END

    BEGIN TRY
        UPDATE users
        SET hoten       = COALESCE(@hoten, hoten),
            email       = COALESCE(@email, email),
            taikhoan    = COALESCE(@taikhoan, taikhoan),
            matkhau     = COALESCE(@matkhau_hash, matkhau),
            sodienthoai = CASE
                              WHEN @sodienthoai IS NULL THEN sodienthoai
                              ELSE NULLIF(LTRIM(RTRIM(@sodienthoai)), N'')
                          END,
            image_url   = COALESCE(@image_url, image_url)
        WHERE user_id = @user_id AND role = N'ThuThu';
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            THROW 50409, N'Vi phạm ràng buộc duy nhất: tài khoản hoặc email đã tồn tại.', 1;
        END;
        THROW;
    END CATCH
END;
GO

PRINT N'HOÀN TẤT: Script 17 - Bổ sung số điện thoại cho Thủ thư.';
GO
