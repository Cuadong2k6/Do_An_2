-- ============================================================
-- Script 03: Dữ liệu mẫu (Seed Data) - Hệ Thống Quản Lý Thư Viện
-- Mật khẩu mặc định: 123456  →  MD5 = e10adc3949ba59abbe56e057f20f883e
-- ============================================================
USE DoAn2;
GO

-- ==========================
-- 1. TÀI KHOẢN QUẢN TRỊ
-- ==========================
-- Cập nhật / đảm bảo tồn tại tài khoản Admin mặc định
IF NOT EXISTS (SELECT 1 FROM users WHERE taikhoan = 'admin')
BEGIN
    INSERT INTO users (user_id, hoten, email, taikhoan, matkhau, role)
    VALUES (
        NEWID(),
        N'Quản Trị Viên',
        'admin@library.com',
        'admin',
        'e10adc3949ba59abbe56e057f20f883e',  -- 123456
        'Admin'
    );
    PRINT N'✅ Thêm tài khoản Admin thành công.';
END
ELSE
BEGIN
    -- Đảm bảo mật khẩu đúng là hash của 123456
    UPDATE users SET matkhau = 'e10adc3949ba59abbe56e057f20f883e', role = 'Admin'
    WHERE taikhoan = 'admin';
    PRINT N'✅ Đã cập nhật tài khoản Admin.';
END;

-- Thủ thư
IF NOT EXISTS (SELECT 1 FROM users WHERE taikhoan = 'thuthu')
BEGIN
    INSERT INTO users (user_id, hoten, email, taikhoan, matkhau, role)
    VALUES (
        NEWID(),
        N'Lê Thị Thư',
        'thuthu@library.com',
        'thuthu',
        'e10adc3949ba59abbe56e057f20f883e',  -- 123456
        'ThuThu'
    );
    PRINT N'✅ Thêm tài khoản Thủ thư thành công.';
END;
GO

-- ==========================
-- 2. SÁCH MẪU
-- ==========================
DECLARE @book1 UNIQUEIDENTIFIER = NEWID();
DECLARE @book2 UNIQUEIDENTIFIER = NEWID();
DECLARE @book3 UNIQUEIDENTIFIER = NEWID();
DECLARE @book4 UNIQUEIDENTIFIER = NEWID();

IF NOT EXISTS (SELECT 1 FROM books WHERE isbn = '978-604-1-01001-1')
BEGIN
    INSERT INTO books (book_id, title, isbn, tacgia, theloai, nxb, namxuatban, mota)
    VALUES
        (@book1, N'Cấu Trúc Dữ Liệu và Giải Thuật', '978-604-1-01001-1', N'Nguyễn Thanh Hải',   N'Lập trình',     N'NXB ĐHQG',      2022, N'Sách giáo khoa cấu trúc dữ liệu.'),
        (@book2, N'Lập Trình C# Căn Bản',           '978-604-1-01002-2', N'Microsoft Press',     N'Lập trình',     N'NXB Thông Tin', 2023, N'Nhập môn C# cho người mới bắt đầu.'),
        (@book3, N'Thiết Kế Cơ Sở Dữ Liệu',        '978-604-1-01003-3', N'Trần Minh Phúc',      N'Cơ sở dữ liệu', N'NXB ĐHQG',     2021, N'Thiết kế và tối ưu hóa SQL Server.'),
        (@book4, N'Nhà Giả Kim',                    '978-604-1-01004-4', N'Paulo Coelho',         N'Văn học',       N'NXB Hội Nhà Văn', 2020, N'Tiểu thuyết nổi tiếng thế giới.');
    PRINT N'✅ Thêm 4 đầu sách mẫu thành công.';

    -- ==========================
    -- 3. BẢN SAO VẬT LÝ (copies)
    -- ==========================
    DECLARE @shelf INT = NULL; -- chưa có kệ sách

    INSERT INTO copies (copy_id, book_id, mabancao, status)
    VALUES
        -- Sách 1: 3 bản
        (NEWID(), @book1, 'BK-001-01', 0),
        (NEWID(), @book1, 'BK-001-02', 0),
        (NEWID(), @book1, 'BK-001-03', 0),
        -- Sách 2: 2 bản
        (NEWID(), @book2, 'BK-002-01', 0),
        (NEWID(), @book2, 'BK-002-02', 0),
        -- Sách 3: 2 bản
        (NEWID(), @book3, 'BK-003-01', 0),
        (NEWID(), @book3, 'BK-003-02', 0),
        -- Sách 4: 1 bản
        (NEWID(), @book4, 'BK-004-01', 0);
    PRINT N'✅ Thêm 8 bản sao vật lý thành công.';
