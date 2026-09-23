-- =============================================
-- Script 02: Stored Procedures - Hệ Thống Quản Lý Thư Viện
-- =============================================
USE DoAn2;
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
    WHERE (@keyword    IS NULL OR title LIKE '%' + @keyword + '%' OR isbn LIKE '%' + @keyword + '%' OR tacgia LIKE '%' + @keyword + '%')
      AND (@theloai    IS NULL OR theloai = @theloai)
      AND (@tacgia     IS NULL OR tacgia  LIKE '%' + @tacgia + '%')
      AND (@namxuatban IS NULL OR namxuatban = @namxuatban);

    SELECT b.*,
           (SELECT COUNT(*) FROM copies c WHERE c.book_id = b.book_id) AS tongsobancao,
           (SELECT COUNT(*) FROM copies c WHERE c.book_id = b.book_id AND c.status = 0) AS sobancaosangio
    FROM books b
    WHERE (@keyword    IS NULL OR b.title LIKE '%' + @keyword + '%' OR b.isbn LIKE '%' + @keyword + '%' OR b.tacgia LIKE '%' + @keyword + '%')
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

-- Thêm sách mới (tự tạo bản sao theo tongsobancao)
CREATE OR ALTER PROCEDURE sp_book_create
    @book_id      UNIQUEIDENTIFIER,
    @title        NVARCHAR(300),
    @isbn         VARCHAR(20),
    @tacgia       NVARCHAR(200) = NULL,
    @theloai      NVARCHAR(100) = NULL,
    @nxb          NVARCHAR(200) = NULL,
    @namxuatban   INT           = NULL,
    @mota         NVARCHAR(MAX) = NULL,
    @image_url    NVARCHAR(500) = NULL,
    @tongsobancao INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM books WHERE isbn = @isbn)
        THROW 50101, N'ISBN đã tồn tại. Vui lòng nhập ISBN khác.', 1;

    IF @tongsobancao < 0
        THROW 50111, N'Tổng số bản sao phải lớn hơn hoặc bằng 0.', 1;

    INSERT INTO books (book_id, title, isbn, tacgia, theloai, nxb, namxuatban, mota, image_url)
    VALUES (@book_id, @title, @isbn, @tacgia, @theloai, @nxb, @namxuatban, @mota, @image_url);

    -- Tạo bản sao vật lý tương ứng (mã bản sao tự sinh, không trùng)
    DECLARE @them INT = ISNULL(@tongsobancao, 0), @ma NVARCHAR(50);
    WHILE @them > 0
    BEGIN
        SET @ma = CONCAT('BC-', @isbn, '-', LEFT(CAST(NEWID() AS NVARCHAR(36)), 6));
        IF NOT EXISTS (SELECT 1 FROM copies WHERE mabancao = @ma)
        BEGIN
            INSERT INTO copies (copy_id, book_id, mabancao, status)
            VALUES (NEWID(), @book_id, @ma, 0);
            SET @them = @them - 1;
        END
    END
END;
GO

