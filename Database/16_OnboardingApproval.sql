-- ============================================================
-- Script 16: Reader onboarding approval workflow (manual)
-- Database: DoAn2
--
-- Phạm vi:
--   1) Thêm trạng thái onboarding = 4 (Rejected) bằng CHECK constraint idempotent.
--   2) Tạo các stored procedure atomic cho luồng duyệt thủ công:
--      - sp_reader_activation_request  (reader: 1 -> 2)
--      - sp_reader_activation_pending  (staff queue: chỉ onboarding = 2)
--      - sp_reader_activation_approve  (staff: 2 -> 3, trangthai = 0)
--      - sp_reader_activation_reject   (staff: 2 -> 4, trangthai = 2)
--
-- QUAN TRỌNG:
--   - Script chỉ để publish/review, KHÔNG tự chạy trong unit này.
--   - Idempotent: chạy lại không lỗi.
-- ============================================================
USE DoAn2;
GO

-- ============================================================
-- 1) CHECK constraint cho readers.trangthai_onboarding: cho phép 0,1,2,3,4
-- ============================================================
DECLARE @tenCkOnboarding SYSNAME;

SELECT TOP 1 @tenCkOnboarding = dc.name
FROM sys.check_constraints dc
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.readers')
  AND dc.definition LIKE N'%trangthai_onboarding%';

IF @tenCkOnboarding IS NOT NULL
BEGIN
    DECLARE @sqlDrop NVARCHAR(500);
    SET @sqlDrop = N'ALTER TABLE dbo.readers DROP CONSTRAINT ' + QUOTENAME(@tenCkOnboarding) + N';';
    EXEC sp_executesql @sqlDrop;
    PRINT N'OK: Đã drop CHECK onboarding cũ để cập nhật miền giá trị.';
END
ELSE
BEGIN
    PRINT N'SKIP: Không có CHECK onboarding cũ cần thay.';
END;
GO

ALTER TABLE dbo.readers
    WITH CHECK ADD CONSTRAINT CK_readers_trangthai_onboarding
    CHECK (trangthai_onboarding IN (0,1,2,3,4));
GO

PRINT N'OK: Đã thiết lập CHECK onboarding cho các trạng thái 0,1,2,3,4.';
GO

-- ============================================================
-- 2) Reader gửi yêu cầu kích hoạt: onboarding 1 -> 2
-- ============================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_reader_activation_request
    @reader_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @onboarding TINYINT;

    SELECT @onboarding = trangthai_onboarding
    FROM dbo.readers WITH (UPDLOCK, ROWLOCK)
    WHERE reader_id = @reader_id;

    IF @onboarding IS NULL
        THROW 51601, N'Khong tim thay ban doc.', 1;

    IF @onboarding <> 1
        THROW 51602, N'Chi tai khoan dang cho thanh toan moi duoc gui yeu cau kich hoat.', 1;

    UPDATE dbo.readers
    SET trangthai_onboarding = 2
    WHERE reader_id = @reader_id;
END;
GO

-- ============================================================
-- 3) Queue staff: chỉ lấy pending approval (onboarding = 2)
-- ============================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_reader_activation_pending
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.reader_id,
        r.hoten,
        r.email,
        r.so_the,
        r.sodienthoai,
        r.diachi,
        r.trangthai,
        r.trangthai_onboarding,
        r.ngaycap,
        r.ngayhethan
    FROM dbo.readers r
    WHERE r.trangthai_onboarding = 2
    ORDER BY r.ngaycap DESC, r.hoten ASC;
END;
GO

-- ============================================================
-- 4) Staff duyệt: onboarding 2 -> 3, readers.trangthai = 0
-- ============================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_reader_activation_approve
    @reader_id UNIQUEIDENTIFIER,
    @staff_id  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.users
        WHERE user_id = @staff_id
          AND role IN (N'Admin', N'ThuThu')
    )
        THROW 51603, N'Nguoi xu ly khong hop le hoac khong du quyen.', 1;

    DECLARE @onboarding TINYINT;

    SELECT @onboarding = trangthai_onboarding
    FROM dbo.readers WITH (UPDLOCK, ROWLOCK)
    WHERE reader_id = @reader_id;

    IF @onboarding IS NULL
        THROW 51601, N'Khong tim thay ban doc.', 1;

    IF @onboarding <> 2
        THROW 51604, N'Chi cho phep duyet yeu cau dang cho duyet.', 1;

    UPDATE dbo.readers
    SET trangthai_onboarding = 3,
        trangthai = 0
    WHERE reader_id = @reader_id;
END;
GO

-- ============================================================
-- 5) Staff từ chối: onboarding 2 -> 4, readers.trangthai = 2 (locked)
--    Bị từ chối sẽ không thể duyệt lại trong flow này.
-- ============================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_reader_activation_reject
    @reader_id UNIQUEIDENTIFIER,
    @staff_id  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.users
        WHERE user_id = @staff_id
          AND role IN (N'Admin', N'ThuThu')
    )
        THROW 51603, N'Nguoi xu ly khong hop le hoac khong du quyen.', 1;

    DECLARE @onboarding TINYINT;

    SELECT @onboarding = trangthai_onboarding
    FROM dbo.readers WITH (UPDLOCK, ROWLOCK)
    WHERE reader_id = @reader_id;

    IF @onboarding IS NULL
        THROW 51601, N'Khong tim thay ban doc.', 1;

    IF @onboarding <> 2
        THROW 51605, N'Chi cho phep tu choi yeu cau dang cho duyet.', 1;

    UPDATE dbo.readers
    SET trangthai_onboarding = 4,
        trangthai = 2
    WHERE reader_id = @reader_id;
END;
GO
