-- ============================================================
-- Script 08: Seed dữ liệu khối lượng lớn (≥ 2.000 bản ghi)
-- ------------------------------------------------------------
-- Mục đích : đáp ứng yêu cầu PRD "dữ liệu mẫu ≥ 2.000 bản ghi".
-- An toàn  : CHỈ INSERT thêm (top-up theo ngưỡng), KHÔNG xóa /
--            không sửa dữ liệu cũ, không đụng schema.
-- Idempotent: chạy lại không tạo trùng bản ghi (guard theo
--            COUNT(*) / NOT EXISTS). Các mã định danh sinh theo
--            số lượng hiện có nên chạy dở rồi chạy lại vẫn an toàn.
-- Thứ tự   : phải chạy sau 01_CreateTables -> 03_SeedData (cần FK).
--
-- Mục tiêu số bản ghi (sau khi chạy):
--   shelves 30 | books 420 | copies 1100 | readers 320 | users 6
--   loans 360 | loan_details ~708 | reservations 150
--   fines ~110 (giới hạn bởi số phiếu trả trễ/quá hạn) | payments ~89
--   -> tổng ~3.293 (≥ 2.000) ✓
-- ============================================================
USE DoAn2;
GO

SET NOCOUNT ON;

-- BCrypt của '123456' (cost 12) - dùng chung cho users/readers seed
DECLARE @MK NVARCHAR(255) = N'$2a$12$h4B10Fio3SBtaN8DF0SOduZ5hBBYUeuuqC4lGkOFCw5eMx4VTF4ZK';


-- ============================================================
-- 1. SHELVES -> đạt 30 kệ
-- ============================================================
DECLARE @needShelf INT = 30 - (SELECT COUNT(*) FROM shelves);
IF @needShelf > 0
BEGIN
    ;WITH n AS (
        SELECT TOP (@needShelf) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
        FROM sys.all_objects a CROSS JOIN sys.all_objects b
    )
    INSERT INTO shelves (location_code, mota)
    SELECT N'K-' + RIGHT('000' + CAST(i AS VARCHAR(5)), 4),
           N'Kệ mẫu ' + CAST(i AS NVARCHAR(5))
    FROM n;
    PRINT N'OK: 08 - shelves +' + CAST(@needShelf AS NVARCHAR(10));
END


