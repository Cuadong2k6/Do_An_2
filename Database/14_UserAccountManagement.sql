-- ============================================================
-- Script 14: Quản lý tài khoản staff (Admin/ThuThu)
-- Database: DoAn2
--
-- Phạm vi:
--   1. Thêm cột is_locked vào bảng users (idempotent).
--   2. CRUD stored procedures cho ThuThu; Admin account không bị tác động.
--   3. BCrypt hash do C# tạo, SP chỉ nhận và lưu hash.
--   4. Hard delete nếu role = 'ThuThu' (không có FK tham chiếu users).
--   5. KHÔNG bao gồm chức năng reader.
--   6. Idempotent: chạy lại nhiều lần không lỗi, không tạo dữ liệu trùng.
--   7. Tất cả SP quản lý staff chỉ hoạt động trên role = 'ThuThu'.
--   8. Không expose mật khẩu hash trong bất kỳ SELECT nào.
--   9. Phân trang bounded: page_size tối đa 100, offset BIGINT.
--  10. Bắt duplicate key từ unique constraint, không nuốt lỗi SQL khác.
--
-- LƯU Ý:
--   - Không sửa 01_CreateTables.sql, 02_StoredProcedures.sql, 03_SeedData.sql.
--   - Không tự chạy SQL. Chỉ tạo script để DBA/review trước khi apply.
-- ============================================================
USE DoAn2;
GO

-- ============================================================
-- 1. ALTER TABLE: thêm cột is_locked nếu chưa tồn tại
--    0 = hoạt động bình thường, 1 = bị khóa (không login được)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.users')
      AND name = N'is_locked'
)
BEGIN
    ALTER TABLE dbo.users ADD is_locked BIT NOT NULL DEFAULT 0;
    PRINT N'OK: 14 - Đã thêm cột is_locked vào bảng users.';
END
ELSE
BEGIN
    PRINT N'SKIP: 14 - Cột is_locked đã tồn tại.';
END;
GO

-- ============================================================
-- 2. sp_user_list — Danh sách staff (chỉ ThuThu), phân trang
--    Không expose tài khoản Admin.
--    Bounded pagination: page_size tối đa 100, offset dùng BIGINT.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_user_list
    @page_index  INT = 1,
    @page_size   INT = 20,
    @total       BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Chặn giá trị phân trang phi lý
    IF @page_index < 1 SET @page_index = 1;
    IF @page_size < 1 SET @page_size = 20;
    IF @page_size > 100 SET @page_size = 100;

    -- Dùng BIGINT để tránh tràn khi offset lớn
    DECLARE @offset BIGINT = CAST(@page_index - 1 AS BIGINT) * CAST(@page_size AS BIGINT);

    -- Đếm tổng (chỉ ThuThu)
    SELECT @total = COUNT(*)
    FROM users
    WHERE role = N'ThuThu';

    -- Trả kết quả (KHÔNG trả matkhau, chỉ ThuThu)
    SELECT user_id, hoten, email, taikhoan, role, image_url, ngaytao, is_locked
    FROM users
    WHERE role = N'ThuThu'
    ORDER BY ngaytao DESC
    OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;
END;
GO

-- ============================================================
-- 3. sp_user_detail — Chi tiết 1 staff theo user_id
--    Chỉ trả về tài khoản role = 'ThuThu', không expose Admin.
--    KHÔNG trả matkhau.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_user_detail
    @user_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra tồn tại
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id)
    BEGIN
        THROW 50404, N'Không tìm thấy tài khoản.', 1;
    END

    -- Kiểm tra đúng role ThuThu
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id AND role = N'ThuThu')
    BEGIN
        THROW 50403, N'Tài khoản không phải ThuThu, không thể xem qua SP này.', 1;
    END

    -- Trả kết quả (KHÔNG trả matkhau)
    SELECT user_id, hoten, email, taikhoan, role, image_url, ngaytao, is_locked
    FROM users
    WHERE user_id = @user_id AND role = N'ThuThu';
END;
GO

-- ============================================================
-- 4. sp_user_create_thuthu — Tạo tài khoản ThuThu mới
--    CHỈ tạo role = 'ThuThu'.
--    @matkhau_hash: BCrypt hash do C# tạo sẵn, SP không hash.
--    Kiểm tra trùng taikhoan/email trước khi insert.
--    Bọc TRY...CATCH để bắt duplicate key từ unique constraint.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_user_create_thuthu
    @hoten        NVARCHAR(200),
    @email        NVARCHAR(200),
    @taikhoan     NVARCHAR(100),
    @matkhau_hash NVARCHAR(255),
    @image_url    NVARCHAR(500) = NULL,
    @new_user_id  UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra trùng taikhoan
    IF EXISTS (SELECT 1 FROM users WHERE taikhoan = @taikhoan)
    BEGIN
        THROW 50401, N'Tài khoản đã tồn tại.', 1;
    END

    -- Kiểm tra trùng email
    IF EXISTS (SELECT 1 FROM users WHERE email = @email)
    BEGIN
        THROW 50402, N'Email đã được sử dụng.', 1;
    END

    SET @new_user_id = NEWID();

    BEGIN TRY
        INSERT INTO users (user_id, hoten, email, taikhoan, matkhau, role, image_url, ngaytao, is_locked)
        VALUES (@new_user_id, @hoten, @email, @taikhoan, @matkhau_hash, N'ThuThu', @image_url, GETDATE(), 0);
    END TRY
    BEGIN CATCH
        -- Bắt duplicate key từ unique constraint (error 2601 hoặc 2627)
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            THROW 50409, N'Vi phạm ràng buộc duy nhất: tài khoản hoặc email đã tồn tại.', 1;
        END;
        -- Không nuốt lỗi khác: ném lại nguyên bản
        THROW;
    END CATCH
