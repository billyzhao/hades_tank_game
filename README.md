# 废土中继

《废土中继》是一款使用 Godot 4.7 .NET / C# 开发的俯视角动作肉鸽坦克游戏，围绕弹道反弹、地形利用和中继站守卫展开。

## 当前阶段

Task 5 的三类敌人、三波守点玩法与混合表现专项已经完成，Task 6“三选一协议和第二场战斗”尚未开始。Task 4/5 尚待偿还的架构暂缓项已单独登记，不代表 Task 6 已经进入开发。

Godot 项目入口为 [`game1/project.godot`](game1/project.godot)。当前环境要求：

- Godot 4.7 .NET；
- .NET 8 SDK/运行时用于游戏项目；
- .NET 10 SDK/运行时用于独立测试项目；
- 逻辑画布为 480×270，默认窗口为 1440×810，以整数 3 倍显示。

## 构建、测试和启动

以下命令均从仓库根目录执行：

```powershell
dotnet build game1/Game1.csproj --nologo
dotnet test GodotTank.sln
godot --path game1
```

需要在 OpenGL Compatibility 模式下启动时：

```powershell
.\game1\run-game-opengl.cmd
.\game1\run-editor-opengl.cmd
```

更完整的当前操作与可玩内容见 [`game1/README.md`](game1/README.md)。

## 文档入口

- [游戏策划](docs/superpowers/specs/2026-07-15-roguelite-tank-design.md)
- [Godot 技术设计](docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md)
- [MVP 开发计划](docs/superpowers/plans/2026-07-15-roguelite-tank-mvp.md)
- [多智能体协同与项目治理](docs/superpowers/specs/2026-07-17-agent-collaboration-governance-design.md)
- [迭代执行记录入口](docs/iterations/README.md)
- [混合表现验收脚本](docs/acceptance/2026-07-16-combat-presentation-acceptance.md)
- [素材流程与当前状态](asset_sources/README.md)
- [AI 原型资源登记](asset_sources/AI_PROTOTYPE_ASSETS.md)
- [第三方素材候选清单](asset_sources/THIRD_PARTY_ASSETS.md)

## 仓库规则

所有实现必须服从已确认的策划、技术、计划、素材授权和验收文档。只有用户明确验收通过后，才允许由主智能体提交或推送仓库。
# 当前阶段：迭代 06B 待用户可见验收

协议奖励循环之后会依据 `RoomIndex` 进入第二张非镜像黄沙工业区房间。两张房间均使用三层 `TileMapLayer`；砖墙可被炮弹摧毁，随即刷新 A* 导航。敌军出生边、波次、场景和网格均来自 `RoomDefinition` 资源。

本轮改动尚未用户验收，因此未提交、未推送。
# 当前开发：迭代 07A 待用户验收

已加入路障指挥车 Boss 骨架：独立生命条、50% 两阶段提示和 Boss 验收入口；路障、冲锋、召唤与胜利结算仍属于后续 07B。当前改动尚未提交或推送。
