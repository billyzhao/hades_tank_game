# BC-01 封锁城区正式单区流程实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把现有“首区功能纵切 + 第二竞技场占位”改成可从标题开始、连续完成五波与 Boss、进入正式胜负结算并重开的封锁城区单区试玩流程。

**Architecture:** 保留 `RunState`、`RunController`、`ArenaController`、`WaveDirector` 和多竞技场索引结构；只让 `RunController` 接收显式的可玩竞技场数量，当前组合根配置为 1。`AppRoot` 继续只装配和转发状态事实，胜负结算由 `RunPhase` 驱动，不在 Boss 回调中复制另一套通关判断。

**Tech Stack:** Godot 4.7 stable .NET、C#、游戏程序集 `net8.0`、NUnit 测试程序集 `net10.0`、Godot headless、OpenGL Compatibility、480×270 逻辑画布。

## Global Constraints

- 当前只交付封锁城区；第二至第五竞技场不制作占位代码、场景、素材或音频。
- 当前 Release 的 `playableArenaCount` 为 1，但公共类型必须支持未来 1～5 个竞技场。
- Boss 完成后进入 `RunPhase.Completed`，保存胜利摘要并显示正式结算。
- 正常流程不得依赖 Debug 验收菜单；Debug 构建继续保留快速验证能力。
- 不新增第三方依赖，不改变 Godot、.NET、渲染管线或逻辑画布。
- 不把波次、奖励、Boss 或完成规则重新硬编码进 `AppRoot`。
- 同一功能块连续实现；全部任务完成后才执行集中全量自检。
- 用户明确验收通过前不得暂存、提交或推送；`.superpowers/` 和 `asset_sources/ai_generated/batch-01-units/` 始终排除。

---

## File Structure

### 新建

- `game1/scripts/ui/StartScreen.cs`：标题、开始和退出请求，只负责界面与信号。
- `game1/tests/headless/SingleArenaFlowTestHost.cs`：真实 Godot 运行时中的单区完成、胜利结算和开始界面契约。
- `game1/tests/headless/single_arena_flow_test_host.tscn`：单区流程测试宿主场景。
- `docs/iterations/iteration-bc01-single-arena-flow.md`：实施范围、测试证据和用户验收记录。

### 修改

- `game1/scripts/run/RunController.cs`：显式可玩竞技场数量与最后竞技场完成裁决。
- `game1/scripts/app/AppRoot.cs`：标题入口、单区配置、胜负结算、重开和 Debug/Release 边界。
- `game1/scripts/ui/PauseCoordinator.cs`：增加标题界面暂停原因。
- `game1/scripts/ui/PauseController.cs`：标题或结果界面持有暂停时不显示战术暂停遮罩。
- `game1/scripts/ui/RunResultScreen.cs`：封锁城区胜负文案、核心/等级/耗时摘要和退出请求。
- `game1/scenes/app/main.tscn`：HUD 从“竞技场 1/5”改为“封锁城区”，移除玩家可见占位文案。
- `game1/tests/headless/ProtocolRuntimeTestHost.cs`：配置 1/5 个竞技场的顶层状态回归。
- `game1/tests/headless/AcceptanceMenuTestHost.cs`：Debug 验收菜单继续可用的回归。
- `game1/tests/integration/MvpTestRunner.cs`：适配 `RunController` 新构造签名。
- `README.md`、`game1/README.md`：更新 BC-01 实际启动和完成流程。

---

### Task 1: 可配置单区顶层状态机

**Files:**
- Modify: `game1/scripts/run/RunController.cs`
- Modify: `game1/tests/headless/ProtocolRuntimeTestHost.cs`
- Modify: `game1/tests/integration/MvpTestRunner.cs`

**Interfaces:**
- Consumes: `RunState.ArenaIndex`、`RunState.AdvanceArena()`、`RunState.RestoreArmorForNextArena()`。
- Produces: `RunController(RunState state, BuildController build, int playableArenaCount)`；最后一个已配置竞技场完成后 `Phase == RunPhase.Completed`，否则发出 `ArenaRequested`。

- [x] **Step 1: 写入单区完成红灯测试**

在 `ProtocolRuntimeTestHost` 增加并注册：