END;
GO

-- ============================================================
-- 5. sp_user_update_thuthu — Cập nhật thông tin ThuThu
--    CHỈ cập nhật tài khoản role = 'ThuThu'.
--    @matkhau_hash: NULL = không đổi mật khẩu; có giá trị = cập nhật hash mới.
--    Bọc TRY...CATCH để bắt duplicate key từ unique constraint.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_user_update_thuthu
    @user_id      UNIQUEIDENTIFIER,
    @hoten        NVARCHAR(200) = NULL,
    @email        NVARCHAR(200) = NULL,
    @taikhoan     NVARCHAR(100) = NULL,
    @matkhau_hash NVARCHAR(255) = NULL,
    @image_url    NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra user tồn tại
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id)
    BEGIN
        THROW 50404, N'Không tìm thấy tài khoản.', 1;
    END

    -- Yêu cầu target phải là ThuThu (không thao tác trên Admin hay role khác)
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id AND role = N'ThuThu')
    BEGIN
        THROW 50403, N'Tài khoản không phải ThuThu, không thể cập nhật qua SP này.', 1;
    END

    -- Kiểm tra trùng taikhoan (nếu đổi)
    IF @taikhoan IS NOT NULL
       AND EXISTS (SELECT 1 FROM users WHERE taikhoan = @taikhoan AND user_id <> @user_id)
    BEGIN
        THROW 50401, N'Tài khoản đã tồn tại.', 1;
    END

    -- Kiểm tra trùng email (nếu đổi)
    IF @email IS NOT NULL
       AND EXISTS (SELECT 1 FROM users WHERE email = @email AND user_id <> @user_id)
    BEGIN
        THROW 50402, N'Email đã được sử dụng.', 1;
    END

    BEGIN TRY
        UPDATE users
        SET hoten     = COALESCE(@hoten, hoten),
            email     = COALESCE(@email, email),
            taikhoan  = COALESCE(@taikhoan, taikhoan),
            matkhau   = COALESCE(@matkhau_hash, matkhau),
            image_url = COALESCE(@image_url, image_url)
        WHERE user_id = @user_id AND role = N'ThuThu';
    END TRY
    BEGIN CATCH
        -- Bắt duplicate key từ unique constraint
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            THROW 50409, N'Vi phạm ràng buộc duy nhất: tài khoản hoặc email đã tồn tại.', 1;
        END;
        -- Không nuốt lỗi khác
        THROW;
    END CATCH
END;
GO

-- ============================================================
-- 6. sp_user_delete_thuthu — Xóa cứng tài khoản ThuThu
--    CHỈ xóa tài khoản role = 'ThuThu'.
--    Schema không có FK tham chiếu users → xóa trực tiếp.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_user_delete_thuthu
    @user_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra user tồn tại
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id)
    BEGIN
        THROW 50404, N'Không tìm thấy tài khoản.', 1;
    END

    -- Yêu cầu target phải là ThuThu
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id AND role = N'ThuThu')
    BEGIN
        THROW 50403, N'Tài khoản không phải ThuThu, không thể xóa qua SP này.', 1;
    END

    DELETE FROM users WHERE user_id = @user_id AND role = N'ThuThu';
END;
GO

-- ============================================================
-- 7. sp_user_lock — Khóa tài khoản ThuThu
--    CHỈ khóa tài khoản role = 'ThuThu'.
--    Trả lỗi rõ ràng nếu không tìm thấy hoặc không phải ThuThu.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_user_lock
    @user_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra user tồn tại
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id)
    BEGIN
        THROW 50404, N'Không tìm thấy tài khoản.', 1;
    END

    -- Yêu cầu target phải là ThuThu
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id AND role = N'ThuThu')
    BEGIN
        THROW 50403, N'Tài khoản không phải ThuThu, không thể khóa qua SP này.', 1;
    END

    UPDATE users SET is_locked = 1 WHERE user_id = @user_id AND role = N'ThuThu';
END;
GO

-- ============================================================
-- 8. sp_user_unlock — Mở khóa tài khoản ThuThu
--    CHỈ mở khóa tài khoản role = 'ThuThu'.
--    Trả lỗi rõ ràng nếu không tìm thấy hoặc không phải ThuThu.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_user_unlock
    @user_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra user tồn tại
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id)
    BEGIN
        THROW 50404, N'Không tìm thấy tài khoản.', 1;
    END

    -- Yêu cầu target phải là ThuThu
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = @user_id AND role = N'ThuThu')
    BEGIN
        THROW 50403, N'Tài khoản không phải ThuThu, không thể mở khóa qua SP này.', 1;
    END

    UPDATE users SET is_locked = 0 WHERE user_id = @user_id AND role = N'ThuThu';
END;
GO

PRINT N'HOÀN TẤT: Script 14 - Quản lý tài khoản staff.';
GO
