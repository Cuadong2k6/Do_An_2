-- =============================================
-- Script 04: Kiểm thử Stored Procedures (tiếng Việt)
-- Chạy bằng: sqlcmd -S . -d DoAn2 -E -f 65001 -b -i 04_TestSP.sql
--   -b : trả exit code khác 0 khi script lỗi (để CI/CD hoặc batch tự dừng)
-- Lưu ý: EXEC inline chỉ nhận biến/constant (không nhận DATEADD/GETDATE trực tiếp)
--
-- Cơ chế assert:
--   Mỗi ca FAIL tăng biến @fail. Cuối script gọi THROW nếu @fail > 0
--   → sqlcmd dừng với exit code != 0, thay vì chỉ in dòng "FAIL: ..." rồi chạy tiếp.
-- =============================================
USE DoAn2;
GO
SET NOCOUNT ON;
PRINT N'========== BAT DAU TEST SP ==========';

DECLARE @fail INT = 0;

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
    WHERE b.isbn IN ('TEST-VN-001', 'TEST-VN-002');
DELETE FROM books WHERE isbn IN ('TEST-VN-001', 'TEST-VN-002');
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
    SET @fail = @fail + 1;
    PRINT N'FAIL: chua chan trung ma ke';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

DECLARE @shelf_id INT = (SELECT shelf_id FROM shelves WHERE location_code = N'KE-A1');
IF @shelf_id IS NULL
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: tao ke xong khong lay duoc shelf_id';
END
ELSE PRINT N'OK: lay duoc shelf_id = ' + CAST(@shelf_id AS NVARCHAR(10));

EXEC sp_shelf_update @shelf_id = @shelf_id, @location_code = N'KE-A1', @mota = N'Kệ A1 - Sách khoa học';
PRINT N'OK: cap nhat ke';

BEGIN TRY
    EXEC sp_shelf_update @shelf_id = 999999, @location_code = N'KE-X', @mota = N'x';
    SET @fail = @fail + 1;
    PRINT N'FAIL: chua chan ke khong ton tai';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

EXEC sp_shelf_getlist @keyword = N'khoa', @page_index = 1, @page_size = 10, @total = @total OUTPUT;
IF @total IS NULL OR @total = 0
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: tim kiem ke tieng Viet tra ve total = 0';
END
ELSE PRINT N'OK: tim kiem ke tieng Viet, total = ' + CAST(@total AS NVARCHAR(20));

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
    SET @fail = @fail + 1;
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
    SET @fail = @fail + 1;
    PRINT N'FAIL: chua chan trung ma ban sao';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

EXEC sp_copy_update @copy_id = @copy1, @shelf_id = @shelf_id, @status = 2;
EXEC sp_copy_update @copy_id = @copy1, @shelf_id = @shelf_id, @status = 0;
PRINT N'OK: doi trang thai ban sao (0->2->0)';

EXEC sp_copy_getbyid @copy_id = @copy1;
EXEC sp_copy_getlist @book_id = @book_id, @page_index = 1, @page_size = 10, @total = @total OUTPUT;
IF @total IS NULL OR @total <> 2
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: dsach ban sao phai co 2 ban sao, actual total = ' + CAST(ISNULL(@total, -1) AS NVARCHAR(20));
END
ELSE PRINT N'OK: lay chi tiet + dsach ban sao, total = ' + CAST(@total AS NVARCHAR(20));

BEGIN TRY
    EXEC sp_book_delete @book_id = @book_id;
    SET @fail = @fail + 1;
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
    SET @fail = @fail + 1;
    PRINT N'FAIL: chua chan khong ton tai';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    SET @ngay = DATEADD(DAY, -5, GETDATE());
    EXEC sp_reader_renew @reader_id = @reader_id, @ngayhethan = @ngay;
    SET @fail = @fail + 1;
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
DECLARE @ngayMuon1 DATETIME = GETDATE();
EXEC sp_loan_create @loan_id = @loan1, @reader_id = @reader_id,
     @loan_date = @ngayMuon1, @due_date = @ngay, @listjson_chitiet = @json1;
PRINT N'OK: tao phieu muon 2 ban sao';

-- Assert: 2 bản sao phải chuyển sang status = 1 (đã mượn)
DECLARE @soDaMuan INT = (SELECT COUNT(*) FROM copies
                         WHERE copy_id IN (@copy1, @copy2) AND status = 1);
