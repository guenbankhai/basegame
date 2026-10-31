# Stats: ID của definition là field `_id`

`StatDefinition`, `ResourceDefinition`, `EffectDefinition` và `EffectGroupDefinition` giữ ID trong field `_id` trên asset, tách khỏi tên file, để sắp xếp hay đổi tên asset không làm hỏng save và tên file không bị bó theo luật của ID. ID theo chuẩn PascalCase (chữ hoa đầu, sau đó chỉ chữ và số ASCII), và lõi là nơi duy nhất giữ chuẩn này, nên asset và code dựng tay dùng chung một luật.

Lúc chạy game chỉ đọc `_id`: trống hoặc sai chuẩn thì bị lõi từ chối, không bao giờ dùng tên asset làm dự phòng. Tên asset chỉ được dùng trong Editor: Inspector điền `_id` đang trống từ tên asset nếu tên đúng chuẩn, để đỡ phải gõ.

## Considered Options

- ID là tên asset: một nguồn sự thật, nhưng đổi tên file là đổi ID và hỏng save, tên file bị bó theo chuẩn ID.
- `_id` trống thì lúc chạy lấy tên asset: tiện, nhưng có hai luật, và đổi tên file vẫn có thể hỏng save.
- Tự điền trong `OnValidate`: không cần custom Editor, nhưng import hay load asset có thể tự sửa file và dính tên tạm.
- GUID của asset: chắc chắn duy nhất, nhưng save khó đọc và code không viết tay được ID.

## Consequences

- Đổi ID là save cũ không khớp; giữ save qua lần đổi ID là việc của game.
- Tên file và ID có thể lệch nhau; ID hiện trên Inspector.
- Duplicate asset sẽ copy luôn ID, nên Inspector báo lỗi khi trùng ID với asset cùng loại. Trùng trong một profile thì lõi từ chối.
