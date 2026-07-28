# BC-05B：构筑外观与受击反馈补强

## 目标与非目标

目标：让玩家无需依赖卡片文字或日志，就能从坦克本体看出协议部门、辅助机等级和受击状态；同时移除普通敌军贴图背后的程序色块。

非目标：不修改协议数值、奖励概率、辅助机槽位、辅助攻击行为、碰撞体、敌军 AI、波次或 Boss 规则；不扩展第二竞技场。

## 已确认范围

1. 所有普通敌军只显示透明职责贴图，不显示背景板；
2. 玩家受到有效伤害时显示更明显的火花、闪色和短促机械位移，但物理坐标不变；
3. 四部门协议分别装配到玩家坦克的独立槽位，同部门强度只改变该模块的视觉阶段；
4. 四种辅助机保持坦克外部独立单元，并按 Mk.I～Mk.III 切换独立外观；
5. 新增或升阶播放短促装配动画；重新开局或创建玩家实例时按 `BuildSnapshot` 重建；
6. Debug 验收菜单提供四部门协议和四种辅助机的正式写入口，不直接改私有状态。

## 架构合同

- `RunState` / `BuildController` 继续持有协议和辅助等级真值；
- `TankBuildVisualController` 只读取快照并投影坦克外观，不识别具体协议 Id；
- `TankBuildVisualCatalog.tres` 是只读视觉映射，不保存运行状态；
- `AuxiliaryHost` 继续执行既有辅助攻击，同时按稳定辅助 Id 读取三阶视觉谱系；
- `TankVisualAnimator` 的受击位移只作用于视觉子节点，不改变 `PlayerTank.GlobalPosition`；
- `AcceptanceMenu` 只发请求，由 `AppRoot` 调用 `BuildController` 正式入口。

## 素材范围

- Batch 10：四部门坦克模块、四种辅助机 Mk.II、四种辅助机 Mk.III；
- 首张 3×4 母版第三行因越过分格边缘被拒收；Mk.III 使用重新生成的 1×4 母版；
- 运行文件在用户实机验收前登记为 `CANDIDATE`，不得提前改为 `CONFIRMED`。

## 策划验收标准

1. 普通敌军周围不再出现矩形、菱形或圆形背景色块；
2. 分别授予军械、侦察、后勤、工程协议后，玩家坦克上出现位置和颜色不同的实体模块；
3. 同部门协议达到更高阶段后，已有模块加强，不额外堆出重叠部件；
4. 分别把任一辅助机升至 Mk.I、Mk.II、Mk.III，外部单元每级都有清晰外形变化；
5. 辅助机仍位于坦克外部，环绕、侧挂、后挂和顶部装置不会被误认为车体底图；
6. 玩家被击中时出现明显受击表现，坦克不会因表现位移穿墙、改变碰撞或自行移动；
7. 暂停、升级完全暂停、失败、重启、重开和正常战斗流程不受影响。

## 自动验证

- `bc05b_build_visual_test_host.tscn`：目录覆盖、敌军无底板、四部门模块、辅助三阶换图、受击物理坐标不变；
- `acceptance_menu_test_host.tscn`：协议和辅助 Debug 按钮只发一次正式 Id；
- 既有构筑、辅助、敌军、波次、主流程与 BC-05 发布审计回归；
- 完整构建、Godot 主场景启动和玩家可见验收。

## 门禁与进度

- [x] 策划前置
- [x] 测试前置
- [x] 开发理解
- [x] 开发
- [x] 架构合规
- [x] 测试后置
- [x] 策划复验
- [x] 主智能体自检
- [x] 用户验收
- [x] 提交/推送

## 仓库规则

2026-07-28 用户确认当前整体功能与画面大致无问题并授权提交推送。`.superpowers/` 和未确认的 `batch-01-units/` 不进入范围。

## 集中自检结果

- `dotnet build game1/Game1.csproj --no-restore`：0 警告、0 错误；
- `dotnet test GodotTank.sln --no-restore`：68/68 通过；
- Godot `--import`：12 张 Batch 10 运行 PNG 全部完成导入，无缺失资源；
- 22 个标准 headless 宿主全部通过；BC-04 音频宿主使用 `--audio-driver Dummy` 验证，避免无音频设备导致的错误结果；
- `ProtocolRuntimeTestHost` 的 `reward_catalog`、`navigation_grid`、`boss_phase`、`boss_encounter` 四套件全部通过；
- `MvpTestRunner` 9 项集成流程通过；
- 主场景 headless 运行 180 帧退出码 0，无脚本或资源加载错误；
- `git diff --check` 通过；Batch 10 视觉目录中的 19 个脚本/贴图引用均存在；
- `.superpowers/` 与未确认 `batch-01-units/` 保持未纳入范围。

## 策划复验结论

构筑视觉只消费正式构筑快照，敌军、玩家伤害、协议、辅助、波次和 Boss 玩法规则未偏离；本轮没有新增第二竞技场、数值系统或性能工作。Batch 10 已通过用户当前阶段实机验收并登记为 `CONFIRMED`。
