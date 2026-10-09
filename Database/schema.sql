-- =====================================================
-- schema.sql - CSDL LibraryRfidDB (sinh tu Class Diagram)
-- Chay trong SSMS. Script xoa va tao lai toan bo bang.
-- =====================================================
IF DB_ID(N'LibraryRfidDB') IS NULL
    CREATE DATABASE LibraryRfidDB;
GO

USE LibraryRfidDB;
GO

-- Xoa bang cu (theo thu tu khoa ngoai)
IF OBJECT_ID(N'dbo.Fines', N'U') IS NOT NULL DROP TABLE dbo.Fines;
IF OBJECT_ID(N'dbo.Reservations', N'U') IS NOT NULL DROP TABLE dbo.Reservations;
IF OBJECT_ID(N'dbo.Loans', N'U') IS NOT NULL DROP TABLE dbo.Loans;
IF OBJECT_ID(N'dbo.BookItems', N'U') IS NOT NULL DROP TABLE dbo.BookItems;
IF OBJECT_ID(N'dbo.Books', N'U') IS NOT NULL DROP TABLE dbo.Books;
IF OBJECT_ID(N'dbo.Librarians', N'U') IS NOT NULL DROP TABLE dbo.Librarians;
IF OBJECT_ID(N'dbo.Readers', N'U') IS NOT NULL DROP TABLE dbo.Readers;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- Lop User (abstract)
CREATE TABLE dbo.Users (
    userID        NVARCHAR(20)  NOT NULL PRIMARY KEY,
    fullName      NVARCHAR(100) NOT NULL,
    email         NVARCHAR(100) NOT NULL,
    accountStatus NVARCHAR(20)  NOT NULL DEFAULT N'Active'
);

-- Lop Reader (ke thua User)
CREATE TABLE dbo.Readers (
    userID     NVARCHAR(20)  NOT NULL PRIMARY KEY,
    readerType NVARCHAR(20)  NOT NULL,
    totalFines DECIMAL(18,0) NOT NULL DEFAULT 0,
    borrowLimit INT          NOT NULL DEFAULT 5,
    CONSTRAINT FK_Readers_Users FOREIGN KEY (userID) REFERENCES dbo.Users(userID)
);

-- Lop Librarian (ke thua User)
CREATE TABLE dbo.Librarians (
    userID     NVARCHAR(20)  NOT NULL PRIMARY KEY,
    employeeID NVARCHAR(20)  NOT NULL UNIQUE,
    department NVARCHAR(100) NULL,
    roleLevel  NVARCHAR(20)  NULL,
    CONSTRAINT FK_Librarians_Users FOREIGN KEY (userID) REFERENCES dbo.Users(userID)
);

-- Lop Book (dau sach)
CREATE TABLE dbo.Books (
    isbn      NVARCHAR(20)  NOT NULL PRIMARY KEY,
    title     NVARCHAR(255) NOT NULL,
    author    NVARCHAR(255) NULL,
    publisher NVARCHAR(255) NULL,
    category  NVARCHAR(100) NULL
);

-- Lop BookItem (ban sao co chip RFID, bookID = ma RFID)
CREATE TABLE dbo.BookItems (
    bookID            NVARCHAR(20) NOT NULL PRIMARY KEY,
    isbn              NVARCHAR(20) NOT NULL,
    status            NVARCHAR(20) NOT NULL DEFAULT N'Available',
    isSecurityEnabled BIT          NOT NULL DEFAULT 1,
    CONSTRAINT FK_BookItems_Books FOREIGN KEY (isbn) REFERENCES dbo.Books(isbn)
);

-- Lop Loan
CREATE TABLE dbo.Loans (
    loanID     NVARCHAR(20) NOT NULL PRIMARY KEY,
    userID     NVARCHAR(20) NOT NULL,
    bookID     NVARCHAR(20) NOT NULL,
    borrowDate DATETIME2    NOT NULL,
    dueDate    DATETIME2    NOT NULL,
    returnDate DATETIME2    NULL,
    status     NVARCHAR(20) NOT NULL,
    CONSTRAINT FK_Loans_Readers   FOREIGN KEY (userID) REFERENCES dbo.Readers(userID),
    CONSTRAINT FK_Loans_BookItems FOREIGN KEY (bookID) REFERENCES dbo.BookItems(bookID)
);

-- Lop Fine (loanID la cot bo sung de hien thuc quan he Loan 1 - 0..* Fine)
CREATE TABLE dbo.Fines (
    fineID    NVARCHAR(20)  NOT NULL PRIMARY KEY,
    userID    NVARCHAR(20)  NOT NULL,
    bookID    NVARCHAR(20)  NOT NULL,
    loanID    NVARCHAR(20)  NOT NULL,
    amount    DECIMAL(18,0) NOT NULL,
    reason    NVARCHAR(255) NULL,
    status    NVARCHAR(20)  NOT NULL DEFAULT N'Unpaid',
    createdAt DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Fines_Readers   FOREIGN KEY (userID) REFERENCES dbo.Readers(userID),
    CONSTRAINT FK_Fines_BookItems FOREIGN KEY (bookID) REFERENCES dbo.BookItems(bookID),
    CONSTRAINT FK_Fines_Loans     FOREIGN KEY (loanID) REFERENCES dbo.Loans(loanID)
);

-- Lop Reservation
CREATE TABLE dbo.Reservations (
    reservationID  NVARCHAR(20) NOT NULL PRIMARY KEY,
    userID         NVARCHAR(20) NOT NULL,
    bookID         NVARCHAR(20) NOT NULL,
    reservationDate DATETIME2   NOT NULL,
    expirationDate DATETIME2    NOT NULL,
    status         NVARCHAR(20) NOT NULL,
    CONSTRAINT FK_Reservations_Readers   FOREIGN KEY (userID) REFERENCES dbo.Readers(userID),
    CONSTRAINT FK_Reservations_BookItems FOREIGN KEY (bookID) REFERENCES dbo.BookItems(bookID)
);
GO
