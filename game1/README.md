# 废土中继（Godot 原型）

Godot 4.7 .NET / C# 制作中的俯视角肉鸽坦克游戏原型。当前已经完成 Task 5 的三类敌人、三波守点玩法与混合表现专项；Task 6 的三选一协议和第二场战斗尚未开始。

## 当前可玩内容

- WASD / 方向键移动坦克；
- 鼠标或右摇杆独立瞄准炮塔；
- 空格冲刺：3 倍速度、0.14 秒持续、0.8 秒冷却，不能穿过墙体；
- 鼠标左键持续开火：炮弹有 0.22 秒射击冷却；
- 炮弹以扫掠射线检测墙体，命中钢墙后反弹一次；
- 黄/橙/紫三类敌人分别承担巡逻追击、快速突击和优先攻击中继站的攻城职责；
- 敌军按三波进攻中继站，清完第三波后房间进入清场状态；
- 中继站耐久归零时本局失败；玩家装甲归零时先消耗一次战场重启，1.2 秒后在中继站旁以 50% 装甲恢复，重启耗尽后再次报废则失败；
- 房间四周和右侧中央钢墙均可用于验证碰撞与反弹。

当前已进入高质量像素街机原型阶段：主角、敌军、中继站和黄沙工业战场使用可替换像素原型资源，并加入移动重量、炮塔后坐、炮口火光、沙尘、命中、镜头震动与最小 SFX。肉鸽协议和正式商用素材仍在后续迭代完成。

## 构建

```powershell
dotnet build game1/Game1.csproj --nologo
```

以上命令从仓库根目录执行。

## 启动

```powershell
godot-gui --path game1
```

The first editor launch may spend a few seconds scanning C# scripts and building the assembly.

If Vulkan pipeline errors appear, use the OpenGL compatibility launchers:

```powershell
.\game1\run-editor-opengl.cmd
.\game1\run-game-opengl.cmd
```

## 操作

- WASD / 方向键：移动
- 鼠标 / 右摇杆：独立瞄准
- 鼠标左键：射击
- 空格：冲刺
- Z：验收用玩家伤害
- X：验收用中继站伤害
- M：切换 SFX 静音

Z/X 仅用于当前原型的可见验收，帮助稳定触发玩家伤害、中继站伤害和重启边界；它们不是正式玩家功能。