IF @soDaMuan <> 2
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: sau khi muon, 2 ban sao phai status = 1, actual = ' + CAST(@soDaMuan AS NVARCHAR(10));
END
ELSE PRINT N'OK: 2 ban sao chuyen sang trang thai da muon';

EXEC sp_copy_getlist @book_id = @book_id, @status = 1, @page_index = 1, @page_size = 10, @total = @total OUTPUT;
IF @total IS NULL OR @total <> 2
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: loc ban sao dang muon phai ra 2, actual total = ' + CAST(ISNULL(@total, -1) AS NVARCHAR(20));
END
ELSE PRINT N'OK: ban sao dang muon, total = ' + CAST(@total AS NVARCHAR(20));

BEGIN TRY
    EXEC sp_copy_update @copy_id = @copy1, @shelf_id = @shelf_id, @status = 0;
    SET @fail = @fail + 1;
    PRINT N'FAIL: cho doi trang thai sach dang muon';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    EXEC sp_loan_renew @loan_id = '00000000-0000-0000-0000-000000000000';
    SET @fail = @fail + 1;
    PRINT N'FAIL: chua chan phieu khong ton tai';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

EXEC sp_loan_renew @loan_id = @loan1, @themngay = 7;
SELECT @str = CONVERT(NVARCHAR(30), due_date, 120) FROM loans WHERE loan_id = @loan1;
PRINT N'OK: gia han phieu muon, due = ' + @str;

-- Trả phiếu 1 → trả lặp phải lỗi → gia hạn phiếu đã trả phải lỗi
EXEC sp_loan_return @loan_id = @loan1;
PRINT N'OK: tra sach lan 1';

-- Assert: sau khi trả, 2 bản sao phải trở lại status = 0
DECLARE @soDaSan INT = (SELECT COUNT(*) FROM copies
                        WHERE copy_id IN (@copy1, @copy2) AND status = 0);
IF @soDaSan <> 2
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: sau khi tra, 2 ban sao phai status = 0, actual = ' + CAST(@soDaSan AS NVARCHAR(10));
END
ELSE PRINT N'OK: 2 ban sao tra ve trang thai san co';

BEGIN TRY
    EXEC sp_loan_return @loan_id = @loan1;
    SET @fail = @fail + 1;
    PRINT N'FAIL: cho tra sach lap lan 2';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    EXEC sp_loan_renew @loan_id = @loan1;
    SET @fail = @fail + 1;
    PRINT N'FAIL: gia han phieu da tra';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

-- Phiếu quá hạn → không cho gia hạn
DECLARE @loan2 UNIQUEIDENTIFIER = NEWID();
DECLARE @json2 NVARCHAR(200) = N'[{"copy_id":"' + CAST(@copy2 AS NVARCHAR(36)) + N'"}]';
SET @ngay = DATEADD(DAY, -3, GETDATE());
DECLARE @ngayMuon2 DATETIME = DATEADD(DAY, -6, GETDATE());
EXEC sp_loan_create @loan_id = @loan2, @reader_id = @reader_id,
     @loan_date = @ngayMuon2, @due_date = @ngay, @listjson_chitiet = @json2;
PRINT N'OK: tao phieu qua han (due -3 ngay)';

BEGIN TRY
    EXEC sp_loan_renew @loan_id = @loan2;
    SET @fail = @fail + 1;
    PRINT N'FAIL: gia han phieu qua han';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

EXEC sp_loan_return @loan_id = @loan2;
PRINT N'OK: tra phieu qua han';

-- ========== [4b] GIỎ HÀNG BẠN ĐỌC: sp_loan_create_auto ==========
PRINT N'--- [4b] sp_loan_create_auto (gio hang) ---';

-- Sách thứ 2 (chỉ 1 bản sao) để test giỏ nhiều cuốn khác nhau
DECLARE @book_id2 UNIQUEIDENTIFIER = NEWID();
EXEC sp_book_create @book_id = @book_id2, @title = N'Sách Test Thứ Hai', @isbn = 'TEST-VN-002',
     @tacgia = N'Tác Giả Phụ', @theloai = N'Công nghệ', @nxb = N'NXB ĐHQG',
     @namxuatban = 2026, @mota = N'Sách phụ để test giỏ hàng.';
