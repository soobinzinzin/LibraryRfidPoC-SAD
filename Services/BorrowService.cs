using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using LibraryRfidPoC.Data;
using LibraryRfidPoC.Models;
using LibraryRfidPoC.Models.Entities;

namespace LibraryRfidPoC.Services;

/// <summary>
/// Kết quả kiểm tra điều kiện mượn sách của độc giả.
/// </summary>
public class UserConditionResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ReaderName { get; set; } = string.Empty;
    public int CurrentBorrowing { get; set; }
    public int BorrowLimit { get; set; }
    public bool LimitExceeded { get; set; }
}

/// <summary>
/// Thông tin từng cuốn sách trong biên nhận mượn sách.
/// </summary>
public class BorrowLoanItem
{
    [JsonPropertyName("loanID")]
    public string LoanID { get; set; } = string.Empty;

    [JsonPropertyName("rfid")]
    public string Rfid { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("dueDate")]
    public string DueDate { get; set; } = string.Empty;
}

/// <summary>
/// Kết quả xử lý mượn sách toàn diện phía server.
/// </summary>
public class BorrowProcessResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<BorrowLoanItem> Loans { get; set; } = new();
}

/// <summary>
/// Dịch vụ nghiệp vụ mượn sách tại trạm Kiosk RFID (UC01).
/// </summary>
public class BorrowService
{
    private readonly LibraryDbContext _context;
    private readonly LibraryRulesOptions _rules;

    public BorrowService(LibraryDbContext context, IOptions<LibraryRulesOptions> rulesOptions)
    {
        _context = context;
        _rules = rulesOptions.Value;
    }

    /// <summary>
    /// Xử lý quét nhận diện thẻ độc giả và danh sách tài liệu RFID khi mượn sách (UC01):
    /// - Loại bỏ mã trống và mã trùng lặp (không phân biệt hoa thường).
    /// - Kiểm tra điều kiện từng cuốn sách qua CheckBookConditions.
    /// - Kiểm tra điều kiện độc giả qua CheckUserConditions với số sách hợp lệ.
    /// - Tính toán trạng thái readerOk, limitExceeded, canConfirm và trả về kết quả quét.
    /// </summary>
    public async Task<ScanBorrowResult> ScanBorrow(string readerId, List<string> rfids)
    {
        var cleanReaderId = readerId?.Trim() ?? string.Empty;
        var cleanRfids = rfids?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var items = new List<ScanBorrowItem>();
        int validBookCount = 0;

        foreach (var rfid in cleanRfids)
        {
            var (valid, reason, title, author) = await CheckBookConditions(rfid, cleanReaderId);
            if (valid)
            {
                validBookCount++;
            }
            items.Add(new ScanBorrowItem
            {
                Rfid = rfid,
                Valid = valid,
                Reason = reason,
                Title = title,
                Author = author
            });
        }

        // Số cuốn hợp lệ dùng làm newBookCount cho CheckUserConditions
        var userCheck = await CheckUserConditions(cleanReaderId, validBookCount);

        bool readerOk;
        bool limitExceeded;
        string readerMessage;

        if (!userCheck.IsValid)
        {
            if (userCheck.LimitExceeded)
            {
                readerOk = true;
                limitExceeded = true;
                readerMessage = userCheck.Message;
            }
            else
            {
                readerOk = false;
                limitExceeded = false;
                readerMessage = userCheck.Message;
            }
        }
        else
        {
            readerOk = true;
            limitExceeded = false;
            readerMessage = string.Empty;
        }

        bool canConfirm = readerOk && !limitExceeded && validBookCount > 0;

        return new ScanBorrowResult
        {
            ReaderOk = readerOk,
            ReaderMessage = readerMessage,
            ReaderName = userCheck.ReaderName,
            CurrentBorrowing = userCheck.CurrentBorrowing,
            BorrowLimit = userCheck.BorrowLimit,
            LimitExceeded = limitExceeded,
            CanConfirm = canConfirm,
            Items = items
        };
    }

