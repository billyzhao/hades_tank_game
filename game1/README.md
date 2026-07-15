# 废土中继（Godot 原型）

Godot 4.7 .NET / C# 制作中的俯视角肉鸽坦克游戏原型。当前目标是先验证 FC 坦克大战式弹道、墙体和守点玩法，再逐步接入肉鸽构筑、敌人和中继站系统。

## 当前可玩内容

- WASD / 方向键 / 左摇杆移动坦克；
- 鼠标或右摇杆独立瞄准炮塔；
- 空格冲刺：3 倍速度、0.14 秒持续、0.8 秒冷却，不能穿过墙体；
- 鼠标左键持续开火：炮弹有 0.22 秒射击冷却；
- 炮弹以扫掠射线检测墙体，命中钢墙后反弹一次；
- 房间四周和右侧中央钢墙均可用于验证碰撞与反弹。

目前视觉为程序化灰盒占位，素材、音效、敌人、中继站和肉鸽协议将在后续迭代加入。

## Build

```powershell
dotnet build "D:\my program\codex\godot\game1\Game1.csproj"
```

## Run

```powershell
godot-gui --path "D:\my program\codex\godot\game1"
```

The first editor launch may spend a few seconds scanning C# scripts and building the assembly.

If Vulkan pipeline errors appear, use the OpenGL compatibility launchers:

```powershell
.\run-editor-opengl.cmd
.\run-game-opengl.cmd
```

## 操作

- 移动：WASD、方向键或左摇杆
- 瞄准：鼠标或右摇杆
- 射击：鼠标左键
- 冲刺：空格
