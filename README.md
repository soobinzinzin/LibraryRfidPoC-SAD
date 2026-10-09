# LibraryRfidPoC - Hệ Thống Mô Phỏng Mượn/Trả Sách Tự Động Qua RFID (Kiosk)

Dự án PoC (.NET 8 MVC + SQL Server) mô phỏng quy trình mượn sách tự động (UC01) và trả sách tự động (UC02) tại trạm Kiosk RFID thư viện.

---

## 1. Yêu cầu môi trường

- **.NET 8 SDK** (phiên bản 8.0 trở lên).
- **Microsoft SQL Server** (bản LocalDB, Express, hoặc Developer/Enterprise).
- Trình duyệt web hiện đại (Chrome, Edge, Firefox,...).

---

## 2. Hướng dẫn cài đặt và chạy ứng dụng

### Bước 1: Khởi tạo Cơ sở Dữ liệu
Mở SQL Server Management Studio (SSMS), Azure Data Studio hoặc tiện ích dòng lệnh `sqlcmd`, thực thi 2 tập tin theo thứ tự:
1. `Database/schema.sql`: Tạo cơ sở dữ liệu `LibraryRfidDB` và cấu trúc các bảng.
2. `Database/seed.sql`: Nạp dữ liệu mẫu ban đầu.

### Bước 2: Cấu hình Connection String
Mở file `appsettings.json` tại thư mục gốc và cập nhật tên Server SQL của bạn trong chuỗi kết nối:
```json
"ConnectionStrings": {
  "LibraryDb": "Server=<TÊN_SERVER_CỦA_BẠN>;Database=LibraryRfidDB;Trusted_Connection=True;TrustServerCertificate=True"
}
```
*(Ví dụ: `Server=.\\MSSQLSERVER01;Database=LibraryRfidDB;...` hoặc `Server=localhost;...`)*

### Bước 3: Chạy ứng dụng
Mở terminal tại thư mục gốc của project và thực hiện lệnh:
```bash
dotnet restore
dotnet run
```
Truy cập ứng dụng tại địa chỉ hiển thị trên terminal (mặc định: `http://localhost:5000` hoặc cổng được cấu hình).
- **Màn hình Mượn sách (UC01):** `/Kiosk/Borrow`
- **Màn hình Trả sách (UC02):** `/Kiosk/Return`

---

## 3. Dữ liệu mẫu kiểm thử (Dựa theo `Database/seed.sql`)

### Tài khoản Độc giả / Người dùng:
| Mã Thẻ | Họ và Tên | Trạng Thái | Tiền Phạt | Đang Mượn | Mục Đích Kiểm Thử |
|---|---|---|---|---|---|
| **R001** | Nguyễn Văn An | Active | 0đ | 2 cuốn (RFID-004, RFID-005) | Độc giả bình thường, đủ điều kiện mượn tối đa 3 cuốn nữa. |
| **R002** | Trần Thị Bình | Active | 60.000đ | 0 cuốn | Bị khóa mượn do nợ phạt vượt ngưỡng (>= 50.000đ). Đang đặt trước sách `RFID-003`. |
| **R003** | Lê Hoàng Cường | Locked | 0đ | 0 cuốn | Thẻ bị khóa trực tiếp, không được phép mượn sách. |
| **L001** | Phạm Thu Hà | Active | - | - | Tài khoản Thủ thư (Librarian). |

### Bản sao Tài liệu (Mã thẻ RFID):
| Mã RFID | Nhan Đề Sách | Trạng Thái | Còi RFID | Tình Trạng Hiện Tại & Kịch Bản Test |
|---|---|---|---|---|
| **RFID-001** | Phân tích và Thiết kế Hệ thống Thông tin | Available | Bật (1) | Khả dụng, sẵn sàng mượn. |
| **RFID-002** | Lập trình Ứng dụng Web với ASP.NET Core | Available | Bật (1) | Khả dụng, sẵn sàng mượn. |
| **RFID-003** | Ứng dụng Công nghệ RFID trong Quản lý | Available | Bật (1) | Đang được **R002 đặt trước**. Nếu người khác mượn sẽ bị báo lỗi. |
| **RFID-004** | Phân tích và Thiết kế Hệ thống Thông tin | Borrowed | Tắt (0) | **R001 đang mượn, đã quá hạn 6 ngày**. Dùng để test trả sách có phạt (30.000đ). |
| **RFID-005** | Lập trình Ứng dụng Web với ASP.NET Core | Borrowed | Tắt (0) | **R001 đang mượn, còn hạn**. Dùng để test trả sách đúng hạn (phạt 0đ). |
| **RFID-006** | Ứng dụng Công nghệ RFID trong Quản lý | Available | Bật (1) | Khả dụng (đã được trả trước đó). |
| **RFID-007** | Cơ sở dữ liệu nâng cao | Available | Bật (1) | Khả dụng, sẵn sàng mượn. |