-- ============================================================
-- 2. BOOKS -> đạt 420 đầu sách (30 đầu mẫu, sinh thêm tập)
-- ============================================================
DECLARE @baseB INT  = (SELECT COUNT(*) FROM books);
DECLARE @needB  INT = 420 - @baseB;
IF @needB > 0
BEGIN
    ;WITH n AS (
        SELECT TOP (@needB) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
        FROM sys.all_objects a CROSS JOIN sys.all_objects b
    ),
    t AS (
        SELECT * FROM (VALUES
            ( 1, N'Đắc Nhân Tâm',              N'Dale Carnegie',      N'Kỹ năng sống',        N'NXB Trẻ'),
            ( 2, N'Nhà Giả Kim',               N'Paulo Coelho',       N'Văn học nước ngoài',  N'NXB Hội Nhà Văn'),
            ( 3, N'Tuổi Trẻ Đáng Giá Bao Nhiêu', N'Rosie Nguyễn',     N'Kỹ năng sống',        N'NXB Hội Nhà Văn'),
            ( 4, N'Hạt Giống Tâm Hồn',         N'Nhiều tác giả',      N'Kỹ năng sống',        N'NXB Tổng hợp TP.HCM'),
            ( 5, N'Tôi Thấy Hoa Vàng Trên Cỏ Xanh', N'Nguyễn Nhật Ánh', N'Văn học Việt Nam',  N'NXB Trẻ'),
            ( 6, N'Có Hai Con Mèo Ngồi Bên Cửa Sổ', N'Nguyễn Nhật Ánh', N'Văn học Việt Nam',  N'NXB Trẻ'),
            ( 7, N'Cho Tôi Xin Một Vé Đi Tuổi Thơ', N'Nguyễn Nhật Ánh', N'Văn học Việt Nam',  N'NXB Trẻ'),
            ( 8, N'Sống Ảo, Ảo Sống',          N'Phong Điệp',         N'Văn học đương đại',   N'NXB Trẻ'),
            ( 9, N'Cà Phê Cùng Tony',          N'Tony Buổi Sáng',     N'Kinh doanh',          N'NXB Trẻ'),
            (10, N'Trên Đường Băng',           N'Tony Buổi Sáng',     N'Kỹ năng sống',        N'NXB Trẻ'),
            (11, N'Nghĩ Giàu Làm Giàu',        N'Napoleon Hill',      N'Kinh doanh',          N'NXB Trẻ'),
            (12, N'Tôi Tài Giỏi, Bạn Cũng Thế!', N'Adam Khoo',        N'Kỹ năng học tập',     N'NXB Phụ Nữ'),
            (13, N'Bí Mật Của Sự May Mắn',     N'Alex Rovira',        N'Kỹ năng sống',        N'NXB Trẻ'),
            (14, N'Dám Ước Mơ',                N'Khánh Vy',           N'Hồi ký',              N'NXB Trẻ'),
            (15, N'Giáo Trình Lập Trình C#',   N'Phạm Văn Tùng',      N'Công nghệ thông tin', N'NXB Giáo Dục'),
            (16, N'Cấu Trúc Dữ Liệu Và Giải Thuật', N'Nguyễn Văn Hòa', N'Công nghệ thông tin', N'NXB ĐHQG Hà Nội'),
            (17, N'Cơ Sở Dữ Liệu',             N'Trần Đức Quang',     N'Công nghệ thông tin', N'NXB ĐHQG Hà Nội'),
            (18, N'Mạng Máy Tính',             N'Lê Mạnh Hùng',       N'Công nghệ thông tin', N'NXB Bách Khoa'),
            (19, N'Trí Tuệ Nhân Tạo',          N'Ngô Đức Minh',       N'Công nghệ thông tin', N'NXB Bách Khoa'),
            (20, N'An Toàn Thông Tin',         N'Hoàng Quốc Huy',     N'Công nghệ thông tin', N'NXB Bách Khoa'),
            (21, N'Toán Cao Cấp - Tập 1',      N'Nguyễn Đình Trí',    N'Toán học',            N'NXB Giáo Dục'),
            (22, N'Giải Tích 1',               N'Phạm Vũ Nhật',       N'Toán học',            N'NXB Giáo Dục'),
            (23, N'Đại Số Tuyến Tính',         N'Lê Tuấn Hoa',        N'Toán học',            N'NXB ĐHQG Hà Nội'),
            (24, N'Vật Lý Đại Cương',          N'Lương Duyên Bình',   N'Vật lý',              N'NXB Giáo Dục'),
            (25, N'Hóa Học Đại Cương',         N'Nguyễn Đức Huệ',     N'Hóa học',             N'NXB Giáo Dục'),
            (26, N'Giáo Trình Tiếng Anh Giao Tiếp', N'Nhiều tác giả',  N'Ngoại ngữ',          N'NXB Giáo Dục'),
            (27, N'Giáo Trình Tiếng Nhật Minna',   N'Nhiều tác giả',   N'Ngoại ngữ',          N'NXB Trẻ'),
            (28, N'Việt Nam Sử Lược',          N'Trần Trọng Kim',     N'Lịch sử',             N'NXB Văn Hóa'),
            (29, N'Kể Chuyện Các Vua Hùng',    N'Nhiều tác giả',      N'Lịch sử',             N'NXB Kim Đồng'),
            (30, N'Những Bài Thơ Hay Nhất',    N'Nhiều tác giả',      N'Thơ ca',              N'NXB Hội Nhà Văn')
        ) AS v(stt, title, tacgia, theloai, nxb)
    )
    INSERT INTO books (book_id, title, isbn, tacgia, theloai, nxb, namxuatban, mota, ngaytao)
    SELECT NEWID(),
           t.title + N' - Q' + RIGHT('00' + CAST(((n.i - 1) / 30) + 1 AS VARCHAR(3)), 2),
           '978604' + RIGHT('00000000' + CAST(@baseB + n.i AS VARCHAR(20)), 8),
           t.tacgia,
           t.theloai,
           t.nxb,
           2010 + ((n.i - 1) % 16),
           N'Sách mẫu thuộc tủ ' + t.theloai,
           DATEADD(DAY, -((n.i - 1) % 400), GETDATE())
    FROM n
    JOIN t ON t.stt = ((n.i - 1) % 30) + 1;
    PRINT N'OK: 08 - books +' + CAST(@needB AS NVARCHAR(10));
END


