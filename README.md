# 废土中继

《废土中继》是一款使用 Godot 4.7 .NET / C# 开发的俯视角坦克肉鸽原型。核心体验是弹道反弹、可破坏地形、中继站守卫、跨房间总耐久，以及“核心模块 + 四部门协议”的局内构筑。

## 当前阶段

迭代 08 已通过用户验收并完成 MVP 纵切封版。当前进入 Alpha 01A：统一一格一车的单位比例、底部中继站防区和上方来敌构图；验收后再扩充敌军素材、敌人池和第三张城区房间。

Godot 项目入口为 [`game1/project.godot`](game1/project.godot)。环境要求：

- Godot 4.7 .NET；
- .NET 8 用于 Godot 游戏程序集；
- .NET 10 用于独立测试项目；
- 逻辑画布 480×270，默认验收窗口 1440×810。

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

更完整的操作说明见 [`game1/README.md`](game1/README.md)。

## 权威文档

- [游戏策划](docs/superpowers/specs/2026-07-15-roguelite-tank-design.md)
- [Godot 技术设计](docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md)
- [MVP 开发计划](docs/superpowers/plans/2026-07-15-roguelite-tank-mvp.md)
- [项目治理](docs/superpowers/specs/2026-07-17-agent-collaboration-governance-design.md)
- [迭代记录](docs/iterations/README.md)
- [素材来源与授权状态](asset_sources/README.md)

## 仓库规则

开发必须服从已确认的策划、技术、计划、素材授权和验收文档。用户明确验收通过前，不提交、不推送；发现实质偏离时先请求确认并同步权威文档。
