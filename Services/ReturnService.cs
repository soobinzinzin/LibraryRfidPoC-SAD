using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using LibraryRfidPoC.Data;
using LibraryRfidPoC.Models;
using LibraryRfidPoC.Models.Entities;

namespace LibraryRfidPoC.Services;

/// <summary>
/// Thông tin chi tiết một cuốn sách vừa được trả thành công.
/// </summary>
public class ReturnItemResult
{
    [JsonPropertyName("loanID")]
    public string LoanID { get; set; } = string.Empty;

    [JsonPropertyName("rfid")]
    public string Rfid { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("daysOverdue")]
    public int DaysOverdue { get; set; }

    [JsonPropertyName("fineAmount")]
    public decimal FineAmount { get; set; }

    [JsonPropertyName("newStatus")]
    public string NewStatus { get; set; } = string.Empty;
}

/// <summary>
/// Kết quả xử lý trả sách toàn diện phía server.
/// </summary>
public class ReturnProcessResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("totalFine")]
    public decimal TotalFine { get; set; }

    [JsonPropertyName("returns")]
    public List<ReturnItemResult> Returns { get; set; } = new();
}

/// <summary>
/// Dịch vụ nghiệp vụ trả sách tại trạm Kiosk RFID (UC02).
/// </summary>
public class ReturnService
{
    private readonly LibraryDbContext _context;
    private readonly LibraryRulesOptions _rules;

    public ReturnService(LibraryDbContext context, IOptions<LibraryRulesOptions> rulesOptions)
    {
        _context = context;
        _rules = rulesOptions.Value;
    }

    /// <summary>
    /// Xử lý quét nhận diện tài liệu RFID khi trả sách tại Kiosk (UC02):
    /// - Loại bỏ mã trống và mã trùng lặp (không phân biệt hoa thường).
    /// - Kiểm tra tồn tại mã RFID và phiếu mượn tương ứng (Status = Borrowed).
    /// - Tính số ngày trễ hạn và số tiền phạt nếu có.
    /// - Trả về canConfirm (khi có ít nhất 1 sách hợp lệ), tổng tiền phạt và danh sách items.
    /// </summary>
    public async Task<ScanReturnResult> ScanReturn(List<string> rfids)
    {
        var cleanRfids = rfids?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var items = new List<ScanReturnItem>();
        decimal totalFine = 0m;
        int validCount = 0;
        var now = DateTime.Now;

        foreach (var rfid in cleanRfids)
        {
            var bookItem = await _context.BookItems.FirstOrDefaultAsync(b => b.BookID == rfid);
            if (bookItem == null)
            {
                items.Add(new ScanReturnItem
                {
                    Rfid = rfid,
                    Valid = false,
                    Reason = "Không tìm thấy mã RFID",
                    Title = string.Empty,
                    Borrower = string.Empty,
                    DueDate = string.Empty,
                    DaysOverdue = 0,
                    FineAmount = 0m
                });
                continue;
            }

            var loan = await GetActiveLoan(rfid);
            if (loan == null)
            {
                var book = await _context.Books.FirstOrDefaultAsync(b => b.Isbn == bookItem.Isbn);
                items.Add(new ScanReturnItem
                {
                    Rfid = rfid,
                    Valid = false,
                    Reason = "Tài liệu không ở trạng thái đang mượn",
                    Title = book?.Title ?? string.Empty,
                    Borrower = string.Empty,
                    DueDate = string.Empty,
                    DaysOverdue = 0,
                    FineAmount = 0m
                });
                continue;
            }

            validCount++;
            var bookObj = await _context.Books.FirstOrDefaultAsync(b => b.Isbn == bookItem.Isbn);
            string title = bookObj?.Title ?? string.Empty;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == loan.UserID);
            string borrower = user?.FullName ?? loan.UserID;

            int daysOverdue = CheckOverdue(loan, now);
            decimal fineAmount = CalculateFine(daysOverdue);
            totalFine += fineAmount;

            items.Add(new ScanReturnItem
            {
                Rfid = rfid,
                Valid = true,
                Reason = string.Empty,
                Title = title,
                Borrower = borrower,
                DueDate = loan.DueDate.ToString("dd/MM/yyyy"),
                DaysOverdue = daysOverdue,
                FineAmount = fineAmount
            });
        }

        bool canConfirm = validCount >= 1;