-- Cập nhật sách (kèm điều chỉnh số bản sao)
CREATE OR ALTER PROCEDURE sp_book_update
    @book_id      UNIQUEIDENTIFIER,
    @title        NVARCHAR(300),
    @isbn         VARCHAR(20),
    @tacgia       NVARCHAR(200) = NULL,
    @theloai      NVARCHAR(100) = NULL,
    @nxb          NVARCHAR(200) = NULL,
    @namxuatban   INT           = NULL,
    @mota         NVARCHAR(MAX) = NULL,
    @image_url    NVARCHAR(500) = NULL,
    @tongsobancao INT           = NULL   -- NULL = giữ nguyên số bản sao
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM books WHERE book_id = @book_id)
        THROW 50026, N'Đầu sách không tồn tại.', 1;

    IF EXISTS (SELECT 1 FROM books WHERE isbn = @isbn AND book_id <> @book_id)
        THROW 50027, N'ISBN đã tồn tại.', 1;

    IF @tongsobancao < 0
        THROW 50111, N'Tổng số bản sao phải lớn hơn hoặc bằng 0.', 1;

    UPDATE books
    SET title = @title, isbn = @isbn, tacgia = @tacgia, theloai = @theloai,
        nxb = @nxb, namxuatban = @namxuatban, mota = @mota, image_url = @image_url
    WHERE book_id = @book_id;

    -- Điều chỉnh số bản sao theo yêu cầu
    IF @tongsobancao IS NOT NULL
    BEGIN
        DECLARE @hientai INT = (SELECT COUNT(*) FROM copies WHERE book_id = @book_id);

        -- TĂNG: tạo thêm bản sao mới (mã tự sinh, không trùng)
        IF @tongsobancao > @hientai
        BEGIN
            DECLARE @them INT = @tongsobancao - @hientai, @ma NVARCHAR(50);
            WHILE @them > 0
            BEGIN
                SET @ma = CONCAT('BC-', @isbn, '-', LEFT(CAST(NEWID() AS NVARCHAR(36)), 6));
                IF NOT EXISTS (SELECT 1 FROM copies WHERE mabancao = @ma)
                BEGIN
                    INSERT INTO copies (copy_id, book_id, mabancao, status)
                    VALUES (NEWID(), @book_id, @ma, 0);
                    SET @them = @them - 1;
                END
            END
        END

        -- GIẢM: chỉ xoá được bản sao CHƯA TỪNG mượn (copy có lịch sử mượn giữ lại do khoá ngoại)
        IF @tongsobancao < @hientai
        BEGIN
            DECLARE @choxoa INT = @hientai - @tongsobancao;

            DECLARE @trong INT = (
                SELECT COUNT(*) FROM copies c
                WHERE c.book_id = @book_id
                  AND NOT EXISTS (SELECT 1 FROM loan_details d WHERE d.copy_id = c.copy_id));

            IF @choxoa > @trong
            BEGIN
                DECLARE @msg NVARCHAR(500) = CONCAT(
                    N'Không thể giảm xuống ', @tongsobancao,
                    N': chỉ có ', @trong, N' bản sao chưa từng mượn (bản sao có lịch sử mượn không thể xoá).');
                THROW 50110, @msg, 1;
            END

            -- Ưu tiên xoá bản sao Hỏng trước khi xoá bản sao còn tốt
            ;WITH xoa AS (
                SELECT TOP (@choxoa) c.copy_id
                FROM copies c
                WHERE c.book_id = @book_id
                  AND NOT EXISTS (SELECT 1 FROM loan_details d WHERE d.copy_id = c.copy_id)
                ORDER BY CASE WHEN c.status = 2 THEN 0 ELSE 1 END, c.ngaynhap DESC, c.copy_id
            )
            DELETE FROM xoa;
        END
    END
END;
GO

-- Xoá sách (chỉ khi chưa có bản sao)
CREATE OR ALTER PROCEDURE sp_book_delete
    @book_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM books WHERE book_id = @book_id)
        THROW 50026, N'Đầu sách không tồn tại.', 1;

    IF EXISTS (SELECT 1 FROM copies WHERE book_id = @book_id)
        THROW 50028, N'Không thể xoá: đầu sách còn bản sao.', 1;

    DELETE FROM books WHERE book_id = @book_id;
END;
GO

-- ==========================
-- KỆ SÁCH (Shelves)
-- ==========================

-- Danh sách kệ sách (phân trang)
CREATE OR ALTER PROCEDURE sp_shelf_getlist
    @keyword    NVARCHAR(100) = NULL,
    @page_index INT = 1,
    @page_size  INT = 10,
    @total      BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @offset INT = (@page_index - 1) * @page_size;

    SELECT @total = COUNT(*)
    FROM shelves
    WHERE @keyword IS NULL OR location_code LIKE '%' + @keyword + '%' OR mota LIKE '%' + @keyword + '%';

    SELECT s.*,
           (SELECT COUNT(*) FROM copies c WHERE c.shelf_id = s.shelf_id) AS sobancao
    FROM shelves s
    WHERE @keyword IS NULL OR s.location_code LIKE '%' + @keyword + '%' OR s.mota LIKE '%' + @keyword + '%'
    ORDER BY s.location_code
    OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;
END;
GO

