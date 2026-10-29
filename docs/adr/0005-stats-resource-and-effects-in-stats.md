# Stats: Resource và Effect nằm trong package Stats

Package Stats giữ cả Stat, Resource (giá trị hiện tại bị chặn bởi một Stat làm max) và Effect (gói modifier có thời hạn), để chỗ nối giữa chúng (effect là source của modifier, max của resource là stat) được thiết kế một lần. Đổi lại, package to hơn, và Health, Needs phụ thuộc Stats thay vì tự đứng một mình.

## Considered Options

- Chỉ có Stat: package nhỏ hơn, nhưng Health và Needs mỗi nơi tự viết giá trị hiện tại, clamp theo max và event chạm 0.
- Mọi thứ đều là Stat, đổi stat chỉ qua effect (kiểu Unreal GAS): một khái niệm duy nhất, nhưng nặng, và mất cách dùng "chỉ con số có modifier".
- Effects là package riêng phụ thuộc Stats: mỗi package nhỏ, nhưng thiết kế hai lần và chỗ nối dễ vụng.