```csharp
private static void RunSingleArenaCompletionEndsRun()
{
    RunState state = RunState.CreateNew(seed: 42);
    RunController run = CreateRunController(state, playableArenaCount: 1);
    int arenaRequests = 0;
    run.ArenaRequested += _ => arenaRequests++;

    run.OnArenaCompleted();

    Assert(run.Phase == RunPhase.Completed, "封锁城区是当前最后竞技场，完成后必须结束本局。");
    Assert(state.ArenaIndex == 0, "单区完成不得推进到未交付的竞技场 2。");
    Assert(arenaRequests == 0, "单区完成不得请求第二竞技场占位。");
}
```

保留多区扩展回归，并把现有推进测试改为显式 `playableArenaCount: 5`。

- [x] **Step 2: 运行红灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_encounter
```

Expected: 构建或测试因 `RunController` 尚无 `playableArenaCount` 参数而失败。

- [x] **Step 3: 实现最小状态机修改**

`RunController` 使用以下字段和判断：

```csharp
private readonly RunState _state;
private readonly int _playableArenaCount;

public RunController(RunState state, BuildController build, int playableArenaCount)
{
    _state = state ?? throw new ArgumentNullException(nameof(state));
    ArgumentNullException.ThrowIfNull(build);
    if (playableArenaCount is < 1 or > 5)
        throw new ArgumentOutOfRangeException(nameof(playableArenaCount));
    if (_state.ArenaIndex >= playableArenaCount)
        throw new ArgumentException("当前竞技场索引超出可玩竞技场配置。", nameof(state));

    _playableArenaCount = playableArenaCount;
    Phase = RunPhase.Arena;
}

public void OnArenaCompleted()
{
    if (Phase != RunPhase.Arena)
        throw new InvalidOperationException("只有进行中的竞技场可以完成。");

    if (_state.ArenaIndex + 1 >= _playableArenaCount)
    {
        SetPhase(RunPhase.Completed);
        return;
    }

    _state.RestoreArmorForNextArena();
    _state.AdvanceArena();
    ArenaRequested?.Invoke(_state.ArenaIndex);
}
```

所有构造点必须显式传入 1 或 5，不保留隐藏默认值。

- [x] **Step 4: 运行定向绿灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_encounter
```

Expected: `run_single_arena_completion_ends_run` 和现有五区推进契约全部 PASS。

---

### Task 2: Boss 后正式胜利结算

**Files:**
- Modify: `game1/scripts/app/AppRoot.cs`
- Modify: `game1/scripts/ui/RunResultScreen.cs`
- Create: `game1/tests/headless/SingleArenaFlowTestHost.cs`
- Create: `game1/tests/headless/single_arena_flow_test_host.tscn`

**Interfaces:**
- Consumes: `RunController.PhaseChanged`、`ArenaController.OnBossDefeated()`、`RunResultSnapshot`、`SaveLastRun()`。
- Produces: `ShowRunVictory()`；胜利时结果界面可见并保存 `Result = "victory"`。

- [x] **Step 1: 写入胜利结果红灯测试**

测试宿主模拟五波和 Boss，验证顶层结果：

```csharp
RunState state = RunState.CreateNew(seed: 20260724);
BuildController build = new(state, CreateCatalog());
RunController run = new(state, build, playableArenaCount: 1);
ArenaController arena = new(state);
arena.BeginArena(WaveSchedule.CreateApproved(), arenaIndex: 0);
arena.OnIntroFinished();

for (int wave = 0; wave < 5; wave++)
{
    arena.OnWaveSpawnWindowEnded();
    arena.OnAllEnemiesCleared();
    arena.ConfirmReward($"bc01_wave_{wave + 1}");
}

arena.OnBossStarted();
arena.OnBossDefeated();
run.OnArenaCompleted();

Assert(run.Phase == RunPhase.Completed, "Boss 完成后必须结束封锁城区本局。");
Assert(state.ArenaIndex == 0, "胜利结算不得推进未交付竞技场。");
```

实例化 `RunResultScreen` 后调用 `ShowResult(snapshot, true)`，断言 `Visible`、标题包含“封锁城区突破”和摘要包含核心、等级与耗时。

