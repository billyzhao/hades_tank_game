# Project Governance Baseline Reconciliation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将已确认的多智能体协同治理方案落入项目级指令、迭代模板和现有文档基线，使后续迭代能够从一致、可审计的方案与状态开始。

**Architecture:** 在仓库根目录使用 `AGENTS.md` 作为所有智能体的项目级入口，但不复制完整策划或技术方案；它只声明权威文档、角色权限、双门禁、偏离流程和仓库门禁。历史执行状态回写到原开发计划，已经发生的原型架构简化单独登记为受限暂缓项，并在策划、技术、素材和 README 文档中建立交叉链接。

**Tech Stack:** Markdown、Git、Godot 4.7 .NET、C#/.NET 8 游戏项目、.NET 10 NUnit 测试项目、PowerShell 验证命令。

## Global Constraints

- 本计划只修改文档和项目级智能体说明，不修改游戏生产代码、场景、测试逻辑或素材文件。
- 主智能体是唯一面向用户、裁决架构、执行最终提交和推送的角色。
- 开发智能体没有设计权；任何实质偏离都必须暂停、获得用户确认、先更新权威文档，再继续开发。
- 历史架构简化只能登记为当前单房间原型的受限暂缓，不能成为后续智能体继续偏离技术设计的授权。
- 不伪造缺失的素材提示词、许可证、测试结果、验收记录或历史完成证据。
- 只有用户明确验收通过后才执行 `git add`、`git commit` 和 `git push`。
- 当前本地 `main` 已比 `origin/main` 超前两个既有提交；本计划不得重写、拆分或丢弃这些提交。
- 所有新文档使用 UTF-8，中文说明为主，路径使用仓库相对路径。

## Source Documents

- 协同治理设计：`docs/superpowers/specs/2026-07-17-agent-collaboration-governance-design.md`
- 游戏策划：`docs/superpowers/specs/2026-07-15-roguelite-tank-design.md`
- 技术设计：`docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md`
- MVP 计划：`docs/superpowers/plans/2026-07-15-roguelite-tank-mvp.md`
- 表现计划：`docs/superpowers/plans/2026-07-16-combat-presentation-prototype.md`
- 素材流程：`asset_sources/README.md`
- 第三方候选：`asset_sources/THIRD_PARTY_ASSETS.md`
- AI 原型登记：`asset_sources/AI_PROTOTYPE_ASSETS.md`

---

### Task 1: 建立项目级智能体入口与迭代模板

**Files:**
- Create: `AGENTS.md`
- Create: `docs/iterations/README.md`
- Create: `docs/iterations/_template.md`
- Reference: `docs/superpowers/specs/2026-07-17-agent-collaboration-governance-design.md`

`.superpowers/sdd/` 下的任务简报、实施报告和审查包是控制器临时证据，不属于项目交付文件、不得暂存提交，也不计入上述产品文件写入边界。

**Interfaces:**
- Produces: 所有后续智能体进入项目时必须读取的治理入口。
- Produces: 每个迭代复制后填写的单文件执行台账模板。
- Consumes: 已确认的角色边界、双门禁、偏离门禁和仓库门禁。

- [x] **Step 1: 创建根目录 `AGENTS.md`**

文件必须包含以下明确条款，不能只链接而不摘要关键禁令：

