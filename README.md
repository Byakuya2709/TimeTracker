# Time Tracker App

Time Tracker là một ứng dụng Desktop (WPF .NET 10) giúp bạn đo lường, phân tích và tối ưu hóa thời gian sử dụng máy tính của mình. Ứng dụng tập trung vào việc áp dụng các khái niệm về **Time-use research** để phân loại các hoạt động của bạn và đưa ra điểm số hiệu suất chính xác nhất, từ đó khuyến khích thói quen làm việc năng suất và lành mạnh hơn.

## 🚀 Tính Năng Chính

- **Theo dõi tự động:** Cập nhật liên tục tên ứng dụng, cửa sổ hoặc trình duyệt mà bạn đang sử dụng ở Background.
- **Tính điểm Focus Score thông minh:** Phân bổ thời gian theo danh mục thay vì đếm thời gian thô.
- **Hệ thống cảnh báo Idle & Tạm dừng:** Tự động nhận diện khi bạn rời khỏi máy tính sau khoảng thời gian nhất định (Mặc định 300 giây = 5 phút).
- **Phân loại thông minh (Time Category):** Dựa trên từ khóa của chương trình/ứng dụng để tự động xếp loại hoạt động.
- **Thông báo & Gợi ý (Suggestions):** Nhắc nhở nghỉ ngơi khi ngồi quá lâu hoặc nhắc nhở tập trung lại khi mải mê giải trí.

---

## 🕒 Cơ Chế Của "Time-use research"

Ứng dụng chia thời gian người dùng tương tác với máy tính thành **4 mảng (Category)** chính:

1. **Contracted Time (Thời gian làm việc):** Các ứng dụng tạo ra giá trị trực tiếp như _Visual Studio, VS Code, Rider, Word, Excel, Figma, v.v._
2. **Committed Time (Thời gian cam kết):** Các ứng dụng hỗ trợ, quản lý tiến độ, trao đổi liên lạc như _Teams, Slack, Mail, Notion, Todoist, ChatGPT, v.v._
3. **Necessary Time (Thời gian sinh hoạt/Nghỉ ngơi):** Thời gian thiết bị rơi vào trạng thái _Idle (Máy mở nhưng người dùng không thao tác phím/chuột)_.
4. **Free Time (Thời gian rảnh rỗi/Giải trí):** Các hoạt động xem video, lướt mạng xã hội như _YouTube, Facebook, TikTok, Netflix, Steam, v.v._

_(Lưu ý: Bạn có thể cập nhật các danh sách từ khóa này bên trong source code `TrackingRules.cs` > `ContractedApps`, `CommittedApps`, v.v.)_

---

## 📊 Cơ Chế Tính Điểm "Focus Score"

Hệ thống tính điểm không đơn thuần hoạt động theo tuyến tính (cộng/trừ dần đều) mà sử dụng thuật toán **Phi tuyến tính (Non-linear)** để phản ánh đúng tâm lý và hiệu suất con người:

- **Khởi điểm:** 0/100 Điểm.
- **Contracted & Committed Time:** Tích lũy điểm dần dần (1.2 điểm/phút cho Contracted, 0.8 điểm/phút cho Committed). Cần sự bền bỉ để đạt điểm tối đa. Đặc biệt nếu giữ focus trên 30 phút, bạn sẽ nhận được một lượng _Bonus Logarit_ nhẹ.
- **Tác động của Free Time:** Xem giải trí một chút (vài phút) sẽ không lập tức phá hủy điểm số của bạn. Tuy nhiên, nếu bạn chìm đắm _quá 10-15 phút_, một hàm **Exponential (Hàm mũ)** sẽ được kích hoạt `(Điểm trừ = số phút^1.3 * 0.8)` và giảm toàn bộ Focus Score một cách chóng mặt.
- **Phạt chuyển Tab/App liên tục:** Nếu ứng dụng phát hiện bạn liên tục nhảy qua lại giữa nhiều ứng dụng (Alt-Tab liên tục do mất tập trung), điểm sẽ bị trừ dần dựa trên số lần chuyển ứng dụng (Sử dụng hàm Logarit để tránh trừ quá gắt).
- **Thưởng Focus (Continuous Bonus):** Ngược lại, nếu bạn giữ nguyên một cửa sổ (ví dụ: màn hình code) liên tục > 15 phút không rời đi, cơ chế sẽ bắt đầu thưởng thêm điểm đều đặn vì sự tập trung đáng kinh ngạc của bạn..

---

## ⏸ Cơ Chế "Idle"

Ứng dụng tự động theo dõi I/O (Chuột, Bàn phím) thông qua hệ điều hành (`user32.dll` API).

- Nếu trong một khoảng thời gian thiết lập trước (ví dụ 5 phút) mà **không có bất kỳ tín hiệu nhập liệu nào**, ứng dụng tự động cắt ngang thời gian của app cuối cùng, và chuyển sang trạng thái ghi nhận là **`Idle` (Necessary Time)**.
- Trạng thái Idle đóng vai trò trung tính (giữ điểm tạm thời).
- Khi bạn cầm vào chuột/bàn phím trở lại, Timer sẽ tiếp tục tự đo lường ứng dụng hiện tại.

---

## 🛠 Hướng Dẫn Cài Đặt (Build & Chạy)

**Yêu cầu hệ thống:**
Nếu bạn build từ Source: .NET 10 SDK & Visual Studio 2022.
Nếu chạy file thực thi (`.exe`): Chỉ cần Windows 64-bit hoặc .NET 10 Desktop Runtime tùy theo cách bạn chọn build.

### 📥 Tải Bản Cài Đặt Nhanh (Releases)

Nếu bạn chỉ muốn sử dụng phần mềm mà không cần cài đặt mã nguồn hay can thiệp vào code, hãy làm theo các bước sau:

1. Truy cập vào phần **[Releases](https://github.com/Byakuya2709/TimeTracker/releases)** ở thanh bên phải của trang Repository này.
2. Tải về phiên bản mới nhất (file có định dạng `TimeTracker-vx.x.x-win-x64.exe`).
3. Chấp nhận các cảnh báo bảo mật nếu có và click đúp để mở phần mềm trực tiếp (Không cần cài đặt .NET runtime do ứng dụng đã nhúng sẵn).

### 🖥️ Dành cho Developer (Clone & Build)

1. Clone kho lưu trữ này về máy:
   ```powershell
   git clone https://github.com/Byakuya2709/TimeTracker.git
   ```
2. Mở PowerShell hoặc Terminal tại root project.
3. Build ứng dụng dạng **Self-contained** (Chạy trực tiếp không cần cài đặt thêm .NET 10):
   ```powershell
   dotnet publish TimeTracker.App\TimeTracker.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
   ```
4. Sau khi quá trình hoàn tất, truy cập thư mục:
   `\TimeTracker.App\bin\Release\net10.0-windows\win-x64\publish\` để lấy duy nhất file `TimeTracker.exe` (đã có chứa icon đầy đủ) và khởi chạy!

---

## 💡 Định Hướng Đóng Góp (Contributing)

Đây là một dự án mở để hỗ trợ công việc. Mọi góp ý liên quan đến thuật toán tối ưu hóa thời gian (Time-use Research) là rất được hoan nghênh.
Cảm ơn bạn đã quan tâm đến dự án **Time Tracker App**!