DECLARE @copy3 UNIQUEIDENTIFIER = NEWID();
EXEC sp_copy_create @copy_id = @copy3, @book_id = @book_id2, @mabancao = N'TEST-COPY-03';
PRINT N'OK: tao sach thu 2 + 1 ban sao';

-- 1) Giỏ 2 cuốn KHÁC NHAU → hệ thống tự gán bản sao
DECLARE @loan3 UNIQUEIDENTIFIER = NEWID();
DECLARE @json3 NVARCHAR(500) = N'[{"book_id":"' + CAST(@book_id  AS NVARCHAR(36)) + N'"},{"book_id":"' + CAST(@book_id2 AS NVARCHAR(36)) + N'"}]';
SET @ngay = DATEADD(DAY, 14, GETDATE());
EXEC sp_loan_create_auto @loan_id = @loan3, @reader_id = @reader_id,
     @due_date = @ngay, @listjson_chitiet = @json3;
PRINT N'OK: tao phieu gio 2 sach khac nhau';

-- Assert: phiếu có đúng 2 chi tiết
DECLARE @soChiTiet INT = (SELECT COUNT(*) FROM loan_details WHERE loan_id = @loan3);
IF @soChiTiet <> 2
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: phieu gio phai co 2 chi tiet, actual = ' + CAST(@soChiTiet AS NVARCHAR(10));
END
ELSE PRINT N'OK: phieu gio co 2 chi tiet';

-- Assert: bản sao của cả 2 sách đều chuyển sang status = 1
DECLARE @soDaMuan2 INT = (SELECT COUNT(*) FROM copies c
                           JOIN loan_details ld ON ld.copy_id = c.copy_id
                           WHERE ld.loan_id = @loan3 AND c.status = 1);
IF @soDaMuan2 <> 2
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: 2 ban sao trong gio phai chuyen sang da muon, actual = ' + CAST(@soDaMuan2 AS NVARCHAR(10));
END
ELSE PRINT N'OK: he thong tu gan 2 ban sao sang trang thai da muon';

-- 2) Giỏ chứa CÙNG 1 sách 2 lần → phải gán 2 bản sao KHÁC NHAU
EXEC sp_loan_return @loan_id = @loan3;
DECLARE @loan4 UNIQUEIDENTIFIER = NEWID();
DECLARE @json4 NVARCHAR(500) = N'[{"book_id":"' + CAST(@book_id AS NVARCHAR(36)) + N'"},{"book_id":"' + CAST(@book_id AS NVARCHAR(36)) + N'"}]';
EXEC sp_loan_create_auto @loan_id = @loan4, @reader_id = @reader_id,
     @due_date = @ngay, @listjson_chitiet = @json4;
PRINT N'OK: tao phieu gio 2 lan cung 1 sach';

DECLARE @soBanSaoKhacNhau INT = (
    SELECT COUNT(DISTINCT ld.copy_id) FROM loan_details ld WHERE ld.loan_id = @loan4);
IF @soBanSaoKhacNhau <> 2
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: 2 ban sao phai KHAC NHAU, actual = ' + CAST(@soBanSaoKhacNhau AS NVARCHAR(10));
END
ELSE PRINT N'OK: 2 ban sao duoc gan la khac nhau';

EXEC sp_loan_return @loan_id = @loan4;
PRINT N'OK: tra phieu gio (loan4)';

-- 3) Sách HẾT bản sao rảnh → phải lỗi và KHÔNG tạo phiếu (all-or-nothing)
--    Mượn hết bản sao của sách 2, rồi thử đưa sách 2 vào giỏ
DECLARE @loan5 UNIQUEIDENTIFIER = NEWID();
DECLARE @json5 NVARCHAR(200) = N'[{"copy_id":"' + CAST(@copy3 AS NVARCHAR(36)) + N'"}]';
EXEC sp_loan_create @loan_id = @loan5, @reader_id = @reader_id,
     @loan_date = @ngayMuon1, @due_date = @ngay, @listjson_chitiet = @json5;

BEGIN TRY
    DECLARE @loan6 UNIQUEIDENTIFIER = NEWID();
    DECLARE @json6 NVARCHAR(500) = N'[{"book_id":"' + CAST(@book_id  AS NVARCHAR(36)) + N'"},{"book_id":"' + CAST(@book_id2 AS NVARCHAR(36)) + N'"}]';
    EXEC sp_loan_create_auto @loan_id = @loan6, @reader_id = @reader_id,
         @due_date = @ngay, @listjson_chitiet = @json6;
    SET @fail = @fail + 1;
    PRINT N'FAIL: cho muon sach da het ban sao rang';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

