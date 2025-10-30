Tuyệt vời, mình sẽ đưa lại toàn bộ README.md hoàn chỉnh (bản đầy đủ mới nhất), đã nhét luôn phần các package .NET phải cài, cách migrate database, cách chạy Flutter, v.v.

Bạn chỉ cần tạo file `README.md` ở root repo (`scrapapp/README.md`) và dán nguyên nội dung dưới đây vào.

````markdown
# ScrapApp ♻️  
Quản lý thu mua phế liệu / rác thải: khách đặt lịch thu gom, doanh nghiệp điều phối nhân viên, định vị GPS, và đăng/tìm nguồn cung phế liệu.

## 0. Mục tiêu hệ thống

Hệ thống gồm 2 phần chính:

1. **Khách hàng / Người bán phế liệu**
   - Lưu thông tin khách (tên, sđt, địa chỉ)
   - Đặt lịch thu gom: chọn loại phế liệu (giấy, nhựa,...), số kg ước lượng, ghi chú
   - Gửi vị trí GPS (lat/lng)
2. **Doanh nghiệp thu mua**
   - Nhận danh sách yêu cầu thu gom (pickup requests)
   - Nhân viên thu gom (collector) nhận job, cập nhật trạng thái
   - Tự động gán job cho collector gần nhất (dispatch-nearest)
   - Đăng thông tin nguồn cung phế liệu (listing) và tìm kiếm nguồn quanh vị trí

---

## 1. Kiến trúc dự án

Repo này có 2 phần:

```text
scrapapp/
 ├─ scrap_app/           # Flutter app (mobile UI)
 │   ├─ lib/
 │   │   ├─ api.dart                 # gọi REST API backend
 │   │   ├─ models.dart              # model Dart
 │   │   ├─ env.dart                 # baseUrl backend
 │   │   └─ screens/
 │   │        ├─ customer_booking_screen.dart   # Khách đặt lịch thu gom
 │   │        ├─ collector_screen.dart          # Điều phối & trạng thái pickup
 │   │        ├─ management_screen.dart         # Quản lý KH / Công ty / Collector
 │   │        ├─ listings_screen.dart           # Đăng và tìm nguồn cung phế liệu
 │   │        └─ ...
 │   ├─ pubspec.yaml
 │   └─ ...
 └─ backend/
     └─ ScrapApi/
         ├─ Program.cs
         ├─ Data/
         │    ├─ AppDb.cs        # DbContext (EF Core)
         │    └─ SeedData.cs     # Seed dữ liệu mẫu
         ├─ Models/
         │    ├─ Customer.cs
         │    ├─ CollectorCompany.cs
         │    ├─ Collector.cs
         │    ├─ PickupRequest.cs
         │    ├─ ScrapListing.cs
         │    └─ enums (PickupStatus,...)
         ├─ Migrations/          # EF Core migrations
         ├─ appsettings.json
         └─ ScrapApi.csproj
````

---

## 2. Chức năng chính

### 2.1. Quản lý khách hàng

* Tạo khách hàng mới (tên, điện thoại, địa chỉ)
* Cập nhật vị trí gần nhất (lat/lng)
* CRUD khách hàng trong màn `Quản lý`

### 2.2. Đặt lịch thu gom

* Khách chọn loại phế liệu (`Giấy`, `Nhựa`, `Kim loại`, `Điện tử`, `Khác`)
* Ước lượng số kg
* Chọn thời gian thu gom mong muốn
* Gửi tọa độ GPS hiện tại bằng Geolocator
* API tạo `PickupRequest` với trạng thái ban đầu `Pending`

### 2.3. Điều phối thu gom

Bên thu gom dùng màn hình `Bên thu gom`:

* Xem danh sách yêu cầu mới
* Gán job cho 1 collector (`accept`)
* Chuyển trạng thái: `Pending → Accepted → InProgress → Completed`
* Hủy job nếu cần
* Nút **Auto-dispatch** = backend tự tìm collector gần nhất (dựa trên vị trí đã báo cáo)

Collector có thể gửi vị trí GPS hiện tại lên server (`/api/collectors/{id}/location`) để giúp auto-dispatch hoạt động đúng.

### 2.4. Nguồn cung phế liệu

Màn hình `Nguồn cung`:

* Đăng tin bán phế liệu: tiêu đề, mô tả, giá/kg, kèm vị trí hiện tại
* Tìm kiếm listings theo từ khóa và bán kính (km) xung quanh vị trí của mình

### 2.5. Quản trị

Màn hình `Quản lý`:

* Tab Khách hàng (CRUD)
* Tab Doanh nghiệp & Collector:

  * Tạo doanh nghiệp thu gom
  * Thêm collector cho doanh nghiệp
  * Xem danh sách collector thuộc công ty
  * Xóa doanh nghiệp / xóa collector

---

## 3. Chuẩn bị môi trường

### 3.1 Cài Flutter

Yêu cầu:

* Flutter SDK (channel stable)
* Android Studio hoặc VS Code với plugin Flutter/Dart
* Android SDK / emulator hoặc điện thoại Android thật

Các package Flutter đang dùng (xem `pubspec.yaml`):

* `http` (gọi REST API)
* `intl` (format ngày giờ)
* `geolocator` (lấy GPS)
* `material` (Flutter Material 3 UI)

Trước khi chạy lần đầu:

```bash
cd scrap_app
flutter pub get
```

Chỉnh `lib/env.dart` để app biết backend URL:

```dart
class Env {
  static const baseUrl = 'http://10.0.2.2:5000'; 
  // Nếu chạy emulator Android:
  //   10.0.2.2 trỏ về localhost của PC
  // Nếu test bằng điện thoại thật qua WiFi:
  //   đổi thành 'http://<IP-của-PC>:5000'
}
```

Chạy app:

```bash
flutter run
```

---

### 3.2 Backend (ASP.NET Core + SQL Server)

#### 3.2.1 Cần cài:

1. **.NET SDK 8.x**

   * Kiểm tra:

     ```bash
     dotnet --version
     ```

     nên ra kiểu `8.x.x`.

2. **SQL Server**

   * Dùng `(localdb)\MSSQLLocalDB` (có sẵn trên Windows với Visual Studio)
     hoặc
   * Cài SQL Server Express / Developer Edition.
   * Nếu không muốn đặt password user `sa`, bạn có thể xài LocalDB cho dev.

3. **EF Core CLI tool** (để tạo migration / cập nhật DB):

   ```bash
   dotnet tool install --global dotnet-ef
   ```

4. **Các package NuGet cần cho dự án `ScrapApi`**
   (chỉ cần làm nếu clone repo mới và gói chưa restore, hoặc bạn thấy build báo thiếu package)

   ```bash
   cd backend/ScrapApi

   dotnet add package Microsoft.EntityFrameworkCore
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   dotnet add package Microsoft.EntityFrameworkCore.Design
   dotnet add package Microsoft.EntityFrameworkCore.Tools
   dotnet add package Microsoft.Data.SqlClient

   dotnet add package Microsoft.AspNetCore.OpenApi
   dotnet add package Swashbuckle.AspNetCore
   ```

   Ghi chú (tùy thuộc bạn có dùng auth/JWT hay chưa):

   ```bash
   # Chỉ cần nếu bạn bật JWT authentication sau này
   dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
   ```

#### 3.2.2 Connection string

Mở `backend/ScrapApi/appsettings.json` và chỉnh cho khớp SQL bạn dùng:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ScrapApiDb;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
  }
}
```

