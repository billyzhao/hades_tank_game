# Alpha 02A 文档统一与无中继站视觉基线实施计划

> **执行要求：** 使用 `executing-plans` 由单主智能体逐项实施。项目规则禁止默认启动子智能体。步骤使用复选框跟踪。

**目标：** 在修改生产代码前，消除新旧策划/技术/素材文档冲突，冻结移动核心技术边界，并用一张真实游戏比例图确认无中继站竞技场的单位、HUD、敌军密度和奖励界面。

**架构：** 新策划和新技术文档作为当前权威；旧 MVP 文档保留历史正文并标记被替代。素材历史登记不删除，但旧中继站原型改为弃用状态；新素材按首区纵切小批生成。

**技术栈：** Markdown、Godot 4.7 .NET / C# 架构说明、Codex 内置图像生成、Git 文本检查。

## 全局约束

- 不修改 `game1/scripts/`、`game1/scenes/`、`game1/resources/` 或测试代码。
- 不删除当前运行所需的中继站文件；生产代码移除属于 Alpha 02B。
- 不下载或购买第三方素材。
- 不批量生成五个竞技场素材。
- 本迭代只生成一张真实比例总览图；用户确认后才进入玩家/首区素材批次。
- 用户验收前不提交、不推送。
- `.superpowers/` 不得暂存。
- 当前工作区已有 Alpha 01A 代码差异和早期生成素材；本迭代必须在执行记录保存初始 `git status --short`，并只暂存下方列出的 Alpha 02A 文件。

---

### Task 1：建立文档替代关系

**Files:**

- Modify: `docs/superpowers/specs/2026-07-15-roguelite-tank-design.md`
- Modify: `docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md`
- Modify: `docs/superpowers/plans/2026-07-15-roguelite-tank-mvp.md`
- Modify: `docs/superpowers/specs/2026-07-19-alpha-01a-battlefield-composition-design.md`
- Modify: `docs/superpowers/plans/2026-07-19-alpha-01a-battlefield-composition.md`
- Modify: `docs/iterations/iteration-alpha-01a-battlefield-composition.md`

**Produces:** 每个历史文档都明确指向当前策划、当前技术和总路线图，不再被误认为当前实现目标。

- [ ] **Step 1：在旧策划顶部加入历史声明**

加入以下内容，保留原正文：

```markdown
> **历史 MVP 策划基线：已被替代。** 本文记录固定中继站防守原型的已确认历史方案，不再作为当前玩法实现依据。当前权威玩法见《移动核心竞技场肉鸽重构设计》，实施顺序见《移动核心竞技场重构总实施路线图》。
```

- [ ] **Step 2：在旧技术顶部加入历史声明**

```markdown
> **历史 MVP 技术基线：已被替代。** 本文中的 `RelayStation`、中继耐久、房间清场和基地目标只用于解释现有旧代码。移动核心目标架构以 2026-07-20 新技术设计为准；迁移完成前不得把旧字段继续扩展到新系统。
```

- [ ] **Step 3：给旧 MVP 计划和 Alpha 01A 文档加入历史声明**

声明必须同时说明：玩家约一格尺度与竞技场边界仍可复用；底部基地、上方来敌和中继站构图已经废止。

- [ ] **Step 4：检查替代链接**

Run:

```powershell
rg -n "已被替代|移动核心竞技场" docs/superpowers/specs docs/superpowers/plans docs/iterations/iteration-alpha-01a-battlefield-composition.md
```

Expected: 上述六份历史文档都至少出现一条替代声明；新策划和路线图链接路径可解析。

---

### Task 2：编写移动核心技术设计

**Files:**

- Create: `docs/superpowers/specs/2026-07-20-mobile-core-arena-technical-design.md`
- Reference: `docs/superpowers/specs/2026-07-20-mobile-core-arena-roguelite-redesign.md`
- Reference: `docs/superpowers/plans/2026-07-20-mobile-core-arena-roadmap.md`

**Produces:** Alpha 02B–02H 可直接引用的运行时类型、节点所有权、状态流、数据资源、暂停语义、存档迁移和测试边界。

- [ ] **Step 1：写入技术设计头部与不可变基线**

