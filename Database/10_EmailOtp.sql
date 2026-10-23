-- ============================================================
-- Script 10: Xác thực OTP qua email (Email OTP Verification)
-- Database: DoAn2
--
-- Unit ĐỘC LẬP: KHÔNG tạo tài khoản, KHÔNG đăng ký.
-- Luồng:
--   1) sp_otp_create  @email_norm, @otp_hash
--      - App sinh OTP 6 số bằng CSPRNG, HMAC-SHA256(secret, otp)
--        => @otp_hash CHAR(64) hex thường. SQL không sinh, không lưu OTP thô.
--      - OTP KHÔNG bị vô hiệu khi resend: mã cũ còn giá trị đến
--        expires_at / ngưỡng attempts của chính nó (chỉ INSERT thêm).
--      - Cooldown 60 giây THEO EMAIL, applock transaction-owned
--        resource 'otp:issue:<email>' serialize check-then-insert.
--   2) sp_otp_verify  @email_norm, @otp_hash, @proof_hash
--      - So khớp (email, otp_hash). Trạng thái phân biệt:
--        60202 sai mã / 60203 đã dùng / 60204 hết hạn / 60205 bị khóa.
--      - Nhập SAI: tăng attempts trên OTP hiện hành (mới nhất, chưa
--        dùng, còn hạn, chưa khóa). Đủ 5 lần sai => khóa OTP đó.
--      - Đúng mã: consume OTP (used_at) + INSERT proof đăng ký
--        15 phút, một lần, gắn email chuẩn hóa. Raw proof chỉ ở app.
--   3) sp_proof_consume @proof_hash, @email_norm OUTPUT
--      - Tiêu thu proof: chưa dùng + chưa hết hạn. Trả email_norm
--        cho bước tạo tài khoản (unit khác).
--
-- Mã lỗi (không đụng dải 502xx của script 09):
--   60201 cooldown 60s
--   60202 mã OTP không đúng
--   60203 mã OTP đã dùng
--   60204 mã OTP hết hạn
--   60205 mã OTP bị khóa (quá 5 lần sai)
--   60206 tham số hash/email không hợp lệ
--   60207 proof không hợp lệ / hết hạn / đã dùng
--   60208 không lấy được applock
-- ============================================================

USE DoAn2;
GO

IF OBJECT_ID(N'dbo.email_otps', N'U') IS NULL
BEGIN
    CREATE TABLE email_otps (
        otp_id     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        email_norm NVARCHAR(200)     NOT NULL,
        otp_hash   CHAR(64)          NOT NULL,
        expires_at DATETIME          NOT NULL,
        used_at    DATETIME          NULL,
        attempts   INT               NOT NULL CONSTRAINT df_otp_attempts DEFAULT (0),
        locked_at  DATETIME          NULL,
        created_at DATETIME          NOT NULL CONSTRAINT df_otp_created DEFAULT (GETDATE()),
        CONSTRAINT ck_otp_hash CHECK (
            DATALENGTH(otp_hash) = 64
            AND otp_hash COLLATE Latin1_General_BIN NOT LIKE '%[^0-9a-f]%'
        )
    );
    PRINT N'Đã tạo bảng email_otps.';
END
ELSE
    PRINT N'Bảng email_otps đã tồn tại, bỏ qua CREATE.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'ix_email_otps_email' AND object_id = OBJECT_ID(N'dbo.email_otps')
)
BEGIN
    CREATE INDEX ix_email_otps_email ON email_otps (email_norm, otp_hash);
    PRINT N'Đã tạo index ix_email_otps_email.';
END
GO

IF OBJECT_ID(N'dbo.registration_proofs', N'U') IS NULL
BEGIN
    CREATE TABLE registration_proofs (
        proof_id   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        email_norm NVARCHAR(200)     NOT NULL,
        proof_hash CHAR(64)          NOT NULL,
        expires_at DATETIME          NOT NULL,
        used_at    DATETIME          NULL,
        created_at DATETIME          NOT NULL CONSTRAINT df_proof_created DEFAULT (GETDATE()),
        CONSTRAINT uq_proof_hash UNIQUE (proof_hash),
        CONSTRAINT ck_proof_hash CHECK (
            DATALENGTH(proof_hash) = 64
            AND proof_hash COLLATE Latin1_General_BIN NOT LIKE '%[^0-9a-f]%'
        )
    );
    PRINT N'Đã tạo bảng registration_proofs.';