Ví dụ nếu bạn dùng SQL Server Express với user `sa`:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Data Source=YOURPC\\SQLEXPRESS;Initial Catalog=ScrapApiDb;User ID=sa;Password=YourStrongPassword123;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

`Program.cs` đang gọi:

```csharp
builder.Services.AddDbContext<AppDb>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer") ??
        @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ScrapApiDb;
          Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"));
```

nghĩa là nếu appsettings không có, nó fallback dùng LocalDB mặc định.

#### 3.2.3 Tạo / cập nhật database bằng EF Core

Trong thư mục `backend/ScrapApi`, chạy:

```bash
dotnet build
dotnet ef database update
```

* Lệnh `database update` sẽ đọc các file trong thư mục `Migrations/` và tự tạo bảng:
  Customers, Collectors, CollectorCompanies, PickupRequests, ScrapListings, v.v.
  và có thể chạy `SeedData.InitAsync()` khi app khởi động để thêm dữ liệu mẫu.

Nếu bạn chỉnh model (ví dụ thêm trường `CurrentLat`, `CurrentLng`, `LastSeenAt` vào `Collector`) và chưa có migration, bạn tạo migration mới:

```bash
dotnet ef migrations add AddCollectorLocation
dotnet ef database update
```

> Nếu lệnh `dotnet ef` báo lỗi "No executable found matching command", nghĩa là bạn quên cài tool `dotnet-ef` ở bước trên.

#### 3.2.4 Chạy API

Vẫn trong `backend/ScrapApi`:

```bash
dotnet run
```

