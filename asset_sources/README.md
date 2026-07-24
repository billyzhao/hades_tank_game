# 素材源文件区

此目录保存下载包、授权凭证、AI 生成源文件和加工中的素材。它位于 Godot 项目 `game1` 之外，避免 Godot 自动导入完整素材包和大型音频库。

## 工作流程

1. 先在 `THIRD_PARTY_ASSETS.md` 登记来源、作者、许可证和状态；
2. 免费资源下载到 `downloads/free/`，购买资源下载到 `downloads/purchased/`；
3. 把网页许可证、包内许可证和购买凭证保存到 `licenses/`；
4. 在 `working/` 完成裁切、统一色板、命名和响度处理；
5. 只把游戏实际使用的最终文件复制到 `game1/assets/`；
6. AI 只补充现成素材无法覆盖的核心坦克差异、敌军、Boss、关键特效、UI 或宣传概念，源文件保存在 `ai_generated/` 并同样登记。

任何未来的第三方素材下载或购买，都必须先完成登记并获得用户明确确认。

## 当前状态

- [`THIRD_PARTY_ASSETS.md`](THIRD_PARTY_ASSETS.md) 中的第三方候选仍未下载或购买；
- 已生成并在 [`AI_PROTOTYPE_ASSETS.md`](AI_PROTOTYPE_ASSETS.md) 登记一批内部 AI 像素原型；
- 当前素材生产以 [`MOBILE_CORE_ASSET_PLAN.md`](MOBILE_CORE_ASSET_PLAN.md) 为权威批次方案；
- 游戏元素、运行文件、源批次和确认状态统一登记在 [`GAME_ASSET_CATALOG.md`](GAME_ASSET_CATALOG.md)；
- 2026-07-24 起只开放封锁城区正式素材、特效、UI 和音频生产，后四区统一为 `DEFERRED`；
- `batch-00-style` 中包含中继站的旧风格图、旧比例稿与提示词已经失效，并已在 Alpha 02B 删除；
- `batch-01-units` 是尚未通过批次门禁的本地候选，不会直接进入游戏；
- 这些 AI 原型只用于内部玩法与表现验收，不视为最终商用素材；
- 未来下载或购买第三方素材时，仍必须先登记来源、授权和状态，并获得用户明确确认。

## 授权原则

- 优先 CC0 或明确允许商业项目、修改和发布的授权；
- 不使用来源不明、从其他游戏提取或授权条款含糊的素材；
- 不把购买的原始素材包重新分发；
- 若未来公开源代码仓库，需把受限商业素材移出公开仓库，通过私有构建资产或本地安装脚本提供；
- 发布前逐项复核 `THIRD_PARTY_ASSETS.md` 与 `licenses/`。