-- Assert all-or-nothing: phiếu loan6 KHÔNG được tạo
IF EXISTS (SELECT 1 FROM loans WHERE loan_id = @loan6)
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: phieu that bai van bi tao (phai all-or-nothing)';
END
ELSE PRINT N'OK: phieu that bai khong duoc tao (all-or-nothing)';

-- Assert: cuốn hợp lệ trong cùng giỏ giữ nguyên bản sao rảnh (không bị "mượn nửa chừng")
DECLARE @soConRanh INT = (SELECT COUNT(*) FROM copies WHERE copy_id IN (@copy1, @copy2) AND status = 0);
IF @soConRanh <> 2
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: ban sao cua cuon hop len phai giu nguyen, actual = ' + CAST(@soConRanh AS NVARCHAR(10));
END
ELSE PRINT N'OK: khong doi trang thai ban sao cua cuon khong the muon';

EXEC sp_loan_return @loan_id = @loan5;
PRINT N'OK: tra phieu giu sach 2 (loan5)';

-- 4) Bạn đọc hết hạn → phải từ chối
BEGIN TRY
    DECLARE @loan7 UNIQUEIDENTIFIER = NEWID();
    DECLARE @json7 NVARCHAR(200) = N'[{"book_id":"' + CAST(@book_id AS NVARCHAR(36)) + N'"}]';
    EXEC sp_loan_create_auto @loan_id = @loan7, @reader_id = @reader_hethan,
         @due_date = @ngay, @listjson_chitiet = @json7;
    SET @fail = @fail + 1;
    PRINT N'FAIL: cho muon bang the het han';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

-- 5) Giỏ rỗng → phải từ chối (không tạo phiếu rỗng)
BEGIN TRY
    DECLARE @loan8 UNIQUEIDENTIFIER = NEWID();
    EXEC sp_loan_create_auto @loan_id = @loan8, @reader_id = @reader_id,
         @due_date = @ngay, @listjson_chitiet = N'[]';
    SET @fail = @fail + 1;
    PRINT N'FAIL: cho tao phieu rong';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

-- ========== [4c] sp_reader_sodangmuon — đếm sách đang mượn (giới hạn thẻ) ==========
PRINT N'--- [4c] sp_reader_sodangmuon ---';

DECLARE @book_id9 UNIQUEIDENTIFIER = NEWID();
EXEC sp_book_create @book_id = @book_id9, @title = N'Sách Test Đếm Đang Mượn', @isbn = 'TEST-VN-004',
     @tacgia = N'Tác Giả Test', @theloai = N'Công nghệ', @nxb = N'NXB ĐHQG',
     @namxuatban = 2026, @mota = N'Dùng để test sp_reader_sodangmuon.';

DECLARE @copy9a UNIQUEIDENTIFIER = NEWID();
EXEC sp_copy_create @copy_id = @copy9a, @book_id = @book_id9, @mabancao = N'TEST-COPY-09A';
DECLARE @copy9b UNIQUEIDENTIFIER = NEWID();
EXEC sp_copy_create @copy_id = @copy9b, @book_id = @book_id9, @mabancao = N'TEST-COPY-09B';

-- Mượn 2 bản sao cùng 1 đầu sách (kiểu giỏ hàng có số lượng)
DECLARE @loan9 UNIQUEIDENTIFIER = NEWID();
SET @ngay = DATEADD(DAY, 7, GETDATE());
DECLARE @json9 NVARCHAR(500) = N'[{"book_id":"' + CAST(@book_id9 AS NVARCHAR(36)) + N'"},{"book_id":"' + CAST(@book_id9 AS NVARCHAR(36)) + N'"}]';
EXEC sp_loan_create_auto @loan_id = @loan9, @reader_id = @reader_id,
     @due_date = @ngay, @listjson_chitiet = @json9;

DECLARE @tmpMuon TABLE (so_luong INT);
INSERT INTO @tmpMuon EXEC sp_reader_sodangmuon @reader_id = @reader_id;
DECLARE @soDangMuon INT = (SELECT so_luong FROM @tmpMuon);
IF @soDangMuon <> 2
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: sp_reader_sodangmuon phai tra 2 cuon, actual = ' + CAST(@soDangMuon AS NVARCHAR(10));
END
ELSE PRINT N'OK: muon 2 ban sao cung 1 dau sach → dang muon = ' + CAST(@soDangMuon AS NVARCHAR(10));

