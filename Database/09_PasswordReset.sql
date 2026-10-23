-- ============================================================
-- Script 09: Quên mật khẩu / Đặt lại mật khẩu (Password Reset)
-- Database: DoAn2
--
-- Token GẮN account_id (reader_id / user_id), KHÔNG gắn email.
-- Cột email trên token chỉ audit (email đang lưu trên tài khoản).
--
-- Luồng:
--   1) sp_pwreset_create_token  @email, @token_hash, @account_type OUTPUT
--      - App sinh CSPRNG 32 bytes, SHA-256 hex thường => @token_hash
--        VARCHAR(64), đúng 64 byte [0-9a-f]. SQL không sinh, không lưu raw token.
--      - Một applock transaction-owned Exclusive, resource HẰNG
--        'pwreset:issue'. Serialize MỌI phát hành, không phụ thuộc
--        collation cột email hay database. Reset hiếm: correctness > throughput.
--        KHÔNG hash email, KHÔNG canonical hóa CI_AI làm khóa lock.
--      - Predicate email = @norm trên cột (collation + index DB).
--        Không ép COLLATE lên cột. Không khẳng định cột = Latin1_General_CI_AI.
--      - Trong applock: UPDLOCK, HOLDLOCK trên users.
--        Range lock giữ nguyên để đọc nhất quán; lock global là lớp
--        serialize giữa các collation khác nhau.
--      - Chỉ đúng 1 user_id. Reader-only/không có: 50201; trùng users: 50202.
--      - Vô hiệu token used_at IS NULL của đúng account_type + account_id.
--        TTL 30 phút, một lần.
--      - @account_type luôn 'users', chỉ gán khi sắp ghi.
--        50201 / 50202 phân biệt nội bộ; API public vẫn generic.
--      - Mọi lỗi: @account_type = NULL. Runtime error sau BEGIN TRAN
--        vào CATCH: rollback đúng một lần nếu XACT_STATE() <> 0, rồi THROW.
--   2) sp_pwreset_consume_token @token_hash, @matkhau_hash
--      - @token_hash VARCHAR(64). DATALENGTH = 64 và chỉ [0-9a-f].
--        CHAR(64) + LEN nuốt trailing space; input không còn CHAR.
--      - App băm raw token (không gửi raw xuống SQL).
--      - UPDLOCK, HOLDLOCK theo token_hash. Hết hạn / đã dùng / không có:
--        50203, không đổi mật khẩu.
--      - used_at = GETDATE() rồi chỉ cập nhật users.
--        Một transaction. Token một lần.
--
-- Mã lỗi:
--   50201 không có tài khoản
--   50202 email trùng (cùng bảng hoặc cả hai bảng)
--   50203 token không hợp lệ / hết hạn / đã dùng
--   50204 token_hash không phải SHA-256 hex thường
--   50205 không lấy được applock
--   50206 email audit đang lưu dài hơn NVARCHAR(200), không thu hẹp
-- ============================================================

USE DoAn2;
GO

IF OBJECT_ID(N'dbo.password_reset_tokens', N'U') IS NULL
BEGIN
    CREATE TABLE password_reset_tokens (
        token_id     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        account_id   UNIQUEIDENTIFIER NOT NULL,
        email        NVARCHAR(200)    NOT NULL,
        account_type VARCHAR(10)      NOT NULL,
        token_hash   CHAR(64)         NOT NULL,
        expires_at   DATETIME         NOT NULL,
        used_at      DATETIME         NULL,
        created_at   DATETIME         NOT NULL CONSTRAINT df_pwreset_created DEFAULT (GETDATE()),
        CONSTRAINT ck_pwreset_account_type CHECK (account_type IN ('readers', 'users')),
        CONSTRAINT ck_pwreset_token_hash CHECK (
            DATALENGTH(token_hash) = 64
            AND token_hash COLLATE Latin1_General_BIN NOT LIKE '%[^0-9a-f]%'
        )
    );
    PRINT N'Đã tạo bảng password_reset_tokens.';
END
ELSE
BEGIN
    PRINT N'Bảng password_reset_tokens đã tồn tại, bỏ qua CREATE.';
    -- Schema gốc email = NVARCHAR(200). Bảng tạo lúc còn NVARCHAR(255)
    -- thì thu hẹp; từ chối nếu có giá trị dài hơn 200 (không cắt im).
    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.password_reset_tokens')
          AND c.name = N'email'
          AND c.max_length > 400
    )
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.password_reset_tokens WHERE DATALENGTH(email) > 400)
            THROW 50206, N'password_reset_tokens.email có giá trị dài hơn NVARCHAR(200); không thu hẹp.', 1;
        ALTER TABLE dbo.password_reset_tokens ALTER COLUMN email NVARCHAR(200) NOT NULL;
        PRINT N'Đã thu hẹp password_reset_tokens.email về NVARCHAR(200).';
    END
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'ix_pwreset_account_unused'
      AND object_id = OBJECT_ID(N'dbo.password_reset_tokens')
)
BEGIN
    CREATE INDEX ix_pwreset_account_unused
        ON password_reset_tokens (account_type, account_id)
        WHERE used_at IS NULL;
    PRINT N'Đã tạo index ix_pwreset_account_unused.';
