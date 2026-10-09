namespace LibraryRfidPoC.Models.Entities;

/// <summary>
/// Thực thể phiếu phạt người dùng.
/// </summary>
public class Fine
{
    public string FineID { get; set; } = string.Empty;
    public string UserID { get; set; } = string.Empty;
    public string BookID { get; set; } = string.Empty;
    public string LoanID { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Tính số tiền phạt theo số ngày trễ hạn và mức phạt mỗi ngày (nguyên tắc Information Expert).
    /// </summary>
    public static decimal CalculateAmount(int daysOverdue, int finePerDay) => daysOverdue <= 0 ? 0m : daysOverdue * (decimal)finePerDay;
}

