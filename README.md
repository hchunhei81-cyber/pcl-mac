# PCL Mac 第三方重构版

> 基于 PCL2 源码的 macOS 第三方重构版本，非官方项目。

## 这是什么

本项目是对 [PCL2 (Plain Craft Launcher 2)](https://github.com/Meloong-Git/PCL) 的 macOS 平台重构尝试。原版 PCL2 基于 C# / VB.NET 与 WPF 开发，无法原生运行在 macOS 上。本项目保留其业务逻辑层，使用跨平台 UI 框架重写表示层，目标是让 Mac 用户获得接近 PCL2 的使用体验。

**本项目与 PCL 官方无关，不代表官方立场，也不是官方后续作品。**

## 当前状态

**第一阶段（核心 MVP）已验证通过**：在 macOS / .NET 10 / Temurin 25 上，通过 PCL2 的业务逻辑层成功启动 Minecraft 26.2（Metal 渲染，Apple M5）。

| 验证项 | 状态 |
|---|---|
| `PCL.Modules` 跨平台编译 | ✅ |
| `ModBase` 静态初始化 | ✅ |
| `McInstance.Check()` / `Load()` | ✅ |
| 版本 JSON 解析（`mainClass` / `libraries` / `arguments`） | ✅ |
| classpath 组装 | ✅ |
| JVM / game 参数占位符替换 | ✅ |
| 平台规则过滤（`-XstartOnFirstThread` 仅 macOS） | ✅ |
| **Minecraft 26.2 实际启动** | ✅ |

## 仓库结构
pcl-mac/
├── PCL.Modules/ # PCL2 业务逻辑层（VB.NET，net10.0）
│ ├── Compat/ # 跨平台适配层（桩、抽出的类型、清理过的 Settings）
│ └── PCL.Modules.vbproj
├── PCLCS.Shared/ # PCLCS 的跨平台副本（C#，net10.0）
├── PCL.CliTest/ # 命令行验证工具（VB.NET，net10.0）
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
- **Java 25**（Temurin）：`brew install --cask temurin@25`

### 2. 拉取原 PCL2 源码

./scripts/setup.sh