Mặc định dev server sẽ:

* mở Swagger UI tại `http://localhost:5000/swagger` (có thể là 5001 nếu HTTPS, tuỳ cấu hình launchSettings.json)
* bật CORS `AllowAnyOrigin` để Flutter gọi API (chỉ dùng khi dev)
* tự migrate + seed dữ liệu mỗi lần start (trong `Program.cs` đoạn `db.Database.MigrateAsync()` và `SeedData.InitAsync(db)`)

---

## 4. Các endpoint chính (tóm tắt)

### Khách hàng

* `GET /api/customers` → danh sách khách
* `POST /api/customers` → tạo khách
* `PUT /api/customers/{id}` → sửa khách
* `DELETE /api/customers/{id}` → xóa khách
* `POST /api/customers/{id}/location?lat=...&lng=...` → cập nhật vị trí cuối

### Thu gom (Pickups)

* `GET /api/pickups?status=...&collectorId=...`
* `POST /api/pickups` → khách tạo yêu cầu thu gom
* `POST /api/pickups/{id}/accept?collectorId=...` → collector nhận job
* `POST /api/pickups/{id}/status?status=...` → đổi trạng thái
* `POST /api/pickups/{id}/dispatch-nearest?jobLat=...&jobLng=...&radiusKm=10&companyId=...`
  → tự động gán collector gần nhất

### Collector

* `POST /api/collectors/{id}/location?lat=...&lng=...`
  → collector cập nhật toạ độ hiện tại
* `GET /api/collectors/{id}/pickups?status=...`
  → xem job của riêng collector đó

### Công ty thu gom & nhân viên

* `GET /api/companies` → company + danh sách collector
* `POST /api/companies` → tạo công ty
* `DELETE /api/companies/{id}` → xóa công ty
* `POST /api/collectors` → thêm collector (thuộc công ty)
* `DELETE /api/collectors/{id}` → xóa collector

### Nguồn cung phế liệu (Listings)

* `POST /api/listings` → đăng bán (tiêu đề, mô tả, giá/kg, lat/lng)
* `GET /api/listings/search?q=nhua&lat=...&lng=...&radiusKm=5`
  → tìm nguồn cung quanh vị trí hiện tại
* `GET /api/listings` → tất cả listing (mới nhất trước)

---

## 5. Dòng chạy tổng quát demo

1. Mở Flutter app → màn hình chính (Grid 4 ô):

   * Khách đặt lịch
   * Bên thu gom
   * Quản lý
   * Nguồn cung

2. Vào **Khách đặt lịch**:

   * Nhập tên/sđt → bấm "Lưu / dùng khách này"
   * Bấm "Lấy vị trí" để lấy GPS (lat/lng)
   * Chọn loại phế liệu + kg + thời gian
   * Bấm "Đặt lịch" → gọi `/api/pickups`

3. Vào **Bên thu gom**:

   * Thấy yêu cầu mới (Pending)
   * Bấm "Auto-dispatch" để hệ thống chọn collector gần nhất
     hoặc "Nhận" để collector đang chọn nhận job
   * Collector sau đó có thể bấm "Bắt đầu" → "Hoàn tất"

4. Vào **Nguồn cung**:

   * Đăng thông tin bán phế liệu
   * Tìm quanh bán kính X km dựa trên GPS

5. Vào **Quản lý**:

   * Tab "Khách hàng": sửa / xóa khách
   * Tab "DN & Collector": thêm công ty thu gom, thêm collector cho công ty

---

## 6. Ghi chú dev

* UI Flutter đang dùng Material 3 với theme trong `lib/ui/app_theme.dart`
* App hiện chạy dạng demo nội bộ, chưa có:

  * đăng nhập, phân quyền
  * upload hình ảnh phế liệu
  * định tuyến bản đồ (chỉ lưu lat/lng thô)
* Backend chưa bật HTTPS bắt buộc, chưa có auth, chưa rate-limit → không để public production như vậy

---

## 7. Kết luận

* **Flutter app** = giao diện nhập liệu, gọi REST.
* **.NET backend** = Minimal API với Entity Framework Core, SQL Server.
* **EF Core** giữ schema và quản lý migration DB.
* **Geolocator** dùng để lấy vị trí thật và gửi lên server.

Bạn có thể đưa README này cho bất kỳ ai clone repo để họ tự dựng môi trường chạy full end-to-end.

```

Bạn cứ copy cả khối trên (từ `# ScrapApp ♻️` tới hết) vào `README.md` trong repo là xong 👍
::contentReference[oaicite:0]{index=0}
```
