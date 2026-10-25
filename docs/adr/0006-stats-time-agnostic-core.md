# Stats: lõi không biết thời gian

Modifier không có thời hạn và Stats không phụ thuộc Calendar; mọi thứ có thời hạn đi qua Effects, lớp này nhận `Advance(delta)` với đơn vị do game quy ước. Lý do: package dùng được cho game tính giây thật hoặc theo lượt. Đổi lại, modifier tạm thời nào cũng phải là một effect, và scenario phải tự nối thời gian vào.

## Considered Options

- Modifier tự có `duration`: có modifier tạm thời đơn giản không cần Effects, nhưng trùng việc với Effects và mỗi stat phải tự lo skip.
- Stats phụ thuộc Calendar: tiện cho game này, nhưng ép một cách tính thời gian lên mọi game dùng package.
