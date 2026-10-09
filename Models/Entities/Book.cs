namespace LibraryRfidPoC.Models.Entities;

/// <summary>
/// Thực thể sách (thông tin đầu sách chung).
/// </summary>
public class Book
{
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

