# BaseGame

Kho module gameplay dùng lại được cho Unity. Mỗi module là một cơ chế độc lập, được thử trong playground trước khi thành package.

## Language

### Project

**Module**:
Một cơ chế gameplay đóng gói thành package, có thể mang sang game khác dùng lại.
_Avoid_: system, feature

**Experiment**:
Bản thử một cơ chế trong Playground, được tự do sửa hoặc xóa.
_Avoid_: prototype, demo

**Scenario**:
Bản ghép nhiều module thật trong Integration để kiểm tra chúng chạy cùng nhau.
_Avoid_: test scene, sample

### Calendar

**Step**:
Khoảng thời gian game nhỏ nhất mà đồng hồ nhảy qua; không có thời điểm nằm giữa hai step.
_Avoid_: tick, interval. Ngoại lệ: `GameClock.Tick(realDeltaSeconds)` là tên API nạp giây thật, không phải một step.

**Day 0**:
Ngày đầu tiên của một game mới, gốc để đếm mọi thời điểm.

**Day boundary**:
Thời điểm một ngày mới bắt đầu.
_Avoid_: midnight, day reset

**Clock time**:
Giờ mà đồng hồ trong game hiển thị, có thể lệch so với số phút đã trôi kể từ Day 0.
_Avoid_: real time, local time

**Fixed-length season**:
Mùa có số ngày định trước, tự kết thúc khi đủ ngày.

**Command-only season**:
Mùa không có độ dài định trước, chỉ kết thúc khi game yêu cầu chuyển mùa.
_Avoid_: infinite season, open season

**Skip**:
Nhảy thời gian tới một thời điểm trong một lần; thế giới coi như khoảng đó đã trôi qua.
_Avoid_: jump, warp

**Fast-forward**:
Thời gian trôi nhanh hơn qua nhiều frame nhưng vẫn đi qua từng step.
_Avoid_: speed-up, time skip

**Restore**:
Nạp lại trạng thái lịch đã lưu; thời gian trong thế giới không trôi.
_Avoid_: load

**Base speed**:
Tốc độ thời gian khi không fast-forward.

**Running speed**:
Tốc độ thời gian đang áp dụng: tốc độ fast-forward khi đang fast-forward, còn lại là base speed.
_Avoid_: current speed

**Pause source**:
Thứ đang giữ đồng hồ dừng; đồng hồ chỉ chạy khi không còn pause source nào.
_Avoid_: pause reason

**Snapshot**:
Trạng thái lịch vừa đủ để lưu và restore.
_Avoid_: save data

**Accumulating module**:
Module có giá trị thay đổi dần theo thời gian trôi, như đói hay cây lớn.

**Milestone module**:
Module phản ứng với các thời điểm cố định, như lớp học lúc 8:00.

**State-based module**:
Module có trạng thái suy ra từ thời điểm hiện tại, như cửa hàng đang mở hay đóng.
