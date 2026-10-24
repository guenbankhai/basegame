# Cấu trúc project

## Bố cục

```text
basegame/
├── AGENTS.md, README.md, GLOSSARY.md
├── docs/
│   ├── roadmap.md, structure.md, coding-standard.md, module-workflow.md
│   ├── modules/                   mỗi module một tài liệu thiết kế; _template.md
│   └── adr/                       NNNN-<module>-<slug>.md
├── unity/                         project Unity chính
│   ├── Assets/
│   │   ├── Scenes/                scene của experiment: <Experiment>.unity
│   │   ├── Configs/               config và asset dữ liệu của experiment: <Experiment>*.asset
│   │   ├── Settings/              cấu hình render pipeline (URP)
│   │   ├── Playground/<Experiment>/
│   │   └── Integration/<Scenario>/
│   └── Packages/
│       └── com.guenbankhai.<module>/
│           ├── package.json
│           ├── Runtime/
│           ├── Editor/
│           └── Tests/
└── validation/                    project Unity trống, kiểm tra một module cài riêng được (tạo sau)
```

Thư mục và package chỉ tạo khi cần.

| Khu vực | Vai trò |
|---|---|
| Playground | Experiment: thử, so sánh, viết lại hoặc xóa tự do (code ở đây; scene ở `Scenes/`, config ở `Configs/`) |
| Integration | Scenario ghép nhiều module; chỉ dùng module thật |
| Packages | Module: dùng lại được, phải đạt chuẩn chất lượng của project |
| validation/ | Chứng minh module cài và chạy được trong project sạch |

## Phụ thuộc

| Từ \ Tới | Packages | Integration | Playground |
|---|---|---|---|
| Playground | ✅ | ❌ | ❌ (experiment không dùng lẫn nhau) |
| Integration | ✅ | — | ❌ |
| Packages | ✅ có khai báo, không vòng | ❌ | ❌ |

- Phụ thuộc giữa các module khai báo ở cả `package.json` lẫn asmdef.
- Unity không chặn mọi vi phạm (package vẫn reference được asmdef trong `Assets/`), nên các quy tắc này được giữ bằng review.

## Đặt tên

`GuenBanKhai.*` cho thứ mang đi dùng lại được (module); `BaseGame.*` cho thứ chỉ thuộc repo này (experiment, scenario). Nhìn namespace là biết code đó là thư viện hay đồ thử nghiệm.

| Mục | Quy ước | Ví dụ |
|---|---|---|
| Package | `com.guenbankhai.<module>` (chữ thường) | `com.guenbankhai.inventory` |
| Namespace / asmdef của module | `GuenBanKhai.<Module>`, `.Editor`, `.Tests` | `GuenBanKhai.Inventory.Tests` |
| `displayName`, menu tạo asset | `GuenBanKhai <Module>`, `GuenBanKhai/…` | `GuenBanKhai/Calendar Config` |
| Namespace / asmdef của experiment | `BaseGame.Playground.<Experiment>`, `.Tests` | `BaseGame.Playground.InventoryGrid` |
| Namespace / asmdef của scenario | `BaseGame.Integration.<Scenario>` | `BaseGame.Integration.CraftingLoop` |
| Phiên bản Unity trong `package.json` | `"unity": "6000.6"`, `"unityRelease": "5f1"` | |
| Company / Product (project này) | `GuenBanKhai` / `BaseGame` | |

## Ghi chú thiết lập

- **Scene và config của experiment**: để phẳng trong `Assets/Scenes/` và `Assets/Configs/`, đặt tên theo experiment (`Calendar.unity`, `CalendarConfig.asset`). Product phát triển ở project riêng, nên các thư mục này chỉ chứa đồ của experiment. Xóa experiment thì xóa luôn scene và config của nó.
- **`Assets/Settings/`**: chỉ chứa cấu hình render pipeline (URP Global Settings, volume profile, URP Asset nếu có). Còn cài package URP thì URP tự tạo lại Global Settings khi thiếu, nên không xóa thư mục này. Không để đồ của experiment ở đây.
- **Experiment**: một asmdef ở gốc experiment. Chỉ reference asmdef của module và assembly của package Unity mà nó dùng (ví dụ `Unity.InputSystem`). Code không có asmdef sẽ vào `Assembly-CSharp`, mà asmdef không reference được assembly này.
- **Test của experiment** (không bắt buộc): asmdef `Tests/` tên `BaseGame.Playground.<Experiment>.Tests`, chỉ cho platform Editor, reference asmdef của experiment và test framework. Không thêm asmdef nào khác.
- **Module**: asmdef `Runtime/` tên `GuenBanKhai.<Module>`; asmdef `Editor/` tên `GuenBanKhai.<Module>.Editor`, chỉ cho platform Editor; asmdef `Tests/` tên `GuenBanKhai.<Module>.Tests`, reference test framework. Tên file asmdef trùng tên assembly.
- **Test của module**: test trong package embedded (nằm dưới `unity/Packages/`) chạy được không cần thiết lập thêm. Package cài theo cách khác (đường dẫn local, Git, registry), ví dụ trong `validation/`, phải được liệt kê trong `"testables"` của `manifest.json` ở project đó.
- Module không bao giờ hard-code đường dẫn save; dùng `Application.persistentDataPath`.