-- ============================================================
-- 3. COPIES -> đạt 1.100 bản sao (phân bố đều cho từng đầu sách)
--    status = 0 (Sẵn có); các bản được mượn sẽ do bước 8 cập nhật
-- ============================================================
DECLARE @baseC INT  = (SELECT COUNT(*) FROM copies);
DECLARE @needC  INT = 1100 - @baseC;
IF @needC > 0
BEGIN
    ;WITH n AS (
        SELECT TOP (@needC) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
        FROM sys.all_objects a CROSS JOIN sys.all_objects b
    ),
    bk AS (
        SELECT book_id,
               ROW_NUMBER() OVER (ORDER BY book_id) AS rn,
               COUNT(*) OVER () AS cnt
        FROM books
    ),
    sh AS (
        SELECT shelf_id,
               ROW_NUMBER() OVER (ORDER BY shelf_id) AS rn,
               COUNT(*) OVER () AS cnt
        FROM shelves
    )
    INSERT INTO copies (copy_id, book_id, shelf_id, mabancao, status, ngaynhap)
    SELECT NEWID(),
           b.book_id,
           s.shelf_id,
           N'BC-SEED-' + RIGHT('000000' + CAST(@baseC + n.i AS VARCHAR(10)), 6),
           0,
           DATEADD(DAY, -((n.i - 1) % 200), GETDATE())
    FROM n
    JOIN bk b ON b.rn = ((n.i - 1) % b.cnt) + 1
    JOIN sh s ON s.rn = ((n.i - 1) % s.cnt) + 1;
    PRINT N'OK: 08 - copies +' + CAST(@needC AS NVARCHAR(10));
END


-- ============================================================
-- 4. READERS -> đạt 320 bạn đọc (thẻ còn hạn, đang hoạt động)
-- ============================================================
DECLARE @baseR INT  = (SELECT COUNT(*) FROM readers);
DECLARE @needR  INT = 320 - @baseR;
IF @needR > 0
BEGIN
    ;WITH n AS (
        SELECT TOP (@needR) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
        FROM sys.all_objects a CROSS JOIN sys.all_objects b
    ),
    ho AS (
        SELECT * FROM (VALUES
            (1, N'Nguyễn'), (2, N'Trần'), (3, N'Lê'), (4, N'Phạm'), (5, N'Hoàng'),
            (6, N'Phan'),   (7, N'Vũ'),   (8, N'Đặng'), (9, N'Bùi'), (10, N'Đỗ'),
            (11, N'Hồ'),    (12, N'Ngô'), (13, N'Dương'), (14, N'Lý'), (15, N'Võ')
        ) AS v(stt, ho)
    ),
    ten AS (
        SELECT * FROM (VALUES
            (1, N'An'),   (2, N'Bình'), (3, N'Châu'), (4, N'Dũng'), (5, N'Hà'),
            (6, N'Hùng'), (7, N'Lan'),   (8, N'Minh'), (9, N'Ngọc'), (10, N'Phúc'),
            (11, N'Quân'),(12, N'Thảo'), (13, N'Trang'), (14, N'Việt'), (15, N'Yến')
        ) AS v(stt, ten)
    )
    INSERT INTO readers (reader_id, hoten, email, sodienthoai, diachi, so_the, matkhau, ngaycap, ngayhethan, trangthai, somughin)
    SELECT NEWID(),
           ho.ho + N' ' + ten.ten + N' ' + CAST(@baseR + n.i AS NVARCHAR(10)),
           N'bandoc' + RIGHT('000000' + CAST(@baseR + n.i AS VARCHAR(10)), 6) + N'@thuvien.vn',
           '0' + RIGHT('000000000' + CAST(300000000 + @baseR + n.i AS VARCHAR(20)), 9),
           N'Địa chỉ sample ' + CAST(@baseR + n.i AS NVARCHAR(10)),
           N'RD-' + RIGHT('000000' + CAST(@baseR + n.i AS VARCHAR(10)), 6),
           @MK,
           DATEADD(DAY, -400, GETDATE()),
           DATEADD(DAY, 400, GETDATE()),
           0,
           3
    FROM n
    JOIN ho ON ho.stt = ((n.i - 1) % 15) + 1
    JOIN ten ON ten.stt = (((n.i - 1) / 15) % 15) + 1;
    PRINT N'OK: 08 - readers +' + CAST(@needR AS NVARCHAR(10));
END


