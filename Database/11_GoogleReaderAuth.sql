-- =============================================
-- Script 11: Thêm cột hỗ trợ Google OAuth cho bạn đọc
-- Phạm vi: CHỈ schema migration, KHÔNG áp dụng dữ liệu
-- Idempotent: chạy nhiều lần an toàn
-- LƯU Ý: Không ALTER matkhau nullable tại đây.
--         Việc cho phép matkhau NULL sẽ xử lý riêng trong auth unit
--         sau khi reader password login/reset được bảo vệ,
--         tránh nguy cơ null runtime và mở reset reader.
-- =============================================
USE DoAn2;
GO

-- -----------------------------------------------
-- 1. Thêm cột google_sub
-- Lưu Google "sub" claim (subject identifier) từ token.
-- NULL = reader không dùng Google OAuth (manual).
-- -----------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('readers')
    AND name = 'google_sub'
)
BEGIN
    ALTER TABLE readers ADD google_sub NVARCHAR(255) NULL;
END;
GO

-- -----------------------------------------------
-- 2. Unique filtered index trên google_sub
-- Đảm bảo mỗi Google subject map đúng 1 reader.
-- Chỉ áp dụng cho dòng có google_sub IS NOT NULL.
-- -----------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_readers_google_sub'
    AND object_id = OBJECT_ID('readers')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_readers_google_sub
    ON readers(google_sub)
    WHERE google_sub IS NOT NULL;
END;
GO

-- -----------------------------------------------
-- 3. Thêm cột trangthai_onboarding
-- Tách biệt luồng onboarding Google khỏi trangthai hiện tại
-- (trangthai: 0=Hoạt động, 1=Hết hạn, 2=Bị khoá — GIỮ NGUYÊN).
--
-- Giá trị:
--   0 = KhongApDung (manual readers cũ, không qua onboarding)
--   1 = PendingPayment (đăng ký Google, chờ thanh toán)
--   2 = PendingApproval (đã thanh toán, chờ thủ thư duyệt)
--   3 = HoanTat (onboarding xong, reader active bình thường)
--
-- Default = 0: manual readers cũ KHÔNG bị đánh dấu pending.
-- -----------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('readers')
    AND name = 'trangthai_onboarding'
)
BEGIN
    ALTER TABLE readers ADD trangthai_onboarding TINYINT NOT NULL DEFAULT 0;
END;
GO

-- -----------------------------------------------
-- 4. Index tìm kiếm theo trạng thái onboarding
-- Hỗ trợ query lọc reader đang chờ thanh toán/duyệt.
-- -----------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_readers_trangthai_onboarding'
    AND object_id = OBJECT_ID('readers')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_readers_trangthai_onboarding
    ON readers(trangthai_onboarding);
END;
GO
