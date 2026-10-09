namespace LibraryRfidPoC.Models;

/// <summary>
/// DTO chứa danh sách mã RFID gửi lên khi quét hoặc xác nhận trả sách tại Kiosk.
/// </summary>
public class ReturnRequest
{
    public List<string> Rfids { get; set; } = new();
}
