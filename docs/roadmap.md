# Roadmap

Danh sách module định hướng phát triển cho BaseGame, xếp theo sản phẩm chính đang nhắm tới. Đây là tài liệu sống: cập nhật khi quyết định thay đổi hoặc khi một module đổi trạng thái.

**Trạng thái:** `Chưa bắt đầu` · `Thử nghiệm` · `Đang phát triển` · `Ổn định` (khớp với bảng Modules trong `README.md`).

---

## 1. Sản phẩm chính

Game **life-sim chủ đề trường học**, không gói gọn trong trường mà mở ra cả **thị trấn** để khám phá và tương tác.

- **Trọng tâm:** nhân vật phát triển như ngoài đời: đi học, làm thêm, giao lưu, xây dựng quan hệ.
- **Có thêm:** loot, combat (chủ yếu cận chiến, mức nhẹ), crafting.
- **Góc nhìn:** 3D dạng 2.5D, top-down/isometric. TPS làm sau.
- **Nền tảng:** Unity 6 (6000.6), URP, C#, PC. Single-player trước; multiplayer tính sau khi các cơ chế chính ổn định.

**Game tham khảo**

| Game | Lấy gì |
|---|---|
| Bully: Scholarship Edition | Trường học + thị trấn, lớp học theo giờ (nhưng không bạo lực) |
| Stardew Valley | NPC sinh hoạt theo lịch, ngày đêm, sự kiện theo lịch |
| Nobody: The Turnaround | Nhu cầu sinh tồn, đi làm, giải trí, quan hệ NPC |
| Escape from Duckov | Góc nhìn top-down, loot, combat, crafting |

## 2. Cách game vận hành

- **Mở, không tuyến tính.** Người chơi tự chọn làm gì, tham gia gì, tự kích hoạt sự kiện.
- **Thời gian trôi liên tục**, có ngày đêm, ngủ để sang ngày mới.
- **Ba cấp thời gian:**
  - **Ngày:** sinh hoạt bình thường (học, làm thêm, gặp gỡ, ăn, ngủ).
  - **Tuần:** sự kiện chung lặp lại (lớp Toán thứ 2 lúc 8h, chợ phiên thứ 7…).
  - **Mùa / năm:** mùa nóng và mùa lạnh. Mùa và năm **chỉ chuyển khi hoàn thành các sự kiện chính** của giai đoạn đó, nên mỗi mùa là một **chương**; sang chương mới thì mở ra sự kiện mới.
- **Lớp học và sự kiện có giờ và địa điểm cố định.** Người chơi tự đến đúng nơi, đúng giờ; lỡ thì đợi tuần sau.
- **Kỹ năng riêng lẻ** (Toán, Văn, Thể lực, Giao tiếp, Nấu ăn…), mỗi kỹ năng có cấp riêng, không có cấp chung cho nhân vật. Lên cấp mở hoạt động, việc làm, lựa chọn hội thoại mới.
- **Quan hệ hai cấp:** độ thân thiết với từng NPC và danh tiếng với từng nhóm (lớp, câu lạc bộ, hội bạn).
- **Nhu cầu sinh tồn:** đói, khát, mệt, tâm trạng, ốm; giảm theo thời gian và chịu ảnh hưởng của mùa.

## 3. Nguyên tắc phát triển

- Mỗi cơ chế là một **module độc lập, dùng lại được**; luật riêng của game nằm ngoài module.
- Quy trình mỗi module: **Playground (thử nghiệm) → chốt thiết kế → package**.
- Làm **từng module một**. Câu hỏi thuộc module sau thì để tới lúc làm mới quyết.
- Unity-first: dùng Input System, Cinemachine, NavMesh, ScriptableObject… trước khi tự xây.

## 4. Danh sách module

### A. Nền tảng

Hầu hết module khác đều cần; làm trước.

| Module | Làm gì | Câu hỏi nghiên cứu | Trạng thái |
|---|---|---|---|
| **Time & Calendar** | Giờ, ngày, tuần, mùa, năm; tốc độ thời gian; tạm dừng; ngủ / tua nhanh; event khi sang giờ, ngày, tuần, mùa | Trôi theo `Update` hay tick? Tạm dừng thế nào? Chuyển mùa do bên ngoài gọi | Chưa bắt đầu |
| **Stats** | Chỉ số và modifier (cộng, nhân %, có thời hạn) | Thứ tự áp modifier? Định nghĩa bằng ScriptableObject hay class thuần? | Chưa bắt đầu |
| **Items** | Định nghĩa item, instance, stack, độ bền, thuộc tính | Tách dữ liệu tĩnh và trạng thái riêng từng món? ID ổn định | Chưa bắt đầu |
| **Inventory** | Container ô slot + giới hạn khối lượng; dùng cho túi, rương, kho | Di chuyển, tách, gộp đồ; event khi thay đổi | Chưa bắt đầu |
| **Interaction** | Phát hiện vật gần, gợi ý, thực hiện (nhặt, mở, nói chuyện, dùng) | Trigger hay overlap? Ưu tiên khi nhiều vật gần nhau | Chưa bắt đầu |
| **Game Flags** | Kho "sự thật" của thế giới (đã gặp ai, đã xong gì) cho các điều kiện | Đặt key, kiểu dữ liệu, cách viết điều kiện | Chưa bắt đầu |
| **Save** | Lưu / tải; mỗi module tự cung cấp dữ liệu cần lưu | Định dạng file, xử lý đổi phiên bản dữ liệu, dùng `persistentDataPath` | Chưa bắt đầu |

