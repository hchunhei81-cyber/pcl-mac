# macOS 移植笔记

记录 PCL2 业务逻辑层在 macOS / .NET 上编译、运行、启动游戏的过程中所做的解耦和适配。

## 项目结构
PCL macos 第三方重构版/
├── PCL.Modules/ # PCL2 业务逻辑层（VB.NET，net10.0）
│ ├── Compat/ # 跨平台适配层（桩、抽出的类型、修改过的 Settings）
│ │ ├── Stubs.vb
│ │ ├── LoadingTypes.vb
│ │ ├── HintCompat.vb
│ │ └── Settings.vb
│ └── PCL.Modules.vbproj
├── PCLCS.Shared/ # PCLCS 的跨平台副本（C#，net10.0）
├── PCL.CliTest/ # 命令行验证工具（VB.NET，net10.0）
└── docs/

text

## 已排除的文件（不参与跨平台编译）

| 文件 | 原因 | 替代方案 |
|---|---|---|
| `Modules/ModMain.vb` | 页面导航 + WPF 依赖属性 + 大量 UI 类型引用 | Avalonia 导航服务 + `Compat/HintCompat.vb` 抽出的 `Hint` |
| `Modules/ModEvent.vb` | WPF 附加属性事件总线 | Avalonia 消息中心（后续） |
| `Modules/Base/ModAnimation.vb` | WPF 动画 | Avalonia 动画（后续） |
| `Modules/Base/MyBitmap.vb` | System.Drawing 图像处理 | Avalonia 图像 API（后续） |
| `Modules/ModMusic.vb` | NAudio（Windows 专属音频库） | Avalonia/跨平台音频（后续） |
| `Modules/ModSecret.vb` | WPF 颜色/画刷彩蛋 | 可选，后续 |
| `Modules/ThirdParty/DragHelper.vb` | Windows 句柄拖拽 | Avalonia 拖拽 API（后续） |
| `Pages/PageSetup/SettingService.vb` | WPF 附加属性 | 不参与跨平台编译 |

## 桩（`Compat/Stubs.vb`）

为被排除的 UI/平台符号提供最小声明，让业务逻辑层能编译。这些桩在运行时是空操作，后续由 Avalonia UI 层替换。

- `FrmMain` / `FormMain` / `FrmSpeedLeft` / `FrmLogin*` / `Page*`：`Object = Nothing`，靠 `Option Strict Off` 的晚期绑定通过编译
- `MyMsgBox` / `MyMsgBoxInput` / `MyMsgBoxSelect`：打印到控制台，返回默认值
- `Application`：`Current` 返回空实例
- `Telemetry`：空操作
- `Feedback` / `FeedbackInfo`：空操作
- `Dialogs` / `WindowsUtils` / `SetGPUPreference`：空操作
- `ClipboardCompat`：内存剪贴板
- `ResourceManagerCompat`：最小资源管理器
- `McInstallRequest` / `McInstallState` / `McInstallLoadersGet`：安装相关桩
- `MyBitmap`：空类，`Pic` / `Save` / `Clip` 空方法
- `SettingService`：空类，`GetKey` / `SetKey` / `RefreshSettings` 空方法
- `IMyRadio` / `MyRadioBox`：空接口/类
- `Size`：自定义 `Width` / `Height` 结构体（替代 `System.Windows.Size`）
- `ComputerCompat`：`Info.AvailablePhysicalMemory` / `OSVersion`
- `LinqCompat`：自定义 `DistinctBy` / `MaxBy` 扩展，绕过 .NET 10 的 VB 重载歧义

## 抽出的跨平台类型

- `Compat/LoadingTypes.vb`：`ILoadingTrigger`、`MyLoadingState`、`MyLoadingStateSimulator`（从 `Controls/MyLoading.xaml.vb` 抽出）
- `Compat/HintCompat.vb`：`HintType`、`HintMessage`、`Hint` 方法（从 `ModMain.vb` 抽出）
- `Compat/Settings.vb`：`Settings` 类（从 `Pages/PageSetup/Settings.vb` 复制，删掉 31 处 `OnChanged:=AddressOf` UI 回调，`Sources.Registry` 全部替换为 `Sources.Normal`）

## 对原 `Modules` 代码的修改

### `Modules/Base/ModBase.vb`
- `MyColor` 删掉 6 个 WPF 颜色运算符、3 个 WPF 构造函数
- `New(HexString)` 改成手动解析十六进制
- `DPI` 字段改成 `GetDpi()` 函数，macOS 返回 96
- `IsVisibleInForm` / `IsTextTrimmed` / `ToVisibility` 三个 UI 扩展方法用 `#If False` 包起来
- `ClipboardSet` / `ClipboardGetText` 用 `#If False` 包起来
- `XamlXmlReader` 块用 `#If False` 包起来
- `AppDomain.CurrentDomain.SetupInformation.ApplicationName` → 硬编码 `"Plain Craft Launcher 2"`
- `My.Computer.Clipboard` → `ClipboardCompat`
- `My.Resources.ResourceManager` → `ResourceManagerCompat.ResourceManager`
- `DispatcherPriority` 分支简化成 `Action()`

