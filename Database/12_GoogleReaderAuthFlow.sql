-- ============================================================
-- Script 12: Google Reader Auth Flow
-- Database: DoAn2
-- Phạm vi:
--   (1) ALTER readers.matkhau nullable
--   (2) sp_google_reader_login: atomic resolve Google login
--   (3) Password reset restriction được định nghĩa duy nhất trong Script 09.
-- Idempotent: chạy nhiều lần an toàn; yêu cầu Script 09 đã chạy.
-- KHÔNG áp dụng data migration/backfill
-- ============================================================
USE DoAn2;
GO

-- -----------------------------------------------
-- 1. ALTER readers.matkhau NULLABLE
-- Reader dùng Google OAuth không có mật khẩu.
-- Kiểm tra trước khi ALTER để idempotent.
-- -----------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.readers')
      AND name = N'matkhau'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.readers ALTER COLUMN matkhau NVARCHAR(255) NULL;
    PRINT N'Da ALTER readers.matkhau sang NULLABLE.';
END
ELSE
BEGIN
    PRINT N'readers.matkhau da NULLABLE hoac khong ton tai, bo qua.';
END
GO

-- -----------------------------------------------
-- 2. sp_google_reader_login
-- Resolve reader từ Google OAuth token.
--
-- Tham số:
--   @google_sub  - Google "sub" claim (stable reader ID)
--   @email       - Email đã verify từ Google (NVARCHAR 200)
--   @hoten       - Họ tên từ Google profile (NVARCHAR 200)
--   @so_the      - Số thẻ do app sinh (GUID format, NVARCHAR 50)
--
-- Luồng (atomic, transaction + applock exclusive):
--   a) Tìm reader theo google_sub → nếu có, trả về unchanged
--   b) Không có sub → tìm reader theo email (UPDLOCK/HOLDLOCK)
--      - Có row, google_sub NULL → set google_sub
--      - Có row, google_sub khác → conflict (đã gắn account khác)
--   c) Email tồn tại trong users → conflict (không tạo/link reader)
--   d) Không có account nào → INSERT reader mới
--      matkhau=NULL, trangthai=0, trangthai_onboarding=1
--
-- Mã status_code:
--   0     - Thành công
--   50301 - Email đã gắn user
--   50302 - Email đã gắn reader khác (sub conflict)
--   50303 - Không lấy được applock (THROW)
--   50304 - Input bắt buộc không hợp lệ (THROW)
--   50305 - google_sub tồn tại nhưng email đã đổi
--   50306 - so_the trùng; caller sinh card GUID khác rồi retry
--
-- Kết quả trả về (SELECT):
--   reader_id, hoten, email, so_the, trangthai,
--   trangthai_onboarding, is_new (BIT), status_code (INT)
-- -----------------------------------------------
CREATE OR ALTER PROCEDURE sp_google_reader_login
    @google_sub NVARCHAR(255),
    @email      NVARCHAR(200),
    @hoten      NVARCHAR(200),
    @so_the     NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @google_sub IS NULL OR LTRIM(RTRIM(@google_sub)) = N''
        THROW 50304, N'google_sub bat buoc.', 1;

    DECLARE @norm_email NVARCHAR(200) = LTRIM(RTRIM(@email));
    IF @norm_email IS NULL OR @norm_email = N''
        THROW 50304, N'email bat buoc.', 1;

        DECLARE @norm_hoten NVARCHAR(200) = LTRIM(RTRIM(@hoten));
        IF @norm_hoten IS NULL OR @norm_hoten = N''
            THROW 50304, N'hoten bat buoc.', 1;

    DECLARE @norm_sothe NVARCHAR(50) = LTRIM(RTRIM(@so_the));
    IF @norm_sothe IS NULL OR @norm_sothe = N''
        THROW 50304, N'so_the bat buoc.', 1;

    BEGIN TRY
        BEGIN TRAN;

        -- Applock exclusive global: serialize mọi call
        DECLARE @lock INT;
        EXEC @lock = sp_getapplock
            @Resource = N'google_reader_login',
            @LockMode = N'Exclusive',
            @LockOwner = N'Transaction',
            @LockTimeout = 10000;

        IF @lock < 0
            THROW 50303, N'Khong lay duoc khoa google_reader_login.', 1;

        DECLARE @found_reader_id UNIQUEIDENTIFIER = NULL;
        DECLARE @found_hoten NVARCHAR(200);
        DECLARE @found_email NVARCHAR(200);
        DECLARE @found_sothe NVARCHAR(50);
        DECLARE @found_trangthai INT;
        DECLARE @found_onboarding INT;
        DECLARE @is_new BIT = 0;
        DECLARE @status_code INT = 0;
        DECLARE @existing_sub NVARCHAR(255);

        -- Bước a: Tìm theo google_sub
        SELECT TOP (1)
            @found_reader_id = reader_id,
            @found_hoten = hoten,
            @found_email = email,
            @found_sothe = so_the,
            @found_trangthai = trangthai,
            @found_onboarding = trangthai_onboarding
        FROM dbo.readers WITH (UPDLOCK, HOLDLOCK)
        WHERE google_sub = @google_sub;

        IF @found_reader_id IS NOT NULL
        BEGIN
            -- sub định danh chính. Email chỉ dùng để liên kết ban đầu;
            -- không tự đổi email hồ sơ khi Google account đổi email.
            IF @found_email <> @norm_email
            BEGIN
                SET @status_code = 50305;
                SELECT reader_id = CAST(NULL AS UNIQUEIDENTIFIER),
                       hoten = CAST(NULL AS NVARCHAR(200)), email = @found_email,
                       so_the = CAST(NULL AS NVARCHAR(50)), trangthai = CAST(NULL AS INT),
                       trangthai_onboarding = CAST(NULL AS INT), is_new = CAST(0 AS BIT),
                       status_code = @status_code;
                COMMIT;
                RETURN;
            END;

            SET @status_code = 0;
            SELECT
                reader_id = @found_reader_id,
                hoten = @found_hoten,
                email = @found_email,
                so_the = @found_sothe,
                trangthai = @found_trangthai,
                trangthai_onboarding = @found_onboarding,
                is_new = @is_new,
                status_code = @status_code;

            COMMIT;
            RETURN;
        END

        -- Bước b: Tìm theo email (UPDLOCK/HOLDLOCK)
        SELECT TOP (1)
            @found_reader_id = reader_id,
            @found_hoten = hoten,
            @found_email = email,
            @found_sothe = so_the,
            @found_trangthai = trangthai,
            @found_onboarding = trangthai_onboarding,
            @existing_sub = google_sub
        FROM dbo.readers WITH (UPDLOCK, HOLDLOCK)
        WHERE email = @norm_email;

        IF @found_reader_id IS NOT NULL
        BEGIN
            IF @existing_sub IS NULL
            BEGIN
                -- Reader tồn tại chưa gắn sub → link
                UPDATE dbo.readers
                SET google_sub = @google_sub
                WHERE reader_id = @found_reader_id;

                SET @status_code = 0;
                SELECT
                    reader_id = @found_reader_id,
                    hoten = @found_hoten,
                    email = @found_email,
                    so_the = @found_sothe,
                    trangthai = @found_trangthai,
                    trangthai_onboarding = @found_onboarding,
                    is_new = @is_new,
                    status_code = @status_code;

                COMMIT;
                RETURN;
            END
            ELSE
            BEGIN
                -- Email đã gắn sub khác → conflict
                SET @status_code = 50302;
                SELECT
                    reader_id = CAST(NULL AS UNIQUEIDENTIFIER),
                    hoten = CAST(NULL AS NVARCHAR(200)),
                    email = CAST(NULL AS NVARCHAR(200)),
                    so_the = CAST(NULL AS NVARCHAR(50)),
                    trangthai = CAST(NULL AS INT),
                    trangthai_onboarding = CAST(NULL AS INT),
                    is_new = CAST(0 AS BIT),
                    status_code = @status_code;

                COMMIT;
                RETURN;
            END
        END

        -- Không tạo reader với email đã thuộc Admin/Thủ thư.
        IF EXISTS (
            SELECT 1 FROM dbo.users WITH (UPDLOCK, HOLDLOCK)
            WHERE email = @norm_email
        )
        BEGIN
            SET @status_code = 50301;
            SELECT reader_id = CAST(NULL AS UNIQUEIDENTIFIER),
                   hoten = CAST(NULL AS NVARCHAR(200)), email = @norm_email,
                   so_the = CAST(NULL AS NVARCHAR(50)), trangthai = CAST(NULL AS INT),
                   trangthai_onboarding = CAST(NULL AS INT), is_new = CAST(0 AS BIT),
                   status_code = @status_code;
            COMMIT;
            RETURN;
        END;

        -- Bước d: Tạo reader mới
        SET @found_reader_id = NEWID();
        SET @is_new = 1;
        SET @status_code = 0;

        IF EXISTS (
            SELECT 1 FROM dbo.readers WITH (UPDLOCK, HOLDLOCK)
            WHERE so_the = @norm_sothe
        )
        BEGIN
            SET @status_code = 50306;
            SELECT reader_id = CAST(NULL AS UNIQUEIDENTIFIER),
                   hoten = CAST(NULL AS NVARCHAR(200)), email = @norm_email,
                   so_the = @norm_sothe, trangthai = CAST(NULL AS INT),
                   trangthai_onboarding = CAST(NULL AS INT), is_new = CAST(0 AS BIT),
                   status_code = @status_code;
            COMMIT;
            RETURN;
        END;

        INSERT INTO dbo.readers (
            reader_id, hoten, email, sodienthoai, diachi,
            so_the, matkhau, ngaycap, ngayhethan,
            trangthai, somughin, google_sub, trangthai_onboarding
        )
        VALUES (
            @found_reader_id,
            @norm_hoten,
            @norm_email,
            NULL,           -- sodienthoai
            NULL,           -- diachi
            @norm_sothe,
            NULL,           -- matkhau (reader Google không có password)
            GETDATE(),      -- ngaycap
            NULL,           -- ngayhethan
            0,              -- trangthai default = 0 (Hoạt động)
            3,              -- somughin default
            @google_sub,
            1               -- trangthai_onboarding = 1 (PendingPayment)
        );

        SELECT
            reader_id = @found_reader_id,
            hoten = @norm_hoten,
            email = @norm_email,
            so_the = @norm_sothe,
            trangthai = 0,
            trangthai_onboarding = 1,
            is_new = @is_new,
            status_code = @status_code;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK;
        THROW;
    END CATCH
END
GO

-- Reader self-service: chỉ cho sửa tên, điện thoại, địa chỉ.
-- Không nhận email, số thẻ, password, trạng thái hay hạn thẻ từ client.
CREATE OR ALTER PROCEDURE sp_reader_update_profile
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

-- -----------------------------------------------
-- Password reset procedures nằm trong Script 09, sau khi tạo bảng.
-- Không lặp lại DDL phụ thuộc password_reset_tokens ở đây.
-- -----------------------------------------------
