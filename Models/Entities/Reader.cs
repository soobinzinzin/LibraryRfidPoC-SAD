namespace LibraryRfidPoC.Models.Entities;

/// <summary>
/// Thực thể độc giả (kế thừa User theo mô hình TPT).
/// </summary>
public class Reader : User
{
    public string ReaderType { get; set; } = string.Empty;
    public decimal TotalFines { get; set; }
    public int BorrowLimit { get; set; }
}

