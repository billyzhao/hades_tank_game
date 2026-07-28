# BC-05：平衡、稳定与发布候选版

## 目标与非目标

目标：交付封锁城区 `0.1.0-rc1` Windows 独立试玩候选版，完成玩法、稳定性、Release 边界和最终资源审计。

非目标：不扩展第二竞技场，不新增敌军、构筑、商店、装备或表现系统，不执行无测量依据的性能重构。

## 权威文档版本

- `docs/superpowers/specs/2026-07-24-blockade-city-deliverable-convergence-design.md`
- `docs/superpowers/specs/2026-07-20-mobile-core-arena-technical-design.md`
- `docs/superpowers/plans/2026-07-20-mobile-core-arena-roadmap.md`
- `docs/superpowers/plans/2026-07-27-bc05-release-candidate.md`
- `asset_sources/MOBILE_CORE_ASSET_PLAN.md`
- `asset_sources/GAME_ASSET_CATALOG.md`

## 策划验收标准

1. 三个核心均可从标题开始，连续完成五波、Boss 和结算；
2. 固定种子候选稳定，不出现重复、已满或不可选择的死卡；
3. 维修边界、即时升级完全暂停、失败、重启和重开符合已确认规则；
4. 现有敌军、炮弹和持续效果结构回归无卡死、崩溃或数量失控；
5. Release 玩家界面不出现策划验收工具；
6. 独立 Windows 构建不依赖 Godot 编辑器即可启动并重开。

## 开发任务合同

### 必须实现

- BC-05 流程/平衡审计宿主；
- Debug/Release 边界审计；
- Windows 导出预设、版本、说明和构建；
- 最终素材与已知限制登记。

### 必须保持不变

- 已确认三核心、五波、奖励、维修、精英、Boss 和单区结算规则；
- `RunState`、`ArenaController`、`WaveDirector`、构筑控制器和 `AppRoot` 的职责边界；
- 480×270 逻辑画布、1440×810 默认窗口和 OpenGL Compatibility。

### 禁止实现方式

- 不在每个私有方法完成后执行全量自检；
- 不通过删内容、降低目标或硬编码测试结果通过门禁；
- 不在无测量证据时引入对象池、ECS 或跨模块重构。

### 允许/禁止修改的文件

允许：BC-05 测试宿主、明确失败的对应运行模块、项目/导出配置、README、素材主清单和本迭代记录。

禁止：后四区资源与代码、未确认本地素材目录、`.superpowers/`、仓库外系统配置。

## 测试矩阵

| 风险 | 验证 |
|---|---|
| 三核心/种子/构筑死路 | BC-05 流程与奖励审计 |
| 维修 30% 边界 | 29%、30%、31% 定向断言 |
| 残敌、待出生与重复清场 | 现有波次宿主 + BC-05 状态机回归 |
| 异步退出/重开 | 单区流程、Boss 恢复与重复开局 |
| 战斗对象结构完整性 | 现有综合宿主的敌军、炮弹与正式持续效果数量回归 |
| Debug 泄漏 | Debug 与 Release 构建/独立运行检查 |
| 导出缺资源 | Windows Release 导出与脱离编辑器启动 |
| 素材遗漏 | 运行引用与 `GAME_ASSET_CATALOG.md` 审计 |

## 开发理解回执

BC-05 是收敛和交付迭代，不是新增内容迭代。先测量基线，只处理能被测试或实际运行复现的缺陷；完成全部开发后一次性集中自检。

## 架构合规矩阵

| 架构条款 | 实现文件/接口 | 验证证据 | 状态 |
|---|---|---|---|
| 单局事实只进 RunState | `RunState`、既有构筑/奖励控制器 | BC-05 三核心 × 24 种子完整状态审计 | 通过 |
| 五波/Boss 仍由 ArenaController 编排 | `ArenaController`、`SingleArenaFlowTestHost` | 五波、精英、Boss、结算与重开回归 | 通过 |
| 刷新与残敌仍由 WaveDirector 管理 | `WaveDirector`、`ArenaWaveTestHost`、`SpawnEntranceLayoutTestHost` | 刷新结束、待出生、残敌和安全入口回归 | 通过 |
| Release 不含 Debug 工具 | `AcceptanceMenu`、`DebugOverlay`、`Game1.csproj`、`export_presets.cfg` | Release 排除 `tests/**`，正式程序集不含测试宿主；独立 EXE 启动通过 | 通过 |

## 偏离记录

2026-07-27 用户明确取消 BC-05 性能测试与调优门禁，要求集中做好功能测试和验收。已移除新增性能宿主，并撤回为跑分尝试的炮弹更新频率改动；保留既有功能性数量回归。

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

## 工作区与仓库状态

- 当前分支：`main`
- 工作区差异：BC-05 已完成并通过用户验收；`.superpowers/` 与未确认 `batch-01-units/` 不进入范围
- 暂存状态：无
- 本地提交状态：BC-05～BC-05B 已进入 `main`
- 推送状态：已同步至 `origin/main`

## 自动化与运行验证

- `dotnet test GodotTank.sln --no-restore`：65/65 通过；
- `dotnet build game1/Game1.csproj -c ExportRelease --no-restore`：0 警告、0 错误；
- Godot headless：21 个标准宿主/集成场景通过；`ProtocolRuntimeTestHost` 的 `reward_catalog`、`navigation_grid` 两个套件分别通过；
- `bc05_release_audit`：覆盖 3 核心 × 24 固定种子、五波/奖励/Boss、维修 29%/30%/31%、即时升级 FIFO 与干净重开；
- 主场景 headless 冒烟通过；
- Windows x86_64 Release 导出日志无 `ERROR`/`WARNING`；
- 独立 EXE headless 启动并正常退出，退出码 0；
- Release 程序集未检出测试宿主；产品元数据为“废土中继” `0.1.0.0`；
- EXE SHA-256：`2303711010C2FC83985187AB4645E78858569851E5BEAF03B9743D8ED5D07CC6`。

## 策划复验

主智能体按已确认核心玩法复验通过：未扩展第二竞技场，三核心、五波、维护奖励、即时升级、精英、路障指挥车、胜败结算与重开规则未偏离；性能跑分仍按用户确认延期。

## 用户验收

2026-07-28 用户确认当前整体功能与画面大致无问题，并授权提交推送。

## 提交与推送

用户已授权；提交与推送结果以本轮 Git 记录为准。
