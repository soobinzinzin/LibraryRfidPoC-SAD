# TÀI LIỆU BÀN GIAO KẾT QUẢ HIỆN THỰC POC CHO A

Tài liệu này tổng hợp toàn bộ kết quả triển khai mã nguồn thực nghiệm (Proof of Concept - PoC) của đồ án Hệ thống Quản lý Thư viện thông minh tích hợp RFID. Tài liệu chỉ ra các ánh xạ cụ thể giữa thiết kế và mã nguồn, các điểm thống nhất thay đổi, kết quả kiểm thử và hướng dẫn vận hành.

---

## Mục 1. Tóm tắt PoC

### 1.1. Phạm vi đã hoàn thành
- **UC01 - Mượn sách tại Kiosk:** Độc giả tự thực hiện quét mã thẻ thư viện và đặt danh sách sách gắn chip RFID lên khay. Hệ thống kiểm tra hợp lệ tài khoản độc giả (tồn tại, trạng thái Active, nợ phạt < 50.000đ, hạn mức mượn) cùng điều kiện từng cuốn sách (trạng thái Available, không bị người khác đặt trước). Khi xác nhận mượn, hệ thống tạo các bản ghi phiếu mượn mới, chuyển sách sang trạng thái `Borrowed` và tự động tắt còi an ninh RFID (`isSecurityEnabled = 0`).
- **UC02 - Trả sách tại quầy:** Thủ thư thao tác tiếp nhận tài liệu do độc giả đưa tại quầy. Hệ thống quét danh sách mã RFID, kiểm tra phiếu mượn đang lưu hành, tính số ngày quá hạn và tiền phạt phát sinh (5.000đ/ngày). Khi xác nhận trả, hệ thống cập nhật ngày trả phiếu mượn, tạo bản ghi phạt nếu có trễ hạn, cộng dồn nợ vào độc giả, kích hoạt lại còi an ninh RFID (`isSecurityEnabled = 1`), chuyển sách sang `Available` hoặc chuyển sang `Reserved` nếu có người đặt trước.

### 1.2. Phạm vi chưa thực hiện (nằm ngoài phạm vi PoC)
- UC03: Đặt trước sách (Reservation).
- UC04: Xử lý tiền phạt và thanh toán qua cổng VNPay.
- UC05: Quản lý thẻ độc giả.
- UC06: Tìm kiếm tài liệu.
- SSO: Đăng nhập một lần với hệ thống xác thực của trường.

### 1.3. Công nghệ sử dụng
- **Nền tảng:** ASP.NET Core MVC trên .NET 8.
- **ORM:** Entity Framework Core 8 (ánh xạ lớp vào bảng đã tạo bằng script schema.sql, không dùng migration).
- **Hệ quản trị CSDL:** Microsoft SQL Server. Tên CSDL: `LibraryRfidDB`.

---

## Mục 2. Ánh xạ thiết kế sang code

Bảng đối chiếu chi tiết giữa từng thao tác nghiệp vụ trong thiết kế và phương thức hiện thực thực tế trong mã nguồn:

| Thao tác trong thiết kế | Hiện thực trong code |
| :--- | :--- |
| Hiển thị màn hình Kiosk mượn sách | `Controllers/KioskController.cs` -> `Borrow()` |
| Nhận diện thẻ độc giả và quét mã RFID mượn | `Controllers/KioskController.cs` -> `Scan(ScanRequest request)` |
| Điều phối logic quét mượn sách phía server | `Services/BorrowService.cs` -> `ScanBorrow(string readerId, List<string> rfids)` |
| Kiểm tra tài khoản, nợ phạt, hạn mức của độc giả | `Services/BorrowService.cs` -> `CheckUserConditions(string userID, int newBookCount)` |
| Kiểm tra tình trạng sẵn sàng và đặt trước của sách | `Services/BorrowService.cs` -> `CheckBookConditions(string bookID, string userID)` |
| Kiểm tra sách có bị độc giả khác đặt trước | `Services/BorrowService.cs` -> `CheckReservation(string bookID, string userID)` |
| Xác nhận mượn sách và sinh giao dịch | `Controllers/KioskController.cs` -> `ConfirmBorrow(ScanRequest request)` |
| Điều phối quy trình mượn sách toàn diện | `Services/BorrowService.cs` -> `ProcessBorrowing(string userID, List<string> bookIDs)` |
| Tạo phiếu mượn `Loan` mới trong CSDL | `Services/BorrowService.cs` -> `InsertBorrowTransaction(string userID, List<string> bookIDs)` |
| Tắt còi bảo mật RFID (`isSecurityEnabled = false`) | `Services/BorrowService.cs` -> `DisableRFIDSecurity(string bookID)` |
| Hiển thị màn hình Thủ thư trả sách | `Controllers/KioskController.cs` -> `Return()` |
| Tiếp nhận danh sách mã RFID quét trả tại quầy | `Controllers/KioskController.cs` -> `ScanReturn(ReturnRequest request)` |
| Điều phối logic nhận diện và kiểm tra sách trả | `Services/ReturnService.cs` -> `ScanReturn(List<string> rfids)` |
| Tìm phiếu mượn đang hoạt động của cuốn sách | `Services/ReturnService.cs` -> `GetActiveLoan(string bookID)` |
| Tính toán số ngày trễ hạn của phiếu mượn | `Models/Entities/Loan.cs` -> `CheckOverdue(DateTime currentDate)` |
| Gọi tính số ngày trễ từ entity phiếu mượn | `Services/ReturnService.cs` -> `CheckOverdue(Loan loan, DateTime currentDate)` |
| Tính toán số tiền phạt theo công thức quá hạn | `Models/Entities/Fine.cs` -> `CalculateAmount(int daysOverdue, int finePerDay)` |
| Gọi tính số tiền phạt từ entity khoản phạt | `Services/ReturnService.cs` -> `CalculateFine(int daysOverdue)` |
| Khởi tạo bản ghi phiếu phạt `Fine` mới | `Services/ReturnService.cs` -> `CreateFine(Loan loan, int daysOverdue)` |
| Cập nhật ngày trả và trạng thái phiếu mượn | `Services/ReturnService.cs` -> `UpdateLoanReturn(Loan loan, DateTime returnDate)` |
| Bật còi bảo mật RFID và gán trạng thái mới | `Services/ReturnService.cs` -> `EnableRFIDSecurity(string bookID)` |
| Xác nhận trả sách và cập nhật CSDL | `Controllers/KioskController.cs` -> `ConfirmReturn(ReturnRequest request)` |
| Điều phối toàn bộ quy trình xử lý trả sách | `Services/ReturnService.cs` -> `ProcessReturn(List<string> rfids)` |

---

## Mục 3. Các điểm KHÁC so với thiết kế hiện tại

Dưới đây là các điểm sai khác kỹ thuật đã được thống nhất trong quá trình hiện thực mã nguồn:

- Actor UC02 là Thủ thư tại quầy (đã thống nhất).
- Loan.CheckOverdue(currentDate) và Fine.CalculateAmount(daysOverdue, finePerDay) nằm ở domain; ReturnService chỉ điều phối.
- Kiểm tra điều kiện mượn nằm trong BorrowService vì cần truy vấn CSDL.
- Nhánh Reserved khi trả sách và bật lại isSecurityEnabled.
- Fine có loanID (FK).
- Tiền dùng DECIMAL(18,0), không dùng float.
- Bảng Users dùng số nhiều để tránh từ khóa SQL.
- Mã RFID là BookItem.bookID.
- Trả nhiều mã cùng lúc: xử lý từng mã trong cùng một SaveChanges.

---

## Mục 4. Thay đổi A cần cập nhật trong sơ đồ

A cần thực hiện các điều chỉnh sau trên các sơ đồ thiết kế UML để khớp hoàn toàn với code đã chạy:

- Sequence "Trả sách": thêm nhánh alt cho Reserved; thêm bước bật lại còi bảo mật; ghi chú lặp cho nhiều mã RFID.
- Class Diagram: thêm CheckOverdue và CalculateAmount; bỏ calculateFine khỏi ReturnController.
- Use Case Diagram và Spec: đánh số UC01–UC06 theo đúng danh sách đã chốt.
- Bổ sung quan hệ khóa ngoại giữa `Fine` và `Loan` (`Fine.loanID` trỏ đến `Loan.loanID`).
- Cập nhật kiểu dữ liệu thuộc tính tiền tệ trong Class Diagram thành `decimal` thay vì `float` hoặc `double`.
- Cập nhật tên thực thể người dùng thành `Users` hoặc ghi rõ ánh xạ bảng CSDL để tránh xung đột từ khóa hệ thống.

