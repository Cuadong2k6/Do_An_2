-- =============================================
-- Script 01: Tạo các bảng cho Hệ Thống Quản Lý Thư Viện
-- Database: LibraryDB
-- =============================================
USE LibraryDB;
GO

-- 1. Kệ sách
CREATE TABLE shelves (
    shelf_id       INT IDENTITY(1,1) PRIMARY KEY,
    location_code  NVARCHAR(50)  NOT NULL,
    mota           NVARCHAR(200) NULL
);
GO

-- 2. Đầu sách
CREATE TABLE books (
    book_id     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    title       NVARCHAR(300) NOT NULL,
    isbn        VARCHAR(20)   UNIQUE NOT NULL,
    tacgia      NVARCHAR(200) NULL,
    theloai     NVARCHAR(100) NULL,
    nxb         NVARCHAR(200) NULL,
    namxuatban  INT           NULL,
    mota        NVARCHAR(MAX) NULL,
    image_url   NVARCHAR(500) NULL,
    ngaytao     DATETIME      DEFAULT GETDATE()
);
GO

-- 3. Bản sao vật lý (status: 0=Sẵn có, 1=Đã mượn, 2=Hỏng)
CREATE TABLE copies (
    copy_id    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    book_id    UNIQUEIDENTIFIER NOT NULL REFERENCES books(book_id),
    shelf_id   INT              NULL REFERENCES shelves(shelf_id),
    mabancao   NVARCHAR(50)     NOT NULL UNIQUE,
    status     INT              NOT NULL DEFAULT 0,
    ngaynhap   DATETIME         DEFAULT GETDATE()
);
GO

-- 4. Thẻ bạn đọc (trangthai: 0=Hoạt động, 1=Hết hạn, 2=Bị khoá)
CREATE TABLE readers (
    reader_id    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    hoten        NVARCHAR(200) NOT NULL,
    email        NVARCHAR(200) UNIQUE NOT NULL,
    sodienthoai  VARCHAR(15)   NULL,
    diachi       NVARCHAR(300) NULL,
    so_the       NVARCHAR(50)  NOT NULL UNIQUE,
    matkhau      NVARCHAR(255) NOT NULL,
    ngaycap      DATETIME      DEFAULT GETDATE(),
    ngayhethan   DATETIME      NULL,
    trangthai    INT           NOT NULL DEFAULT 0,
    somughin     INT           NOT NULL DEFAULT 3  -- Giới hạn số sách mượn đồng thời
);
GO

-- 5. Phiếu mượn trả (trangthai: 0=Đang mượn, 1=Đã trả, 2=Quá hạn)
CREATE TABLE loans (
    loan_id      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    reader_id    UNIQUEIDENTIFIER NOT NULL REFERENCES readers(reader_id),
    loan_date    DATETIME         NOT NULL DEFAULT GETDATE(),
    due_date     DATETIME         NOT NULL,
    return_date  DATETIME         NULL,
    trangthai    INT              NOT NULL DEFAULT 0,
    ghichu       NVARCHAR(500)    NULL
);
GO

-- 6. Chi tiết phiếu mượn (1 phiếu mượn nhiều bản sao)
CREATE TABLE loan_details (
    id       INT              IDENTITY(1,1) PRIMARY KEY,
    loan_id  UNIQUEIDENTIFIER NOT NULL REFERENCES loans(loan_id),
    copy_id  UNIQUEIDENTIFIER NOT NULL REFERENCES copies(copy_id)
);
GO

-- 7. Đặt chỗ sách (trangthai: 0=Đang chờ, 1=Đã nhận, 2=Đã huỷ, 3=Hết hạn)
CREATE TABLE reservations (
    res_id       UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    book_id      UNIQUEIDENTIFIER NOT NULL REFERENCES books(book_id),
    reader_id    UNIQUEIDENTIFIER NOT NULL REFERENCES readers(reader_id),
    res_date     DATETIME         NOT NULL DEFAULT GETDATE(),
    expiry_date  DATETIME         NOT NULL,
    trangthai    INT              NOT NULL DEFAULT 0
);
GO

-- 8. Phiếu phạt trễ hạn
CREATE TABLE fines (
    fine_id   UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    loan_id   UNIQUEIDENTIFIER NOT NULL REFERENCES loans(loan_id),
    songaytre INT              NOT NULL DEFAULT 0,
    amount    DECIMAL(18,2)    NOT NULL DEFAULT 0,
    is_paid   BIT              NOT NULL DEFAULT 0,
    ngaytao   DATETIME         DEFAULT GETDATE()
);
GO

-- 9. Thanh toán tiền phạt
CREATE TABLE payments (
    pay_id        UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    fine_id       UNIQUEIDENTIFIER NOT NULL REFERENCES fines(fine_id),
    sotien        DECIMAL(18,2)    NOT NULL,
    payment_date  DATETIME         DEFAULT GETDATE(),
    phuongthuc    NVARCHAR(50)     NULL,
    ghichu        NVARCHAR(300)    NULL
);
GO

-- 10. Tài khoản quản trị (Admin/Thủ thư)
CREATE TABLE users (
    user_id   UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    hoten     NVARCHAR(200) NOT NULL,
    email     NVARCHAR(200) UNIQUE NOT NULL,
    taikhoan  NVARCHAR(100) NOT NULL UNIQUE,
    matkhau   NVARCHAR(255) NOT NULL,
    role      NVARCHAR(50)  NOT NULL DEFAULT 'ThuThu',  -- Admin | ThuThu
    image_url NVARCHAR(500) NULL,
    ngaytao   DATETIME      DEFAULT GETDATE()
);
GO

PRINT 'Tạo bảng thành công!';