- [x] **Step 2: 运行红灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/single_arena_flow_test_host.tscn
```

Expected: 因结果标题/摘要和 `AppRoot` 胜利流程尚未实现而 FAIL。

- [x] **Step 3: 让 RunPhase 统一驱动胜负**

`AppRoot.OnRunPhaseChanged` 改为：

```csharp
private void OnRunPhaseChanged(RunPhase phase)
{
    switch (phase)
    {
        case RunPhase.Completed:
            ShowRunVictory();
            break;
        case RunPhase.Failed:
            _arenaController?.OnPlayerRunFailed();
            ShowRunFailure();
            break;
    }
}
```

Boss 回调只上报竞技场完成事实：

```csharp
private void ShowBossResult()
{
    if (_resultShown) return;
    _activeBoss = null;
    _arenaController.OnBossDefeated();
    ClearCombatActors();
    CurrentRun.RestoreArmorForNextArena();
    _playerHealth.SetArmor(CurrentRun.PlayerArmor);
    _runController.OnArenaCompleted();
}
```

删除 `ShowNextArenaPlaceholder()` 和 `ArenaRequested += ShowNextArenaPlaceholder`。当前 `AppRoot` 使用：

```csharp
_runController = new RunController(CurrentRun, _buildController, playableArenaCount: 1);
```

- [x] **Step 4: 实现胜利结果**

```csharp
private void ShowRunVictory()
{
    if (_resultShown) return;
    _resultShown = true;
    _bossHud.Unbind();
    ClearCombatActors();
    _waveRewardPanel.Hide();
    RunResultSnapshot snapshot = CreateResultSnapshot();
    SaveLastRun(snapshot, "victory");
    _runResultScreen.ShowResult(snapshot, true);
    _waveLabel.Text = "封锁城区  已完成";
    _arenaLabel.Text = "封锁城区";
    _eventLabel.Text = "封锁城区突破完成：可重新开始挑战其他核心与构筑。";
}
```

`CreateResultSnapshot()` 必须写入真实核心：

```csharp
string coreId = CurrentRun.SelectedCore?.ToString() ?? string.Empty;
return new RunResultSnapshot(
    CurrentRun.Seed,
    CurrentRun.SelectedProtocolIds,
    coreId,
    CurrentRun.ArenaIndex,
    CurrentRun.WaveIndex,
    CurrentRun.Level,
    TimeSpan.FromMilliseconds(elapsedMsec));
```

`RunResultScreen` 的胜利标题改为“封锁城区突破”，摘要增加核心，并把重试提示改为“从核心选择重新开始”。

- [x] **Step 5: 运行定向绿灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/single_arena_flow_test_host.tscn
```

Expected: `[PASS] bc01_single_arena_flow`。

---

### Task 3: 标题、开始和退出入口

**Files:**
- Create: `game1/scripts/ui/StartScreen.cs`
- Modify: `game1/scripts/app/AppRoot.cs`
- Modify: `game1/scripts/ui/PauseCoordinator.cs`
- Modify: `game1/scripts/ui/PauseController.cs`
- Modify: `game1/tests/headless/SingleArenaFlowTestHost.cs`

**Interfaces:**
- Consumes: `PauseCoordinator.Acquire/Release()`、`ShowCoreSelection()`。
- Produces: `StartScreen.StartRequested`、`StartScreen.QuitRequested`、`PauseReason.StartScreen`。

- [x] **Step 1: 写入开始界面红灯**

在测试宿主实例化 `StartScreen`，等待一帧后验证：

```csharp
StartScreen screen = new();
AddChild(screen);
await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

Assert(screen.Visible, "标题界面初始必须可见。");
Assert(screen.GetNode<Button>("Panel/StartButton").Text == "进入封锁城区",
    "开始按钮必须说明当前交付内容。");
Assert(screen.GetNode<Button>("Panel/QuitButton").Text == "退出游戏",
    "标题界面必须提供退出入口。");
```

- [x] **Step 2: 运行红灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/single_arena_flow_test_host.tscn
```

Expected: `StartScreen` 类型不存在，测试 FAIL。

- [x] **Step 3: 实现 StartScreen**

`StartScreen` 使用全屏 `Control`，内部节点固定命名为 `Panel`、`TitleLabel`、`StartButton`、`QuitButton`；按钮只发信号：

```csharp
[Signal] public delegate void StartRequestedEventHandler();
[Signal] public delegate void QuitRequestedEventHandler();

