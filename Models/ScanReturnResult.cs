using System.Text.Json.Serialization;

namespace LibraryRfidPoC.Models;

/// <summary>
/// Kết quả nhận diện từng tài liệu RFID khi quét trả sách.
/// </summary>
public class ScanReturnItem
{
    [JsonPropertyName("rfid")]
    public string Rfid { get; set; } = string.Empty;

    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("borrower")]
    public string Borrower { get; set; } = string.Empty;

    [JsonPropertyName("dueDate")]
    public string DueDate { get; set; } = string.Empty;

    [JsonPropertyName("daysOverdue")]
    public int DaysOverdue { get; set; }

    [JsonPropertyName("fineAmount")]
    public decimal FineAmount { get; set; }
}

/// <summary>
/// Kết quả tổng thể của thao tác quét trả sách tại Kiosk (UC02).
/// </summary>
public class ScanReturnResult
{
    [JsonPropertyName("canConfirm")]
    public bool CanConfirm { get; set; }

    [JsonPropertyName("totalFine")]
    public decimal TotalFine { get; set; }

    [JsonPropertyName("items")]
    public List<ScanReturnItem> Items { get; set; } = new();
}