-- Thêm kệ sách
CREATE OR ALTER PROCEDURE sp_shelf_create
    @location_code NVARCHAR(50),
    @mota          NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM shelves WHERE location_code = @location_code)
        THROW 50016, N'Mã kệ đã tồn tại.', 1;

    INSERT INTO shelves (location_code, mota)
    VALUES (@location_code, @mota);
END;
GO

-- Cập nhật kệ sách
CREATE OR ALTER PROCEDURE sp_shelf_update
    @shelf_id      INT,
    @location_code NVARCHAR(50),
    @mota          NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM shelves WHERE shelf_id = @shelf_id)
        THROW 50017, N'Kệ sách không tồn tại.', 1;

    IF EXISTS (SELECT 1 FROM shelves WHERE location_code = @location_code AND shelf_id <> @shelf_id)
        THROW 50016, N'Mã kệ đã tồn tại.', 1;

    UPDATE shelves
    SET location_code = @location_code, mota = @mota
    WHERE shelf_id = @shelf_id;
END;
GO

-- ==========================
-- BẢN SAO (Copies)
-- ==========================

-- Danh sách bản sao (phân trang)
CREATE OR ALTER PROCEDURE sp_copy_getlist
    @book_id    UNIQUEIDENTIFIER = NULL,
    @status     INT              = NULL,
    @page_index INT              = 1,
    @page_size  INT              = 10,
    @total      BIGINT           OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @offset INT = (@page_index - 1) * @page_size;

    SELECT @total = COUNT(*)
    FROM copies c
    WHERE (@book_id IS NULL OR c.book_id = @book_id)
      AND (@status  IS NULL OR c.status = @status);

    SELECT c.*, b.title AS book_title, b.isbn, s.location_code AS shelf_location
    FROM copies c
    INNER JOIN books   b ON b.book_id  = c.book_id
    LEFT  JOIN shelves s ON s.shelf_id = c.shelf_id
    WHERE (@book_id IS NULL OR c.book_id = @book_id)
      AND (@status  IS NULL OR c.status = @status)
    ORDER BY b.title, c.mabancao
    OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;
END;
GO

-- Chi tiết bản sao
CREATE OR ALTER PROCEDURE sp_copy_getbyid
    @copy_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.*, b.title AS book_title, b.isbn, s.location_code AS shelf_location
    FROM copies c
    INNER JOIN books   b ON b.book_id  = c.book_id
    LEFT  JOIN shelves s ON s.shelf_id = c.shelf_id
    WHERE c.copy_id = @copy_id;
END;
GO

-- Thêm bản sao mới
CREATE OR ALTER PROCEDURE sp_copy_create
    @copy_id  UNIQUEIDENTIFIER,
    @book_id  UNIQUEIDENTIFIER,
    @mabancao NVARCHAR(50),
    @shelf_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM books WHERE book_id = @book_id)
        THROW 50018, N'Đầu sách không tồn tại.', 1;

    IF @shelf_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM shelves WHERE shelf_id = @shelf_id)
        THROW 50019, N'Kệ sách không tồn tại.', 1;

    IF EXISTS (SELECT 1 FROM copies WHERE mabancao = @mabancao)
        THROW 50020, N'Mã bản sao đã tồn tại.', 1;

    INSERT INTO copies (copy_id, book_id, shelf_id, mabancao)
    VALUES (@copy_id, @book_id, @shelf_id, @mabancao);
END;
GO

