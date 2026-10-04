-- =============================================
-- Script 06: Thêm bảng NHẬT KÝ THAY ĐỔI + trigger ghi tự động
-- Mục đích: giữ lại dấu vết khi Admin/Thủ thư SỬA hoặc XÓA sách / bạn đọc.
--           Trước đây dữ liệu cũ bị UPDATE ghi đè mất không dấu vết.
-- Chạy 1 lần trên DB đã có dữ liệu (DoAn2).
-- =============================================
USE DoAn2;
GO

IF OBJECT_ID('nhatky', 'U') IS NULL
BEGIN
    CREATE TABLE nhatky (
        nhatky_id  BIGINT IDENTITY(1,1) PRIMARY KEY,
        thoigian   DATETIME      NOT NULL DEFAULT GETDATE(),
        nguoithuc  NVARCHAR(50)  NOT NULL DEFAULT N'ADMIN/ThuThu',
        bang       NVARCHAR(30)  NOT NULL,   -- 'books' | 'readers'
        doituong   NVARCHAR(300) NOT NULL,   -- books → ISBN, readers → số thẻ
        hanhdong   NVARCHAR(20)  NOT NULL,   -- 'SUA' | 'XOA'
        truong     NVARCHAR(100) NULL,       -- tên cột bị thay đổi (NULL nếu là xoá)
        truoc      NVARCHAR(500) NULL,
        sau        NVARCHAR(500) NULL
    );
    PRINT 'Đã tạo bảng nhatky.';
END
ELSE PRINT 'Bảng nhatky đã tồn tại, bỏ qua.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_nhatky_thoigian')
BEGIN
    CREATE INDEX ix_nhatky_thoigian ON nhatky(thoigian DESC);
    PRINT 'Đã tạo index ix_nhatky_thoigian.';
END
GO

-- =============================================
-- Trigger ghi nhật ký
-- Ghi khi SỬA và XÓA. KHÔNG ghi khi thêm mới (không mất được gì).
-- Cố ý bỏ qua: books.mota (text dài) và readers.matkhau (không ghi mật khẩu).
-- =============================================

IF OBJECT_ID('trg_books_nhatky', 'TR') IS NOT NULL DROP TRIGGER trg_books_nhatky;
GO

CREATE TRIGGER trg_books_nhatky ON books
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- XÓA: 'inserted' rỗng khi xoá, và không rỗng khi sửa
    -- (trigger không bắt INSERT nên rỗng = chắc chắn là DELETE)
    IF NOT EXISTS (SELECT 1 FROM inserted)
    BEGIN
        INSERT INTO nhatky (nguoithuc, bang, doituong, hanhdong, truoc, sau)
        SELECT N'ADMIN/ThuThu', N'books', d.isbn, N'XOA',
               N'Tiêu đề: ' + ISNULL(d.title, N'(trống)'), NULL
        FROM deleted d;
    END

    -- SỬA: mỗi trường thật sự đổi sinh 1 dòng (CROSS APPLY + WHERE loại trường không đổi)
    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        INSERT INTO nhatky (nguoithuc, bang, doituong, hanhdong, truong, truoc, sau)
        SELECT N'ADMIN/ThuThu', N'books', i.isbn, N'SUA',
               v.truong, v.cu, v.moi
        FROM inserted i
        INNER JOIN deleted d ON d.book_id = i.book_id
        CROSS APPLY (VALUES
            (N'Tiêu đề',   CAST(d.title       AS NVARCHAR(500)), CAST(i.title       AS NVARCHAR(500))),
            (N'ISBN',       CAST(d.isbn        AS NVARCHAR(500)), CAST(i.isbn        AS NVARCHAR(500))),
            (N'Tác giả',    CAST(d.tacgia      AS NVARCHAR(500)), CAST(i.tacgia      AS NVARCHAR(500))),
            (N'Thể loại',   CAST(d.theloai     AS NVARCHAR(500)), CAST(i.theloai     AS NVARCHAR(500))),
            (N'NXB',        CAST(d.nxb         AS NVARCHAR(500)), CAST(i.nxb         AS NVARCHAR(500))),
            (N'Năm XB',     CAST(d.namxuatban  AS NVARCHAR(500)), CAST(i.namxuatban  AS NVARCHAR(500))),
            (N'Ảnh bìa',    CAST(d.image_url   AS NVARCHAR(500)), CAST(i.image_url   AS NVARCHAR(500)))
        ) v(truong, cu, moi)
        WHERE ISNULL(v.cu, N'') <> ISNULL(v.moi, N'');
    END
END
GO

IF OBJECT_ID('trg_readers_nhatky', 'TR') IS NOT NULL DROP TRIGGER trg_readers_nhatky;
GO

CREATE TRIGGER trg_readers_nhatky ON readers
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- XÓA
    IF NOT EXISTS (SELECT 1 FROM inserted)
    BEGIN
        INSERT INTO nhatky (nguoithuc, bang, doituong, hanhdong, truoc, sau)
        SELECT N'ADMIN/ThuThu', N'readers', d.so_the, N'XOA',
               N'Họ tên: ' + ISNULL(d.hoten, N'(trống)'), NULL
        FROM deleted d;
    END

    -- SỬA (không ghi matkhau — không bao giờ lưu mật khẩu vào nhật ký)
    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        INSERT INTO nhatky (nguoithuc, bang, doituong, hanhdong, truong, truoc, sau)
        SELECT N'ADMIN/ThuThu', N'readers', i.so_the, N'SUA',
               v.truong, v.cu, v.moi
        FROM inserted i
        INNER JOIN deleted d ON d.reader_id = i.reader_id
        CROSS APPLY (VALUES
            (N'Họ tên',      CAST(d.hoten       AS NVARCHAR(500)), CAST(i.hoten       AS NVARCHAR(500))),
            (N'Email',       CAST(d.email       AS NVARCHAR(500)), CAST(i.email       AS NVARCHAR(500))),
            (N'SĐT',         CAST(d.sodienthoai AS NVARCHAR(500)), CAST(i.sodienthoai AS NVARCHAR(500))),
            (N'Địa chỉ',     CAST(d.diachi      AS NVARCHAR(500)), CAST(i.diachi      AS NVARCHAR(500))),
            (N'Số thẻ',      CAST(d.so_the      AS NVARCHAR(500)), CAST(i.so_the      AS NVARCHAR(500))),
            (N'Hạn thẻ',     CAST(d.ngayhethan  AS NVARCHAR(500)), CAST(i.ngayhethan  AS NVARCHAR(500))),
            (N'Trạng thái',  CAST(d.trangthai   AS NVARCHAR(500)), CAST(i.trangthai   AS NVARCHAR(500))),
            (N'Số sách mượn tối đa', CAST(d.somughin AS NVARCHAR(500)), CAST(i.somughin AS NVARCHAR(500)))
        ) v(truong, cu, moi)
        WHERE ISNULL(v.cu, N'') <> ISNULL(v.moi, N'');
    END
END
GO

PRINT 'Hoàn tất script 06 - Nhật ký thay đổi.';
GO