public override void _Ready()
{
    SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    ProcessMode = ProcessModeEnum.Always;
    MouseFilter = MouseFilterEnum.Stop;

    ColorRect shade = new()
    {
        Name = "Panel",
        Color = new Color(0.02f, 0.025f, 0.035f, 0.94f),
        AnchorRight = 1f,
        AnchorBottom = 1f
    };
    AddChild(shade);

    Label title = new()
    {
        Name = "TitleLabel",
        Text = "废土中继\n封锁城区",
        Position = new Vector2(130, 62),
        Size = new Vector2(220, 70),
        HorizontalAlignment = HorizontalAlignment.Center
    };
    shade.AddChild(title);

    Button start = new()
    {
        Name = "StartButton",
        Text = "进入封锁城区",
        Position = new Vector2(165, 150),
        Size = new Vector2(150, 30)
    };
    start.Pressed += () => EmitSignal(SignalName.StartRequested);
    shade.AddChild(start);

    Button quit = new()
    {
        Name = "QuitButton",
        Text = "退出游戏",
        Position = new Vector2(165, 188),
        Size = new Vector2(150, 30)
    };
    quit.Pressed += () => EmitSignal(SignalName.QuitRequested);
    shade.AddChild(quit);
}
```

节点路径以实际父子结构为准固定为 `Panel/StartButton` 和 `Panel/QuitButton`；不得让测试依赖按钮索引。

- [x] **Step 4: 接入 AppRoot 与暂停所有权**

`PauseReason` 增加 `StartScreen`。`AppRoot` 创建界面时：

```csharp
_startScreen = new StartScreen();
ui.AddChild(_startScreen);
_startScreen.StartRequested += BeginRunFromTitle;
_startScreen.QuitRequested += () => GetTree().Quit();
_pauseCoordinator.Acquire(PauseReason.StartScreen);
```

开始事件先取得核心选择暂停，再释放标题暂停，避免中间一帧解锁战斗：

```csharp
private void BeginRunFromTitle()
{
    _pauseCoordinator.Acquire(PauseReason.CoreSelection);
    _startScreen.Visible = false;
    _coreSelectionPanel.ShowChoices(MobileCoreCatalog);
    _pauseCoordinator.Release(PauseReason.StartScreen);
    _eventLabel.Text = "开局整备：选择移动核心后进入第一波";
}
```

移除 `_Ready()` 末尾直接调用 `ShowCoreSelection()`。`PauseController` 把 `PauseReason.StartScreen` 视为由专属 UI 拥有的暂停，不叠加战术暂停遮罩。

- [x] **Step 5: 运行绿灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/single_arena_flow_test_host.tscn
```

Expected: 开始界面、信号和暂停所有权契约 PASS。

---

### Task 4: 正式 HUD 与 Debug/Release 边界

**Files:**
- Modify: `game1/scenes/app/main.tscn`
- Modify: `game1/scripts/app/AppRoot.cs`
- Modify: `game1/scripts/ui/RunResultScreen.cs`
- Modify: `game1/tests/headless/AcceptanceMenuTestHost.cs`
- Modify: `game1/tests/headless/HudLayoutTestHost.cs`

**Interfaces:**
- Consumes: `OS.IsDebugBuild()`、现有 HUD 节点。
- Produces: 正式 HUD 只显示“封锁城区”和五波状态；Debug 构建保留验收菜单，Release 不展示验收入口。

- [x] **Step 1: 写入 HUD 与 Debug 红灯**

在 HUD 测试中断言：

```csharp
Assert(arenaLabel.Text == "封锁城区", "单区试玩不得继续显示竞技场 1/5。");
Assert(!arenaLabel.Text.Contains("/5"), "正式 HUD 不得暗示四张尚未交付的地图。");
```

验收菜单测试继续验证其全部按钮和信号在 Debug headless 中存在，避免隐藏 Release 菜单时误删策划工具。

