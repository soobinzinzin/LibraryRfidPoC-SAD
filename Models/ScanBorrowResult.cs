using System.Text.Json.Serialization;

namespace LibraryRfidPoC.Models;

/// <summary>
/// Kết quả nhận diện từng tài liệu RFID khi quét mượn sách.
/// </summary>
public class ScanBorrowItem
{
    [JsonPropertyName("rfid")]
    public string Rfid { get; set; } = string.Empty;

    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;
}

/// <summary>
/// Kết quả tổng thể của thao tác quét mượn sách tại Kiosk (UC01).
/// </summary>
public class ScanBorrowResult
{
    [JsonPropertyName("readerOk")]
    public bool ReaderOk { get; set; }

    [JsonPropertyName("readerMessage")]
    public string ReaderMessage { get; set; } = string.Empty;

    [JsonPropertyName("readerName")]
    public string ReaderName { get; set; } = string.Empty;

    [JsonPropertyName("currentBorrowing")]
    public int CurrentBorrowing { get; set; }

    [JsonPropertyName("borrowLimit")]
    public int BorrowLimit { get; set; }

    [JsonPropertyName("limitExceeded")]
    public bool LimitExceeded { get; set; }

    [JsonPropertyName("canConfirm")]
    public bool CanConfirm { get; set; }

    [JsonPropertyName("items")]
    public List<ScanBorrowItem> Items { get; set; } = new();
}
