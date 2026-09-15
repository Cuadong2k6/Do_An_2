-- =============================================
-- Script 02: Stored Procedures - Hệ Thống Quản Lý Thư Viện
-- =============================================
USE LibraryDB;
GO

-- ==========================
-- SÁCH (Books)
-- ==========================

-- Tìm kiếm sách nâng cao (phân trang)
CREATE OR ALTER PROCEDURE sp_book_search
    @keyword    NVARCHAR(300) = NULL,
    @theloai    NVARCHAR(100) = NULL,
    @tacgia     NVARCHAR(200) = NULL,
    @namxuatban INT           = NULL,
    @page_index INT           = 1,
    @page_size  INT           = 10,
    @total      BIGINT        OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @offset INT = (@page_index - 1) * @page_size;

    SELECT @total = COUNT(*)
    FROM books
    WHERE (@keyword    IS NULL OR title  LIKE '%' + @keyword + '%' OR isbn LIKE '%' + @keyword + '%')
      AND (@theloai    IS NULL OR theloai = @theloai)
      AND (@tacgia     IS NULL OR tacgia  LIKE '%' + @tacgia + '%')
      AND (@namxuatban IS NULL OR namxuatban = @namxuatban);

    SELECT b.*,
           (SELECT COUNT(*) FROM copies c WHERE c.book_id = b.book_id) AS tongsobancao,
           (SELECT COUNT(*) FROM copies c WHERE c.book_id = b.book_id AND c.status = 0) AS sobancaosangio
    FROM books b
    WHERE (@keyword    IS NULL OR b.title  LIKE '%' + @keyword + '%' OR b.isbn LIKE '%' + @keyword + '%')
      AND (@theloai    IS NULL OR b.theloai = @theloai)
      AND (@tacgia     IS NULL OR b.tacgia  LIKE '%' + @tacgia + '%')
      AND (@namxuatban IS NULL OR b.namxuatban = @namxuatban)
    ORDER BY b.title
    OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;
END;
GO

-- Lấy chi tiết sách theo ID
CREATE OR ALTER PROCEDURE sp_book_getbyid
    @book_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT b.*,
           (SELECT COUNT(*) FROM copies c WHERE c.book_id = b.book_id) AS tongsobancao,
           (SELECT COUNT(*) FROM copies c WHERE c.book_id = b.book_id AND c.status = 0) AS sobancaosangio
    FROM books b
    WHERE b.book_id = @book_id;
END;
GO

-- Thêm sách mới
CREATE OR ALTER PROCEDURE sp_book_create
    @book_id    UNIQUEIDENTIFIER,
    @title      NVARCHAR(300),
    @isbn       VARCHAR(20),
    @tacgia     NVARCHAR(200) = NULL,
    @theloai    NVARCHAR(100) = NULL,
    @nxb        NVARCHAR(200) = NULL,
    @namxuatban INT           = NULL,
    @mota       NVARCHAR(MAX) = NULL,
    @image_url  NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO books (book_id, title, isbn, tacgia, theloai, nxb, namxuatban, mota, image_url)
    VALUES (@book_id, @title, @isbn, @tacgia, @theloai, @nxb, @namxuatban, @mota, @image_url);
END;
GO

-- ==========================
-- BẠN ĐỌC (Readers)
-- ==========================

-- Lấy danh sách bạn đọc (phân trang)
CREATE OR ALTER PROCEDURE sp_reader_getlist
    @keyword    NVARCHAR(200) = NULL,
    @trangthai  INT           = NULL,
    @page_index INT           = 1,
    @page_size  INT           = 10,
    @total      BIGINT        OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @offset INT = (@page_index - 1) * @page_size;

    SELECT @total = COUNT(*) FROM readers
    WHERE (@keyword   IS NULL OR hoten LIKE '%' + @keyword + '%' OR so_the LIKE '%' + @keyword + '%' OR email LIKE '%' + @keyword + '%')
      AND (@trangthai IS NULL OR trangthai = @trangthai);

    SELECT reader_id, hoten, email, sodienthoai, diachi, so_the, ngaycap, ngayhethan, trangthai, somughin
    FROM readers
    WHERE (@keyword   IS NULL OR hoten LIKE '%' + @keyword + '%' OR so_the LIKE '%' + @keyword + '%' OR email LIKE '%' + @keyword + '%')
      AND (@trangthai IS NULL OR trangthai = @trangthai)
    ORDER BY hoten
    OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;
END;
GO