```markdown
# 《废土中继》项目智能体规则

## 权威文档

开始任何任务前，必须读取与任务相关的已确认策划、技术、计划、素材和验收文档。完整协同规则见：
`docs/superpowers/specs/2026-07-17-agent-collaboration-governance-design.md`。

## 强制规则

- 主智能体承担总制作人、主策、主美把关和主程序/架构负责人的最终责任。
- 每个迭代重新创建角色智能体；子智能体只执行任务合同，没有改变方案的权力。
- 开发智能体没有设计权，不得用硬编码、临时替代或范围削减绕开已确认技术架构。
- 状态、数据驱动、导航、存档和跨模块重构等高风险任务必须增加只读架构审查智能体。
- 任何玩法、范围、架构、依赖、素材授权、验收标准或迭代顺序的实质偏离都必须立即暂停并请求用户确认。
- 用户确认偏离后，必须先更新相关权威文档，再继续实现。
- 采用策划/测试前置门禁和架构/测试/策划后置门禁。
- 同一时间不允许两个智能体修改同一组文件；写入范围必须在任务合同中列明。
- 自动化测试通过不能替代 Godot 运行验证和策划可见验收。
- 用户明确验收通过前，不得提交或推送；只有主智能体可以执行最终仓库操作。

## 进度沟通

主智能体在迭代启动、门禁完成、发现问题、发生偏离和验证完成时主动汇报；长时间操作原则上不超过约 60 秒没有进度更新。
```

- [x] **Step 2: 创建 `docs/iterations/README.md`**

说明迭代记录的目的、命名和生命周期：

```markdown
# 迭代执行记录

每个正式开发迭代从 `_template.md` 复制一份记录，命名为 `iteration-NN-<topic>.md`。记录只保存执行证据，不替代策划、技术或开发计划；方案发生变化时必须同步修改真正的权威文档。

状态顺序固定为：策划前置 → 测试前置 → 开发理解 → 开发 → 架构合规 → 测试后置 → 策划复验 → 主智能体自检 → 用户验收 → 提交/推送。
```

- [x] **Step 3: 创建 `docs/iterations/_template.md`**

模板必须实际包含以下栏目，而不是写“按需填写”：

```markdown
# Iteration NN: <名称>

## 目标与非目标
## 权威文档版本
## 策划验收标准
## 开发任务合同
### 必须实现
### 必须保持不变
### 禁止实现方式
### 允许/禁止修改的文件
## 测试矩阵
## 开发理解回执
## 架构合规矩阵
| 架构条款 | 实现文件/接口 | 验证证据 | 状态 |
|---|---|---|---|
## 偏离记录
## 门禁与进度
- [ ] 策划前置
- [ ] 测试前置
- [ ] 开发理解
- [ ] 开发
- [ ] 架构合规
- [ ] 测试后置
- [ ] 策划复验
- [ ] 主智能体自检
- [ ] 用户验收
- [ ] 提交/推送
## 工作区与仓库状态
- 当前分支：
- 工作区差异：
- 暂存状态：
- 本地提交状态：
- 推送状态：
## 自动化与运行验证
## 策划复验
## 用户验收
## 提交与推送
```

- [x] **Step 4: 验证治理入口完整性**

Run:

```powershell
rg -n "开发智能体没有设计权|实质偏离|用户明确验收通过前|60 秒" AGENTS.md
rg -n "策划前置|测试前置|开发理解|架构合规|测试后置|用户验收|提交/推送|工作区与仓库状态|暂存状态" docs/iterations/_template.md
```

Expected: 第一条至少返回 4 个强制规则位置；第二条返回完整门禁生命周期和仓库状态栏目。

---

### Task 2: 回填历史进度并登记原型期架构暂缓项

**Files:**
- Create: `docs/decisions/0001-prototype-architecture-deferrals.md`
- Modify: `docs/superpowers/specs/2026-07-15-roguelite-tank-design.md`
- Modify: `docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md`
- Modify: `docs/superpowers/plans/2026-07-15-roguelite-tank-mvp.md`
- Modify: `docs/superpowers/plans/2026-07-16-combat-presentation-prototype.md`

**Interfaces:**
- Produces: 当前阶段的可信状态基线。
- Produces: 已发生偏差的边界、原因和强制偿还门槛。
- Consumes: 已存在代码、测试、用户验收和 Git 提交证据。

- [x] **Step 1: 创建架构暂缓决策记录**

`docs/decisions/0001-prototype-architecture-deferrals.md` 必须明确写明：

