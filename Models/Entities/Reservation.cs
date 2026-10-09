namespace LibraryRfidPoC.Models.Entities;

/// <summary>
/// Thực thể phiếu đặt trước tài liệu.
/// </summary>
public class Reservation
{
    public string ReservationID { get; set; } = string.Empty;
    public string UserID { get; set; } = string.Empty;
    public string BookID { get; set; } = string.Empty;
    public DateTime ReservationDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

