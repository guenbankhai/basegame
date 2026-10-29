# Stats: ID của definition là tên asset

`StatDefinition`, `ResourceDefinition`, `EffectDefinition` và `EffectGroupDefinition` lấy ID từ tên asset, để chỉ có một nguồn sự thật và tên asset không bao giờ lệch với ID. Đổi lại, đổi tên asset là đổi ID: snapshot cũ chứa ID đó bị từ chối cả snapshot; giữ save qua lần đổi tên là việc của game (module Save tự nâng cấp save trước khi gọi `Restore`). ID của group không được save, nên đổi tên group không ảnh hưởng save.

## Considered Options

- Field ID riêng, điền sẵn từ tên asset: đổi tên asset không hỏng save, nhưng tên và ID dần lệch nhau và dễ hiểu nhầm.
- GUID của asset: chắc chắn duy nhất, nhưng save khó đọc, code không viết được ID tay, và phải lưu GUID vào asset bằng code Editor.
