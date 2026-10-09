namespace LibraryRfidPoC.Models.Entities;

/// <summary>
/// Thực thể người dùng cơ sở trong hệ thống thư viện.
/// </summary>
public abstract class User
{
    public string UserID { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = string.Empty;
}

