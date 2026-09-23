-- =============================================
-- Script 04: Kiểm thử Stored Procedures (tiếng Việt)
-- Chạy bằng: sqlcmd -S . -d DoAn2 -E -f 65001 -i 04_TestSP.sql
-- Lưu ý: EXEC inline chỉ nhận biến/constant (không nhận DATEADD/GETDATE trực tiếp)
-- =============================================
USE DoAn2;
GO
SET NOCOUNT ON;
PRINT N'========== BAT DAU TEST SP ==========';

-- Dọn dữ liệu test của lần chạy trước (nếu có)
DELETE ld FROM loan_details ld
    INNER JOIN loans l ON l.loan_id = ld.loan_id
    INNER JOIN readers r ON r.reader_id = l.reader_id
    WHERE r.so_the IN ('THE-SPTEST', 'THE-HETHAN');
DELETE rv FROM reservations rv
    INNER JOIN readers r ON r.reader_id = rv.reader_id
    WHERE r.so_the IN ('THE-SPTEST', 'THE-HETHAN');
DELETE l FROM loans l
    INNER JOIN readers r ON r.reader_id = l.reader_id
    WHERE r.so_the IN ('THE-SPTEST', 'THE-HETHAN');
DELETE c FROM copies c
    INNER JOIN books b ON b.book_id = c.book_id
    WHERE b.isbn = 'TEST-VN-001';
DELETE FROM books WHERE isbn = 'TEST-VN-001';
DELETE FROM readers WHERE so_the IN ('THE-SPTEST', 'THE-HETHAN');
DELETE FROM shelves WHERE location_code = 'KE-A1';
PRINT N'OK: don du lieu test lan truoc (neu co)';

DECLARE @total BIGINT;
DECLARE @ngay DATETIME;
DECLARE @str  NVARCHAR(100);

-- ========== [1] KỆ SÁCH ==========
PRINT N'--- [1] sp_shelf_* ---';
EXEC sp_shelf_create @location_code = N'KE-A1', @mota = N'Kệ A1 - Khoa học tự nhiên';
PRINT N'OK: tao ke A1 (tieng Viet)';

BEGIN TRY
    EXEC sp_shelf_create @location_code = N'KE-A1', @mota = N'Trung';
    PRINT N'FAIL: chua chan trung ma ke';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

DECLARE @shelf_id INT = (SELECT shelf_id FROM shelves WHERE location_code = N'KE-A1');
EXEC sp_shelf_update @shelf_id = @shelf_id, @location_code = N'KE-A1', @mota = N'Kệ A1 - Sách khoa học';
PRINT N'OK: cap nhat ke';

BEGIN TRY
    EXEC sp_shelf_update @shelf_id = 999999, @location_code = N'KE-X', @mota = N'x';
    PRINT N'FAIL: chua chan ke khong ton tai';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

EXEC sp_shelf_getlist @keyword = N'khoa', @page_index = 1, @page_size = 10, @total = @total OUTPUT;
PRINT N'OK: tim kiem ke tieng Viet, total = ' + CAST(@total AS NVARCHAR(20));

-- ========== [2] SÁCH + BẢN SAO ==========
PRINT N'--- [2] sp_book_* / sp_copy_* ---';
DECLARE @book_id UNIQUEIDENTIFIER = NEWID();
SET @ngay = DATEADD(YEAR, 1, GETDATE());
EXEC sp_book_create @book_id = @book_id, @title = N'Sách Test Tiếng Việt', @isbn = 'TEST-VN-001',
     @tacgia = N'Tác Giả Test', @theloai = N'Công nghệ', @nxb = N'NXB ĐHQG',
     @namxuatban = 2026, @mota = N'Mô tả có dấu đầy đủ: ăâđêôơư ạảấầẩẫậ.';
PRINT N'OK: tao sach tieng Viet';

EXEC sp_book_update @book_id = @book_id, @title = N'Sách Test Đã Cập Nhật', @isbn = 'TEST-VN-001',
     @tacgia = N'Nguyễn Văn Test', @theloai = N'Công nghệ', @nxb = N'NXB ĐHQG',
     @namxuatban = 2026, @mota = N'Đã cập nhật tiếng Việt.';
PRINT N'OK: cap nhat sach';