### B. Nhân vật và điều khiển

| Module | Làm gì | Trạng thái |
|---|---|---|
| **Character Movement** | Di chuyển top-down, xoay theo chuột, chạy nhanh | Chưa bắt đầu |
| **Camera Rig** | Camera top-down/isometric trên Cinemachine | Chưa bắt đầu |
| **Health & Damage** | Nhận sát thương, chấn thương, ngất | Chưa bắt đầu |

### C. Sinh tồn và kinh tế

| Module | Làm gì | Trạng thái |
|---|---|---|
| **Status Effects** | Buff/debuff theo thời gian (ốm, mệt, no…) | Chưa bắt đầu |
| **Needs & Condition** | Đói, khát, mệt, tâm trạng; giảm theo thời gian | Chưa bắt đầu |
| **Economy** | Tiền, mua bán, cửa hàng | Chưa bắt đầu |
| **Crafting** | Công thức, nguyên liệu, trạm chế tạo, thời gian chế tạo | Chưa bắt đầu |

### D. Life-sim (trọng tâm)

| Module | Làm gì | Trạng thái |
|---|---|---|
| **Schedule** | Dữ liệu lịch "khung giờ, ngày trong tuần → việc gì, ở đâu"; dùng chung cho NPC và lớp học | Chưa bắt đầu |
| **NPC Routines** | NPC tự đi tới đúng chỗ và làm việc theo lịch (NavMesh) | Chưa bắt đầu |
| **Opportunities** | Lớp học, ca làm, sự kiện mở theo giờ và địa điểm; tham gia hoặc lỡ | Chưa bắt đầu |
| **Activities** | Hoạt động tốn thời gian, có chi phí và phần thưởng (học, làm, giải trí, ngủ) | Chưa bắt đầu |
| **Skills** | Kỹ năng riêng lẻ, có XP và cấp, mở khóa nội dung | Chưa bắt đầu |
| **Relationships** | Thân thiết từng người + danh tiếng từng nhóm; mở khóa câu chuyện | Chưa bắt đầu |
| **Dialogue** | Hội thoại có lựa chọn và điều kiện | Chưa bắt đầu |
| **Quests & Story** | Chuỗi mục tiêu chính/phụ theo chương (mùa); xong sự kiện chính thì chuyển mùa | Chưa bắt đầu |
| **Minigame Framework** | Khung chung bắt đầu / kết thúc / trả kết quả minigame cho lớp học và việc làm | Chưa bắt đầu |

### E. Combat và loot

Làm sau phần life-sim cốt lõi.

| Module | Làm gì | Trạng thái |
|---|---|---|
| **Melee Combat** | Đánh cận chiến mức nhẹ | Chưa bắt đầu |
| **NPC AI** | NPC nhận biết (nhìn, nghe) và phản ứng | Chưa bắt đầu |
| **Loot** | Bảng loot có trọng số, độ hiếm | Chưa bắt đầu |
| **Equipment** | Ô trang bị | Chưa bắt đầu |

### G. Để sau

Chỉ làm khi thật sự cần: súng · base · quần áo / ngoại hình · phương tiện (xe đạp, ván trượt) · thời tiết · UI framework · audio · settings · đổi phím (Input Rebinding) · localization · inventory dạng lưới.

### Chủ đề học Unity

Experiment để hiểu, không thành module: Input System (bộ action cho top-down, đổi phím) · Cinemachine · NavMesh · Animator + Animation Rigging · ánh sáng ngày đêm · `ObjectPool<T>` · physics queries · ScriptableObject làm dữ liệu.

## 5. Thứ tự đề xuất

1. Time & Calendar
2. Stats
3. Items → Inventory
4. Interaction, Game Flags, Save
5. Nhóm B và C
6. Nhóm D
7. Nhóm E

## 6. Để quyết sau

Quyết khi làm tới module liên quan:

- **Chuẩn bị cho multiplayer.** Đề xuất: tách dữ liệu khỏi MonoBehaviour, ID ổn định, đổi trạng thái qua hàm rõ ràng, không có `Player.Instance` toàn cục.
- **Lỡ lớp học** thì có hậu quả gì.
- **Ranh giới Opportunities và Quests & Story.**
- **Integration scenario đầu tiên.** Dự kiến "Một tuần ở thị trấn": lớp học và ca làm theo lịch, 2–3 NPC theo lịch, nhu cầu và mua đồ ăn, 3 sự kiện chính để chuyển mùa, ngủ và save.
