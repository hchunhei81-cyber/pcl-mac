# PCL Mac 第三方重构版

> 基于 PCL2 源码的 macOS 第三方重构版本，非官方项目。

## 下载

最新版本：[PCL Mac v1.0.1 (macOS ARM64)](https://github.com/hchunhei81-cyber/pcl-mac/releases/tag/v1.0.1-mac)

## 这是什么

本项目是对 [PCL2 (Plain Craft Launcher 2)](https://github.com/Meloong-Git/PCL) 的 macOS 平台重构尝试。原版 PCL2 基于 C# / VB.NET 与 WPF 开发，无法原生运行在 macOS 上。本项目保留其业务逻辑层，使用跨平台 UI 框架（Avalonia）重写表示层，目标是让 Mac 用户获得接近 PCL2 的使用体验。

**本项目与 PCL 官方无关，不代表官方立场，也不是官方后续作品。**

## 当前状态

**核心功能已跑通**：在 macOS / .NET 10 上，可以下载 Minecraft、搜索 Mod 与整合包、手动导入整合包、使用离线账户启动游戏。

| 功能 | 状态 |
|---|---|
| 业务逻辑层跨平台编译 | ✅ |
| Avalonia 图形界面 | ✅ |
| 离线账户登录（自定义 UUID） | ✅ |
| 下载 Minecraft 原版（支持 BMCLAPI 镜像加速） | ✅ |
| 搜索/浏览 Modrinth 的 Mod 与整合包 | ✅ |
| 手动导入本地整合包（.zip / .mrpack） | ✅ |
| 启动 Minecraft（自动检测 Java） | ✅ |
| Forge / Fabric 等 Mod 加载器的自动安装 | ✅ |
| 配置持久化（记住用户名、版本、内存等） | ✅ |
| 打包为 macOS `.app` | ✅ |
| **微软正版登录** | ❌ **暂不可用（见下方说明）** |

## ⚠️ 关于微软正版登录

**我们非常抱歉地通知您，当前版本的启动器暂不支持微软正版账号登录。**

原因并非代码逻辑问题，而是微软与 Mojang 对第三方启动器的**合规限制**：

1. 在 Azure 注册应用后，调用 Minecraft 服务端接口（`login_with_xbox`）需要你的 Client ID 进入 Mojang 的**白名单**。目前 Mojang 对个人开发者不开放公开的自动审批通道，提交表单后是否通过、何时通过，均无明确保障。
2. 市面上部分启动器通过“借用”海外开源项目或官方客户端的 Client ID 来绕过这一限制，但这伴随着**随时被封禁、违反服务条款、引发开源社区投诉**的风险，不适合作为一个准备长期维护的开源项目。
3. 本项目选择**不做有合规风险的事**。

**因此，当前版本的“在线”登录按钮处于禁用状态。** 你依然可以使用离线模式完整地下载、管理、启动游戏。如果你的微软账号无法登录，我们深表歉意——这是微软生态的硬性限制，不是本项目可以绕过的。

## 仓库结构
pcl-mac/
├── PCL.Modules/ # PCL2 业务逻辑层（VB.NET，net10.0）
│ ├── Compat/ # 跨平台适配层（桩、抽出的类型、清理过的 Settings）
│ └── PCL.Modules.vbproj
├── PCLCS.Shared/ # PCLCS 的跨平台副本（C#，net10.0）
├── PCL.Launcher/ # 启动核心（下载、Java 检测、离线 UUID、版本扫描）
├── PCL.CliTest/ # 命令行验证工具
├── PCL.Avalonia/ # Avalonia 图形界面
├── docs/
│ └── porting-notes.md # 移植过程记录（解耦、桩、修改点）
├── scripts/
│ └── setup.sh # 拉取原 PCL2 源码到 vendor/
├── vendor/ # 原 PCL2 源码（setup.sh 克隆，已 gitignore）
├── Directory.Build.props # 定义 $(PCL2Root) 变量
└── README.md


## 快速开始

### 1. 安装依赖

- **.NET 10 SDK**：[dotnet.microsoft.com](https://dotnet.microsoft.com/download)
  - 如果 `dotnet --version` 不是 10.x，用完整路径调用，或调整 PATH 让 10.x SDK 优先
- **Java 25**（Temurin）：`brew install --cask temurin@25`

### 2. 拉取原 PCL2 源码

./scripts/setup.sh
这会把 PCL2 原仓库和 MeloongCore 子模块克隆到 vendor/PCL2/。

3. 编译并运行

bash
cd PCL.Avalonia
dotnet run
4. 打包为 macOS .app
cd PCL.Avalonia
dotnet publish -c Release -r osx-arm64 --self-contained true
产物在 bin/Release/net10.0/osx-arm64/publish/。把里面的文件放进 PCL.app/Contents/MacOS/，并配置 Info.plist 即可双击运行。

## 技术路线
UI 框架：Avalonia（替换原 WPF 表示层）
业务逻辑：复用 PCL2 源码中的 Modules 层，按需解耦
目标平台：macOS（Apple Silicon / Intel）

## 已知限制
正版登录暂不可用（见上方说明）
部分 Windows 专属功能（注册表、GDI+、NAudio、Windows 任务栏进度条）已用桩替代或排除，详见 docs/porting-notes.md
Forge 安装依赖官方 installer 的 --installClient 模式，首次安装可能需要几分钟（取决于网络速度）

# 署名与致谢
本项目基于 龙腾猫跃 开发的 PCL2 进行二次创作。
原作者：龙腾猫跃
原项目：https://github.com/Meloong-Git/PCL
赞助链接：[爱发电 - 龙腾猫跃](https://afdian.com/p/0164034c016c11ebafcb52540025c377)
依据《PCL 存储库合理使用指南》，本项目：

明确为第三方独立二次创作产物
名称以 PCL 开头并标注第三方修改属性
公开源代码
继续沿用《PCL 分发有限许可》与《PCL 存储库合理使用指南》
在软件关于页首位给出原作者署名及赞助链接
启动功能将实现与 McLaunchPrecheck 一致的正版购买劝导与赞助劝导

## 许可证
本项目遵循原项目的《PCL 分发有限许可》与《PCL 存储库合理使用指南》。详见 [LICENCE](LICENCE.md)。

## 免责声明
本项目为个人学习与技术探索性质的开源项目，不提供任何形式的担保。当前为早期开发版本，请勿用于日常游戏。Minecraft 是 Mojang Studios 的商标，本项目与 Mojang 无关。