BEGIN TRY
    EXEC sp_book_update @book_id = @book_id, @title = N'x', @isbn = '978-604-1-01001-1',
         @tacgia = NULL, @theloai = NULL, @nxb = NULL, @namxuatban = NULL, @mota = NULL, @image_url = NULL;
    PRINT N'FAIL: chua chan ISBN trung';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

DECLARE @copy1 UNIQUEIDENTIFIER = NEWID();
EXEC sp_copy_create @copy_id = @copy1, @book_id = @book_id, @mabancao = N'TEST-COPY-01', @shelf_id = @shelf_id;
PRINT N'OK: tao ban sao co ke tieng Viet';

DECLARE @copy2 UNIQUEIDENTIFIER = NEWID();
EXEC sp_copy_create @copy_id = @copy2, @book_id = @book_id, @mabancao = N'TEST-COPY-02';
PRINT N'OK: tao ban sao 2';

BEGIN TRY
    EXEC sp_copy_create @copy_id = @copy2, @book_id = @book_id, @mabancao = N'TEST-COPY-01';
    PRINT N'FAIL: chua chan trung ma ban sao';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

EXEC sp_copy_update @copy_id = @copy1, @shelf_id = @shelf_id, @status = 2;
EXEC sp_copy_update @copy_id = @copy1, @shelf_id = @shelf_id, @status = 0;
PRINT N'OK: doi trang thai ban sao (0->2->0)';

EXEC sp_copy_getbyid @copy_id = @copy1;
EXEC sp_copy_getlist @book_id = @book_id, @page_index = 1, @page_size = 10, @total = @total OUTPUT;
PRINT N'OK: lay chi tiet + dsach ban sao, total = ' + CAST(@total AS NVARCHAR(20));

BEGIN TRY
    EXEC sp_book_delete @book_id = @book_id;
    PRINT N'FAIL: xoa sach con ban sao';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

-- ========== [3] BẠN ĐỌC + GIA HẠN THẺ ==========
PRINT N'--- [3] sp_reader_renew ---';
DECLARE @reader_id UNIQUEIDENTIFIER = NEWID();
SET @ngay = DATEADD(YEAR, 1, GETDATE());
EXEC sp_reader_create @reader_id = @reader_id, @hoten = N'Nguyễn Thị Test', @email = 'test-sp@test.com',
     @sodienthoai = '0900000000', @diachi = N'Đà Nẵng - Quận Hải Châu', @so_the = 'THE-SPTEST',
     @matkhau = 'abc123', @ngayhethan = @ngay;
PRINT N'OK: tao ban doc test tieng Viet';

DECLARE @reader_hethan UNIQUEIDENTIFIER = NEWID();
SET @ngay = DATEADD(DAY, -1, GETDATE());
EXEC sp_reader_create @reader_id = @reader_hethan, @hoten = N'Bạn Đọc Hết Hạn', @email = 'test-hethan@test.com',
     @so_the = 'THE-HETHAN', @matkhau = 'abc123', @ngayhethan = @ngay;
PRINT N'OK: tao ban doc het han';

BEGIN TRY
    SET @ngay = DATEADD(YEAR, 1, GETDATE());
    EXEC sp_reader_renew @reader_id = '00000000-0000-0000-0000-000000000000', @ngayhethan = @ngay;
    PRINT N'FAIL: chua chan khong ton tai';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    SET @ngay = DATEADD(DAY, -5, GETDATE());
    EXEC sp_reader_renew @reader_id = @reader_id, @ngayhethan = @ngay;
    PRINT N'FAIL: chap nhan ngay het han qua khu';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

SET @ngay = DATEADD(YEAR, 2, GETDATE());
EXEC sp_reader_renew @reader_id = @reader_id, @ngayhethan = @ngay;
SELECT @str = CONVERT(NVARCHAR(30), ngayhethan, 120) FROM readers WHERE reader_id = @reader_id;
PRINT N'OK: gia han the, ngay het han = ' + @str;

-- ========== [4] MƯỢN TRẢ + GIA HẠN MƯỢN ==========
PRINT N'--- [4] sp_loan_* ---';
DECLARE @loan1 UNIQUEIDENTIFIER = NEWID();
DECLARE @json1 NVARCHAR(500) = N'[{"copy_id":"' + CAST(@copy1 AS NVARCHAR(36)) + N'"},{"copy_id":"' + CAST(@copy2 AS NVARCHAR(36)) + N'"}]';
SET @ngay = DATEADD(DAY, 10, GETDATE());
EXEC sp_loan_create @loan_id = @loan1, @reader_id = @reader_id,
     @due_date = @ngay, @listjson_chitiet = @json1;