        return new ScanReturnResult
        {
            CanConfirm = canConfirm,
            TotalFine = totalFine,
            Items = items
        };
    }

    /// <summary>
    /// Lấy phiếu mượn đang hoạt động của cuốn sách (Status = "Borrowed").
    /// </summary>
    public async Task<Loan?> GetActiveLoan(string bookID)
    {
        if (string.IsNullOrWhiteSpace(bookID))
        {
            return null;
        }

        return await _context.Loans.FirstOrDefaultAsync(l => l.BookID == bookID && l.Status == "Borrowed");
    }

    /// <summary>
    /// Kiểm tra số ngày quá hạn (nguyên tắc Information Expert - ủy quyền cho Loan).
    /// </summary>
    public int CheckOverdue(Loan loan, DateTime currentDate) => loan.CheckOverdue(currentDate);

    /// <summary>
    /// Tính tiền phạt quá hạn (nguyên tắc Information Expert - ủy quyền cho Fine).
    /// </summary>
    public decimal CalculateFine(int daysOverdue) => Fine.CalculateAmount(daysOverdue, _rules.FinePerDay);

    /// <summary>
    /// Tạo phiếu phạt mới (chưa SaveChanges), cộng tiền phạt vào độc giả.
    /// FineID có định dạng Fxxx (ví dụ F002, tính từ số lớn nhất hiện có).
    /// </summary>
    public async Task<Fine> CreateFine(Loan loan, int daysOverdue)
    {
        decimal amount = CalculateFine(daysOverdue);

        // Tìm số thứ tự lớn nhất của FineID hiện có trong CSDL và trong local ChangeTracker
        var dbIds = await _context.Fines.Select(f => f.FineID).ToListAsync();
        var localIds = _context.Fines.Local.Select(f => f.FineID);
        var allIds = dbIds.Concat(localIds).Distinct();

        int maxNum = 0;
        foreach (var id in allIds)
        {
            if (id.StartsWith("F", StringComparison.OrdinalIgnoreCase) && int.TryParse(id[1..], out int num))
            {
                if (num > maxNum)
                {
                    maxNum = num;
                }
            }
        }

        string fineId = $"F{maxNum + 1:D3}";

        var fine = new Fine
        {
            FineID = fineId,
            UserID = loan.UserID,
            BookID = loan.BookID,
            LoanID = loan.LoanID,
            Amount = amount,
            Reason = $"Trả trễ hạn {daysOverdue} ngày",
            Status = "Unpaid",
            CreatedAt = DateTime.Now
        };

        _context.Fines.Add(fine);

        // Đồng thời cộng Amount vào Reader.TotalFines của người mượn
        var reader = await _context.Readers.FirstOrDefaultAsync(r => r.UserID == loan.UserID);
        if (reader != null)
        {
            reader.TotalFines += amount;
        }

        return fine;
    }

    /// <summary>
    /// Cập nhật trạng thái phiếu mượn. Nếu là "Returned" thì gán ngày trả là thời điểm hiện tại.
    /// </summary>
    public void UpdateLoanStatus(Loan loan, string status)
    {
        loan.Status = status;
        if (string.Equals(status, "Returned", StringComparison.OrdinalIgnoreCase))
        {
            loan.ReturnDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Cập nhật trạng thái của cuốn sách vật lý (BookItem).
    /// </summary>
    public async Task UpdateStatus(string bookID, string status)
    {
        var bookItem = await _context.BookItems.FirstOrDefaultAsync(b => b.BookID == bookID);
        if (bookItem != null)
        {
            bookItem.Status = status;
        }
    }

    /// <summary>
    /// Xử lý trả sách hoàn chỉnh cho danh sách mã RFID trong MỘT lần SaveChanges:
    /// - Cập nhật trạng thái Loan thành Returned.
    /// - Tính phạt và tạo Fine nếu trễ hạn.
    /// - Kích hoạt lại còi bảo mật RFID (IsSecurityEnabled = true).
    /// - Cập nhật trạng thái sách là "Reserved" (nếu có đặt trước) hoặc "Available".
    /// </summary>
    public async Task<ReturnProcessResult> ProcessReturn(List<string> bookIDs)
    {
        var cleanRfids = bookIDs?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var returnItems = new List<ReturnItemResult>();
        decimal totalFine = 0m;
        var now = DateTime.Now;

        foreach (var rfid in cleanRfids)
        {
            var loan = await GetActiveLoan(rfid);
            if (loan == null)
            {
                continue; // Bỏ qua nếu không có phiếu mượn đang active
            }

            // 1. Cập nhật phiếu mượn sang Returned
            UpdateLoanStatus(loan, "Returned");

            // 2. Kiểm tra quá hạn
            int daysOverdue = CheckOverdue(loan, now);
            decimal fineAmount = 0m;

            // 3. Tạo phiếu phạt nếu trả trễ hạn
            if (daysOverdue > 0)
            {
                var fine = await CreateFine(loan, daysOverdue);
                fineAmount = fine.Amount;
                totalFine += fineAmount;
            }

            // 4. Kích hoạt còi bảo mật RFID
            var bookItem = await _context.BookItems.FirstOrDefaultAsync(b => b.BookID == rfid);
            if (bookItem != null)
            {
                bookItem.IsSecurityEnabled = true;
            }

            // 5. Kiểm tra đặt trước: nếu có độc giả khác đặt trước thì thành "Reserved", ngược lại "Available"
            bool isReserved = await _context.Reservations.AnyAsync(r =>
                r.BookID == rfid &&
                r.Status == "Active" &&
                r.ExpirationDate > now);

            string newStatus = isReserved ? "Reserved" : "Available";
            await UpdateStatus(rfid, newStatus);

            // 6. Lấy tên sách
            string title = rfid;
            if (bookItem != null)
            {
                var book = await _context.Books.FirstOrDefaultAsync(b => b.Isbn == bookItem.Isbn);
                if (book != null && !string.IsNullOrWhiteSpace(book.Title))
                {
                    title = book.Title;
                }
            }

            returnItems.Add(new ReturnItemResult
            {
                LoanID = loan.LoanID,
                Rfid = rfid,
                Title = title,
                DaysOverdue = daysOverdue,
                FineAmount = fineAmount,
                NewStatus = newStatus
            });
        }

        if (returnItems.Count == 0)
        {
            return new ReturnProcessResult
            {
                Success = false,
                Message = "Không có tài liệu nào đủ điều kiện để trả.",
                TotalFine = 0m,
                Returns = new List<ReturnItemResult>()
            };
        }

        // Lưu toàn bộ thay đổi trong MỘT lần SaveChanges duy nhất
        await _context.SaveChangesAsync();

        return new ReturnProcessResult
        {
            Success = true,
            Message = $"Trả thành công {returnItems.Count} tài liệu.",
            TotalFine = totalFine,
            Returns = returnItems
        };
    }
}
