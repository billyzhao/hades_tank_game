# 《废土中继》封锁城区 0.1.0-rc1 Windows 交付说明

## 构建内容

- 产品名：废土中继
- 版本：0.1.0-rc1
- 引擎：Godot 4.7.stable.mono
- 平台：Windows x86_64
- 渲染：OpenGL Compatibility
- 默认窗口：1440×810；逻辑画布 480×270
- 当前内容：封锁城区、五波、第 5 波精英、路障指挥车、胜败结算与重开

## 构建

从仓库根目录执行：

```powershell
dotnet build game1/Game1.csproj -c ExportRelease --no-restore --nologo
godot --headless --path game1 --export-release "Windows Desktop" "..\build\windows\废土中继-0.1.0-rc1.exe"
```

导出文件位于：

```text
build/windows/废土中继-0.1.0-rc1.exe
```

`game1/export_presets.cfg` 必须进入仓库；`build/` 是本地交付产物，不提交。

Godot .NET 的 Windows 构建由 EXE、同名 PCK 和 `data_Game1_windows_x86_64/` 运行目录共同组成。验收或分发时必须保留整个 `build/windows/` 目录结构，不能只复制 EXE。

## 启动与操作

双击 `废土中继-0.1.0-rc1.exe`。不需要打开 Godot 编辑器。

- WASD / 方向键：移动
- 鼠标 / 右摇杆：瞄准
- 鼠标左键：射击
- 空格：冲刺
- Esc：暂停/继续
- M：静音/恢复

Release 构建不显示右上角“竞技场验收”和 F8 Debug 信息。

## 完整验收路径

1. 标题点击开始，选择任一移动核心；
2. 完成五波；刷新结束后清空残敌，进入即时升级或波间奖励；
3. 第 2、4 波获得维护奖励，第 5 波出现唯一精英并获得稀有奖励；
4. 路障指挥车半血进入二阶段，冲锋结束后暴露弱点；
5. 击败 Boss 进入封锁城区胜利结算；
6. 选择重试能得到干净新局，返回标题可重新选择核心；
7. 重启耗尽后报废进入失败结算，失败界面同样可重试或返回标题。

## 已知限制

- 当前 Release Candidate 只交付封锁城区；后四竞技场均为延期内容；
- Windows 文件未进行商业代码签名，系统可能显示未知发布者提示；
- 性能跑分与专项调优已按 2026-07-27 用户确认延期，不属于本候选版验收门禁；
- 存档与设置写入 Godot `user://` 用户目录，不写入程序安装目录。