- [x] **Step 2: 运行红灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/hud_layout_test_host.tscn
```

Expected: 当前文本仍为“竞技场 1/5”，测试 FAIL。

- [x] **Step 3: 修改正式文案和显示策略**

`main.tscn` 与 `UpdateHud()` 统一使用：

```csharp
_arenaLabel.Text = "封锁城区";
```

删除“Boss 占位已解锁”“Alpha 03 接入”“下一竞技场”等玩家可见文本。创建 UI 后：

```csharp
bool debugBuild = OS.IsDebugBuild();
_acceptanceMenu.Visible = debugBuild;
_debugOverlay.Visible = debugBuild && _debugOverlay.Visible;
```

F8 只能在 Debug 构建切换调试层；Release 不得通过快捷键显示验收菜单或 DebugOverlay。

- [x] **Step 4: 运行定向绿灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/hud_layout_test_host.tscn
godot --headless --path game1 --scene res://tests/headless/acceptance_menu_test_host.tscn
```

Expected: `[PASS] hud_compact_layout`、`[PASS] acceptance_menu_commands`。

---

### Task 5: BC-01 集中自检与策划验收

**Files:**
- Create: `docs/iterations/iteration-bc01-single-arena-flow.md`
- Modify: `README.md`
- Modify: `game1/README.md`

**Interfaces:**
- Consumes: Tasks 1～4 的单区流程。
- Produces: 可由用户按步骤验收的 BC-01 构建状态；不提交、不推送。

- [x] **Step 1: 更新执行记录**

执行记录必须写明：

```markdown
## 目标
- 正式标题进入核心选择；
- 完成封锁城区后直接显示胜利结算；
- 失败和胜利均可重开；
- Release 不显示 Debug 验收入口。

## 非目标
- 不调整三核心数值；
- 不新增敌军、特效、UI 正式图或音频；
- 不制作竞技场 2～5；
- 不执行 BC-05 完整性能优化。
```

- [x] **Step 2: 运行完整 C# 门禁**

Run:

```powershell
dotnet test GodotTank.sln --no-restore --nologo
dotnet build GodotTank.sln --no-restore --nologo
```

Expected: 全部测试通过，构建 0 warning / 0 error。

- [x] **Step 3: 运行全部相关 Godot headless**

Run:

```powershell
godot --headless --path game1 --scene res://tests/headless/single_arena_flow_test_host.tscn
godot --headless --path game1 --scene res://tests/headless/arena_wave_test_host.tscn
godot --headless --path game1 --scene res://tests/headless/acceptance_menu_test_host.tscn
godot --headless --path game1 --scene res://tests/headless/hud_layout_test_host.tscn
godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_encounter
godot --headless --path game1 --scene res://tests/integration/mvp_test_runner.tscn
godot --headless --path game1 --editor --quit
godot --headless --path game1 --scene res://scenes/app/main.tscn --quit-after 180
```

Expected: 所有宿主 PASS，编辑器解析和主场景启动退出码为 0。

- [x] **Step 4: Godot 可见自检**

从 `D:\my program\codex\godot\game1\project.godot` 启动，按以下路径检查：

1. 启动后只显示标题界面，点击“进入封锁城区”才打开核心选择；
2. 选择核心后进入第 1 波，HUD 显示“封锁城区”而非“1/5”；
3. 使用 Debug 验收菜单推进到第 5 波、精英和 Boss；
4. 击败 Boss 后出现“封锁城区突破”结算，显示核心、协议、等级和耗时；
5. 点击“重试本局”重新回到标题/核心选择；
6. 触发无重启失败后出现失败结算并可重开；
7. 关闭 Debug 构建验收路径不影响正常流程。

- [x] **Step 5: 差异与文档门禁**

Run:

```powershell
git diff --check
git status --short
rg -n "竞技场 2|下一竞技场|Alpha 03|1/5" game1/scripts game1/scenes
```

Expected: 无空白错误；生产玩家文案不存在第二竞技场占位；`.superpowers/` 和 `batch-01-units/` 保持未跟踪且不进入变更范围。

- [x] **Step 6: 交付用户验收**

只汇报：

- 完成内容；
- 修改文件；
- 自动化与 Godot 自检结果；
- 操作式验收标准；
- 已知限制；
- BC-02 预计时间。

用户明确验收通过后，才按精确路径暂存、提交并推送 `main`。