END
ELSE
    PRINT N'Bảng registration_proofs đã tồn tại, bỏ qua CREATE.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'ix_registration_proofs_hash' AND object_id = OBJECT_ID(N'dbo.registration_proofs')
)
BEGIN
    CREATE INDEX ix_registration_proofs_hash ON registration_proofs (proof_hash);
    PRINT N'Đã tạo index ix_registration_proofs_hash.';
END
GO

-- ============================================================
-- sp_otp_create
--   60201 cooldown 60s   60206 tham số sai   60208 applock
--   Chỉ INSERT — OTP cũ KHÔNG bị hủy khi resend.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_otp_create
    @email_norm NVARCHAR(200),
    @otp_hash   CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @otp_hash IS NULL
       OR DATALENGTH(@otp_hash) <> 64
       OR @otp_hash COLLATE Latin1_General_BIN LIKE '%[^0-9a-f]%'
        THROW 60206, N'otp_hash phải là HMAC-SHA256 hex thường (64 ký tự 0-9a-f).', 1;

    IF @email_norm IS NULL OR @email_norm = N'' OR LEN(@email_norm) > 200
        THROW 60206, N'email không hợp lệ.', 1;

    DECLARE @lock INT;
    DECLARE @resource NVARCHAR(255) = N'otp:issue:' + @email_norm;
    BEGIN TRAN;

    EXEC @lock = sp_getapplock
        @Resource   = @resource,
        @LockMode   = N'Exclusive',
        @LockOwner  = N'Transaction',
        @LockTimeout = 10000;

    IF @lock < 0
        THROW 60208, N'Không lấy được khóa phát hành OTP.', 1;

    -- Cooldown 60 giây theo email, bất kể còn OTP cũ hợp lệ hay không.
    IF EXISTS (
        SELECT 1 FROM email_otps
        WHERE email_norm = @email_norm
          AND created_at > DATEADD(SECOND, -60, GETDATE())
    )
    BEGIN
        COMMIT;
        THROW 60201, N'Bạn vừa yêu cầu mã OTP. Vui lòng đợi 60 giây trước khi yêu cầu mã mới.', 1;
    END

    INSERT INTO email_otps (email_norm, otp_hash, expires_at)
    VALUES (@email_norm, @otp_hash, DATEADD(MINUTE, 5, GETDATE()));

    COMMIT;
END;
GO