END
ELSE
    PRINT N'⚠️ Sách mẫu đã tồn tại, bỏ qua.';
GO

-- ==========================
-- 4. BẠN ĐỌC MẪU
-- ==========================
DECLARE @reader1 UNIQUEIDENTIFIER = NEWID();
DECLARE @reader2 UNIQUEIDENTIFIER = NEWID();

IF NOT EXISTS (SELECT 1 FROM readers WHERE email = 'bandoc1@gmail.com')
BEGIN
    INSERT INTO readers (reader_id, hoten, email, sodienthoai, diachi, so_the, matkhau, ngayhethan, trangthai, somughin)
    VALUES
        (@reader1, N'Nguyễn Văn An',  'bandoc1@gmail.com', '0901234567', N'Hà Nội',      'THE-001',
         'e10adc3949ba59abbe56e057f20f883e',  -- 123456
         DATEADD(YEAR, 1, GETDATE()), 0, 3),

        (@reader2, N'Trần Thị Bình',  'bandoc2@gmail.com', '0912345678', N'TP.HCM',      'THE-002',
         'e10adc3949ba59abbe56e057f20f883e',  -- 123456
         DATEADD(YEAR, 1, GETDATE()), 0, 3);
    PRINT N'✅ Thêm 2 bạn đọc mẫu thành công.';
END
ELSE
    PRINT N'⚠️ Bạn đọc mẫu đã tồn tại, bỏ qua.';
GO

-- ==========================
-- 5. SỬA DỮ LIỆU TIẾNG VIỆT BỊ LỖI ENCODING (mojibake)
--    (do chạy file SQL không đúng mã nguồn UTF-8)
-- ==========================
UPDATE books
SET title  = N'Cấu Trúc Dữ Liệu và Giải Thuật',
    tacgia = N'Nguyễn Thanh Hải',
    theloai = N'Lập trình',
    nxb = N'NXB ĐHQG',
    mota = N'Sách giáo khoa cấu trúc dữ liệu.'
WHERE isbn = '978-604-1-01001-1';

UPDATE books
SET title  = N'Lập Trình C# Căn Bản',
    tacgia = N'Microsoft Press',
    theloai = N'Lập trình',
    nxb = N'NXB Thông Tin',
    mota = N'Nhập môn C# cho người mới bắt đầu.'
WHERE isbn = '978-604-1-01002-2';

UPDATE books
SET title  = N'Thiết Kế Cơ Sở Dữ Liệu',
    tacgia = N'Trần Minh Phúc',
    theloai = N'Cơ sở dữ liệu',
    nxb = N'NXB ĐHQG',
    mota = N'Thiết kế và tối ưu hóa SQL Server.'
WHERE isbn = '978-604-1-01003-3';

UPDATE books
SET title  = N'Nhà Giả Kim',
    tacgia = N'Paulo Coelho',
    theloai = N'Văn học',
    nxb = N'NXB Hội Nhà Văn',
    mota = N'Tiểu thuyết nổi tiếng thế giới.'
WHERE isbn = '978-604-1-01004-4';

UPDATE readers SET hoten = N'Nguyễn Văn An', diachi = N'Hà Nội'    WHERE so_the = 'THE-001';
UPDATE readers SET hoten = N'Trần Thị Bình', diachi = N'TP.HCM'     WHERE so_the = 'THE-002';

UPDATE users SET hoten = N'Lê Thị Thư' WHERE taikhoan = 'thuthu';

PRINT N'✅ Đã sửa dữ liệu tiếng Việt bị lỗi encoding.';

-- ==========================
-- KIỂM TRA KẾT QUẢ
-- ==========================
SELECT N'users'   AS [Bảng], COUNT(*) AS [Số lượng] FROM users
UNION ALL
SELECT N'books',    COUNT(*) FROM books
UNION ALL
SELECT N'copies',   COUNT(*) FROM copies
UNION ALL
SELECT N'readers',  COUNT(*) FROM readers;
GO

PRINT N'✅ Seed data hoàn thành!';
GO