    /// <summary>
    /// Kiểm tra các điều kiện mượn sách của người dùng theo thứ tự:
    /// a) Thẻ tồn tại trong hệ thống.
    /// b) Thẻ có trạng thái hoạt động (Active).
    /// c) Tổng tiền phạt chưa chạm ngưỡng khóa (>= 50.000đ).
    /// d) Không vượt quá hạn mức mượn sách cho phép.
    /// </summary>
    public async Task<UserConditionResult> CheckUserConditions(string userID, int newBookCount)
    {
        if (string.IsNullOrWhiteSpace(userID))
        {
            return new UserConditionResult
            {
                IsValid = false,
                Message = "Không tìm thấy thẻ độc giả",
                ReaderName = string.Empty,
                CurrentBorrowing = 0,
                BorrowLimit = 0,
                LimitExceeded = false
            };
        }

        // a) Không tìm thấy Reader -> lỗi "Không tìm thấy thẻ độc giả"
        var reader = await _context.Readers.FirstOrDefaultAsync(r => r.UserID == userID);
        if (reader == null)
        {
            return new UserConditionResult
            {
                IsValid = false,
                Message = "Không tìm thấy thẻ độc giả",
                ReaderName = string.Empty,
                CurrentBorrowing = 0,
                BorrowLimit = 0,
                LimitExceeded = false
            };
        }

        int limit = reader.BorrowLimit > 0 ? reader.BorrowLimit : _rules.MaxBooksPerLoan;

        // b) accountStatus khác "Active" -> lỗi "Thẻ thư viện không hợp lệ hoặc đang bị khóa"
        if (!string.Equals(reader.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return new UserConditionResult
            {
                IsValid = false,
                Message = "Thẻ thư viện không hợp lệ hoặc đang bị khóa",
                ReaderName = reader.FullName,
                CurrentBorrowing = 0,
                BorrowLimit = limit,
                LimitExceeded = false
            };
        }

        int currentBorrowing = await _context.Loans.CountAsync(l => l.UserID == userID && l.Status == "Borrowed");

        // c) TotalFines >= FineLockThreshold (cấu hình) -> lỗi "Tài khoản bị khóa do nợ phạt quá hạn mức (>= 50.000đ). Vui lòng thanh toán tiền phạt để mở khóa"
        if (reader.TotalFines >= _rules.FineLockThreshold)
        {
            return new UserConditionResult
            {
                IsValid = false,
                Message = "Tài khoản bị khóa do nợ phạt quá hạn mức (>= 50.000đ). Vui lòng thanh toán tiền phạt để mở khóa",
                ReaderName = reader.FullName,
                CurrentBorrowing = currentBorrowing,
                BorrowLimit = limit,
                LimitExceeded = false
            };
        }

        // d) Số Loan có Status "Borrowed" của reader + newBookCount > hạn mức -> lỗi "Vượt quá hạn mức mượn sách (X cuốn). Vui lòng bớt sách khỏi khay quét"
        if (currentBorrowing + newBookCount > limit)
        {
            return new UserConditionResult
            {
                IsValid = false,
                Message = $"Vượt quá hạn mức mượn sách ({limit} cuốn). Vui lòng bớt sách khỏi khay quét",
                ReaderName = reader.FullName,
                CurrentBorrowing = currentBorrowing,
                BorrowLimit = limit,
                LimitExceeded = true
            };
        }

        return new UserConditionResult
        {
            IsValid = true,
            Message = string.Empty,
            ReaderName = reader.FullName,
            CurrentBorrowing = currentBorrowing,
            BorrowLimit = limit,
            LimitExceeded = false
        };
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của cuốn sách:
    /// - Không tìm thấy mã -> "Không tìm thấy mã RFID"
    /// - BookItem.Status khác "Available" -> "Tài liệu không khả dụng"
    /// - Có Reservation Status "Active", ExpirationDate > DateTime.Now, UserID khác userID -> "Tài liệu đã được độc giả khác đặt trước"
    /// </summary>
    public async Task<(bool valid, string reason, string title, string author)> CheckBookConditions(string bookID, string userID)
    {
        if (string.IsNullOrWhiteSpace(bookID))
        {
            return (false, "Không tìm thấy mã RFID", string.Empty, string.Empty);
        }

        var item = await _context.BookItems.FirstOrDefaultAsync(bi => bi.BookID == bookID);
        if (item == null)
        {
            return (false, "Không tìm thấy mã RFID", string.Empty, string.Empty);
        }

        // Lấy tên sách và tác giả từ bảng Books qua Isbn
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Isbn == item.Isbn);
        string title = book?.Title ?? string.Empty;
        string author = book?.Author ?? string.Empty;

        // BookItem.Status khác "Available" -> "Tài liệu không khả dụng"
        if (!string.Equals(item.Status, "Available", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Tài liệu không khả dụng", title, author);
        }

        // Kiểm tra xem tài liệu có đang bị độc giả khác đặt trước không
        bool isReservedByOther = await CheckReservation(bookID, userID);
        if (isReservedByOther)
        {
            return (false, "Tài liệu đã được độc giả khác đặt trước", title, author);
        }

        return (true, string.Empty, title, author);
    }

    /// <summary>
    /// Kiểm tra sách có đang bị người KHÁC đặt trước không:
    /// Reservation Status "Active", ExpirationDate > DateTime.Now, UserID khác userID.
    /// </summary>
    public async Task<bool> CheckReservation(string bookID, string userID)
    {
        return await _context.Reservations.AnyAsync(r =>
            r.BookID == bookID &&
            r.Status == "Active" &&
            r.ExpirationDate > DateTime.Now &&
            r.UserID != userID);
    }

    /// <summary>
    /// Thêm giao dịch mượn trong MỘT lần SaveChanges:
    /// - Tạo Loan mới (LoanID LN00x tiếp nối, BorrowDate = Now, DueDate = Now + LoanDays, Status = "Borrowed").
    /// - Gọi DisableRFIDSecurity cho từng sách.
    /// - Đổi Reservation của chính userID thành "Fulfilled" (nếu có).
    /// </summary>
    public async Task<List<Loan>> InsertBorrowTransaction(string userID, List<string> bookIDs)
    {
        var createdLoans = new List<Loan>();
        if (bookIDs == null || bookIDs.Count == 0)
        {
            return createdLoans;
        }

        // Tìm số thứ tự lớn nhất của LoanID hiện có trong CSDL (ví dụ LN003 -> maxNum = 3)
        var existingLoanIds = await _context.Loans.Select(l => l.LoanID).ToListAsync();
        int maxNum = 0;
        foreach (var id in existingLoanIds)
        {
            if (id.StartsWith("LN") && int.TryParse(id[2..], out int num))
            {
                if (num > maxNum)
                {
                    maxNum = num;
                }
            }
        }

        var now = DateTime.Now;
        var dueDate = now.AddDays(_rules.LoanDays);

        foreach (var bookId in bookIDs)
        {
            maxNum++;
            string newLoanId = $"LN{maxNum:D3}";

            var loan = new Loan
            {
                LoanID = newLoanId,
                UserID = userID,
                BookID = bookId,
                BorrowDate = now,
                DueDate = dueDate,
                ReturnDate = null,
                Status = "Borrowed"
            };

            _context.Loans.Add(loan);
            createdLoans.Add(loan);

            // Cập nhật trạng thái sách và tắt còi bảo mật RFID
            await DisableRFIDSecurity(bookId);

            // Nếu chính userID có Reservation Active cho sách đó thì đổi thành "Fulfilled"
            var myReservation = await _context.Reservations.FirstOrDefaultAsync(r =>
                r.BookID == bookId &&
                r.UserID == userID &&
                r.Status == "Active");

            if (myReservation != null)
            {
                myReservation.Status = "Fulfilled";
            }
        }

        // Lưu toàn bộ vào CSDL trong MỘT lần SaveChanges
        await _context.SaveChangesAsync();

        return createdLoans;
    }

    /// <summary>
    /// Tắt còi bảo mật RFID của tài liệu và chuyển trạng thái sang "Borrowed".
    /// </summary>
    public async Task DisableRFIDSecurity(string bookID)
    {
        var item = await _context.BookItems.FirstOrDefaultAsync(bi => bi.BookID == bookID);
        if (item != null)
        {
            item.Status = "Borrowed";
            item.IsSecurityEnabled = false;
        }
    }

    /// <summary>
    /// Xử lý mượn sách toàn diện phía server:
    /// - Kiểm tra điều kiện độc giả và từng cuốn sách.
    /// - Chỉ mượn các cuốn hợp lệ.
    /// - Gọi InsertBorrowTransaction và trả kết quả biên nhận.
    /// </summary>
    public async Task<BorrowProcessResult> ProcessBorrowing(string userID, List<string> bookIDs)
    {
        var cleanBookIds = bookIDs?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct()
            .ToList() ?? new List<string>();

        if (cleanBookIds.Count == 0)
        {
            return new BorrowProcessResult
            {
                Success = false,
                Message = "Danh sách mã RFID trống.",
                Loans = new List<BorrowLoanItem>()
            };
        }

        // 1. Kiểm tra từng cuốn sách để lọc danh sách hợp lệ
        var validBookIds = new List<string>();
        var bookTitles = new Dictionary<string, string>();

        foreach (var bookId in cleanBookIds)
        {
            var (valid, reason, title, author) = await CheckBookConditions(bookId, userID);
            if (valid)
            {
                validBookIds.Add(bookId);
                bookTitles[bookId] = title;
            }
        }

        if (validBookIds.Count == 0)
        {
            return new BorrowProcessResult
            {
                Success = false,
                Message = "Không có tài liệu nào đủ điều kiện để mượn.",
                Loans = new List<BorrowLoanItem>()
            };
        }

        // 2. Kiểm tra điều kiện độc giả phía server
        var userCheck = await CheckUserConditions(userID, validBookIds.Count);
        if (!userCheck.IsValid || userCheck.LimitExceeded)
        {
            return new BorrowProcessResult
            {
                Success = false,
                Message = userCheck.Message,
                Loans = new List<BorrowLoanItem>()
            };
        }

        // 3. Thực hiện giao dịch mượn
        var createdLoans = await InsertBorrowTransaction(userID, validBookIds);

        // 4. Tạo biên nhận cho độc giả
        var receiptLoans = createdLoans.Select(l => new BorrowLoanItem
        {
            LoanID = l.LoanID,
            Rfid = l.BookID,
            Title = bookTitles.ContainsKey(l.BookID) ? bookTitles[l.BookID] : l.BookID,
            DueDate = l.DueDate.ToString("dd/MM/yyyy")
        }).ToList();

        return new BorrowProcessResult
        {
            Success = true,
            Message = $"Mượn thành công {receiptLoans.Count} tài liệu.",
            Loans = receiptLoans
        };
    }
}
