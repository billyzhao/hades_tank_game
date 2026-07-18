# 迭代 08：MVP 收尾与交付加固

状态：2026-07-19 用户可见验收通过，允许提交并推送 `main`。

## 目标与非目标

- 目标：偿还 Task 8 未完成的存档、调试、暂停、集成验收、压力基线和重启安全门槛。
- 目标：形成可重复运行的“战斗 → 奖励 → 战斗 → Boss → 结算”MVP 验收基础。
- 非目标：新增普通敌军、协议、Boss 阶段、正式商业素材或中途续玩存档。

## 实现合同

| 验收项 | 实现边界 | 自动证据 |
|---|---|---|
| 原子存档 | 只保存设置、解锁 ID、最近一局摘要；临时写入、回读校验、原子替换 | `SaveDataTests`、`save_round_trip` |
| 损坏恢复 | 损坏文件保留为 `.broken`，运行时回退默认值 | `save_round_trip` |
| 重启安全 | 清理中继站复位点附近敌弹，恢复后 1.2 秒无敌 | 编译、场景冒烟、可见验收 |
| 调试面板 | F8 显示 FPS、敌军、敌弹、Seed、房间状态；发布构建禁用输入 | 场景冒烟、可见验收 |
| 暂停 | Esc 暂停；失焦自动暂停；恢复必须显式输入 | 场景冒烟、可见验收 |
| 集成验收 | 核心成功/失败分支集中执行并以退出码报告 | `MvpTestRunner` |
| 压力基线 | 同场 30 敌军、160 炮弹、40 危险区域运行两个物理帧 | `stress_30_enemies_160_projectiles_40_hazards` |
| 失败结算 | 中继站归零或重启耗尽时清场、记录摘要并显示可重开结算层 | 主场景冒烟、可见验收 |

## 统一自检门

开发结束后仅执行一轮完整自检：

```powershell
dotnet test GodotTank.sln
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --editor --quit
godot --headless --path game1 --scene res://tests/integration/mvp_test_runner.tscn
```

通过后再进行 Godot 可见短局自验并交付用户验收脚本。

## 2026-07-19 自检结果

- `dotnet build game1/Game1.csproj --nologo`：通过，0 警告、0 错误。
- `dotnet test GodotTank.sln --no-restore --nologo`：35/35 通过。
- `godot --headless --path game1 --editor --quit`：通过。
- `MvpTestRunner`：8/8 运行时检查通过。
- 主场景 180 帧冒烟：通过，无启动错误。
- 用户在 Godot 中完成可见玩法验收：通过。
