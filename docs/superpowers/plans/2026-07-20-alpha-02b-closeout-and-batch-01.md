# Alpha 02B 收尾与 Batch 1 门禁实施计划

> **执行约束：** 本项目使用单主智能体内联执行；用户验收前不得提交或推送。本计划只收尾 Alpha 02B 并交付一张 Batch 1 样张，不提前实现 Alpha 02C。

**目标：** 修复窗口失焦后无法直接继续移动的暂停体验，冻结极稀疏竞技场与素材插入顺序，并生成一张玩家坦克/核心视觉样张供用户确认。

**架构：** 提前落地技术设计已经冻结的 `PauseCoordinator` 原因集合，让 `Manual` 与 `FocusLost` 独立获取/释放；`PauseController` 只转发 Esc 和窗口通知。素材只进入 `asset_sources`，用户确认前不复制到 `game1/assets`。

**技术栈：** Godot 4.7 .NET、C#、NUnit、Godot headless、内置 image generation。

## 全局约束

- 不修改 Godot/.NET 版本，不新增外部依赖；
- 不实现五波、经验、核心规则或新敌人；
- Batch 1 只生成一张样张，不使用未确认 `batch-01-units` 候选作为生成参考；
- 全量自检只在代码开发完成后执行；
- 用户验收前不提交、不推送。

### 任务 1：权威规则同步

**文件：**

- 修改：`docs/superpowers/specs/2026-07-20-mobile-core-arena-roguelite-redesign.md`
- 修改：`docs/superpowers/specs/2026-07-20-mobile-core-arena-technical-design.md`
- 修改：`docs/superpowers/plans/2026-07-20-mobile-core-arena-roadmap.md`
- 修改：`asset_sources/MOBILE_CORE_ASSET_PLAN.md`

- [x] 记录 75%～85% 开放区域、3～5 组障碍岛和三坦克宽通路；
- [x] 记录 `FocusLost` 聚焦后只释放自身原因；
- [x] 把 Batch 1～5 插入对应代码迭代前，不再统一延后素材。

### 任务 2：暂停焦点回归测试

**文件：**

- 修改：`game1/tests/headless/MobileCoreSurvivalTestHost.cs`

**接口：**

- 消费：`PauseCoordinator.Acquire/Release/Contains`
- 验证：失焦暂停、聚焦恢复；手动暂停与失焦重叠后仍保持暂停。

- [x] 先增加调用尚不存在 `PauseCoordinator` 的运行时测试；
- [x] 构建并观察因类型/接口不存在而失败；
- [x] 不在红灯前修改生产暂停代码。

### 任务 3：按冻结架构修复暂停

**文件：**

- 新建：`game1/scripts/ui/PauseCoordinator.cs`
- 修改：`game1/scripts/ui/PauseController.cs`
- 修改：`game1/scripts/app/AppRoot.cs`

**接口：**

```csharp
public enum PauseReason { Manual, FocusLost, LevelUp, InterWaveReward }
public sealed class PauseCoordinator
{
    public bool IsPaused { get; }
    public event Action<bool> PauseChanged;
    public PauseCoordinator(SceneTree sceneTree);
    public void Acquire(PauseReason reason);
    public void Release(PauseReason reason);
    public bool Contains(PauseReason reason);
}
```

- [x] `AppRoot` 创建唯一协调器并注入 `PauseController`；
- [x] Esc 只切换 `Manual`；失焦获取 `FocusLost`，聚焦释放 `FocusLost`；
- [x] 遮罩订阅聚合暂停事实，不直接拥有第二套状态；
- [x] 运行定向测试确认红灯转绿。

### 任务 4：集中验证与记录

**文件：**

- 修改：`docs/iterations/iteration-alpha-02b-mobile-core-survival.md`

- [x] 运行 `dotnet test GodotTank.sln --no-restore`；
- [x] 运行 Alpha 02B 专项 headless、相关旧套件和主场景启动；
- [x] 实机检查失焦后重新聚焦自动恢复，手动 Esc 暂停不会自动解除；
- [x] 登记结果，不提交、不推送。

### 任务 5：Batch 1 单张视觉门禁

**文件：**

- 新建：`asset_sources/ai_generated/batch-01-mobile-core/player-core-sample/`
- 修改：`asset_sources/AI_PROTOTYPE_ASSETS.md`

- [x] 以已确认 Gate 0 真实比例图为唯一活动视觉参考；
- [x] 生成一张玩家坦克与一种核心变化的小样，保持重型机械和低分辨率街机可读性；
- [x] 保存原图、完整提示词、透明 QC 帧、放大预览与状态登记；
- [x] 不裁切进生产资源，不继续 Batch 2，等待用户视觉确认；
- [x] 2026-07-21 用户确认第二版通过，并作为 Batch 1 后续玩家坦克素材基准。
