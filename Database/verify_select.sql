USE LibraryRfidDB;
GO

SELECT loanID, bookID, status, returnDate FROM Loans;
SELECT fineID, userID, loanID, amount, status FROM Fines;
SELECT userID, totalFines FROM Readers;
SELECT bookID, status, isSecurityEnabled FROM BookItems;
GO
