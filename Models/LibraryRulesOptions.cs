namespace LibraryRfidPoC.Models;

/// <summary>
/// Các cấu hình quy định mượn trả của thư viện.
/// </summary>
public class LibraryRulesOptions
{
    public int MaxBooksPerLoan { get; set; }
    public int LoanDays { get; set; }
    public int FinePerDay { get; set; }
    public int FineLockThreshold { get; set; }
}