```markdown
# 0001：单房间原型架构暂缓项

状态：已登记的历史原型简化；仅适用于当前单房间守点与表现验证，不授权未来继续偏离。

## 已发生的简化

1. 砖墙使用独立 `StaticBody2D`，未采用 `TileMapLayer` 运行时格子耐久。
2. 敌军使用 `DefenseRoutePlanner` 固定通路，未采用 `AStarGrid2D`。
3. 敌军职责集中在 `EnemyTank` + `BehaviorId`，尚未采用 `EnemyDefinition` Resource 与独立行为策略。
4. 房间状态目前只有 Loading/Combat/Cleared/Failed，尚未接入 Reward/Exiting。
5. 重启流程尚未实现敌方炮弹清理和重启后短暂无敌。
6. 部分表现参数仍硬编码，尚未集中到表现配置 Resource。

## 强制偿还门槛

- 迭代 6 接入三选一时，必须按技术文档实现 `ProtocolDefinition`、运行时构筑作用域、确定性奖励和 Reward 房间阶段；不得沿用硬编码奖励。
- 添加第二个不同布局的战斗房间前，必须重新确认并落地 `RoomDefinition`、TileMap 地形和可更新导航方案。
- 扩充普通敌人池前，必须将敌人属性和职责迁移到 `EnemyDefinition` Resource/策略边界。
- 引入敌方实体炮弹前，必须补齐重启清弹和短暂无敌。
- 扩充成套 VFX/音频参数前，必须建立集中、可替换的表现配置资源。

任何门槛的变更都属于实质偏离，必须再次获得用户确认并更新技术设计。
```

- [x] **Step 2: 在策划文档添加治理与当前阶段说明**

在游戏策划末尾增加“项目治理与当前阶段”章节，写明：

- 当前主线处于 Task 5 完成、Task 6 开始前；
- 混合表现专项已插入并通过用户验收；
- 玩法方向未发生变化；
- 后续实质偏离必须遵守协同治理设计；
- 链接协同治理设计和架构暂缓决策记录。

- [x] **Step 3: 在技术设计添加当前实现符合性章节**

新增“当前实现基线与受限暂缓项”，将技术条款分为：

- 已符合：Godot 4.7 .NET、net8/net10、480×270、Compatibility、RunState、扫掠炮弹、统一伤害、表现与规则分离；
- 当前原型简化：引用决策记录中的 6 项；
- 后续强制门槛：逐条引用决策记录，不在技术文档中写出不同版本；
- 治理入口：链接协同治理设计和根目录 `AGENTS.md`。

- [x] **Step 4: 回填 MVP 计划状态**

在计划的 Iteration Overview 增加“2026-07-17 基线状态”列，并按证据更新：

- Task 1：完成；原计划 6 个步骤标记 `[x]`；
- Task 2：完成；原计划 5 个步骤标记 `[x]`；
- Task 3：完成；原计划 5 个步骤标记 `[x]`；
- Task 4：功能验收完成、架构部分完成；Step 1–3 标记 `[x]`，Step 4–6 保持 `[ ]` 并在步骤下注明 StaticBody/重启细节差距及决策记录链接；
- Task 5：玩法验收完成、架构部分完成；Step 1 和 Step 4 标记 `[x]`，Step 2、3、5、6 保持 `[ ]` 并注明集中式敌军逻辑、固定通路和精简状态机差距；
- Task 6：未开始；
- Task 7：未开始；
- Task 8：部分提前完成，注明视觉、最小音效、HUD 和验收文档已由表现专项覆盖，但存档、集成 runner、压力测试和完整短局未完成。

不得把“用户看到玩法可运行”写成原技术步骤全部完成。

- [x] **Step 5: 回填表现专项计划状态**

增加基线状态说明，并按实际证据更新：

- Task 1 Step 1–4：`[x]`；
- Task 2 Step 1–5：`[x]`；
- Task 3 Step 1、2、4、5：`[x]`；Step 3 保持 `[ ]`，注明当前使用运行时原型 SFX，尚未完成可替换音频引用资源化；
- Task 4 Step 2–4：`[x]`；Step 1 保持 `[ ]`，注明空资源降级和生命周期清理尚缺独立回归证据；
- 在文件末尾链接实际验收文档和 AI 原型登记。

