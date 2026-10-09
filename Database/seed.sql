-- =====================================================
-- seed.sql - Du lieu mau cho LibraryRfidDB
-- Chay SAU schema.sql. Ngay thang tinh theo thoi diem chay.
-- =====================================================
USE LibraryRfidDB;
GO

-- Nguoi dung: R001 binh thuong, R002 no phat 60.000, R003 bi khoa, L001 thu thu
INSERT INTO dbo.Users (userID, fullName, email, accountStatus) VALUES
(N'R001', N'Nguyễn Văn An',  N'an.nguyen@example.com',  N'Active'),
(N'R002', N'Trần Thị Bình',  N'binh.tran@example.com',  N'Active'),
(N'R003', N'Lê Hoàng Cường', N'cuong.le@example.com',   N'Locked'),
(N'L001', N'Phạm Thu Hà',    N'ha.pham@example.com',    N'Active');

INSERT INTO dbo.Readers (userID, readerType, totalFines, borrowLimit) VALUES
(N'R001', N'Student', 0,     5),
(N'R002', N'Student', 60000, 5),
(N'R003', N'Student', 0,     5);

INSERT INTO dbo.Librarians (userID, employeeID, department, roleLevel) VALUES
(N'L001', N'NV001', N'Phòng Mượn trả', N'Staff');

-- Dau sach
INSERT INTO dbo.Books (isbn, title, author, publisher, category) VALUES
(N'978-604-0001', N'Phân tích và Thiết kế Hệ thống Thông tin', N'Nguyễn Văn A', N'NXB Giáo dục',  N'CNTT'),
(N'978-604-0002', N'Lập trình Ứng dụng Web với ASP.NET Core',  N'Trần Thị B',   N'NXB Khoa học',  N'CNTT'),
(N'978-604-0003', N'Ứng dụng Công nghệ RFID trong Quản lý',    N'Lê Văn C',     N'NXB Bách khoa', N'Công nghệ'),
(N'978-604-0004', N'Cơ sở dữ liệu nâng cao',                   N'Phạm Văn D',   N'NXB Giáo dục',  N'CNTT');

-- Ban sao (bookID = ma RFID)
INSERT INTO dbo.BookItems (bookID, isbn, status, isSecurityEnabled) VALUES
(N'RFID-001', N'978-604-0001', N'Available', 1),
(N'RFID-002', N'978-604-0002', N'Available', 1),
(N'RFID-003', N'978-604-0003', N'Available', 1),  -- dang bi R002 dat truoc
(N'RFID-004', N'978-604-0001', N'Borrowed',  0),  -- R001 muon, DA QUA HAN 6 ngay
(N'RFID-005', N'978-604-0002', N'Borrowed',  0),  -- R001 muon, con han
(N'RFID-006', N'978-604-0003', N'Available', 1),  -- R002 da tra tre truoc do
(N'RFID-007', N'978-604-0004', N'Available', 1);

-- Phieu muon
INSERT INTO dbo.Loans (loanID, userID, bookID, borrowDate, dueDate, returnDate, status) VALUES
(N'LN001', N'R001', N'RFID-004', DATEADD(DAY, -20, SYSDATETIME()), DATEADD(DAY, -6,  SYSDATETIME()), NULL, N'Borrowed'),
(N'LN002', N'R001', N'RFID-005', DATEADD(DAY, -3,  SYSDATETIME()), DATEADD(DAY, 11,  SYSDATETIME()), NULL, N'Borrowed'),
(N'LN003', N'R002', N'RFID-006', DATEADD(DAY, -40, SYSDATETIME()), DATEADD(DAY, -26, SYSDATETIME()), DATEADD(DAY, -14, SYSDATETIME()), N'Returned');

-- Khoan phat cua R002 (12 ngay x 5.000 = 60.000)
INSERT INTO dbo.Fines (fineID, userID, bookID, loanID, amount, reason, status, createdAt) VALUES
(N'F001', N'R002', N'RFID-006', N'LN003', 60000, N'Trả trễ hạn 12 ngày', N'Unpaid', DATEADD(DAY, -14, SYSDATETIME()));

-- Dat truoc: R002 dat RFID-003
INSERT INTO dbo.Reservations (reservationID, userID, bookID, reservationDate, expirationDate, status) VALUES
(N'RS001', N'R002', N'RFID-003', SYSDATETIME(), DATEADD(DAY, 3, SYSDATETIME()), N'Active');
GO
