# BC-04：音频、手感与玩家体验

## 状态

- 开发：完成
- 音频批次：Batch 09 已生成、接入并确认
- 集中自检：通过
- 用户验收：2026-07-27 通过
- 提交/推送：已获授权，随本记录提交

## 验收范围

1. 玩家、四类敌军、精英和 Boss 的关键动作具有可辨识声音；
2. 五波、Boss、环境和 UI 具有统一且不过载的音频层级；
3. 相机震动、短停顿、受击和低装甲提示按事件重要度分级；
4. 暂停、焦点、HUD 和键位不因表现层接入产生回归；
5. Batch 09 来源、运行引用和素材主清单一致。

用户已于 2026-07-27 确认验收通过，BC-04 已满足提交和推送门禁。

## 用户验收结论

2026-07-27 验收通过。Batch 09 音频、分级震动、低装甲提示、暂停与静音操作均进入确认基线。

## 开发证据

- 新增项目自有确定性音频 Batch 09：34 个 WAV，覆盖玩家、四类敌军、精英、Boss、UI、环境和分层音乐；
- 建立 `Master / Music / Ambience / SFX / UI` 总线及旧存档安全的五路音量设置映射；
- 玩法层只新增稳定事件事实，`AudioFeedbackController` 统一订阅并完成五波强度、Boss 过渡、空间音效和 UI 动态按钮接入；
- `CameraShakeController` 以小/中/大三级反馈驱动 `Camera2D.Offset`，重要击毁使用可自动恢复的短停顿；
- 低装甲状态新增不依赖日志的持续危险边框，暂停与操作提示明确使用 `Esc` 和 `M`，未引入 F1；
- Batch 09 生成源、运行目录和素材主清单已同步，34 个源文件与运行文件 SHA-256 一致。

## 集中自检证据

- `dotnet build game1/Game1.csproj --no-restore --nologo`：0 警告、0 错误；
- `dotnet build game1/Game1.csproj -c Release --no-restore --nologo`：0 警告、0 错误；
- `dotnet test GodotTank.sln --no-restore --nologo --verbosity quiet`：65/65 通过；
- `bc04_audio_feel_test_host`：34 项资源可加载、总线、循环层、低装甲提示、相机偏移与坐标隔离通过；
- `single_arena_flow_test_host`：真实 AppRoot、UI 音效绑定、Esc/M 提示和五波至胜利闭环通过；
- `arena_wave`、`enemy_projectile`、`boss_charge_recovery`、`hud_layout`、`acceptance_menu`、`bc03_final_art`、`blockade_city_enemy`：全部通过；
- `protocol_runtime_test_host`：`reward_catalog` 24 项、`navigation_grid` 9 项、`boss_phase` 4 项、`boss_encounter` 3 项全部通过；
- `mvp_test_runner`：9 项综合回归全部通过；
- Godot 编辑器资源解析与主场景 240 帧启动冒烟通过。

Godot 4.7 的 Dummy 渲染器在部分场景退出时仍打印历史性的资源释放警告，但所有相关宿主退出码均为 0，正式运行路径未出现脚本、资源或场景错误。
