# 第三方素材候选清单

> 调研日期：2026-07-15。价格和条款可能变化，实际下载或购买前必须重新打开来源页核对。当前所有条目均为“未下载”。

> 2026-07-27 BC-04 决定：封锁城区当前运行音频采用项目自有 Batch 09 确定性合成，不下载本页音频候选；本清单仅保留为未来替换参考。

## 推荐组合

| 优先级 | 素材 | 用途 | 来源 | 授权摘要 | 当前状态 | 目标目录 |
|---|---|---|---|---|---|---|
| P0 | Ace Of Tanks Assets | 玩家/敌方坦克、16×16 地形、HUD、炮弹、爆炸 | https://alb-pixel-store.itch.io/ace-of-tanks-assets | 允许个人及商业项目、修改、无需署名；禁止重新分发素材包 | 待用户购买；页面曾显示约 US$1.50–3 | `downloads/purchased/ace_of_tanks/` |
| P0 | Wasteland: terrain and highway bridge | 废土地面、道路、桥梁、植被和装饰 | https://shidong.itch.io/wasteland | 允许商业/非商业项目及修改；禁止转售或分发素材 | 待用户购买；页面曾显示约 US$5 | `downloads/purchased/wasteland/` |
| P1 | Kenney Top-down Tanks Redux | 免费坦克、导弹、爆炸、道路、油桶和箱子占位 | https://opengameart.org/content/top-down-tanks-redux | CC0；署名非必需 | 免费候选，未下载 | `downloads/free/kenney_topdown_tanks_redux/` |
| P1 | Kenney Pixel UI Pack | 像素面板、按钮、图标和 HUD 补充 | https://kenney.nl/assets/pixel-ui-pack | CC0 | 免费候选，未下载 | `downloads/free/kenney_pixel_ui/` |
| P1 | Kenney Impact Sounds | 命中、碰撞和破坏音效 | https://www.kenney.nl/assets/impact-sounds | CC0 | 免费候选，未下载 | `downloads/free/kenney_impact_sounds/` |
| P1 | Kenney Interface Sounds | 菜单、选择和提示音效 | https://kenney.nl/assets/interface-sounds | CC0 | 免费候选，未下载 | `downloads/free/kenney_interface_sounds/` |
| P1 | Sonniss GDC 2026 Game Audio Bundle | 爆炸、机械、电气、环境与金属音效候选 | https://gdc.sonniss.com/ | 游戏等媒体项目商业可用、免署名；禁止用于 AI/ML 训练，下载前保存完整许可证 | 免费候选，未下载；包体很大，只筛选需要的文件 | `downloads/free/sonniss_gdc_2026/` |
| P2 | Post-Apocalyptic Morning/Afternoon/Night | 原型环境音乐 | https://opengameart.org/content/post-apocalyptic-morning-afternoon-and-night | CC0 | 免费音乐候选，未下载 | `downloads/free/opengameart_music/` |
| P2 | The World Fell Silent | 标题/废土环境音乐 | https://opengameart.org/content/the-world-fell-silent | CC0 | 免费音乐候选，未下载 | `downloads/free/opengameart_music/` |

## 备选但不同时混用

| 素材 | 适用情况 | 来源 | 授权摘要 | 状态 |
|---|---|---|---|---|
| 2D Top Down Tank Pack | 若 Ace Of Tanks 的 16×16 风格不够理想，可改用 Resurrect-64 色板体系 | https://reallybasicgames.itch.io/2d-top-down-tank-pack | 允许商业项目且无需署名；禁止作为素材包重新分发 | 备选，暂不购买或下载 |
| Kenney Top-Down Tanks | 完全免费原型或补充占位 | https://kenney.nl/assets/top-down-tanks | CC0 | 备选，暂不下载 |

## AI 素材使用规则

仅当清单中的现成资源无法覆盖以下内容时使用 AI：

- 玩家坦克与三种内置核心的机械视觉差异；
- 四类城市敌军、精英变体，以及路障指挥车和后续 Boss 的独特外形；
- 四套辅助系统、统计强化、部门协议、维护奖励和 Boss 奖励的图标与卡面；
- 冲刺、重启、命中、升级和特殊协议对应的关键特效；
- 抵抗军徽记、嵌入式移动核心图标、HUD、封面和宣传概念图。

不得再生成独立中继站、基地防守物、基地血条或商店页面；重构后的游戏没有这些玩法对象。

AI 输出不能直接视为最终游戏资产。必须经过尺寸统一、像素级清理、色板收敛、轮廓可读性检查和逐帧动画校正。所有最终采用的 AI 素材在本清单追加模型/工具、生成日期、提示词存档和人工修改说明。

## 下载前检查清单

- [ ] 来源页仍可访问，作者与素材名称一致；
- [ ] 当前许可证允许计划中的商业发布；
- [ ] 保存网页条款、包内许可证和购买凭证；
- [ ] 确认能否修改、是否要求署名、是否限制公开仓库；
- [ ] 检查文件格式、像素尺寸、动画方向和是否包含源文件；
- [ ] 下载后记录版本、文件哈希和实际使用文件；
- [ ] 只把筛选和加工后的成品复制进 `game1/assets/`。