-- Cập nhật bản sao (status: 0=Sẵn có, 1=Đã mượn, 2=Hỏng)
CREATE OR ALTER PROCEDURE sp_copy_update
    @copy_id  UNIQUEIDENTIFIER,
    @shelf_id INT = NULL,
    @status   INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @statushientai INT;

    SELECT @statushientai = status FROM copies WHERE copy_id = @copy_id;

    IF @statushientai IS NULL
        THROW 50021, N'Bản sao không tồn tại.', 1;

    IF @shelf_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM shelves WHERE shelf_id = @shelf_id)
        THROW 50019, N'Kệ sách không tồn tại.', 1;

    IF @status NOT IN (0, 1, 2)
        THROW 50022, N'Trạng thái bản sao không hợp lệ.', 1;

    -- Bản sao đang mượn không đổi trạng thái được (bắt buộc qua trả sách)
    IF @statushientai = 1 AND @status <> 1
        THROW 50023, N'Bản sao đang được mượn, không thể đổi trạng thái. Hãy dùng trả sách.', 1;

    UPDATE copies
    SET shelf_id = ISNULL(@shelf_id, shelf_id),
        status   = @status
    WHERE copy_id = @copy_id;
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
    WHERE (@keyword   IS NULL OR hoten LIKE '%' + @keyword + '%' OR so_the LIKE '%' + @keyword + '%'
                              OR email LIKE '%' + @keyword + '%' OR sodienthoai LIKE '%' + @keyword + '%')
      AND (@trangthai IS NULL OR trangthai = @trangthai);

    SELECT reader_id, hoten, email, sodienthoai, diachi, so_the, ngaycap, ngayhethan, trangthai, somughin
    FROM readers
    WHERE (@keyword   IS NULL OR hoten LIKE '%' + @keyword + '%' OR so_the LIKE '%' + @keyword + '%'
                              OR email LIKE '%' + @keyword + '%' OR sodienthoai LIKE '%' + @keyword + '%')
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

-- Đăng ký thẻ độc giả
CREATE OR ALTER PROCEDURE sp_reader_create
    @reader_id   UNIQUEIDENTIFIER,
    @hoten       NVARCHAR(200),
    @email       NVARCHAR(200),
    @sodienthoai VARCHAR(15)   = NULL,
    @diachi      NVARCHAR(300) = NULL,
    @so_the      NVARCHAR(50),
    @matkhau     NVARCHAR(255),
    @ngayhethan  DATETIME,
    @somughin    INT = 3
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM readers WHERE email = @email)
        THROW 50102, N'Email đã được đăng ký. Vui lòng nhập email khác.', 1;

    IF EXISTS (SELECT 1 FROM readers WHERE so_the = @so_the)
        THROW 50103, N'Số thẻ đã tồn tại. Vui lòng nhập số thẻ khác.', 1;

    INSERT INTO readers (reader_id, hoten, email, sodienthoai, diachi, so_the, matkhau, ngayhethan, somughin)
    VALUES (@reader_id, @hoten, @email, @sodienthoai, @diachi, @so_the, @matkhau, @ngayhethan, @somughin);
END;
GO

-- Cập nhật thông tin độc giả (kèm hạn thẻ / số sách mượn tối đa)
CREATE OR ALTER PROCEDURE sp_reader_update
    @reader_id   UNIQUEIDENTIFIER,
    @hoten       NVARCHAR(200),
    @sodienthoai VARCHAR(15)   = NULL,
    @diachi      NVARCHAR(300) = NULL,
    @trangthai   INT,
    @ngayhethan  DATETIME = NULL,
    @somughin    INT      = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM readers WHERE reader_id = @reader_id)
        THROW 50107, N'Không tìm thấy độc giả.', 1;

    DECLARE @hancu DATETIME = (SELECT ngayhethan FROM readers WHERE reader_id = @reader_id);

    -- Chỉ kiểm tra khi THAY ĐỔI ngày hết hạn (cùng ngày = giữ nguyên → bỏ qua, không chặn oan)
    IF @ngayhethan IS NOT NULL
       AND CONVERT(DATE, @ngayhethan) <> CONVERT(DATE, @hancu)
       AND @ngayhethan <= GETDATE()
        THROW 50108, N'Ngày hết hạn mới phải lớn hơn ngày hiện tại.', 1;

    IF @somughin IS NOT NULL AND @somughin <= 0
        THROW 50109, N'Số sách mượn tối đa phải lớn hơn 0.', 1;

    -- Logic thẻ: hạn mới hợp lệ → tự kích hoạt lại thẻ đang "Hết hạn" (thẻ bị Khoá giữ nguyên)
    DECLARE @tt INT = @trangthai;
    IF @ngayhethan IS NOT NULL AND @ngayhethan > GETDATE() AND @tt = 1
        SET @tt = 0;

    UPDATE readers
    SET hoten       = @hoten,
        sodienthoai = @sodienthoai,
        diachi      = @diachi,
        trangthai   = @tt,
        ngayhethan  = COALESCE(@ngayhethan, ngayhethan),
        somughin    = COALESCE(@somughin, somughin)
    WHERE reader_id = @reader_id;
END;
GO