-- ============================================================
-- sp_otp_verify
--   60202 sai mã   60203 đã dùng   60204 hết hạn   60205 bị khóa
--   60206 tham số sai
--   Đúng mã: consume OTP + phát hành proof 15 phút (một lần).
-- ============================================================
CREATE OR ALTER PROCEDURE sp_otp_verify
    @email_norm NVARCHAR(200),
    @otp_hash   CHAR(64),
    @proof_hash CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @otp_hash IS NULL
       OR DATALENGTH(@otp_hash) <> 64
       OR @otp_hash COLLATE Latin1_General_BIN LIKE '%[^0-9a-f]%'
        THROW 60206, N'otp_hash phải là HMAC-SHA256 hex thường (64 ký tự 0-9a-f).', 1;

    IF @proof_hash IS NULL
       OR DATALENGTH(@proof_hash) <> 64
       OR @proof_hash COLLATE Latin1_General_BIN LIKE '%[^0-9a-f]%'
        THROW 60206, N'proof_hash phải là SHA-256 hex thường (64 ký tự 0-9a-f).', 1;

    IF @email_norm IS NULL OR @email_norm = N'' OR LEN(@email_norm) > 200
        THROW 60206, N'email không hợp lệ.', 1;

    DECLARE @otp_id   INT,
            @used     DATETIME,
            @expires  DATETIME,
            @attempts INT,
            @locked   DATETIME;

    BEGIN TRAN;

    SELECT TOP (1)
           @otp_id   = otp_id,
           @used     = used_at,
           @expires  = expires_at,
           @attempts = attempts,
           @locked   = locked_at
    FROM email_otps WITH (UPDLOCK, HOLDLOCK)
    WHERE email_norm = @email_norm
      AND otp_hash   = @otp_hash
    ORDER BY otp_id DESC;

    IF @otp_id IS NULL
    BEGIN
        -- Nhập sai: tăng attempts trên OTP hiện hành (mới nhất, chưa dùng,
        -- còn hạn, chưa khóa). Đủ 5 lần sai => khóa OTP đó.
        UPDATE email_otps
           SET attempts  = attempts + 1,
               locked_at = CASE WHEN attempts + 1 >= 5 THEN GETDATE() ELSE locked_at END
         WHERE otp_id = (
            SELECT TOP (1) otp_id
            FROM email_otps
            WHERE email_norm = @email_norm
              AND used_at IS NULL
              AND expires_at > GETDATE()
              AND locked_at IS NULL
              AND attempts < 5
            ORDER BY otp_id DESC
         );

        COMMIT;
        THROW 60202, N'Mã OTP không đúng.', 1;
    END

    IF @used IS NOT NULL
    BEGIN
        COMMIT;
        THROW 60203, N'Mã OTP đã được sử dụng.', 1;
    END

    IF @expires <= GETDATE()
    BEGIN
        COMMIT;
        THROW 60204, N'Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.', 1;
    END

    IF @locked IS NOT NULL OR @attempts >= 5
    BEGIN
        COMMIT;
        THROW 60205, N'Mã OTP đã bị khóa do nhập sai quá 5 lần. Vui lòng yêu cầu mã mới.', 1;
    END

    UPDATE email_otps
       SET used_at = GETDATE()
     WHERE otp_id = @otp_id
       AND used_at IS NULL;

    IF @@ROWCOUNT <> 1
    BEGIN
        COMMIT;
        THROW 60203, N'Mã OTP đã được sử dụng.', 1;
    END

    -- Proof đăng ký: raw chỉ tồn tại ở app, DB giữ SHA-256 hex.
    INSERT INTO registration_proofs (email_norm, proof_hash, expires_at)
    VALUES (@email_norm, @proof_hash, DATEADD(MINUTE, 15, GETDATE()));

    COMMIT;
END;
GO

-- ============================================================
-- sp_proof_consume
--   60207 proof không hợp lệ / hết hạn / đã dùng
--   Trả @email_norm cho bước tạo tài khoản (unit khác).
-- ============================================================
CREATE OR ALTER PROCEDURE sp_proof_consume
    @proof_hash  CHAR(64),
    @email_norm  NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @email_norm = NULL;

    IF @proof_hash IS NULL
       OR DATALENGTH(@proof_hash) <> 64
       OR @proof_hash COLLATE Latin1_General_BIN LIKE '%[^0-9a-f]%'
        THROW 60206, N'proof_hash phải là SHA-256 hex thường (64 ký tự 0-9a-f).', 1;

    BEGIN TRAN;

    SELECT @email_norm = email_norm
    FROM registration_proofs WITH (UPDLOCK, HOLDLOCK)
    WHERE proof_hash = @proof_hash
      AND used_at IS NULL
      AND expires_at > GETDATE();

    IF @email_norm IS NULL
    BEGIN
        COMMIT;
        THROW 60207, N'Token xác thực không hợp lệ, hết hạn hoặc đã dùng.', 1;
    END

    UPDATE registration_proofs
       SET used_at = GETDATE()
     WHERE proof_hash = @proof_hash
       AND used_at IS NULL;

    IF @@ROWCOUNT <> 1
    BEGIN
        COMMIT;
        SET @email_norm = NULL;
        THROW 60207, N'Token xác thực không hợp lệ, hết hạn hoặc đã dùng.', 1;
    END

    COMMIT;
END;
GO
