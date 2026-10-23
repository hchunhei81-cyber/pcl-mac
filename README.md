# PCL Mac 第三方重构版

> 基于 PCL2 源码的 macOS 第三方重构版本，非官方项目。

## 这是什么

本项目是对 [PCL2 (Plain Craft Launcher 2)](https://github.com/Meloong-Git/PCL) 的 macOS 平台重构尝试。原版 PCL2 基于 C# / VB.NET 与 WPF 开发，无法原生运行在 macOS 上。本项目保留其业务逻辑思路，使用跨平台 UI 框架重写表示层，目标是让 Mac 用户获得接近 PCL2 的使用体验。



**本项目与 PCL 官方无关，不代表官方立场，也不是官方后续作品。**

## 当前状态

早期开发中，尚未达到可用状态。目前进度：

- [x] 业务逻辑层（Modules）在 macOS 上独立编译验证
- [ ] 命令行验证：版本 JSON 解析 + classpath 组装 + 参数拼接 + 唤起 Java（离线账户）
- [ ] Avalonia 壳项目 + 单页跑通
- [ ] 缺失 library / asset 的最小下载兜底
- [ ] 微软登录
- [ ] 完整下载管理器（核心 / Assets / Libraries）
- [ ] 设置页 / 关于页 / 版本管理页 / 下载页 UI 迁移
- [ ] 动画与皮肤
- [ ] 整合包
- [ ] 联机（可选，不承诺）

## 技术路线

- UI 框架：Avalonia（替换原 WPF 表示层）
- 业务逻辑：复用 PCL2 源码中的 Modules 层，按需解耦
- 目标平台：macOS（Apple Silicon / Intel）

## 署名与致谢

本项目基于 [龙腾猫跃](https://github.com/Meloong-Git) 开发的 PCL2 进行二次创作。

- 原作者：龙腾猫跃
- 原项目：https://github.com/Meloong-Git/PCL
- 赞助链接：[![爱发电](https://img.shields.io/badge/赞助-爱发电-946ce6?style=flat&labelColor=444444&logoSize=auto)](https://meloong.com/afd/a/LTCat)

依据《PCL 存储库合理使用指南》，本项目：

- 明确为第三方独立二次创作产物
- 名称以 PCL 开头并标注第三方修改属性
- 公开源代码
- 继续沿用《PCL 分发有限许可》与《PCL 存储库合理使用指南》
- 在软件关于页首位给出原作者署名及赞助链接
- 启动功能将实现与 `McLaunchPrecheck` 一致的正版购买劝导与赞助劝导

## 许可证

本项目遵循原项目的《PCL 分发有限许可》与《PCL 存储库合理使用指南》。详见 [LICENCE](LICENCE)。

## 免责声明

本项目为个人学习与技术探索性质的开源项目，不提供任何形式的担保,未完成品不要用于生产。Minecraft 是 Mojang Studios 的商标，本项目与 Mojang 无关。