- [x] **Step 6: 验证历史状态没有被过度标记**

Run:

```powershell
rg -n "Task 4|Task 5|架构部分完成|StaticBody|AStarGrid2D|DefenseRoutePlanner" docs/superpowers/plans/2026-07-15-roguelite-tank-mvp.md docs/decisions/0001-prototype-architecture-deferrals.md
rg -n "运行时原型 SFX|空资源降级|AI_PROTOTYPE_ASSETS" docs/superpowers/plans/2026-07-16-combat-presentation-prototype.md
```

Expected: 原型简化、未完成门槛和对应文件均可检索；Task 4/5 不得被描述为原技术方案全部完成。

---

### Task 3: 同步项目入口、操作说明与素材台账

**Files:**
- Create: `README.md`
- Modify: `game1/README.md`
- Modify: `asset_sources/README.md`
- Modify: `asset_sources/AI_PROTOTYPE_ASSETS.md`
- Verify only: `asset_sources/THIRD_PARTY_ASSETS.md`

**Interfaces:**
- Produces: 仓库访问者和新智能体的统一文档入口。
- Produces: 与当前原型一致的运行、操作和素材状态。

- [x] **Step 1: 创建仓库根目录 README**

`README.md` 必须包含：

- 项目一句话定位；
- 当前阶段：Task 5 + 混合表现完成，Task 6 尚未开始；
- Godot 项目路径 `game1/project.godot`；
- Godot 4.7 .NET、.NET 8 游戏与 .NET 10 测试要求；
- 构建、测试和启动命令；
- 策划、技术、开发计划、协同治理、迭代记录、验收和素材文档索引；
- 仓库规则：用户验收后才提交/推送。

- [x] **Step 2: 更新 `game1/README.md` 的可玩内容和操作**

补充当前三类敌人、三波守点、中继站、重启和表现状态，并列出完整原型操作：

```text
WASD/方向键：移动
鼠标/右摇杆：独立瞄准
鼠标左键：射击
空格：冲刺
Z：验收用玩家伤害
X：验收用中继站伤害
M：切换 SFX 静音
```

明确 Z/X 是原型验收入口，不是正式玩家功能。

- [x] **Step 3: 修正素材流程的当前状态**

将 `asset_sources/README.md` 中“尚未生成任何素材”改为：

- 第三方候选仍未下载或购买；
- 已生成并登记一批内部 AI 像素原型；
- AI 原型不视为最终商用素材；
- 未来下载/购买仍必须先登记和获得用户确认。

- [x] **Step 4: 记录 AI 提示词元数据缺口**

在 `asset_sources/AI_PROTOTYPE_ASSETS.md` 增加说明：

```markdown
当前精确提示词仅存在于原始 Codex 对话，仓库内没有可验证的完整提示词副本。不得根据成品反向编造提示词；若原型继续使用，应在正式素材替换前重新生成并把提示词、模型、日期和处理步骤保存到仓库外源文件区。
```

保持现有文件、工具、日期、用途和人工处理登记不变。

- [x] **Step 5: 确认第三方候选状态没有被误改**

Run:

```powershell
rg -n "未下载|待用户购买|免费候选" asset_sources/THIRD_PARTY_ASSETS.md
rg -n "AI 原型|未下载或购买|不得.*编造提示词" asset_sources/README.md asset_sources/AI_PROTOTYPE_ASSETS.md
```

Expected: 第三方条目仍保持未下载/待购买；AI 原型状态和提示词缺口被明确登记。

---

### Task 4: 完整验证、策划式文档验收与仓库门禁

**Files:**
- Verify: all files changed by Tasks 1–3
- Update status only after verification: `docs/superpowers/plans/2026-07-17-governance-baseline-reconciliation.md`

**Interfaces:**
- Produces: 可供用户审核的文档基线整理结果。
- Produces after user acceptance only: one documentation commit on local `main` and a push attempt to `origin/main`.