-- Gia hạn thẻ bạn đọc
CREATE OR ALTER PROCEDURE sp_reader_renew
    @reader_id  UNIQUEIDENTIFIER,
    @ngayhethan DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM readers WHERE reader_id = @reader_id)
        THROW 50024, N'Bạn đọc không tồn tại.', 1;

    IF @ngayhethan <= GETDATE()
        THROW 50025, N'Ngày hết hạn mới phải lớn hơn ngày hiện tại.', 1;

    UPDATE readers
    SET ngayhethan = @ngayhethan,
        trangthai  = 0  -- Kích hoạt lại thẻ sau khi gia hạn
    WHERE reader_id = @reader_id;
END;
GO

-- Xoá độc giả (chỉ khi chưa có lịch sử mượn / đặt chỗ)
CREATE OR ALTER PROCEDURE sp_reader_delete
    @reader_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM readers WHERE reader_id = @reader_id)
        THROW 50104, N'Không tìm thấy độc giả.', 1;

    IF EXISTS (SELECT 1 FROM loans WHERE reader_id = @reader_id)
        THROW 50105, N'Không thể xoá: độc giả còn lịch sử mượn sách. Hãy dùng "Khoá thẻ" thay thế.', 1;

    IF EXISTS (SELECT 1 FROM reservations WHERE reader_id = @reader_id)
        THROW 50106, N'Không thể xoá: độc giả còn phiếu đặt chỗ.', 1;

    DELETE FROM readers WHERE reader_id = @reader_id;
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

        -- Kiểm tra toàn bộ bản sao tồn tại và đang sẵn có
        IF EXISTS (
            SELECT 1
            FROM OPENJSON(@listjson_chitiet) WITH (copy_id UNIQUEIDENTIFIER '$.copy_id') j
            LEFT JOIN copies c ON c.copy_id = j.copy_id AND c.status = 0
            WHERE c.copy_id IS NULL
        )
            THROW 50002, N'Có bản sao không tồn tại hoặc không sẵn sàng để mượn.', 1;

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
        IF NOT EXISTS (SELECT 1 FROM loans WHERE loan_id = @loan_id)
            THROW 50005, N'Phiếu mượn không tồn tại.', 1;

        IF EXISTS (SELECT 1 FROM loans WHERE loan_id = @loan_id AND return_date IS NOT NULL)
            THROW 50006, N'Phiếu mượn đã được trả trước đó.', 1;

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

-- Gia hạn phiếu mượn (thêm số ngày)
CREATE OR ALTER PROCEDURE sp_loan_renew
    @loan_id  UNIQUEIDENTIFIER,
    @themngay INT = 7
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM loans WHERE loan_id = @loan_id)
        THROW 50007, N'Phiếu mượn không tồn tại.', 1;

    IF EXISTS (SELECT 1 FROM loans WHERE loan_id = @loan_id AND return_date IS NOT NULL)
        THROW 50008, N'Phiếu mượn đã trả, không thể gia hạn.', 1;

    IF EXISTS (SELECT 1 FROM loans WHERE loan_id = @loan_id AND due_date < GETDATE())
        THROW 50009, N'Phiếu mượn đã quá hạn, không thể gia hạn.', 1;

    UPDATE loans
    SET due_date = DATEADD(DAY, @themngay, due_date)
    WHERE loan_id = @loan_id;
END;
GO

