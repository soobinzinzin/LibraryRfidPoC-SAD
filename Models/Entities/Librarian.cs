namespace LibraryRfidPoC.Models.Entities;

/// <summary>
/// Thực thể thủ thư (kế thừa User theo mô hình TPT).
/// </summary>
public class Librarian : User
{
    public string EmployeeID { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string RoleLevel { get; set; } = string.Empty;
}

