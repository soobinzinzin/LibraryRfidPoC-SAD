namespace LibraryRfidPoC.Models.Entities;

/// <summary>
/// Thực thể tài liệu/cuốn sách vật lý gắn mã RFID.
/// </summary>
public class BookItem
{
    public string BookID { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsSecurityEnabled { get; set; }
}

