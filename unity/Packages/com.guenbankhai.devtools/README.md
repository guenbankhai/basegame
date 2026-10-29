# GuenBanKhai DevTools

Công cụ Editor: lưu kết quả test và lỗi compile ra file, cho phép chạy test EditMode từ bên ngoài Unity khi Editor đang mở. Code game không phụ thuộc vào package này.

Version 0.1.0 (`CHANGELOG.md`) · Unity 6000.6+ · Phụ thuộc `com.unity.test-framework` · Cài: chép vào `Packages/`, không cần thiết lập.

## Kết quả

Trong `Logs/TestResults/`, ghi sau mỗi lần chạy test hay compile:

- `EditMode.xml`, `PlayMode.xml`: lần chạy gần nhất (XML NUnit). Dòng `<test-run …>` đầu file là tóm tắt (tổng, pass, fail, thời điểm UTC); test fail là các `<test-case result="Failed">`.
- `compile-errors.txt`: lỗi của lần compile gần nhất; không có file là không lỗi.

## Chạy test từ bên ngoài

Editor đang mở, không ở Play mode.

1. Xóa `Temp/DevTools/run-tests.status` nếu có.
2. Tạo `Temp/DevTools/run-tests.request`: mỗi dòng một assembly test, để trống là chạy tất cả.
3. Chờ file status thành `done passed=<n> failed=<n> skipped=<n>` hoặc `compile-error` (trước đó là `refreshing`, `running`).
