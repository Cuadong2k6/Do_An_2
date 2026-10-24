-- ============================================================
-- Script 15: Staff login lock enforcement
-- Database: DoAn2
--
-- Phạm vi:
--   1. Đảm bảo cột users.is_locked tồn tại (idempotent, self-contained).
--      Đây là guard clause phòng khi script 14 chưa được chạy trước.
--   2. CREATE OR ALTER sp_user_login trả thêm is_locked trong SELECT.
--   3. Không sửa 02_StoredProcedures.sql gốc (delta migration).
--   4. Idempotent: chạy lại không lỗi.
--
-- Dependencies (tài liệu):
--   - 14_UserAccountManagement.sql: script chính thức thêm cột users.is_locked
--     và các SP quản lý staff (sp_user_lock, sp_user_unlock, ...).
--     Script 15 có thể chạy độc lập nhờ guard IF NOT EXISTS bên dưới,
--     nhưng thứ tự khuyến nghị: chạy 14 trước, rồi mới chạy 15.
--   - 02_StoredProcedures.sql (sp_user_login gốc)
--
-- LƯU Ý: Không tự chạy SQL. Chỉ tạo script để DBA/review.
-- ============================================================
USE DoAn2;
GO

-- ============================================================
-- 1. Guard: đảm bảo cột is_locked tồn tại trên bảng users
--    Nếu script 14 đã chạy → SKIP. Nếu chưa → tự thêm.
--    Mục đích: CREATE OR ALTER PROCEDURE bên dưới không bị lỗi
--    biên dịch do thiếu cột.
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.users')
      AND name = N'is_locked'
)
BEGIN
    ALTER TABLE dbo.users ADD is_locked BIT NOT NULL DEFAULT 0;
    PRINT N'OK: 15 - Đã thêm cột is_locked vào bảng users (guard clause).';
END
ELSE
BEGIN
    PRINT N'SKIP: 15 - Cột is_locked đã tồn tại.';
END;
GO

-- ============================================================
-- 2. Override sp_user_login: thêm is_locked vào kết quả SELECT
--    Liệt kê rõ từng cột, tránh dùng SELECT * để không gây
--    ambiguous khi bảng users mở rộng sau này.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_user_login
    @taikhoan NVARCHAR(100),
    @matkhau  NVARCHAR(255) = NULL  -- Giữ tham số cũ để backward compat
AS
BEGIN
    SET NOCOUNT ON;

    -- Trả thông tin user (bao gồm is_locked để caller biết trạng thái)
    SELECT user_id, hoten, email, taikhoan, matkhau, role, image_url, is_locked
    FROM users
    WHERE taikhoan = @taikhoan;
END;
GO

PRINT N'HOÀN TẤT: Script 15 - Staff login lock enforcement.';
GO