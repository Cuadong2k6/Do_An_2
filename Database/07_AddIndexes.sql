-- =============================================
-- Script 07: Thêm index cho các CỘT KHÓA NGOẠI đang được truy vấn
-- Mục đích: 3 bảng copies / loan_details / loans trước đây CHƯA CÓ index nào
--           ngoài PK và UNIQUE. Các truy vấn lọc theo khóa ngoại buộc SQL Server
--           phải quét (Clustered Index Scan) toàn bộ bảng.
-- Chạy 1 lần trên DB đã có dữ liệu (DoAn2). Chạy lại nhiều lần vẫn an toàn.
--
-- KHÔNG thêm index cho:
--   - payments.fine_id : bảng payments chỉ có INSERT, không có đường đọc nào
--     (xem 02_StoredProcedures.sql:1040) → thêm index là thừa.
--   - copies.shelf_id, fines.loan_id, reservations(reader_id, book_id),
--     loan_details.copy_id : cũng đáng thêm nhưng để sau, xem mục "việc còn lại"
--     trong PLAN/tien-do-2026-10-04_*.md.
--
-- LƯU Ý: CREATE INDEX giữ khoá Sch-M trên bảng → chặn truy vấn trong lúc tạo.
--   Với dữ liệu vài chục dòng như hiện tại thì tức thì. Trên dữ liệu thật lớn
--   phải tạo ngoài giờ cao điểm.
-- =============================================
USE DoAn2;
GO

-- -------------------------------------------------------------
-- 1) copies(book_id, status)
--    Nơi dùng:
--      - sp_book_search (dòng 33-34): HAI correlated subquery đếm bản sao,
--        chạy LẶP LẠI cho từng dòng sách trả về. Không có index thì mỗi lần
--        là một lần quét toàn bảng copies. Đây là nút thắt rõ nhất.
--      - sp_book_getbyid (dòng 52-53): cùng kiểu correlated subquery.
--      - sp_loan_create_auto (dòng 662): lọc cả book_id và status = 0.
--      - sp_book_update (132, 156, 171): điều chỉnh số bản sao theo yêu cầu (tăng/giảm)
--      - sp_copy_getlist (281, 295): lọc bản sao theo sách + trạng thái
--      - sp_book_delete (192): kiểm tra tồn tại bản sao trước khi xoá sách
--      - sp_report_tonkho (1114): báo cáo tồn kho theo thể loại.
--    Gộp 2 cột vì phần lớn truy vấn đều lọc CẢ book_id LẪN status.
-- -------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'ix_copies_book_status' AND object_id = OBJECT_ID('dbo.copies'))
BEGIN
    CREATE INDEX ix_copies_book_status ON copies(book_id, status);
    PRINT 'Đã tạo index ix_copies_book_status.';
END
ELSE PRINT 'Index ix_copies_book_status đã tồn tại, bỏ qua.';
GO

-- -------------------------------------------------------------
-- 2) loan_details(loan_id)
--    Nơi dùng:
--      - sp_loan_return (dòng 724): trả sách → lấy copy_id của phiếu để
--        chuyển copies.status về 0. Chạy mỗi lần trả.
--      - sp_reader_sodangmuon (dòng 565): đếm số sách đang mượn để kiểm tra
--        giới hạn thẻ. CHẠY MỖI LẦN bạn đọc bấm mượn.
--      - sp_loan_get_active (824), sp_loan_get_overdue (781): tìm phiếu theo tên sách.
-- -------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'ix_loan_details_loan' AND object_id = OBJECT_ID('dbo.loan_details'))
BEGIN
    CREATE INDEX ix_loan_details_loan ON loan_details(loan_id);
    PRINT 'Đã tạo index ix_loan_details_loan.';
END
ELSE PRINT 'Index ix_loan_details_loan đã tồn tại, bỏ qua.';
GO

-- -------------------------------------------------------------
-- 3) loans(reader_id)
--    Nơi dùng:
--      - sp_reader_sodangmuon (dòng 566): lọc phiếu theo bạn đọc, chạy mỗi lần mượn.
--      - sp_loan_get_by_reader (dòng 855, 860): lịch sử mượn của bạn đọc.
--      - sp_reader_delete (545): chặn xoá độc giả còn lịch sử mượn.
-- -------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'ix_loans_reader' AND object_id = OBJECT_ID('dbo.loans'))
BEGIN
    CREATE INDEX ix_loans_reader ON loans(reader_id);
    PRINT 'Đã tạo index ix_loans_reader.';
END
ELSE PRINT 'Index ix_loans_reader đã tồn tại, bỏ qua.';
GO

PRINT 'Hoàn tất script 07 - Index cho khóa ngoại.';
GO