-- Trả sách → phải về 0
EXEC sp_loan_return @loan_id = @loan9;
DELETE FROM @tmpMuon;
INSERT INTO @tmpMuon EXEC sp_reader_sodangmuon @reader_id = @reader_id;
DECLARE @soSauTra INT = (SELECT so_luong FROM @tmpMuon);
IF @soSauTra <> 0
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: tra sach xong phai tra 0, actual = ' + CAST(@soSauTra AS NVARCHAR(10));
END
ELSE PRINT N'OK: tra sach xong → dang muon = 0';

-- Bạn đọc khác không có phiếu nào chưa trả → phải trả về 0
DECLARE @reader_khac UNIQUEIDENTIFIER = (SELECT TOP 1 reader_id FROM readers WHERE reader_id <> @reader_id);
DECLARE @tmpKhac TABLE (so_luong INT);
INSERT INTO @tmpKhac EXEC sp_reader_sodangmuon @reader_id = @reader_khac;
DECLARE @soNguoiKhac INT = (SELECT so_luong FROM @tmpKhac);
IF @soNguoiKhac <> 0
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: ban doc khong co phieu chua tra phai tra 0, actual = ' + CAST(@soNguoiKhac AS NVARCHAR(10));
END
ELSE PRINT N'OK: ban doc khong co phieu chua tra = 0';

-- ========== [4d] NHẬT KÝ THAY ĐỔI — trigger trg_books_nhatky / trg_readers_nhatky ==========
PRINT N'--- [4d] trg_books_nhatky / trg_readers_nhatky ---';

-- Dùng sách + bạn đọc riêng, không có bản sao/phiếu mượn để test xoá cho sạch
DECLARE @book_id10 UNIQUEIDENTIFIER   = NEWID();
DECLARE @reader_id10 UNIQUEIDENTIFIER = NEWID();

EXEC sp_book_create @book_id = @book_id10, @title = N'Sách Test Nhật Ký', @isbn = 'TEST-VN-010',
     @tacgia = N'Tác Giả Gốc', @theloai = N'Công nghệ', @nxb = N'NXB Gốc',
     @namxuatban = 2020, @mota = N'Không ghi mô tả vào nhật ký.';

EXEC sp_reader_create @reader_id = @reader_id10, @hoten = N'Bạn Đọc Nhật Ký',
     @email = 'nhatky-test@library.com', @sodienthoai = '0900000009', @diachi = N'Địa chỉ gốc',
     @so_the = 'THE-NKTEST', @matkhau = N'123456', @ngayhethan = @ngay, @somughin = 5;

-- Thêm mới KHÔNG được ghi log (không mất được gì)
IF EXISTS (SELECT 1 FROM nhatky WHERE doituong IN ('TEST-VN-010', 'THE-NKTEST'))
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: them moi khong duoc ghi vao nhat ky';
END
ELSE PRINT N'OK: them moi khong ghi nhat ky';

-- Sửa 1 trường của sách → đúng 1 dòng log, có giá trị cũ và mới
EXEC sp_book_update @book_id = @book_id10, @title = N'Sách Test Nhật Ký', @isbn = 'TEST-VN-010',
     @tacgia = N'Tác Giả Gốc', @theloai = N'Công nghệ', @nxb = N'NXB Mới',
     @namxuatban = 2020, @mota = N'Không ghi mô tả vào nhật ký.';

DECLARE @sau1 INT = (SELECT COUNT(*) FROM nhatky
                     WHERE bang = 'books' AND doituong = 'TEST-VN-010' AND hanhdong = 'SUA');
IF @sau1 <> 1
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: sua 1 truong phai ghi 1 dong, actual = ' + CAST(@sau1 AS NVARCHAR(10));
END
ELSE PRINT N'OK: sua 1 truong → 1 dong nhat ky';

-- Sửa KHÔNG được sinh dòng 'XOA' (sẽ xảy ra nếu trigger nhầm lấy 'deleted' làm tín hiệu xoá)
IF EXISTS (SELECT 1 FROM nhatky WHERE bang = 'books' AND doituong = 'TEST-VN-010' AND hanhdong = 'XOA')
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: sua khong duoc ghi nhat ky dang XOA';
END
ELSE PRINT N'OK: sua khong ghi nhat ky dang XOA';