---

## Mục 5. Kết quả kiểm thử

| Mã | Thẻ và mã RFID nhập | Kết quả mong đợi | Kết quả | Ảnh |
| :---: | :--- | :--- | :---: | :--- |
| TC-M01 | R001, RFID-001 và RFID-002 | Mượn thành công, tạo 2 phiếu | Đạt | test_muon_thanh_cong_bien_nhan.png |
| TC-M02 | R002, RFID-001 | Từ chối: nợ phạt từ 50.000đ | Đạt | Chưa có ảnh trong Docs/Images |
| TC-M03 | R003, RFID-001 | Từ chối: thẻ bị khóa | Đạt | test_the_bi_khoa.png |
| TC-M04 | R999, RFID-001 | Báo không tìm thấy thẻ | Đạt | Chưa có ảnh trong Docs/Images |
| TC-M05 | R001, 4 cuốn kèm 2 cuốn đang mượn | Báo vượt hạn mức 5 cuốn | Đạt | Chưa có ảnh trong Docs/Images |
| TC-M06 | R001, RFID-003 | Báo đã được độc giả khác đặt trước | Đạt | test_sach_da_dat_truoc.png |
| TC-M07 | R001, RFID-004 | Báo không khả dụng (đang mượn) | Đạt | Chưa có ảnh trong Docs/Images |
| TC-M08 | R001, RFID-999 | Báo không tìm thấy mã RFID | Đạt | Chưa có ảnh trong Docs/Images |
| TC-R01 | Trả RFID-004 và RFID-005 | RFID-004 phạt 30.000đ, RFID-005 đúng hạn, tạo Fine gắn LN001 | Đạt | test_tra_sach_bien_nhan_va_phat.png |
| TC-R02 | RFID-001 | Báo không ở trạng thái đang mượn | Đạt | test_tra_sach_chua_muon.png |
| TC-R03 | RFID-999 | Báo không tìm thấy mã RFID | Đạt | test_tra_sach_ma_khong_ton_tai.png |
| TC-R04 | RFID-005 có Reservation còn hiệu lực | Trạng thái Reserved, hiện cảnh báo ngăn giữ riêng | Đạt | test_tra_sach_dat_truoc.png |

- Kết quả xác thực CSDL sau giao dịch: ket_qua_select_csdl.png.
- Các mục ghi "Chưa có ảnh" là các kịch bản đã kiểm tra trực tiếp trên giao diện, chưa lưu ảnh trong thư mục Docs/Images.

---

## Mục 6. Cách chạy

Thực hiện 5 bước sau để khởi chạy và kiểm thử hệ thống:

1. Chạy cấu trúc bảng: `sqlcmd -S .\MSSQLSERVER01 -E -f 65001 -i Database\schema.sql`
2. Nạp dữ liệu mẫu ban đầu: `sqlcmd -S .\MSSQLSERVER01 -E -f 65001 -i Database\seed.sql`
3. Khởi chạy ứng dụng: `dotnet run`
4. Mở giao diện mượn sách Kiosk: `http://localhost:5000/Kiosk/Borrow`
5. Mở giao diện quầy trả sách thủ thư: `http://localhost:5000/Kiosk/Return`

---

## Mục 7. Việc còn tồn đọng

Các hạn chế kỹ thuật hiện tại cần được hoàn thiện ở các giai đoạn tiếp theo của đề tài:

- **Mô phỏng phần cứng RFID:** Danh sách mã RFID hiện được nhập giả lập qua ô văn bản thay vì nhận tín hiệu trực tiếp từ đầu đọc phần cứng qua cổng COM hoặc giao thức WebSocket.
- **Thanh toán trực tuyến:** Chưa tích hợp cổng thanh toán VNPay để xử lý nộp phạt trực tiếp. Độc giả hiện chỉ nhận cảnh báo tồn đọng nợ phạt.
- **Dịch vụ thông báo:** Chưa tích hợp máy chủ gửi email tự động cho độc giả khi tài liệu đặt trước được trả về kho hoặc khi phát sinh nợ phạt.
- **Kiểm soát truy cập đồng thời:** Chưa cấu hình cơ chế khóa lạc quan (Optimistic Concurrency Control) khi nhiều máy Kiosk quét trùng cùng một cuốn sách tại cùng thời điểm.