### `Modules/Base/ModLoader.vb`
- `MyLoading.MyLoadingState` → `MyLoadingState`
- `LoaderTaskbarProgressLast` 字段和 `LoaderTaskbarProgressRefresh` 方法用 `#If False` 包起来（Windows 任务栏进度条）

### `Modules/Base/ModNet.vb`
- `CacheCow` HTTP 缓存替换成无缓存 `HttpClient`
- `DistinctBy` 加显式类型 `Function(a As NetFile)`

### `Modules/Minecraft/ModMinecraft.vb`
- `McInstance.New` 里 `McFolderSelected & "versions\"` → `Path.Combine(McFolderSelected, "versions")`
- `Check()` 里 `InheritName` 路径 → `Path.Combine`
- `PageDownloadResourceDetail.CachedFolder.Clear()` 用 `#If False` 包起来
- `My.Computer.Info.OSVersion` → `Environment.OSVersion.VersionString`
- `ToListItem` / `ToResourceItem` / `ApplyLogoToMyImage` 用 `#If False` 包起来

### `Modules/Minecraft/ModLaunch.vb`
- `RunInUiWait(AddressOf FrmLaunchLeft.PageChangeToLaunching)` → lambda
- `FrmLaunchLeft.Dispatcher.Invoke(AddressOf ...)` → lambda
- `My.Computer` → `ComputerCompat`
- `ThemeUnlock(10, False)` 桩成函数

### `Modules/Minecraft/ModJava.vb`
- `FrmSetupLaunch?.UpdateJavaList()` → `If ... Then ...`
- `DistinctBy` 加 `CType(..., List(Of String))`

### `Modules/Resource/ResourceVersion.vb`
- `ToListItem` 用 `#If False` 包起来
- `DistinctBy` 加显式类型 `Function(a As ResourceVersion)`

### `Modules/Resource/ResourceProject.vb`
- `ToResourceItem` + `ApplyLogoToMyImage` 用 `#If False` 包起来

### `Modules/Resource/ResourceSearcher.vb`
- `MaxBy` 加显式类型

### `Modules/ModModpack.vb`
- `DistinctBy` / `MaxBy` 加显式类型
- `AddressOf McInstallState` 的 `McInstallState` 桩成 `Sub`（不是 `Enum`）

### `MeloongCore/Shared/Utils/PathUtils.cs`（submodule）
- `ForCompare` 只在 Windows 上把 `/` 替换成 `\`，macOS 保留 `/`
- `LangVersion 13.0` 保持不变（用 .NET 10 SDK 编译）

## 已知的运行时风险

- `System.Drawing.Common` 在 macOS 上运行时抛 `PlatformNotSupportedException`，所有 GDI+ 相关代码（`Graphics.FromHwnd`、`Bitmap` 等）需要替换成 Avalonia 或 SkiaSharp
- `Microsoft.Win32.Registry` 在 macOS 上抛 `PlatformNotSupportedException`，`Sources.Registry` 已全部替换为 `Sources.Normal`（走 INI 文件）
- `StrConv`（`Microsoft.VisualBasic`）只在 Windows 上支持，`ResourceSearcher.vb` 里有调用，运行时可能抛异常
- `ServicePointManager` 已过时，`HttpClient` 的 SSL 校验行为可能和旧版不同
- 路径分隔符：`Modules` 里仍有大量硬编码 `\` 的路径拼接（如 `PCL\Setup.ini`），在 macOS 上会失败但通常返回默认值，不阻断流程。关键路径（版本、库、资源）已用 `Path.Combine` 修复

## 验证结果

`PCL.CliTest` 在 macOS / .NET 10 / Temurin 25 上：

- `PCL.Modules` 加载 ✅
- `ModBase` 静态初始化 ✅
- `McInstance.Check()` / `Load()` ✅
- 版本 JSON 解析（`mainClass`、`libraries`、`arguments`）✅
- classpath 组装（84 个库 + 版本 jar）✅
- JVM/game 参数占位符替换 ✅
- 平台规则过滤（`-XstartOnFirstThread` 只加 macOS）✅
- **Minecraft 26.2 在 macOS 上启动成功，Metal 渲染，Apple M5** ✅

## 下一步

1. 把 `PCL.CliTest` 的启动逻辑抽成 `PCL.Launcher` 模块
2. Avalonia 壳项目，单页跑通
3. 下载模块（library / asset 自动下载），让启动器自包含
4. UI 层逐步迁移