- [x] **Step 1: 文档完整性与链接检查**

Run:

```powershell
rg -n "TBD|TODO|待定|稍后决定" AGENTS.md README.md docs/iterations docs/decisions docs/superpowers/specs/2026-07-17-agent-collaboration-governance-design.md
rg -n "agent-collaboration-governance-design|prototype-architecture-deferrals|iterations/_template" AGENTS.md README.md docs/superpowers/specs docs/superpowers/plans
git diff --check
```

Expected: 第一条不返回真实占位内容；关键文档互相可追踪；`git diff --check` exit 0。

- [x] **Step 2: 敏感信息和仓库状态检查**

Run:

```powershell
rg -n --hidden -g '!game1/.godot/**' -g '!**/bin/**' -g '!**/obj/**' "github_p[a]t_|g[h]p_|B[E]GIN (RSA|OPENSSH|EC) PRIVATE KEY" .
git status --short --branch
git rev-list --left-right --count origin/main...main
```

Expected: 敏感信息搜索无结果；只出现本计划预期的文档改动；本地 `main` 仍保持既有超前提交，没有历史重写。

- [x] **Step 3: 新鲜项目回归验证**

Run:

```powershell
dotnet test GodotTank.sln --nologo
dotnet build game1/Game1.csproj --nologo
dotnet build game1/Game1.csproj -c Release --nologo
godot --headless --path game1 --editor --quit
godot --headless --path game1 --fixed-fps 60 --quit-after 360
```

Expected: 29/29 或更多测试通过；Debug/Release 均为 0 错误且无新增警告；Godot 编辑器解析和 360 固定物理帧运行均 exit 0。

- [x] **Step 4: 主智能体按文档验收路径自检**

操作与预期：

1. 打开根 `README.md` → 能在两次点击内找到策划、技术、MVP 计划、治理设计、迭代模板和素材登记；
2. 打开 `AGENTS.md` → 能直接看到开发智能体无设计权、偏离确认和验收前不提交/推送；
3. 打开 MVP 计划 → Task 1–3 已完成，Task 4/5 明确为功能完成但架构部分完成；
4. 打开架构暂缓决策 → 每个历史简化都有强制偿还门槛；
5. 打开素材 README → 不再声称尚未生成 AI 素材，第三方资源仍显示未下载；
6. 打开迭代模板 → 能按双门禁流程创建下一次迭代记录。

- [x] **Step 5: 交给用户验收**

报告必须包含：

- 修改文件清单；
- 历史状态如何回填；
- 6 项原型架构暂缓和各自偿还门槛；
- 新智能体约束如何阻止开发自由发挥；
- 文档、敏感信息、测试、构建、Godot 验证结果；
- 当前未提交、未推送状态。

Stop: 等待用户明确回复验收通过。

- [ ] **Step 6: 用户验收后提交并推送**

只有收到明确验收通过后运行：

```powershell
git add AGENTS.md README.md asset_sources/README.md asset_sources/AI_PROTOTYPE_ASSETS.md game1/README.md docs/iterations docs/decisions docs/superpowers/specs/2026-07-15-roguelite-tank-design.md docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md docs/superpowers/specs/2026-07-17-agent-collaboration-governance-design.md docs/superpowers/plans/2026-07-15-roguelite-tank-mvp.md docs/superpowers/plans/2026-07-16-combat-presentation-prototype.md docs/superpowers/plans/2026-07-17-governance-baseline-reconciliation.md
git commit -m "docs: 建立多智能体治理与项目基线"
git push origin main
```

Expected: commit succeeds; push success depends on GitHub network availability. If push fails, keep the verified local commit, report the exact network error, and do not rewrite history or repeat commits.

## Review Checkpoint After Every Task

每个 Task 结束时，主智能体必须报告已修改文件、验证结果、发现的文档冲突、是否存在实质偏离和下一 Task 的前提是否仍成立。Task 1–3 属于文档实施，不允许修改游戏代码；任何意外代码或场景差异都必须立即停止并审计。