-- Lấy chi tiết bạn đọc
CREATE OR ALTER PROCEDURE sp_reader_getbyid
    @reader_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT reader_id, hoten, email, sodienthoai, diachi, so_the, ngaycap, ngayhethan, trangthai, somughin
    FROM readers WHERE reader_id = @reader_id;
END;
GO

-- Đăng ký thẻ bạn đọc
CREATE OR ALTER PROCEDURE sp_reader_create
    @reader_id   UNIQUEIDENTIFIER,
    @hoten       NVARCHAR(200),
    @email       NVARCHAR(200),
    @sodienthoai VARCHAR(15)   = NULL,
    @diachi      NVARCHAR(300) = NULL,
    @so_the      NVARCHAR(50),
    @matkhau     NVARCHAR(255),
    @ngayhethan  DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO readers (reader_id, hoten, email, sodienthoai, diachi, so_the, matkhau, ngayhethan)
    VALUES (@reader_id, @hoten, @email, @sodienthoai, @diachi, @so_the, @matkhau, @ngayhethan);
END;
GO

-- Cập nhật thông tin bạn đọc
CREATE OR ALTER PROCEDURE sp_reader_update
    @reader_id   UNIQUEIDENTIFIER,
    @hoten       NVARCHAR(200),
    @sodienthoai VARCHAR(15)   = NULL,
    @diachi      NVARCHAR(300) = NULL,
    @trangthai   INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE readers SET hoten = @hoten, sodienthoai = @sodienthoai, diachi = @diachi, trangthai = @trangthai
    WHERE reader_id = @reader_id;
END;
GO

-- ==========================
-- MƯỢN TRẢ (Loans)
-- ==========================