必须明确 Godot 4.7 .NET、`net8.0` 游戏程序集、`net10.0` 测试、480×270、OpenGL Compatibility、静态资源只读、运行时状态普通 C# 对象、单主智能体和无新增依赖。

- [ ] **Step 2：冻结状态类型**

技术文档中定义以下签名，后续实现如需改变公共语义必须申请偏离：

```csharp
public enum RunPhase { CoreSelection, Arena, Completed, Failed }
public enum ArenaState { Loading, Intro, WaveCombat, Cleanup, Reward, BossIntro, BossCombat, Completed, Failed }
public enum RewardKind { Stat, NormalProtocol, Maintenance, RareProtocol, CoreEvolution, MajorEvolution }
public enum ProtocolRank { None = 0, MkI = 1, MkII = 2, MkIII = 3 }

public sealed class RunState
{
    public int Seed { get; }
    public int ArenaIndex { get; private set; }
    public int WaveIndex { get; private set; }
    public int PlayerArmor { get; private set; }
    public int MaximumArmor { get; private set; }
    public int RebootsRemaining { get; private set; }
    public int Level { get; private set; }
    public int Experience { get; private set; }
    public CoreId CoreId { get; private set; }
    public IReadOnlyList<OwnedProtocol> Protocols { get; }
    public IReadOnlyList<AuxiliarySlotState> AuxiliarySlots { get; }
}
```

- [ ] **Step 3：冻结控制器职责与依赖方向**

文档必须定义：`RunController` 拥有跨竞技场流程；`ArenaController` 拥有五波与 Boss 衔接；`WaveDirector` 只生成并报告事实；`RewardController` 只生成和应用当前奖励；`BuildController` 只计算构筑；`AppRoot` 只组合节点和界面。

- [ ] **Step 4：冻结数据资源与校验规则**

列出 `CoreDefinition`、`ProtocolDefinition`、`AuxiliaryDefinition`、`EnemyDefinition`、`EliteModifierDefinition`、`WaveDefinition`、`ArenaDefinition`、`BossDefinition` 的字段、稳定 Id、引用关系和 `Validate()` 失败条件。

- [ ] **Step 5：冻结暂停、重启和存档迁移**

明确：完全暂停不清弹；恢复 0.4 秒保护；原地重启 1.2 秒、50% 装甲、无伤击退、2 秒保护；首个纵切默认 1 次重启；存档 schema 升级时删除 `RelayIntegrity` 摘要语义并保留设置/横向解锁。

- [ ] **Step 6：写入测试矩阵和禁止方式**

禁止 `AppRoot` 重新实现状态机、禁止共享 `Resource` 运行时写入、禁止新协议复用旧中继 Id、禁止隐藏 Relay 节点假装移除、禁止在无性能证据时引入对象池。

- [ ] **Step 7：检查技术规格完整性**

Run:

```powershell
rg -n "TBD|TODO|待定|RelayIntegrity|ArenaController|WaveDirector|0.4 秒|1.2 秒|50%|30%" docs/superpowers/specs/2026-07-20-mobile-core-arena-technical-design.md
```

Expected: 不出现 `TBD`、`TODO` 或“待定”；`RelayIntegrity` 只出现在迁移/禁止章节；其余关键接口和数值都有明确条款。

---

### Task 3：同步 README 与素材治理

**Files:**

- Modify: `README.md`
- Modify: `game1/README.md`
- Modify: `asset_sources/README.md`
- Modify: `asset_sources/AI_PROTOTYPE_ASSETS.md`
- Modify: `asset_sources/THIRD_PARTY_ASSETS.md`
- Create: `asset_sources/MOBILE_CORE_ASSET_PLAN.md`

**Produces:** 当前项目介绍、运行入口、素材状态和首区素材批次都与无中继站方案一致。

- [ ] **Step 1：更新项目 README 当前玩法摘要**

README 必须说明：玩家坦克是唯一战斗主体；5 竞技场 × 5 波 + Boss；即时暂停升级；三核心；中继站只作为坦克内部世界观核心。

- [ ] **Step 2：保留并标记旧 AI 原型记录**

将 `relay_station.png` 的状态改为：