PRINT N'OK: tao phieu muon 2 ban sao';

EXEC sp_copy_getlist @book_id = @book_id, @status = 1, @page_index = 1, @page_size = 10, @total = @total OUTPUT;
PRINT N'OK: ban sao dang muon, total = ' + CAST(@total AS NVARCHAR(20));

BEGIN TRY
    EXEC sp_copy_update @copy_id = @copy1, @shelf_id = @shelf_id, @status = 0;
    PRINT N'FAIL: cho doi trang thai sach dang muon';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    EXEC sp_loan_renew @loan_id = '00000000-0000-0000-0000-000000000000';
    PRINT N'FAIL: chua chan phieu khong ton tai';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

EXEC sp_loan_renew @loan_id = @loan1, @themngay = 7;
SELECT @str = CONVERT(NVARCHAR(30), due_date, 120) FROM loans WHERE loan_id = @loan1;
PRINT N'OK: gia han phieu muon, due = ' + @str;

-- Trả phiếu 1 → trả lặp phải lỗi → gia hạn phiếu đã trả phải lỗi
EXEC sp_loan_return @loan_id = @loan1;
PRINT N'OK: tra sach lan 1';

BEGIN TRY
    EXEC sp_loan_return @loan_id = @loan1;
    PRINT N'FAIL: cho tra sach lap lan 2';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    EXEC sp_loan_renew @loan_id = @loan1;
    PRINT N'FAIL: gia han phieu da tra';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

-- Phiếu quá hạn → không cho gia hạn
DECLARE @loan2 UNIQUEIDENTIFIER = NEWID();
DECLARE @json2 NVARCHAR(200) = N'[{"copy_id":"' + CAST(@copy2 AS NVARCHAR(36)) + N'"}]';
SET @ngay = DATEADD(DAY, -3, GETDATE());
EXEC sp_loan_create @loan_id = @loan2, @reader_id = @reader_id,
     @due_date = @ngay, @listjson_chitiet = @json2;
PRINT N'OK: tao phieu qua han (due -3 ngay)';

BEGIN TRY
    EXEC sp_loan_renew @loan_id = @loan2;
    PRINT N'FAIL: gia han phieu qua han';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

EXEC sp_loan_return @loan_id = @loan2;
PRINT N'OK: tra phieu qua han';

-- ========== [5] ĐẶT CHỖ ==========
PRINT N'--- [5] sp_reservation_* ---';
DECLARE @res1 UNIQUEIDENTIFIER = NEWID();
SET @ngay = DATEADD(DAY, 3, GETDATE());
EXEC sp_reservation_create @res_id = @res1, @book_id = @book_id, @reader_id = @reader_id,
     @expiry_date = @ngay;
PRINT N'OK: dat cho tieng Viet';

BEGIN TRY
    DECLARE @res_dup UNIQUEIDENTIFIER = NEWID();
    SET @ngay = DATEADD(DAY, 3, GETDATE());
    EXEC sp_reservation_create @res_id = @res_dup, @book_id = @book_id, @reader_id = @reader_id,
         @expiry_date = @ngay;
    PRINT N'FAIL: cho phep dat trung';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    DECLARE @res_book UNIQUEIDENTIFIER = NEWID();
    SET @ngay = DATEADD(DAY, 3, GETDATE());
    EXEC sp_reservation_create @res_id = @res_book, @book_id = '00000000-0000-0000-0000-000000000000',
         @reader_id = @reader_id, @expiry_date = @ngay;
    PRINT N'FAIL: dat cho sach khong ton tai';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    DECLARE @res_rd UNIQUEIDENTIFIER = NEWID();
    SET @ngay = DATEADD(DAY, 3, GETDATE());
    EXEC sp_reservation_create @res_id = @res_rd, @book_id = @book_id, @reader_id = @reader_hethan,
         @expiry_date = @ngay;
    PRINT N'FAIL: dat cho bang the het han';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

-- Hủy res1 (đang chờ) → nhả chỗ + test hủy khi đang chờ
EXEC sp_reservation_cancel @res_id = @res1;
PRINT N'OK: huy cho dang cho (res1)';

