# 废土中继

《废土中继》是一款使用 Godot 4.7 .NET / C# 开发的俯视角坦克肉鸽。重构后的核心体验以**玩家坦克本体**为唯一战斗主体：在黄沙工业竞技场中持续移动、射击和冲刺，通过战斗内即时升级形成构筑，完成 5 座竞技场、每座 5 波敌军与 1 场 Boss 战的完整单局。

“移动中继核心”只作为坦克内部的世界观与构筑核心存在，不再拥有独立中继站实体、基地血条或守卫失败条件。

## 当前阶段

已完成 Alpha 01A 的单位比例与俯视战场构图验证。当前进入 **Alpha 02A：重构文档与视觉基线**，正在锁定新的技术边界、素材批次和真实游戏比例图。

仓库中的现有可执行版本仍是重构前的历史 MVP，会暂时保留中继站相关运行逻辑，供回归与迁移对照使用；这些内容从 Alpha 02B 起按新架构逐步拆除，不能作为后续功能设计依据。

Godot 项目入口为 [`game1/project.godot`](game1/project.godot)。环境要求：

- Godot 4.7 .NET；
- .NET 8 用于 Godot 游戏程序集；
- .NET 10 用于独立测试项目；
- 逻辑画布 480×270，默认验收窗口 1440×810；
- OpenGL Compatibility 渲染管线。

## 构建、测试和启动

以下命令均从仓库根目录执行：

```powershell
dotnet build game1/Game1.csproj --nologo
dotnet test GodotTank.sln
godot --headless --path game1 --scene res://tests/integration/mvp_test_runner.tscn
godot --path game1
```

OpenGL Compatibility 启动器：

```powershell
.\game1\run-game-opengl.cmd
.\game1\run-editor-opengl.cmd
```

当前可执行版本的操作与历史验收说明见 [`game1/README.md`](game1/README.md)。

## 当前权威文档

- [移动核心竞技场策划重构](docs/superpowers/specs/2026-07-20-mobile-core-arena-roguelite-redesign.md)
- [移动核心竞技场技术设计](docs/superpowers/specs/2026-07-20-mobile-core-arena-technical-design.md)
- [重构开发路线图](docs/superpowers/plans/2026-07-20-mobile-core-arena-roadmap.md)
- [项目治理](docs/superpowers/specs/2026-07-17-agent-collaboration-governance-design.md)
- [迭代记录](docs/iterations/README.md)
- [素材来源、批次与授权状态](asset_sources/README.md)

2026-07-15 的原策划、技术设计和 MVP 计划仅保留为历史决策记录；凡与上述三份重构文档冲突的内容，均以后者为准。

## 仓库规则

开发必须服从已确认的策划、技术、计划、素材授权和验收文档。发现玩法、范围、架构、依赖、素材或验收标准发生实质偏离时，先请求用户确认，再同步权威文档。用户明确验收通过前，不提交、不推送。