-- Đúng giá trị cũ (không có trường nào đổi) → KHÔNG ghi thêm dòng nào
EXEC sp_book_update @book_id = @book_id10, @title = N'Sách Test Nhật Ký', @isbn = 'TEST-VN-010',
     @tacgia = N'Tác Giả Gốc', @theloai = N'Công nghệ', @nxb = N'NXB Mới',
     @namxuatban = 2020, @mota = N'Không ghi mô tả vào nhật ký.';

DECLARE @sau2 INT = (SELECT COUNT(*) FROM nhatky
                     WHERE bang = 'books' AND doituong = 'TEST-VN-010' AND hanhdong = 'SUA');
IF @sau2 <> 1
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: sua khong doi gi tri khong duoc ghi them, actual = ' + CAST(@sau2 AS NVARCHAR(10));
END
ELSE PRINT N'OK: sua khong doi gi tri → van 1 dong nhat ky';

-- Sửa 2 trường cùng lúc (tác giả + năm XB) → thêm đúng 2 dòng
EXEC sp_book_update @book_id = @book_id10, @title = N'Sách Test Nhật Ký', @isbn = 'TEST-VN-010',
     @tacgia = N'Tác Giã Mới', @theloai = N'Công nghệ', @nxb = N'NXB Mới',
     @namxuatban = 2024, @mota = N'Không ghi mô tả vào nhật ký.';

DECLARE @sau3 INT = (SELECT COUNT(*) FROM nhatky
                     WHERE bang = 'books' AND doituong = 'TEST-VN-010' AND hanhdong = 'SUA');
IF @sau3 <> 3
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: sua 2 truong phai ghi them 2 dong, actual = ' + CAST(@sau3 AS NVARCHAR(10));
END
ELSE PRINT N'OK: sua 2 truong → tong 3 dong nhat ky';

-- Nhật ký phải lưu đủ tên trường + giá trị cũ và mới
DECLARE @doiNxb INT = (SELECT COUNT(*) FROM nhatky
                       WHERE bang = 'books' AND doituong = 'TEST-VN-010' AND hanhdong = 'SUA'
                         AND truong = N'NXB' AND sau = N'NXB Mới');
IF @doiNxb = 0
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: nhat ky phai luu ten truong NXB va gia tri moi';
END
ELSE PRINT N'OK: nhat ky luu ten truong + gia tri cu → moi';

-- Sửa bạn đọc: giới hạn mượn 5 → 8, khoá thẻ (trangthai 0 → 2)
UPDATE readers SET somughin = 8, trangthai = 2 WHERE reader_id = @reader_id10;

DECLARE @sau4 INT = (SELECT COUNT(*) FROM nhatky
                     WHERE bang = 'readers' AND doituong = 'THE-NKTEST' AND hanhdong = 'SUA');
IF @sau4 <> 2
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: sua 2 truong ban doc phai ghi 2 dong, actual = ' + CAST(@sau4 AS NVARCHAR(10));
END
ELSE PRINT N'OK: sua ban doc 2 truong → 2 dong nhat ky';

-- Mật khẩu KHÔNG BAO GIỜ được ghi vào nhật ký
UPDATE readers SET matkhau = N'$2a$12$abc123' WHERE reader_id = @reader_id10;
IF EXISTS (SELECT 1 FROM nhatky WHERE truoc LIKE '%$2a%' OR sau LIKE '%$2a%')
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: mat khau bi ghi vao nhat ky';
END
ELSE PRINT N'OK: mat khau khong duoc ghi vao nhat ky';

-- Xoá sách (không còn bản sao) → 1 dòng 'XOA'
EXEC sp_book_delete @book_id = @book_id10;
DECLARE @xoaSach INT = (SELECT COUNT(*) FROM nhatky
                        WHERE bang = 'books' AND doituong = 'TEST-VN-010' AND hanhdong = 'XOA');
IF @xoaSach <> 1
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: xoa sach phai ghi 1 dong XOA, actual = ' + CAST(@xoaSach AS NVARCHAR(10));
END
ELSE PRINT N'OK: xoa sach → 1 dong nhat ky XOA';