-- Lấy danh sách sách quá hạn
CREATE OR ALTER PROCEDURE sp_loan_get_overdue
    @current_date DATETIME     = NULL,
    @keyword      NVARCHAR(200) = NULL,
    @page_index   INT           = 1,
    @page_size    INT           = 10,
    @total        BIGINT        OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @current_date IS NULL SET @current_date = GETDATE();
    DECLARE @offset INT = (@page_index - 1) * @page_size;

    SELECT @total = COUNT(*)
    FROM loans l
    INNER JOIN readers r ON r.reader_id = l.reader_id
    WHERE l.trangthai = 0 AND l.due_date < @current_date
      AND (@keyword IS NULL
           OR r.hoten  LIKE '%' + @keyword + '%'
           OR r.so_the LIKE '%' + @keyword + '%'
           OR EXISTS (SELECT 1
                      FROM loan_details d
                      INNER JOIN copies c ON c.copy_id = d.copy_id
                      INNER JOIN books  b ON b.book_id = c.book_id
                      WHERE d.loan_id = l.loan_id AND b.title LIKE '%' + @keyword + '%'));

    SELECT l.loan_id, l.reader_id, l.loan_date, l.due_date,
           r.hoten AS reader_hoten, r.so_the AS reader_so_the,
           DATEDIFF(DAY, l.due_date, @current_date) AS songaytre
    FROM loans l
    INNER JOIN readers r ON r.reader_id = l.reader_id
    WHERE l.trangthai = 0 AND l.due_date < @current_date
      AND (@keyword IS NULL
           OR r.hoten  LIKE '%' + @keyword + '%'
           OR r.so_the LIKE '%' + @keyword + '%'
           OR EXISTS (SELECT 1
                      FROM loan_details d
                      INNER JOIN copies c ON c.copy_id = d.copy_id
                      INNER JOIN books  b ON b.book_id = c.book_id
                      WHERE d.loan_id = l.loan_id AND b.title LIKE '%' + @keyword + '%'))
    ORDER BY l.due_date ASC
    OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;
END;
GO

-- Lấy danh sách phiếu đang mượn (chưa trả, chưa quá hạn)
CREATE OR ALTER PROCEDURE sp_loan_get_active
    @keyword    NVARCHAR(200) = NULL,
    @page_index INT           = 1,
    @page_size  INT           = 10,
    @total      BIGINT        OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @offset INT = (@page_index - 1) * @page_size;

    SELECT @total = COUNT(*)
    FROM loans l
    INNER JOIN readers r ON r.reader_id = l.reader_id
    WHERE l.return_date IS NULL AND l.due_date >= GETDATE()
      AND (@keyword IS NULL
           OR r.hoten  LIKE '%' + @keyword + '%'
           OR r.so_the LIKE '%' + @keyword + '%'
           OR EXISTS (SELECT 1
                      FROM loan_details d
                      INNER JOIN copies c ON c.copy_id = d.copy_id
                      INNER JOIN books  b ON b.book_id = c.book_id
                      WHERE d.loan_id = l.loan_id AND b.title LIKE '%' + @keyword + '%'));

    SELECT l.loan_id, l.reader_id, l.loan_date, l.due_date, l.return_date, l.trangthai,
           r.hoten AS reader_hoten, r.so_the AS reader_so_the
    FROM loans l
    INNER JOIN readers r ON r.reader_id = l.reader_id
    WHERE l.return_date IS NULL AND l.due_date >= GETDATE()
      AND (@keyword IS NULL
           OR r.hoten  LIKE '%' + @keyword + '%'
           OR r.so_the LIKE '%' + @keyword + '%'
           OR EXISTS (SELECT 1
                      FROM loan_details d
                      INNER JOIN copies c ON c.copy_id = d.copy_id
                      INNER JOIN books  b ON b.book_id = c.book_id
                      WHERE d.loan_id = l.loan_id AND b.title LIKE '%' + @keyword + '%'))
    ORDER BY l.due_date ASC
    OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;
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
-- ĐẶT CHỖ (Reservations)
-- ==========================

-- Đặt chỗ sách
CREATE OR ALTER PROCEDURE sp_reservation_create
    @res_id      UNIQUEIDENTIFIER,
    @book_id     UNIQUEIDENTIFIER,
    @reader_id   UNIQUEIDENTIFIER,
    @expiry_date DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM readers WHERE reader_id = @reader_id AND trangthai = 0 AND ngayhethan >= GETDATE())
        THROW 50010, N'Thẻ bạn đọc không hợp lệ hoặc đã hết hạn.', 1;

    IF NOT EXISTS (SELECT 1 FROM books WHERE book_id = @book_id)
        THROW 50011, N'Đầu sách không tồn tại.', 1;

    IF EXISTS (SELECT 1 FROM reservations
               WHERE reader_id = @reader_id AND book_id = @book_id AND trangthai = 0)
        THROW 50012, N'Bạn đã đặt chỗ sách này và đang chờ xử lý.', 1;

    INSERT INTO reservations (res_id, book_id, reader_id, expiry_date)
    VALUES (@res_id, @book_id, @reader_id, @expiry_date);