-- ============================================================
-- 5. USERS -> đạt 6 tài khoản nhân viên (Admin / ThuThu)
-- ============================================================
DECLARE @needU INT = 6 - (SELECT COUNT(*) FROM users);
IF @needU > 0
BEGIN
    ;WITH u AS (
        SELECT * FROM (VALUES
            (1, N'Quản Trị Viên 2', 'admin2@library.com',   'admin2',   N'Admin'),
            (2, N'Quản Trị Viên 3', 'admin3@library.com',   'admin3',   N'Admin'),
            (3, N'Thủ Thư 2',       'thuthu2@library.com',  'thuthu2',  N'ThuThu'),
            (4, N'Thủ Thư 3',       'thuthu3@library.com',  'thuthu3',  N'ThuThu')
        ) AS v(stt, hoten, email, taikhoan, role)
    )
    INSERT INTO users (user_id, hoten, email, taikhoan, matkhau, role, ngaytao)
    SELECT NEWID(), u.hoten, u.email, u.taikhoan, @MK, u.role, GETDATE()
    FROM u
    WHERE u.stt <= @needU
      AND NOT EXISTS (SELECT 1 FROM users x WHERE x.taikhoan = u.taikhoan);
    PRINT N'OK: 08 - users +' + CAST(@needU AS NVARCHAR(10));
END


-- ============================================================
-- 6. LOANS -> đạt 360 phiếu mượn
--    Phân bố: 10% đã trả trễ  | 20% quá hạn chưa trả | 70% đang mượn
--    (quá hạn xác định động qua due_date < GETDATE() như sp_report_quahan)
-- ============================================================
DECLARE @needL INT = 360 - (SELECT COUNT(*) FROM loans);
IF @needL > 0
BEGIN
    ;WITH n AS (
        SELECT TOP (@needL) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
        FROM sys.all_objects a CROSS JOIN sys.all_objects b
    ),
    rd AS (
        SELECT reader_id,
               ROW_NUMBER() OVER (ORDER BY reader_id) AS rn,
               COUNT(*) OVER () AS cnt
        FROM readers
    )
    INSERT INTO loans (loan_id, reader_id, loan_date, due_date, return_date, trangthai, ghichu)
    SELECT NEWID(),
           r.reader_id,
           CASE (n.i - 1) % 10 WHEN 0 THEN DATEADD(DAY, -12, GETDATE())      -- trả trễ
                               WHEN 1 THEN DATEADD(DAY, -40, GETDATE())      -- quá hạn
                               WHEN 2 THEN DATEADD(DAY, -35, GETDATE())      -- quá hạn
                               ELSE    DATEADD(DAY, -4,  GETDATE()) END,     -- đang mượn
           CASE (n.i - 1) % 10 WHEN 0 THEN DATEADD(DAY, -5,  GETDATE())      -- trả trễ
                               WHEN 1 THEN DATEADD(DAY, -30, GETDATE())      -- quá hạn
                               WHEN 2 THEN DATEADD(DAY, -25, GETDATE())      -- quá hạn
                               ELSE    DATEADD(DAY, 10,  GETDATE()) END,     -- đang mượn
           CASE (n.i - 1) % 10 WHEN 0 THEN DATEADD(DAY, -3,  GETDATE())      -- đã trả (trễ 2 ngày)
                               ELSE NULL END,
           CASE (n.i - 1) % 10 WHEN 0 THEN 1 ELSE 0 END,
           N'Phiếu mượn dữ liệu mẫu'
    FROM n
    JOIN rd r ON r.rn = ((n.i - 1) % r.cnt) + 1;
    PRINT N'OK: 08 - loans +' + CAST(@needL AS NVARCHAR(10));
END


-- ============================================================
-- 7. LOAN_DETAILS -> chi tiết cho các phiếu mượn chưa có chi tiết
--    Mỗi phiếu nhận 1->3 bản sao (chu kỳ 1,2,3 lặp lại).
--    Bản sao được chọn từ pool "sẵn có & chưa nằm trong phiếu nào"
--    để mỗi bản sao chỉ xuất hiện trong đúng 1 phiếu.
-- ============================================================
IF EXISTS (
    SELECT 1 FROM loans l
    WHERE NOT EXISTS (SELECT 1 FROM loan_details d WHERE d.loan_id = l.loan_id)
)
BEGIN
    ;WITH nl AS (
        SELECT l.loan_id,
               ROW_NUMBER() OVER (ORDER BY l.loan_date, l.loan_id) AS k
        FROM loans l
        WHERE NOT EXISTS (SELECT 1 FROM loan_details d WHERE d.loan_id = l.loan_id)
    ),
    pool AS (
        SELECT c.copy_id,
               ROW_NUMBER() OVER (ORDER BY c.copy_id) AS rn
        FROM copies c
        WHERE c.status = 0
          AND NOT EXISTS (SELECT 1 FROM loan_details d WHERE d.copy_id = c.copy_id)
    )
    INSERT INTO loan_details (loan_id, copy_id)
    SELECT nl.loan_id, p.copy_id
    FROM nl
    CROSS APPLY (SELECT ((nl.k - 1) / 3) * 6
                       + CASE (nl.k - 1) % 3 WHEN 0 THEN 0 WHEN 1 THEN 1 ELSE 3 END AS s,
                        ((nl.k - 1) % 3) + 1 AS nd) x
    JOIN pool p ON p.rn > x.s AND p.rn <= x.s + x.nd;
    PRINT N'OK: 08 - loan_details + (chi tiết các phiếu còn thiếu)';