END
ELSE
    PRINT N'Index ix_pwreset_account_unused đã tồn tại, bỏ qua.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'ix_pwreset_token_hash'
      AND object_id = OBJECT_ID(N'dbo.password_reset_tokens')
)
BEGIN
    CREATE UNIQUE INDEX ix_pwreset_token_hash ON password_reset_tokens (token_hash);
    PRINT N'Đã tạo index ix_pwreset_token_hash.';
END
ELSE
    PRINT N'Index ix_pwreset_token_hash đã tồn tại, bỏ qua.';
GO

-- ============================================================
-- sp_pwreset_create_token
--   50201 không có tài khoản
--   50202 email trùng
--   50204 token_hash không phải SHA-256 hex thường
--   50205 không lấy được applock
-- ============================================================
CREATE OR ALTER PROCEDURE sp_pwreset_create_token
    @email        NVARCHAR(200),
    @token_hash   VARCHAR(64),
    @account_type VARCHAR(10) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @account_type = NULL;

    BEGIN TRY
        IF @token_hash IS NULL
           OR DATALENGTH(@token_hash) <> 64
           OR @token_hash COLLATE Latin1_General_BIN LIKE '%[^0-9a-f]%'
            THROW 50204, N'token_hash phải là SHA-256 hex thường (64 ký tự 0-9a-f).', 1;

        DECLARE @norm NVARCHAR(200) = LTRIM(RTRIM(@email));
        IF @norm IS NULL OR @norm = N''
            THROW 50201, N'Không tìm thấy tài khoản.', 1;

        DECLARE @lock INT;
        BEGIN TRAN;

        -- Resource hằng: mọi create xếp hàng, kể khi collation cột khác nhau.
        EXEC @lock = sp_getapplock
            @Resource = N'pwreset:issue',
            @LockMode = N'Exclusive',
            @LockOwner = N'Transaction',
            @LockTimeout = 10000;

        IF @lock < 0
            THROW 50205, N'Không lấy được khóa phát hành token.', 1;

        DECLARE @user_id UNIQUEIDENTIFIER = NULL;
        DECLARE @user_n INT = 0;
        DECLARE @audit_email NVARCHAR(200) = NULL;
        DECLARE @account_id UNIQUEIDENTIFIER = NULL;

        SELECT @user_n = COUNT(*)
        FROM users WITH (UPDLOCK, HOLDLOCK)
        WHERE email = @norm;

        IF @user_n = 1
            SELECT TOP (1) @user_id = user_id, @audit_email = email
            FROM users WITH (UPDLOCK, HOLDLOCK)
            WHERE email = @norm
            ORDER BY user_id;

        IF @user_n = 0
            THROW 50201, N'Không tìm thấy tài khoản.', 1;

        IF @user_n > 1
            THROW 50202, N'Email gắn nhiều tài khoản.', 1;

        SET @account_type = 'users';
        SET @account_id = @user_id;

        UPDATE password_reset_tokens
           SET used_at = GETDATE()
         WHERE account_type = @account_type
           AND account_id = @account_id
           AND used_at IS NULL;

        INSERT INTO password_reset_tokens (account_id, email, account_type, token_hash, expires_at)
        VALUES (@account_id, @audit_email, @account_type, @token_hash, DATEADD(MINUTE, 30, GETDATE()));

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK;
        SET @account_type = NULL;
        THROW;
    END CATCH
END
GO

-- ============================================================
-- sp_pwreset_consume_token
--   50203 token không hợp lệ / hết hạn / đã dùng
--   50204 token_hash không phải SHA-256 hex thường
-- ============================================================
CREATE OR ALTER PROCEDURE sp_pwreset_consume_token
    @token_hash   VARCHAR(64),
    @matkhau_hash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @token_hash IS NULL
       OR DATALENGTH(@token_hash) <> 64
       OR @token_hash COLLATE Latin1_General_BIN LIKE '%[^0-9a-f]%'
        THROW 50204, N'token_hash phải là SHA-256 hex thường (64 ký tự 0-9a-f).', 1;

    IF @matkhau_hash IS NULL OR LEN(@matkhau_hash) = 0
        THROW 50203, N'Token không hợp lệ.', 1;

    DECLARE @account_id UNIQUEIDENTIFIER, @account_type VARCHAR(10);

    BEGIN TRY
        BEGIN TRAN;

        SELECT @account_id = account_id,
               @account_type = account_type
        FROM password_reset_tokens WITH (UPDLOCK, HOLDLOCK)
        WHERE token_hash = @token_hash
          AND used_at IS NULL
          AND expires_at > GETDATE();

        IF @account_id IS NULL
            THROW 50203, N'Token không hợp lệ, hết hạn, hoặc đã dùng.', 1;

        UPDATE password_reset_tokens
           SET used_at = GETDATE()
         WHERE token_hash = @token_hash
           AND used_at IS NULL;

        IF @@ROWCOUNT <> 1
            THROW 50203, N'Token không hợp lệ, hết hạn, hoặc đã dùng.', 1;

        IF @account_type = 'users'
            UPDATE users SET matkhau = @matkhau_hash WHERE user_id = @account_id;
        ELSE
            THROW 50203, N'Token không hợp lệ.', 1;

        IF @@ROWCOUNT <> 1
            THROW 50203, N'Token không hợp lệ.', 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