END;
GO

-- Danh sách đặt chỗ (phân trang)
CREATE OR ALTER PROCEDURE sp_reservation_getlist
    @trangthai  INT = NULL,
    @page_index INT = 1,
    @page_size  INT = 10,
    @total      BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @offset INT = (@page_index - 1) * @page_size;

    SELECT @total = COUNT(*) FROM reservations
    WHERE @trangthai IS NULL OR trangthai = @trangthai;

    SELECT rv.*, b.title AS book_title, b.isbn, r.hoten AS reader_hoten, r.so_the AS reader_so_the
    FROM reservations rv
    INNER JOIN books   b ON b.book_id   = rv.book_id
    INNER JOIN readers r ON r.reader_id = rv.reader_id
    WHERE @trangthai IS NULL OR rv.trangthai = @trangthai
    ORDER BY rv.res_date DESC
    OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;
END;
GO

-- Lịch sử đặt chỗ của bạn đọc
CREATE OR ALTER PROCEDURE sp_reservation_get_by_reader
    @reader_id  UNIQUEIDENTIFIER,
    @page_index INT = 1,
    @page_size  INT = 10,
    @total      BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @offset INT = (@page_index - 1) * @page_size;

    SELECT @total = COUNT(*) FROM reservations WHERE reader_id = @reader_id;

    SELECT rv.*, b.title AS book_title, b.isbn
    FROM reservations rv
    INNER JOIN books b ON b.book_id = rv.book_id
    WHERE rv.reader_id = @reader_id
    ORDER BY rv.res_date DESC
    OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;
END;
GO

-- Xác nhận bạn đọc đã nhận chỗ
CREATE OR ALTER PROCEDURE sp_reservation_receive
    @res_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM reservations WHERE res_id = @res_id AND trangthai = 0)
        THROW 50013, N'Đặt chỗ không tồn tại hoặc đã được xử lý.', 1;

    IF EXISTS (SELECT 1 FROM reservations WHERE res_id = @res_id AND trangthai = 0 AND expiry_date < GETDATE())
        THROW 50014, N'Đặt chỗ đã hết hạn.', 1;

    UPDATE reservations SET trangthai = 1 WHERE res_id = @res_id;
END;
GO

-- Huỷ đặt chỗ
CREATE OR ALTER PROCEDURE sp_reservation_cancel
    @res_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM reservations WHERE res_id = @res_id AND trangthai = 0)
        THROW 50015, N'Đặt chỗ không tồn tại hoặc đã được xử lý.', 1;

    UPDATE reservations SET trangthai = 2 WHERE res_id = @res_id;
END;
GO

-- Cập nhật trạng thái đặt chỗ hết hạn
CREATE OR ALTER PROCEDURE sp_reservation_expire
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE reservations
    SET trangthai = 3
    WHERE trangthai = 0 AND expiry_date < GETDATE();
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
        IF NOT EXISTS (SELECT 1 FROM fines WHERE fine_id = @fine_id)
            THROW 50003, N'Phiếu phạt không tồn tại.', 1;

        IF EXISTS (SELECT 1 FROM fines WHERE fine_id = @fine_id AND is_paid = 1)
            THROW 50004, N'Phiếu phạt đã được thanh toán.', 1;

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

-- Báo cáo sách quá hạn (chưa trả)
CREATE OR ALTER PROCEDURE sp_report_quahan
AS
BEGIN
    SET NOCOUNT ON;

    SELECT l.loan_id, l.loan_date, l.due_date,
           r.hoten AS reader_hoten, r.so_the AS reader_so_the, r.sodienthoai,
           DATEDIFF(DAY, l.due_date, GETDATE()) AS songaytre,
           ISNULL(f.amount, 0) AS sotienphat,
           ISNULL(f.is_paid, 0) AS is_paid
    FROM loans l
    INNER JOIN readers r ON r.reader_id = l.reader_id
    LEFT  JOIN fines   f ON f.loan_id   = l.loan_id
    WHERE l.return_date IS NULL
      AND l.due_date < GETDATE()
    ORDER BY songaytre DESC;
END;
GO

PRINT 'Tạo Stored Procedures thành công!';