-- Xoá bạn đọc (không có lịch sử mượn / đặt chỗ) → 1 dòng 'XOA'
EXEC sp_reader_delete @reader_id = @reader_id10;
DECLARE @xoaDoc INT = (SELECT COUNT(*) FROM nhatky
                       WHERE bang = 'readers' AND doituong = 'THE-NKTEST' AND hanhdong = 'XOA');
IF @xoaDoc <> 1
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: xoa ban doc phai ghi 1 dong XOA, actual = ' + CAST(@xoaDoc AS NVARCHAR(10));
END
ELSE PRINT N'OK: xoa ban doc → 1 dong nhat ky XOA';

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
    SET @fail = @fail + 1;
    PRINT N'FAIL: cho phep dat trung';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    DECLARE @res_book UNIQUEIDENTIFIER = NEWID();
    SET @ngay = DATEADD(DAY, 3, GETDATE());
    EXEC sp_reservation_create @res_id = @res_book, @book_id = '00000000-0000-0000-0000-000000000000',
         @reader_id = @reader_id, @expiry_date = @ngay;
    SET @fail = @fail + 1;
    PRINT N'FAIL: dat cho sach khong ton tai';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    DECLARE @res_rd UNIQUEIDENTIFIER = NEWID();
    SET @ngay = DATEADD(DAY, 3, GETDATE());
    EXEC sp_reservation_create @res_id = @res_rd, @book_id = @book_id, @reader_id = @reader_hethan,
         @expiry_date = @ngay;
    SET @fail = @fail + 1;
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
    SET @fail = @fail + 1;
    PRINT N'FAIL: nhan cho da het han';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

-- Hết hạn → expire set trangthai = 3 (nhả chỗ cho lần tạo sau)
EXEC sp_reservation_expire;
SELECT @str = CAST(trangthai AS NVARCHAR(10)) FROM reservations WHERE res_id = @res_hethan;
IF @str <> N'3'
BEGIN
    SET @fail = @fail + 1;
    PRINT N'FAIL: cho het han phai trangthai = 3, actual = ' + @str;
END
ELSE PRINT N'OK: cap nhat cho het han, trangthai = ' + @str;

-- Đặt chỗ hợp lệ → receive OK → receive lại lỗi → cancel lỗi
DECLARE @res2 UNIQUEIDENTIFIER = NEWID();
SET @ngay = DATEADD(DAY, 5, GETDATE());
EXEC sp_reservation_create @res_id = @res2, @book_id = @book_id, @reader_id = @reader_id,
     @expiry_date = @ngay;
EXEC sp_reservation_receive @res_id = @res2;
PRINT N'OK: nhan cho';

BEGIN TRY
    EXEC sp_reservation_receive @res_id = @res2;
    SET @fail = @fail + 1;
    PRINT N'FAIL: nhan cho lan 2';
END TRY
BEGIN CATCH PRINT N'OK: ' + ERROR_MESSAGE(); END CATCH;

BEGIN TRY
    EXEC sp_reservation_cancel @res_id = @res2;
    SET @fail = @fail + 1;
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
DELETE FROM loan_details WHERE loan_id IN (
    SELECT l.loan_id FROM loans l
    INNER JOIN readers r ON r.reader_id = l.reader_id
    WHERE r.so_the IN ('THE-SPTEST', 'THE-HETHAN'));
DELETE FROM reservations WHERE reader_id IN (@reader_id, @reader_hethan);
DELETE FROM loans WHERE reader_id IN (@reader_id, @reader_hethan);
DELETE FROM copies WHERE book_id IN (@book_id, @book_id2, @book_id9);
DELETE FROM books WHERE book_id IN (@book_id, @book_id2, @book_id9);
DELETE FROM readers WHERE reader_id IN (@reader_id, @reader_hethan);
DELETE FROM shelves WHERE shelf_id = @shelf_id;
-- Nhật ký: dọn sau cùng vì xoá sách/bạn đọc ở trên còn sinh thêm dòng log
DELETE FROM nhatky;
PRINT N'OK: da don du lieu test';

-- ========== [9] KẾT LUẬN ==========
PRINT N'========== KET THUC TEST SP ==========';
IF @fail > 0
BEGIN
    DECLARE @msg NVARCHAR(200) = N'=== TEST SP THAT BAI: ' + CAST(@fail AS NVARCHAR(10)) + N' ca ===';
    THROW 59999, @msg, 1;
END
PRINT N'=== TAT CA TEST SP PASS ===';
GO