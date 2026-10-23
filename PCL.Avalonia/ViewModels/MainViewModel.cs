using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        RefreshVersions();
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
                ProgressCallback = (stage, done, total) =>
                {
                    if (done == total || done % 50 == 0)
                        DownloadStatusText = $"[{stage}] {done}/{total}";
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

    [RelayCommand]
    private void Login()
    {
        if (string.IsNullOrWhiteSpace(PlayerName))
        {
            LoginStatus = "请输入玩家名";
            return;
        }

        if (IsOfflineMode)
        {
            IsLoggedIn = true;
            LoginStatus = $"已登录为离线账户: {PlayerName}";
        }
        else
        {
            LoginStatus = "微软在线登录开发中，敬请期待...";
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
                ProgressCallback = (stage, done, total) =>
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
