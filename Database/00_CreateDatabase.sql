-- =============================================
-- Script 00: Tạo database DoAn2 (nếu chưa tồn tại)
-- Dùng cho triển khai Docker — SQL Server container
-- ban đầu chỉ có database system (master). Các script
-- 01-08 đều giả định database DoAn2 đã tồn tại (USE DoAn2),
-- nên script này phải chạy TRƯỚC script 01.
-- =============================================
USE master;
GO

IF DB_ID('DoAn2') IS NULL
BEGIN
    CREATE DATABASE DoAn2;
    PRINT N'Đã tạo database DoAn2.';
END
ELSE
BEGIN
    PRINT N'Database DoAn2 đã tồn tại — bỏ qua.';
END
GO