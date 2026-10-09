namespace LibraryRfidPoC.Models.Entities;

/// <summary>
/// Thực thể phiếu mượn tài liệu.
/// </summary>
public class Loan
{
    public string LoanID { get; set; } = string.Empty;
    public string UserID { get; set; } = string.Empty;
    public string BookID { get; set; } = string.Empty;
    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Kiểm tra số ngày quá hạn của phiếu mượn (nguyên tắc Information Expert).
    /// </summary>
    public int CheckOverdue(DateTime currentDate) => Math.Max(0, (currentDate.Date - DueDate.Date).Days);
}

