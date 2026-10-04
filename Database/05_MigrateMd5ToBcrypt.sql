-- ============================================================
-- Script 05: Chuyển mật khẩu seed từ MD5 sang BCrypt
-- Mật khẩu mặc định: 123456
-- Chạy 1 lần duy nhất sau khi đã deploy code mới (AuthService chỉ nhận BCrypt)
-- Lưu ý: chỉ sửa dòng còn giữ hash MD5 cũ, tài khoản đã đổi mật khẩu được giữ nguyên
-- ============================================================
USE DoAn2;
GO
SET NOCOUNT ON;

DECLARE @hashMoi NVARCHAR(255) = N'$2a$12$h4B10Fio3SBtaN8DF0SOduZ5hBBYUeuuqC4lGkOFCw5eMx4VTF4ZK';  -- 123456
DECLARE @hashCu  NVARCHAR(255) = N'e10adc3949ba59abbe56e057f20f883e';                                -- MD5 cũ

UPDATE users
SET matkhau = @hashMoi
WHERE matkhau = @hashCu;

PRINT N'Đã cập nhật ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' tài khoản users.';

UPDATE readers
SET matkhau = @hashMoi
WHERE matkhau = @hashCu;

PRINT N'Đã cập nhật ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' tài khoản readers.';

-- Kiểm tra còn sót hash MD5 không
SELECT taikhoan AS tai_khoan, LEN(matkhau) AS do_dai FROM users WHERE matkhau NOT LIKE '$2%';
SELECT so_the    AS so_the,     LEN(matkhau) AS do_dai FROM readers WHERE matkhau NOT LIKE '$2%';
GO