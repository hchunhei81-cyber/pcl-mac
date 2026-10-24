using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PCL.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string CurrentPage { get; set; } = "Launch";

    public bool IsLaunchPage => CurrentPage == "Launch";
    public bool IsDownloadPage => CurrentPage == "Download";
    public bool IsSettingsPage => CurrentPage == "Settings";
    public bool IsMorePage => CurrentPage == "More";

    partial void OnPlayerNameChanged(string value) => SaveConfig();
    partial void OnVersionNameChanged(string? value) => SaveConfig();
    partial void OnMaxMemoryMbChanged(int value) => SaveConfig();
    partial void OnJavaPathChanged(string value) => SaveConfig();
    partial void OnIsOfflineModeChanged(bool value) => SaveConfig();

    partial void OnCurrentPageChanged(string value)
    {
        OnPropertyChanged(nameof(IsLaunchPage));
        OnPropertyChanged(nameof(IsDownloadPage));
        OnPropertyChanged(nameof(IsSettingsPage));
        OnPropertyChanged(nameof(IsMorePage));
    }

    [ObservableProperty]
    public partial string McFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "Application Support", "minecraft");

    partial void OnMcFolderChanged(string value)
    {
        RefreshVersions();
    }

    public ObservableCollection<string> AvailableVersions { get; } = new();
    public ObservableCollection<PCL.Launcher.InstanceManager.InstanceInfo> Instances { get; } = new();

    [ObservableProperty]
    public partial string NewInstanceName { get; set; } = "";

    [ObservableProperty]
    public partial string SkinPathInput { get; set; } = "";

    [ObservableProperty]
    public partial string SelectedBaseVersion { get; set; } = "";

    [ObservableProperty]
    public partial string? VersionName { get; set; }

    [ObservableProperty]
    public partial string PlayerName { get; set; } = "Player";

    [ObservableProperty]
    public partial bool IsOfflineMode { get; set; } = true;

    [ObservableProperty]
    public partial bool IsLoggedIn { get; set; } = false;

    [ObservableProperty]
    public partial string LoginStatus { get; set; } = "未登录";

    [ObservableProperty]
    public partial string LogOutput { get; set; } = "";

    [ObservableProperty]
    public partial bool IsBusy { get; set; } = false;

    [ObservableProperty]
    public partial bool IsUpdatePopupVisible { get; set; } = false;

    [ObservableProperty]
    public partial string UpdatePopupTitle { get; set; } = "发现新版本";

    [ObservableProperty]
    public partial string UpdatePopupMessage { get; set; } = "";

    [ObservableProperty]
    public partial int MaxMemoryMb { get; set; } = 2048;

    [ObservableProperty]
    public partial string JavaPath { get; set; } = "";

    // 下载页相关
    public ObservableCollection<PCL.Launcher.VersionManifest.VersionInfo> DownloadVersions { get; } = new();

    [ObservableProperty]
    public partial bool IsFetchingVersions { get; set; } = false;

    [ObservableProperty]
    public partial string DownloadStatusText { get; set; } = "点击刷新获取版本列表";

    [ObservableProperty]
    public partial bool IsUsingMirror { get; set; } = true; // 默认用镜像

    [ObservableProperty]
    public partial double DownloadProgress { get; set; } = 0;

    [ObservableProperty]
    public partial string FabricInstallStatus { get; set; } = "";

    [ObservableProperty]
    public partial bool IsInstallingFabric { get; set; } = false;

    [ObservableProperty]
    public partial string ForgeInstallStatus { get; set; } = "";

    [ObservableProperty]
    public partial bool IsInstallingForge { get; set; } = false;

    [ObservableProperty]
    public partial PCL.Launcher.VersionManifest.VersionInfo? SelectedDownloadVersion { get; set; }

    partial void OnSelectedDownloadVersionChanged(PCL.Launcher.VersionManifest.VersionInfo? value)
    {
        if (value != null)
        {
            DownloadStatusText = $"已选择版本: {value.Id} ({value.Type})";
        }
    }

    // 资源搜索相关
    public ObservableCollection<PCL.Launcher.ModrinthApi.SearchResult> SearchResults { get; } = new();

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = "";

    [ObservableProperty]
    public partial string SearchProjectType { get; set; } = "mod";

    [ObservableProperty]
    public partial bool IsSearching { get; set; } = false;

    [ObservableProperty]
    public partial string SearchStatus { get; set; } = "输入关键词搜索";

    [ObservableProperty]
    public partial bool IsSearchingMode { get; set; } = false;

    [ObservableProperty]
    public partial bool IsManualInstallMode { get; set; } = false;

    public bool IsAutoInstallMode => !IsSearchingMode && !IsManualInstallMode;
    public bool IsDownloadInstallTab => IsAutoInstallMode;
    public bool IsDownloadModTab => IsSearchingMode && SearchProjectType == "mod";
    public bool IsDownloadModpackTab => IsSearchingMode && SearchProjectType == "modpack";

    partial void OnIsSearchingModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAutoInstallMode));
        OnPropertyChanged(nameof(IsDownloadInstallTab));
        OnPropertyChanged(nameof(IsDownloadModTab));
        OnPropertyChanged(nameof(IsDownloadModpackTab));
    }
    partial void OnIsManualInstallModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAutoInstallMode));
        OnPropertyChanged(nameof(IsDownloadInstallTab));
    }
    partial void OnSearchProjectTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsDownloadInstallTab));
        OnPropertyChanged(nameof(IsDownloadModTab));
        OnPropertyChanged(nameof(IsDownloadModpackTab));
    }

    // 手动安装
    [ObservableProperty]
    public partial string ImportPath { get; set; } = "";

    [ObservableProperty]
    public partial string ImportVersionName { get; set; } = "ImportedPack";

    [ObservableProperty]
    public partial string ImportStatus { get; set; } = "";

    public MainViewModel()
    {
        var config = PCL.Launcher.LauncherConfig.Load();
        PlayerName = config.PlayerName;
        MaxMemoryMb = config.MaxMemoryMb;
        JavaPath = config.JavaPath;
        IsOfflineMode = config.IsOfflineMode;

        RefreshVersions();

        // 恢复上次选的版本
        if (!string.IsNullOrEmpty(config.VersionName) && AvailableVersions.Contains(config.VersionName))
        {
            VersionName = config.VersionName;
        }
    }

    private void SaveConfig()
    {
        var config = new PCL.Launcher.LauncherConfig
        {
            PlayerName = this.PlayerName,
            VersionName = this.VersionName ?? "",
            MaxMemoryMb = this.MaxMemoryMb,
            JavaPath = this.JavaPath,
            IsOfflineMode = this.IsOfflineMode
        };
        config.Save();
    }

    [ObservableProperty]
    public partial string PendingDownloadUrl { get; set; } = "";

    [RelayCommand]
    private void ImportSkin()
    {
        if (string.IsNullOrWhiteSpace(SkinPathInput))
        {
            AppendLog("请输入皮肤图片路径");
            return;
        }

        // 找到当前选中的实例路径
        var inst = Instances.FirstOrDefault(x => x.Name == VersionName);
        if (inst == null)
        {
            AppendLog("请先选择一个独立实例");
            return;
        }

        var success = PCL.Launcher.SkinManager.SaveLocalSkin(inst.Path, SkinPathInput);
        if (success)
        {
            AppendLog($"皮肤已成功导入到 {inst.Name}");
            SkinPathInput = "";
        }
        else
        {
            AppendLog("皮肤导入失败，请检查文件路径是否正确");
        }
    }

    [RelayCommand]
    private void OpenFolder(PCL.Launcher.InstanceManager.InstanceInfo inst)
    {
        if (inst == null) return;
        try
        {
            // 用 macOS 的 open 命令打开 Finder
            System.Diagnostics.Process.Start("open", inst.Path);
        }
        catch (Exception ex)
        {
            AppendLog($"打开文件夹失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CreateInstance()
    {
        if (string.IsNullOrWhiteSpace(NewInstanceName) || string.IsNullOrWhiteSpace(SelectedBaseVersion))
        {
            AppendLog("请填写实例名称和基础版本");
            return;
        }

        var success = PCL.Launcher.InstanceManager.CreateInstance(McFolder, NewInstanceName, SelectedBaseVersion);
        if (success)
        {
            AppendLog($"已创建独立实例: {NewInstanceName}");
            RefreshVersions();
            NewInstanceName = "";
        }
        else
        {
            AppendLog("创建失败，实例可能已存在");
        }
    }

    [RelayCommand]
    private async Task ShowTestUpdateAsync()
    {
        UpdatePopupTitle = "正在检查更新...";
        UpdatePopupMessage = "请稍候";
        IsUpdatePopupVisible = true;

        try
        {
            var info = await Task.Run(() => PCL.Launcher.UpdateChecker.CheckForUpdates());
            if (info.HasUpdate)
            {
                UpdatePopupTitle = $"发现新版本 v{info.LatestVersion}";
                UpdatePopupMessage = $"当前版本: v{info.CurrentVersion}\n\n更新说明:\n{info.ReleaseNotes ?? "（无）"}";
                PendingDownloadUrl = info.DownloadUrl ?? "";
            }
            else
            {
                UpdatePopupTitle = "已是最新版本";
                UpdatePopupMessage = $"当前版本 v{info.CurrentVersion}，无需更新。";
                PendingDownloadUrl = "";
            }
        }
        catch (Exception ex)
        {
            UpdatePopupTitle = "检查更新失败";
            UpdatePopupMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ConfirmUpdateAsync()
    {
        if (string.IsNullOrEmpty(PendingDownloadUrl))
        {
            IsUpdatePopupVisible = false;
            return;
        }

        UpdatePopupTitle = "正在下载更新...";
        UpdatePopupMessage = "请稍候，更新完成后程序将自动重启。";

        try
        {
            var success = await Task.Run(() => PCL.Launcher.AutoUpdater.ApplyUpdate(PendingDownloadUrl));
            if (success)
            {
                // 更新脚本已启动，立即退出当前进程，让脚本接管
                Environment.Exit(0);
            }
            else
            {
                UpdatePopupTitle = "更新失败";
                UpdatePopupMessage = "无法执行自动替换，请手动前往 GitHub Release 下载。";
            }
        }
        catch (Exception ex)
        {
            UpdatePopupTitle = "更新异常";
            UpdatePopupMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void CancelUpdate()
    {
        IsUpdatePopupVisible = false;
        AppendLog("[Test] 用户取消了更新。");
    }

    [RelayCommand]
    private void RefreshVersions()
    {
        AvailableVersions.Clear();
        try
        {
            var list = PCL.Launcher.VersionScanner.ScanVersions(McFolder);
            foreach (var v in list) AvailableVersions.Add(v);
            
            VersionName = AvailableVersions.Count > 0 ? AvailableVersions[0] : null;
        }
        catch (Exception ex)
        {
            AppendLog($"[WARN] 扫描版本失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task FetchDownloadVersionsAsync()
    {
        if (IsFetchingVersions) return;
        IsFetchingVersions = true;
        DownloadStatusText = "正在获取版本列表...";
        DownloadVersions.Clear();
        try
        {
            var list = await Task.Run(() => PCL.Launcher.VersionManifest.FetchMojangVersions());
            foreach (var v in list) DownloadVersions.Add(v);
            DownloadStatusText = $"已加载 {list.Count} 个版本";
        }
        catch (Exception ex)
        {
            DownloadStatusText = $"获取失败: {ex.Message}";
        }
        finally
        {
            IsFetchingVersions = false;
        }
    }

    [RelayCommand]
    private async Task SearchResourcesAsync()
    {
        if (IsSearching) return;
        IsSearching = true;
        SearchStatus = "正在搜索...";
        SearchResults.Clear();
        try
        {
            var list = await Task.Run(() => PCL.Launcher.ModrinthApi.Search(SearchQuery, SearchProjectType, 20));
            foreach (var r in list) SearchResults.Add(r);
            SearchStatus = $"找到 {list.Count} 个结果";
        }
        catch (Exception ex)
        {
            SearchStatus = $"搜索失败: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task InstallFabricAsync()
    {
        if (SelectedDownloadVersion == null || IsInstallingFabric) return;
        IsInstallingFabric = true;
        FabricInstallStatus = "正在安装 Fabric...";

        try
        {
            var options = new PCL.Launcher.FabricInstaller.FabricInstallOptions
            {
                McFolder = McFolder,
                McVersion = SelectedDownloadVersion.Id,
                JavaPath = string.IsNullOrWhiteSpace(JavaPath) ? null : JavaPath
            };
            var result = await Task.Run(() => PCL.Launcher.FabricInstaller.Install(options));
            FabricInstallStatus = result.Message;

            if (result.Success)
            {
                RefreshVersions();
            }
        }
        catch (Exception ex)
        {
            FabricInstallStatus = $"安装失败: {ex.Message}";
        }
        finally
        {
            IsInstallingFabric = false;
        }
    }

    [RelayCommand]
    private async Task InstallForgeAsync()
    {
        if (SelectedDownloadVersion == null || IsInstallingForge) return;
        IsInstallingForge = true;
        ForgeInstallStatus = "正在安装 Forge...";

        try
        {
            // 这里先硬编码一个推荐版本，实际应该从 Forge Maven 查询最新版
            var forgeVersion = $"{SelectedDownloadVersion.Id}-47.2.0";
            var options = new PCL.Launcher.ForgeInstaller.ForgeInstallOptions
            {
                McFolder = McFolder,
                McVersion = SelectedDownloadVersion.Id,
                ForgeVersion = forgeVersion,
                JavaPath = string.IsNullOrWhiteSpace(JavaPath) ? null : JavaPath
            };
            var result = await Task.Run(() => PCL.Launcher.ForgeInstaller.Install(options));
            ForgeInstallStatus = result.Message;

            if (result.Success)
            {
                RefreshVersions();
            }
        }
        catch (Exception ex)
        {
            ForgeInstallStatus = $"安装失败: {ex.Message}";
        }
        finally
        {
            IsInstallingForge = false;
        }
    }

    [RelayCommand]
    private async Task InstallVersionAsync()
    {
        if (SelectedDownloadVersion == null) return;
        DownloadStatusText = $"正在安装 {SelectedDownloadVersion.Id}...";
        await Task.Run(() =>
        {
            var dlOptions = new PCL.Launcher.DownloadCore.DownloadOptions
            {
                McFolder = McFolder,
                VersionName = SelectedDownloadVersion.Id,
                MaxParallel = 8,
                UseMirror = IsUsingMirror,
                ProgressCallback = (stage, done, total, speed) =>
                {
                    DownloadStatusText = $"[{stage}] {done}/{total} - {speed:F1} KB/s";
                    DownloadProgress = total > 0 ? (double)done / total * 100 : 0;
                }
            };
            PCL.Launcher.DownloadCore.Download(dlOptions);
        });
        DownloadStatusText = $"{SelectedDownloadVersion.Id} 安装完成！";
        RefreshVersions();
    }

    [RelayCommand]
    private async Task ImportPackageAsync()
    {
        if (string.IsNullOrEmpty(ImportPath) || string.IsNullOrEmpty(ImportVersionName))
        {
            ImportStatus = "请填写路径和版本名";
            return;
        }
        ImportStatus = "正在导入...";
        var success = await Task.Run(() => PCL.Launcher.PackageImporter.ImportPackage(ImportPath, ImportVersionName, McFolder));
        ImportStatus = success ? "导入成功！" : "导入失败，请查看日志";
        if (success)
        {
            RefreshVersions();
            VersionName = ImportVersionName;
        }
    }

    [RelayCommand]
    private void SwitchDownloadTab(string tab)
    {
        if (tab == "Install")
        {
            IsSearchingMode = false;
            IsManualInstallMode = false;
        }
        else if (tab == "Mod")
        {
            IsSearchingMode = true;
            IsManualInstallMode = false;
            SearchProjectType = "mod";
            _ = SearchResourcesAsync();
        }
        else if (tab == "Modpack")
        {
            IsSearchingMode = true;
            IsManualInstallMode = false;
            SearchProjectType = "modpack";
            _ = SearchResourcesAsync();
        }
    }

    [RelayCommand]
    private void SwitchLoginMode(string mode)
    {
        IsOfflineMode = mode == "Offline";
        IsLoggedIn = false;
        LoginStatus = "未登录";
    }

    [ObservableProperty]
    public partial string DeviceCodeMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool IsDeviceCodeVisible { get; set; } = false;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsOfflineMode)
        {
            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                LoginStatus = "请输入玩家名";
                return;
            }
            IsLoggedIn = true;
            LoginStatus = $"已登录为离线账户: {PlayerName}";
            return;
        }

        // 在线登录
        LoginStatus = "正在获取设备代码...";
        IsDeviceCodeVisible = true;
        DeviceCodeMessage = "正在请求微软授权...";

        try
        {
            var code = await Task.Run(() => PCL.Launcher.MicrosoftAuth.RequestDeviceCode());
            DeviceCodeMessage = code.Message;
            LoginStatus = "等待用户授权...";

            // 复制代码到剪贴板（可选）或提示用户
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(code.VerificationUri) { UseShellExecute = true });

            // 轮询 Token
            var msToken = await Task.Run(() => PCL.Launcher.MicrosoftAuth.PollForToken(code.DeviceCode, code.Interval, code.ExpiresIn));
            LoginStatus = "正在验证 Xbox 和 Minecraft 账号...";
            IsDeviceCodeVisible = false;

            // 换取 MC Token
            var result = await Task.Run(() => PCL.Launcher.MicrosoftAuth.AuthenticateMinecraft(msToken));
            if (result.Success)
            {
                IsLoggedIn = true;
                PlayerName = result.PlayerName;
                LoginStatus = $"已登录为正版账户: {PlayerName}";
                // 这里可以存 result.AccessToken 和 result.Uuid 以便启动时使用
            }
            else
            {
                LoginStatus = result.Message;
            }
        }
        catch (Exception ex)
        {
            LoginStatus = $"登录失败: {ex.Message}";
            IsDeviceCodeVisible = false;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        IsLoggedIn = false;
        LoginStatus = "未登录";
    }

    [RelayCommand]
    private void Navigate(string page)
    {
        CurrentPage = page;
    }

    [RelayCommand]
    private void NavigateMenu(string menu)
    {
        if (menu == "Install")
        {
            CurrentPage = "Download";
            IsSearchingMode = false;
            IsManualInstallMode = false;
        }
        else if (menu == "Manual")
        {
            CurrentPage = "Download";
            IsSearchingMode = false;
            IsManualInstallMode = true;
        }
        else if (menu == "Mod" || menu == "Modpack")
        {
            CurrentPage = "Download";
            IsSearchingMode = true;
            IsManualInstallMode = false;
            SearchProjectType = menu == "Mod" ? "mod" : "modpack";
            _ = SearchResourcesAsync();
        }
    }

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (IsBusy || string.IsNullOrEmpty(VersionName)) return;
        IsBusy = true;
        LogOutput = "";
        AppendLog($"=== 启动 {VersionName} ===");
        AppendLog($"MC 目录: {McFolder}");
        AppendLog($"玩家名: {PlayerName}");
        AppendLog($"最大内存: {MaxMemoryMb} MB");
        AppendLog("");

        try
        {
            AppendLog("--- 下载阶段 ---");
            var dlOptions = new PCL.Launcher.DownloadCore.DownloadOptions
            {
                McFolder = McFolder,
                VersionName = VersionName,
                MaxParallel = 8,
                ProgressCallback = (stage, done, total, speed) =>
                {
                    if (done == total || done % 50 == 0)
                        AppendLog($"[{stage}] {done}/{total}");
                }
            };
            var dlResult = await Task.Run(() => PCL.Launcher.DownloadCore.Download(dlOptions));
            AppendLog($"下载结果: {(dlResult.Success ? "成功" : "失败")} - {dlResult.Message}");
            AppendLog($"  已下载: {dlResult.DownloadedFiles}, 跳过: {dlResult.SkippedFiles}, 失败: {dlResult.FailedFiles}");
            AppendLog("");

            if (!dlResult.Success)
            {
                IsBusy = false;
                return;
            }

            AppendLog("--- 启动阶段 ---");
            var launchOptions = new PCL.Launcher.LaunchCore.LaunchOptions
            {
                McFolder = McFolder,
                VersionName = VersionName,
                PlayerName = PlayerName,
                JavaPath = string.IsNullOrWhiteSpace(JavaPath) ? null : JavaPath,
                MaxMemoryMb = MaxMemoryMb,
                TimeoutMs = 0
            };
            var result = await Task.Run(() => PCL.Launcher.LaunchCore.Launch(launchOptions));
            AppendLog($"启动结果: {(result.Success ? "成功" : "失败")} - {result.Message}");
            AppendLog($"退出码: {result.ExitCode}");
            AppendLog("");
            AppendLog("--- stdout ---");
            foreach (var line in result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                AppendLog(line);
        }
        catch (Exception ex)
        {
            AppendLog($"[FAIL] {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AppendLog(string line)
    {
        LogOutput += line + Environment.NewLine;
    }
}