-- Tạo phiếu mượn (nhận JSON danh sách copy_id)
CREATE OR ALTER PROCEDURE sp_loan_create
    @loan_id         UNIQUEIDENTIFIER,
    @reader_id       UNIQUEIDENTIFIER,
    @due_date        DATETIME,
    @listjson_chitiet NVARCHAR(MAX)    -- JSON: [{"copy_id":"..."},{"copy_id":"..."}]
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Kiểm tra reader tồn tại và còn hạn
        IF NOT EXISTS (SELECT 1 FROM readers WHERE reader_id = @reader_id AND trangthai = 0 AND ngayhethan >= GETDATE())
            THROW 50001, N'Thẻ bạn đọc không hợp lệ hoặc đã hết hạn.', 1;

        -- Tạo phiếu mượn
        INSERT INTO loans (loan_id, reader_id, due_date)
        VALUES (@loan_id, @reader_id, @due_date);

        -- Chèn chi tiết và cập nhật trạng thái bản sao
        INSERT INTO loan_details (loan_id, copy_id)
        SELECT @loan_id, copy_id
        FROM OPENJSON(@listjson_chitiet) WITH (copy_id UNIQUEIDENTIFIER '$.copy_id');

        UPDATE copies SET status = 1
        WHERE copy_id IN (
            SELECT copy_id FROM OPENJSON(@listjson_chitiet) WITH (copy_id UNIQUEIDENTIFIER '$.copy_id')
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Trả sách
CREATE OR ALTER PROCEDURE sp_loan_return
    @loan_id     UNIQUEIDENTIFIER,
    @return_date DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @return_date IS NULL SET @return_date = GETDATE();

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE loans SET return_date = @return_date, trangthai = 1 WHERE loan_id = @loan_id;
        UPDATE copies SET status = 0
        WHERE copy_id IN (SELECT copy_id FROM loan_details WHERE loan_id = @loan_id);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Lấy danh sách sách quá hạn
CREATE OR ALTER PROCEDURE sp_loan_get_overdue
    @current_date DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @current_date IS NULL SET @current_date = GETDATE();

    SELECT l.loan_id, l.reader_id, l.loan_date, l.due_date,
           r.hoten AS reader_hoten, r.so_the AS reader_so_the,
           DATEDIFF(DAY, l.due_date, @current_date) AS songaytre
    FROM loans l
    INNER JOIN readers r ON r.reader_id = l.reader_id
    WHERE l.trangthai = 0 AND l.due_date < @current_date
    ORDER BY l.due_date ASC;
END;
GO

-- Lấy lịch sử mượn của bạn đọc
CREATE OR ALTER PROCEDURE sp_loan_get_by_reader
    @reader_id  UNIQUEIDENTIFIER,
    @page_index INT = 1,
    @page_size  INT = 10,
    @total      BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @offset INT = (@page_index - 1) * @page_size;

    SELECT @total = COUNT(*) FROM loans WHERE reader_id = @reader_id;

    SELECT l.*, r.hoten AS reader_hoten, r.so_the AS reader_so_the
    FROM loans l
    INNER JOIN readers r ON r.reader_id = l.reader_id
    WHERE l.reader_id = @reader_id
    ORDER BY l.loan_date DESC
    OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;
END;
GO

-- ==========================
-- TIỀN PHẠT (Fines)
-- ==========================

-- Tính toán và tạo phiếu phạt (5.000đ/ngày)
CREATE OR ALTER PROCEDURE sp_fine_calculate
    @loan_id UNIQUEIDENTIFIER,
    @tilephattrehan DECIMAL(18,2) = 5000  -- VNĐ/ngày
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @songaytre INT;
    DECLARE @fine_id   UNIQUEIDENTIFIER = NEWID();

    SELECT @songaytre = DATEDIFF(DAY, due_date, ISNULL(return_date, GETDATE()))
    FROM loans WHERE loan_id = @loan_id;

    IF @songaytre > 0 AND NOT EXISTS (SELECT 1 FROM fines WHERE loan_id = @loan_id)
    BEGIN
        INSERT INTO fines (fine_id, loan_id, songaytre, amount)
        VALUES (@fine_id, @loan_id, @songaytre, @songaytre * @tilephattrehan);
    END;
END;
GO

-- Lấy phiếu phạt theo mượn
CREATE OR ALTER PROCEDURE sp_fine_get_by_loan
    @loan_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.*, r.hoten AS reader_hoten, r.so_the AS reader_so_the
    FROM fines f
    INNER JOIN loans l ON l.loan_id = f.loan_id
    INNER JOIN readers r ON r.reader_id = l.reader_id
    WHERE f.loan_id = @loan_id;
END;
GO

-- Thu tiền phạt
CREATE OR ALTER PROCEDURE sp_fine_payment
    @fine_id    UNIQUEIDENTIFIER,
    @sotien     DECIMAL(18,2),
    @phuongthuc NVARCHAR(50) = N'Tiền mặt',
    @ghichu     NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO payments (pay_id, fine_id, sotien, phuongthuc, ghichu)
        VALUES (NEWID(), @fine_id, @sotien, @phuongthuc, @ghichu);

        UPDATE fines SET is_paid = 1 WHERE fine_id = @fine_id;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- ==========================
-- XÁC THỰC (Auth)
-- ==========================

-- Đăng nhập Admin/Thủ thư
CREATE OR ALTER PROCEDURE sp_user_login
    @taikhoan NVARCHAR(100),
    @matkhau  NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT user_id, hoten, email, taikhoan, role, image_url
    FROM users
    WHERE taikhoan = @taikhoan AND matkhau = @matkhau;
END;
GO

-- Đăng nhập bạn đọc
CREATE OR ALTER PROCEDURE sp_reader_login
    @email   NVARCHAR(200),
    @matkhau NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT reader_id, hoten, email, so_the, trangthai, ngayhethan
    FROM readers
    WHERE email = @email AND matkhau = @matkhau AND trangthai = 0;
END;
GO

-- ==========================
-- BÁO CÁO (Reports)
-- ==========================

-- Báo cáo sách mượn nhiều nhất
CREATE OR ALTER PROCEDURE sp_report_sachmuonnhieu
    @topN INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@topN) b.book_id, b.title, b.tacgia, b.theloai,
           COUNT(ld.id) AS sotluongmuon
    FROM loan_details ld
    INNER JOIN copies c ON c.copy_id = ld.copy_id
    INNER JOIN books  b ON b.book_id = c.book_id
    GROUP BY b.book_id, b.title, b.tacgia, b.theloai
    ORDER BY sotluongmuon DESC;
END;
GO

-- Báo cáo tồn kho theo thể loại
CREATE OR ALTER PROCEDURE sp_report_tonkho
AS
BEGIN
    SET NOCOUNT ON;
    SELECT b.theloai,
           COUNT(DISTINCT b.book_id) AS sodausach,
           COUNT(c.copy_id)          AS tongbancao,
           SUM(CASE WHEN c.status = 0 THEN 1 ELSE 0 END) AS bansangio,
           SUM(CASE WHEN c.status = 1 THEN 1 ELSE 0 END) AS danmuon
    FROM books b
    LEFT JOIN copies c ON c.book_id = b.book_id
    GROUP BY b.theloai
    ORDER BY b.theloai;
END;
GO

PRINT 'Tạo Stored Procedures thành công!';
