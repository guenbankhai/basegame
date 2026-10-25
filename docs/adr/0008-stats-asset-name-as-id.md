# Stats: ID của definition là tên asset

`StatDefinition`, `ResourceDefinition` và `EffectDefinition` lấy ID từ tên asset, để chỉ có một nguồn sự thật và tên asset không bao giờ lệch với ID. Đổi lại, đổi tên asset là đổi ID: save cũ mất các entry đó (bị bỏ qua khi restore), và việc giữ save qua các phiên bản là việc của module Save.

## Considered Options

- Field ID riêng, điền sẵn từ tên asset: đổi tên asset không hỏng save, nhưng tên và ID dần lệch nhau và dễ hiểu nhầm.
- GUID của asset: chắc chắn duy nhất, nhưng save khó đọc, code không viết được ID tay, và phải lưu GUID vào asset bằng code Editor.