END


-- ============================================================
-- 8. ĐỒNG BỘ copies.status = 1 cho bản sao đang ở phiếu mượn chưa trả
-- ============================================================
UPDATE c
SET c.status = 1
FROM copies c
JOIN loan_details d ON d.copy_id = c.copy_id
JOIN loans l       ON l.loan_id = d.loan_id
WHERE l.return_date IS NULL
  AND c.status = 0;
PRINT N'OK: 08 - đồng bộ copies.status (bản đang mượn -> 1)';


-- ============================================================
-- 9. RESERVATIONS -> 150 phiếu đặt trước
--    Trạng thái: 0 chờ nhận(30%) | 1 đã nhận(10%) | 2 đã hủy(20%) | 3 hết hạn(40%)
-- ============================================================
DECLARE @needRes INT = 150 - (SELECT COUNT(*) FROM reservations);
IF @needRes > 0
BEGIN
    ;WITH n AS (
        SELECT TOP (@needRes) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
        FROM sys.all_objects a CROSS JOIN sys.all_objects b
    ),
    bk AS (
        SELECT book_id,
               ROW_NUMBER() OVER (ORDER BY book_id) AS rn,
               COUNT(*) OVER () AS cnt
        FROM books
    ),
    rd AS (
        SELECT reader_id,
               ROW_NUMBER() OVER (ORDER BY reader_id) AS rn,
               COUNT(*) OVER () AS cnt
        FROM readers
    )
    INSERT INTO reservations (res_id, book_id, reader_id, res_date, expiry_date, trangthai)
    SELECT NEWID(),
           b.book_id,
           r.reader_id,
           CASE (n.i - 1) % 10
                WHEN 0 THEN DATEADD(DAY, -60, GETDATE())   -- hết hạn
                WHEN 1 THEN DATEADD(DAY, -60, GETDATE())
                WHEN 2 THEN DATEADD(DAY, -60, GETDATE())
                WHEN 3 THEN DATEADD(DAY, -30, GETDATE())   -- đã hủy
                WHEN 4 THEN DATEADD(DAY, -30, GETDATE())
                WHEN 5 THEN DATEADD(DAY, -20, GETDATE())   -- đã nhận
                ELSE         DATEADD(DAY, -3,  GETDATE()) END,  -- chờ nhận
           CASE (n.i - 1) % 10
                WHEN 0 THEN DATEADD(DAY, -45, GETDATE())
                WHEN 1 THEN DATEADD(DAY, -45, GETDATE())
                WHEN 2 THEN DATEADD(DAY, -45, GETDATE())
                WHEN 3 THEN DATEADD(DAY, -15, GETDATE())
                WHEN 4 THEN DATEADD(DAY, -15, GETDATE())
                WHEN 5 THEN DATEADD(DAY, -5,  GETDATE())
                ELSE         DATEADD(DAY, 11,  GETDATE()) END,
           CASE (n.i - 1) % 10
                WHEN 0 THEN 3
                WHEN 1 THEN 3
                WHEN 2 THEN 3
                WHEN 3 THEN 2
                WHEN 4 THEN 2
                WHEN 5 THEN 1
                ELSE         0 END
    FROM n
    JOIN bk b ON b.rn = ((n.i - 1) % b.cnt) + 1
    JOIN rd r ON r.rn = ((n.i - 1) % r.cnt) + 1;
    PRINT N'OK: 08 - reservations +' + CAST(@needRes AS NVARCHAR(10));
END


