namespace LibraryRfidPoC.Models;

/// <summary>
/// DTO chứa thông tin quét từ Kiosk RFID.
/// </summary>
public class ScanRequest
{
    public string ReaderId { get; set; } = string.Empty;
    public List<string> Rfids { get; set; } = new();
}
