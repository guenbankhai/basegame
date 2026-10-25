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

**Snapshot**:
Trạng thái của một module vừa đủ để lưu và restore.
_Avoid_: save data

**Restore**:
Nạp lại trạng thái đã lưu của một module; thời gian trong thế giới không trôi.
_Avoid_: load

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

**Base speed**:
Tốc độ thời gian khi không fast-forward.

**Running speed**:
Tốc độ thời gian đang áp dụng: tốc độ fast-forward khi đang fast-forward, còn lại là base speed.
_Avoid_: current speed

**Pause source**:
Thứ đang giữ đồng hồ dừng; đồng hồ chỉ chạy khi không còn pause source nào.
_Avoid_: pause reason

**Accumulating module**:
Module có giá trị thay đổi dần theo thời gian trôi, như đói hay cây lớn.

**Milestone module**:
Module phản ứng với các thời điểm cố định, như lớp học lúc 8:00.

**State-based module**:
Module có trạng thái suy ra từ thời điểm hiện tại, như cửa hàng đang mở hay đóng.

### Stats

**Stat**:
Một chỉ số có giá trị tính từ base value và các modifier, như tốc độ chạy hay máu tối đa.
_Avoid_: attribute, characteristic

**Base value**:
Giá trị gốc của một stat trước khi áp modifier.
_Avoid_: raw value, initial value

**Derived stat**:
Stat có giá trị phụ thuộc vào stat khác, như máu tối đa tăng theo thể lực.
_Avoid_: computed stat, secondary stat

**Modifier**:
Một thay đổi gắn vào stat, mang source của nó; gỡ modifier thì stat trở về như cũ.
_Avoid_: buff, bonus

**Flat**:
Loại modifier cộng thẳng vào base value.
_Avoid_: additive

**PercentAdd**:
Loại modifier phần trăm được cộng với các PercentAdd khác rồi mới nhân một lần.
_Avoid_: increased, additive percent

**PercentMult**:
Loại modifier phần trăm nhân riêng, chồng lên mọi tầng trước.
_Avoid_: more, multiplier

**Source**:
Thứ đã thêm modifier vào stat (món đồ, effect); dùng để gỡ mọi modifier của nó một lần.
_Avoid_: owner, origin

**Resource**:
Giá trị bị tiêu và hồi trực tiếp, luôn nằm giữa một min cố định và một max là stat, như máu hay độ đói.
_Avoid_: gauge, pool, vital, meter

**Depleted**:
Trạng thái resource vừa chạm min.
_Avoid_: empty, dead

**Filled**:
Trạng thái resource vừa chạm max.
_Avoid_: full, maxed

**Effect**:
Một gói modifier áp lên thực thể trong một thời hạn, hoặc vô hạn cho tới khi bị gỡ, như "Ăn sáng no" hay "Ốm".
_Avoid_: buff, debuff, status effect, moodlet

**Stack**:
Số lần một effect đang chồng lên chính nó; modifier của effect nhân theo số này.
_Avoid_: charge, level

**Stack rule**:
Luật quyết định điều gì xảy ra khi áp lại một effect đang chạy: làm mới, kéo dài, bỏ qua hay thêm stack.
_Avoid_: stacking policy, merge rule

**Stack mode**:
Cách các stack của một effect biến thành modifier: cộng dồn thành một, hay mỗi stack áp riêng.
_Avoid_: stack formula

**Effect group**:
Tập các effect khác nhau loại trừ lẫn nhau, như các loại thuốc tốc độ; mỗi lúc chỉ một effect trong group có tác dụng.
_Avoid_: category, tag, exclusive set

**Rank**:
Độ mạnh của một effect trong group, dùng để chọn effect có tác dụng.
_Avoid_: priority, level, tier

**Suppressed**:
Trạng thái của effect vẫn đang chạy thời hạn nhưng không có tác dụng, vì effect khác cùng group mạnh hơn.
_Avoid_: inactive, paused, hidden
