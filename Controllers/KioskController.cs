using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using LibraryRfidPoC.Models;
using LibraryRfidPoC.Services;

namespace LibraryRfidPoC.Controllers;

/// <summary>
/// Controller điều khiển giao diện mô phỏng Kiosk RFID.
/// Đóng vai trò mỏng: tiếp nhận, làm sạch request -> gọi Service -> trả Json kết quả.
/// </summary>
public class KioskController : Controller
{
    private readonly LibraryRulesOptions _rules;
    private readonly BorrowService _borrowService;
    private readonly ReturnService _returnService;

    public KioskController(
        IOptions<LibraryRulesOptions> rulesOptions,
        BorrowService borrowService,
        ReturnService returnService)
    {
        _rules = rulesOptions.Value;
        _borrowService = borrowService;
        _returnService = returnService;
    }

    /// <summary>
    /// Hiển thị màn hình Kiosk mượn sách.
    /// </summary>
    [HttpGet]
    public IActionResult Borrow()
    {
        ViewBag.LoanDays = _rules.LoanDays;
        return View();
    }

    /// <summary>
    /// Hiển thị màn hình Kiosk trả sách.
    /// </summary>
    [HttpGet]
    public IActionResult Return()
    {
        return View();
    }

    /// <summary>
    /// Xử lý quét nhận diện thẻ độc giả và các tài liệu RFID khi mượn sách (UC01).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Scan([FromBody] ScanRequest request)
    {
        var readerId = request?.ReaderId?.Trim() ?? string.Empty;
        var cleanRfids = request?.Rfids?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var result = await _borrowService.ScanBorrow(readerId, cleanRfids);
        return Json(result);
    }

    /// <summary>
    /// Xác nhận mượn sách và tạo các bản ghi giao dịch thật vào CSDL (UC01).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ConfirmBorrow([FromBody] ScanRequest request)
    {
        var readerId = request?.ReaderId?.Trim() ?? string.Empty;
        var cleanRfids = request?.Rfids?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var result = await _borrowService.ProcessBorrowing(readerId, cleanRfids);
        return Json(result);
    }

    /// <summary>
    /// Xử lý quét nhận diện tài liệu RFID khi trả sách tại Kiosk (UC02).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ScanReturn([FromBody] ReturnRequest request)
    {
        var cleanRfids = request?.Rfids?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var result = await _returnService.ScanReturn(cleanRfids);
        return Json(result);
    }

    /// <summary>
    /// Xác nhận trả sách và cập nhật trạng thái, còi RFID, phiếu phạt vào CSDL (UC02).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ConfirmReturn([FromBody] ReturnRequest request)
    {
        var cleanRfids = request?.Rfids?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var result = await _returnService.ProcessReturn(cleanRfids);
        return Json(result);
    }
}