```markdown
已弃用的历史原型；仅保留来源与处理记录，不进入游戏、不用于新素材参考。新比例基线确认且 Alpha 02B 解除运行依赖后，删除图片文件，只保留来源与删除原因的文字记录。
```

不得删除该行，因为它是来源证据。

- [ ] **Step 3：修正第三方与 AI 需求**

从未来 AI 需求中删除“中继站”；加入三核心差异、四种城区敌人、精英规则、四种辅助系统、路障指挥车、即时属性选项和协议卡片。

- [ ] **Step 4：建立首区素材生产计划**

`MOBILE_CORE_ASSET_PLAN.md` 必须包含：

```text
Gate 0 真实游戏比例总览图
Batch 1 玩家坦克与三核心视觉差异
Batch 2 城区地形和四周入口预警
Batch 3 四种普通敌人 + 一个精英变体
Batch 4 路障指挥车及阶段部件
Batch 5 HUD、即时属性选项、协议/维护/Boss 卡片
Batch 6 炮弹、命中、冲刺、重启和奖励特效
```

每个批次都必须先生成少量样张、用户确认、再生成完整批次；记录提示词、工具、日期、尺寸、人工处理和状态。

- [ ] **Step 5：检查中继素材状态**

Run:

```powershell
rg -n "中继站|relay_station|relay-station" asset_sources README.md game1/README.md
```

Expected: 命中内容只能是历史弃用登记、禁止事项或“坦克内部中继核心”的世界观说明；不能仍把独立中继站列为待生产或当前玩法素材。

---

### Task 4：生成无中继站真实比例总览图

**Files:**

- Create: `asset_sources/ai_generated/batch-02-mobile-core/full-gameplay-proportion/README.md`
- Create: `asset_sources/ai_generated/batch-02-mobile-core/full-gameplay-proportion/mobile-core-gameplay-proportion.prompt.txt`
- Create: `asset_sources/ai_generated/batch-02-mobile-core/full-gameplay-proportion/mobile-core-gameplay-proportion.png`
- Modify: `asset_sources/AI_PROTOTYPE_ASSETS.md`

**Produces:** 一张按照真实 16:9 游戏画面组织的比例验证图，不是素材母版或概念拼贴。

- [ ] **Step 1：保存完整生成提示词**

提示词必须明确包含：

```text
480x270 logical gameplay screenshot, 16:9, top-down pixel arcade combat;
bright yellow-sand wasteland industrial arena;
one 24x24 player tank with separated hull and cyan-core turret;
normal enemies 0.8-1.0 tile, drones 0.5-0.7 tile, one elite 1.4-1.7 tiles;
one 2.3-2.5 tile roadblock commander boss shown in a separate boss-state inset;
four perimeter spawn entrances with readable warning markers;
top-left armor/reboot/core HUD, top-center wave timer, top-right arena/wave,
bottom XP/dash/two auxiliary slots;
compact three-choice stat upgrade overlay and separate full-size protocol card overlay;
heavy mechanical sci-fi tank language, bright readable sand and walls,
modular industrial UI;
no relay station, no base, no defense objective, no second health bar,
no asset-sheet layout, no isometric view, no oversized tanks.
```

- [ ] **Step 2：调用图像生成并保存原始结果**

生成数量保持最小，只生成一张候选图。不得在本步骤扩展为玩家、敌人、地图或 UI 的批量生产。

- [ ] **Step 3：视觉自检**

逐项检查：玩家约一格；普通敌人不大于玩家；Boss 不超过约 2.5 格；HUD 不遮挡入口；升级选项可读但不遮满战场；画面不存在中继站或基地。

- [ ] **Step 4：登记生成记录**

在目录 README 和 `AI_PROTOTYPE_ASSETS.md` 写入工具、日期、用途、提示词文件、原始尺寸、人工处理和“等待比例验收”状态。

- [ ] **Step 5：用户视觉门禁**

向用户展示图片，并按以下格式验收：

```text
观察完整战斗画面 → 玩家约一格、敌军密度可读、四周入口明确
观察 HUD → 装甲/重启/核心、计时、波次、经验和辅助槽不遮挡战斗
观察即时升级 → 三项紧凑、完全暂停语义明确
观察协议奖励 → 三张完整卡片用于波间构筑
扫描全图 → 不存在中继站、基地或第二生命值
```