-- ============================================================
-- 10. FINES -> đạt 150 phiếu phạt (chỉ cho phiếu mượn trả trễ/quá hạn)
--     amount = songaytre * 5.000 (đúng quy ước sp reports / tilephattrehan)
--     is_paid = 1 cho 80% (phục vụ bảng payments)
-- ============================================================
DECLARE @needF INT = 150 - (SELECT COUNT(*) FROM fines);
IF @needF > 0
BEGIN
    ;WITH fl AS (
        SELECT l.loan_id,
               DATEDIFF(DAY, l.due_date, ISNULL(l.return_date, GETDATE())) AS songaytre,
               ROW_NUMBER() OVER (ORDER BY l.due_date) AS rn
        FROM loans l
        WHERE NOT EXISTS (SELECT 1 FROM fines f WHERE f.loan_id = l.loan_id)
          AND (  (l.return_date IS NULL     AND l.due_date < GETDATE())
              OR (l.return_date IS NOT NULL AND l.return_date > l.due_date) )
    )
    INSERT INTO fines (fine_id, loan_id, songaytre, amount, is_paid, ngaytao)
    SELECT NEWID(),
           fl.loan_id,
           fl.songaytre,
           CAST(fl.songaytre * 5000 AS DECIMAL(18,2)),
           CASE WHEN fl.rn % 5 = 0 THEN 0 ELSE 1 END,
           DATEADD(DAY, -(fl.rn % 40), GETDATE())
    FROM fl
    WHERE fl.rn <= @needF;
    PRINT N'OK: 08 - fines +' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' (số phiếu trả trễ/quá hạn có sẵn)';
END


-- ============================================================
-- 11. PAYMENTS -> đạt 110 giao dịch thanh toán (cho phiếu phạt đã trả)
-- ============================================================
DECLARE @needP INT = 110 - (SELECT COUNT(*) FROM payments);
IF @needP > 0
BEGIN
    ;WITH pf AS (
        SELECT f.fine_id,
               f.ngaytao,
               ROW_NUMBER() OVER (ORDER BY f.ngaytao, f.fine_id) AS rn
        FROM fines f
        WHERE f.is_paid = 1
          AND NOT EXISTS (SELECT 1 FROM payments p WHERE p.fine_id = f.fine_id)
    )
    INSERT INTO payments (pay_id, fine_id, sotien, payment_date, phuongthuc, ghichu)
    SELECT NEWID(),
           pf.fine_id,
           (SELECT f.amount FROM fines f WHERE f.fine_id = pf.fine_id),
           DATEADD(DAY, pf.rn % 7, pf.ngaytao),
           CASE pf.rn % 3 WHEN 0 THEN N'Chuyển khoản'
                          WHEN 1 THEN N'Tiền mặt'
                          ELSE N'Ví điện tử' END,
           N'Thanh toán phiếu phạt (dữ liệu mẫu)'
    FROM pf
    WHERE pf.rn <= @needP;
    PRINT N'OK: 08 - payments +' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' (phiếu phạt đã đóng)';
END


-- ============================================================
-- 12. KIỂM TRA TỔNG HỢP
-- ============================================================
SELECT 'users'        AS bang, COUNT(*) AS so_ban_ghi FROM users
UNION ALL SELECT 'shelves', COUNT(*) FROM shelves
UNION ALL SELECT 'books', COUNT(*) FROM books
UNION ALL SELECT 'copies', COUNT(*) FROM copies
UNION ALL SELECT 'readers', COUNT(*) FROM readers
UNION ALL SELECT 'loans', COUNT(*) FROM loans
UNION ALL SELECT 'loan_details', COUNT(*) FROM loan_details
UNION ALL SELECT 'reservations', COUNT(*) FROM reservations
UNION ALL SELECT 'fines', COUNT(*) FROM fines
UNION ALL SELECT 'payments', COUNT(*) FROM payments
UNION ALL SELECT 'nhatky', COUNT(*) FROM nhatky;

DECLARE @tong INT =
      (SELECT COUNT(*) FROM users)
    + (SELECT COUNT(*) FROM shelves)
    + (SELECT COUNT(*) FROM books)
    + (SELECT COUNT(*) FROM copies)
    + (SELECT COUNT(*) FROM readers)
    + (SELECT COUNT(*) FROM loans)
    + (SELECT COUNT(*) FROM loan_details)
    + (SELECT COUNT(*) FROM reservations)
    + (SELECT COUNT(*) FROM fines)
    + (SELECT COUNT(*) FROM payments)
    + (SELECT COUNT(*) FROM nhatky);

IF @tong >= 2000
    PRINT N'✅ 08 - TỔNG SỐ BẢN GHI = ' + CAST(@tong AS NVARCHAR(10)) + N' (≥ 2.000)';
ELSE
    THROW 51000, N'08 - Tổng bản ghi < 2.000, cần kiểm tra lại.', 1;
GO