-- Đặt chỗ hết hạn → receive phải báo lỗi
DECLARE @res_hethan UNIQUEIDENTIFIER = NEWID();
SET @ngay = DATEADD(DAY, -1, GETDATE());
EXEC sp_reservation_create @res_id = @res_hethan, @book_id = @book_id, @reader_id = @reader_id,
     @expiry_date = @ngay;
PRINT N'OK: tao cho het han (expiry -1 ngay)';

BEGIN TRY
    EXEC sp_reservation_receive @res_id = @res_hethan;
    PRINT N'FAIL: nhan cho da het han';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

-- Hết hạn → expire set trangthai = 3 (nhả chỗ cho lần tạo sau)
EXEC sp_reservation_expire;
SELECT @str = CAST(trangthai AS NVARCHAR(10)) FROM reservations WHERE res_id = @res_hethan;
PRINT N'OK: cap nhat cho het han, trangthai = ' + @str;

-- Đặt chỗ hợp lệ → receive OK → receive lại lỗi → cancel lỗi
DECLARE @res2 UNIQUEIDENTIFIER = NEWID();
SET @ngay = DATEADD(DAY, 5, GETDATE());
EXEC sp_reservation_create @res_id = @res2, @book_id = @book_id, @reader_id = @reader_id,
     @expiry_date = @ngay;
EXEC sp_reservation_receive @res_id = @res2;
PRINT N'OK: nhan cho';

BEGIN TRY
    EXEC sp_reservation_receive @res_id = @res2;
    PRINT N'FAIL: nhan cho lan 2';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    EXEC sp_reservation_cancel @res_id = @res2;
    PRINT N'FAIL: huy cho da xu ly';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

-- Đặt chỗ đang chờ → cancel OK
DECLARE @res3 UNIQUEIDENTIFIER = NEWID();
SET @ngay = DATEADD(DAY, 2, GETDATE());
EXEC sp_reservation_create @res_id = @res3, @book_id = @book_id, @reader_id = @reader_id,
     @expiry_date = @ngay;
EXEC sp_reservation_cancel @res_id = @res3;
PRINT N'OK: huy cho';

EXEC sp_reservation_getlist @trangthai = NULL, @page_index = 1, @page_size = 5, @total = @total OUTPUT;
PRINT N'OK: dsach dat cho, total = ' + CAST(@total AS NVARCHAR(20));

EXEC sp_reservation_get_by_reader @reader_id = @reader_id, @page_index = 1, @page_size = 5, @total = @total OUTPUT;
PRINT N'OK: dsach dat cho cua ban doc, total = ' + CAST(@total AS NVARCHAR(20));

-- ========== [6] BÁO CÁO ==========
PRINT N'--- [6] sp_report_* ---';
EXEC sp_report_sachmuonnhieu @topN = 5;
PRINT N'OK: bao cao sach muon nhieu';
EXEC sp_report_tonkho;
PRINT N'OK: bao cao ton kho';
EXEC sp_report_quahan;
PRINT N'OK: bao cao qua han';

-- ========== [7] KIỂM TRA TIẾNG VIỆT TRONG DB ==========
PRINT N'--- [7] Verify tieng Viet ---';
SELECT title, tacgia, theloai, mota FROM books WHERE isbn IN ('TEST-VN-001', '978-604-1-01001-1', '978-604-1-01004-4');
SELECT hoten, diachi FROM readers WHERE so_the IN ('THE-001', 'THE-002', 'THE-SPTEST');
SELECT hoten FROM users WHERE taikhoan = 'thuthu';

-- ========== [8] DỌN DỮ LIỆU TEST ==========
PRINT N'--- [8] Cleanup ---';
DELETE FROM loan_details WHERE loan_id IN (@loan1, @loan2);
DELETE FROM reservations WHERE reader_id IN (@reader_id, @reader_hethan);
DELETE FROM loans WHERE reader_id IN (@reader_id, @reader_hethan);
DELETE FROM copies WHERE book_id = @book_id;
DELETE FROM books WHERE book_id = @book_id;
DELETE FROM readers WHERE reader_id IN (@reader_id, @reader_hethan);
DELETE FROM shelves WHERE shelf_id = @shelf_id;
PRINT N'OK: da don du lieu test';

PRINT N'========== KET THUC TEST SP ==========';
GO