Expected: 用户明确确认比例与布局，或给出需要修订的具体项。未确认时不得进入 Batch 1。

---

### Task 5：文档自审与 Alpha 02A 交付

**Files:**

- Create: `docs/iterations/iteration-alpha-02a-document-and-visual-baseline.md`
- Review: 本计划涉及的全部文档与图片登记

- [ ] **Step 1：建立迭代执行记录**

按 `docs/iterations/_template.md` 填写目标、非目标、权威版本、文件边界、视觉验收、偏离记录和门禁状态。

- [ ] **Step 2：占位符和冲突扫描**

Run:

```powershell
rg -n "TBD|TODO|待定|待确认|固定中继站防守|底部基地.*当前" docs/superpowers/specs/2026-07-20-mobile-core-arena-roguelite-redesign.md docs/superpowers/specs/2026-07-20-mobile-core-arena-technical-design.md docs/superpowers/plans/2026-07-20-mobile-core-arena-roadmap.md docs/superpowers/plans/2026-07-20-alpha-02a-document-and-visual-baseline.md asset_sources/MOBILE_CORE_ASSET_PLAN.md
```

Expected: 无占位符；旧玩法词只出现在历史、废止或迁移说明中。

- [ ] **Step 3：链接与工作区审计**

Run:

```powershell
git status --short
git diff --check -- README.md game1/README.md asset_sources/README.md asset_sources/AI_PROTOTYPE_ASSETS.md asset_sources/THIRD_PARTY_ASSETS.md docs/superpowers/specs/2026-07-15-roguelite-tank-design.md docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md docs/superpowers/plans/2026-07-15-roguelite-tank-mvp.md docs/superpowers/specs/2026-07-19-alpha-01a-battlefield-composition-design.md docs/superpowers/plans/2026-07-19-alpha-01a-battlefield-composition.md docs/iterations/iteration-alpha-01a-battlefield-composition.md
```

Expected: 本迭代没有越界产生新的生产代码差异；执行前已存在的 Alpha 01A 差异保持原样；获准文档无空白错误；`.superpowers/` 保持未暂存。

- [ ] **Step 4：向用户交付 Alpha 02A 验收包**

交付内容：文档替代关系、新技术设计、素材批次计划、真实比例图、自审结果和已知限制。不得把文档/图片自审描述为游戏功能已实现。

- [ ] **Step 5：等待用户验收后提交推送**

只有用户明确回复 Alpha 02A 验收通过后才执行：

```powershell
git add -- README.md game1/README.md asset_sources/README.md asset_sources/AI_PROTOTYPE_ASSETS.md asset_sources/THIRD_PARTY_ASSETS.md asset_sources/MOBILE_CORE_ASSET_PLAN.md asset_sources/ai_generated/batch-02-mobile-core/full-gameplay-proportion/README.md asset_sources/ai_generated/batch-02-mobile-core/full-gameplay-proportion/mobile-core-gameplay-proportion.prompt.txt asset_sources/ai_generated/batch-02-mobile-core/full-gameplay-proportion/mobile-core-gameplay-proportion.png docs/superpowers/specs/2026-07-15-roguelite-tank-design.md docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md docs/superpowers/specs/2026-07-19-alpha-01a-battlefield-composition-design.md docs/superpowers/specs/2026-07-20-mobile-core-arena-roguelite-redesign.md docs/superpowers/specs/2026-07-20-mobile-core-arena-technical-design.md docs/superpowers/plans/2026-07-15-roguelite-tank-mvp.md docs/superpowers/plans/2026-07-19-alpha-01a-battlefield-composition.md docs/superpowers/plans/2026-07-20-mobile-core-arena-roadmap.md docs/superpowers/plans/2026-07-20-alpha-02a-document-and-visual-baseline.md docs/iterations/iteration-alpha-01a-battlefield-composition.md docs/iterations/iteration-alpha-02a-document-and-visual-baseline.md
git commit -m "docs: 确立移动核心竞技场重构基线"
git push origin main
```

Expected: 提交只包含 Alpha 02A 获准文件，不包含 `.superpowers/` 或后续生产代码。
