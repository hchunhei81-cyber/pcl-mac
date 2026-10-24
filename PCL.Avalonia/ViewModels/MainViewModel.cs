using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PCL.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] public partial string CurrentPage { get; set; } = "Launch";
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

    [ObservableProperty] public partial string McFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "minecraft");
    partial void OnMcFolderChanged(string value) => RefreshVersions();

    public ObservableCollection<string> AvailableVersions { get; } = new();
    public ObservableCollection<PCL.Launcher.InstanceManager.InstanceInfo> Instances { get; } = new();
    public ObservableCollection<PCL.Launcher.ModScanner.ModInfo> CurrentMods { get; } = new();

    [ObservableProperty] public partial PCL.Launcher.InstanceManager.InstanceInfo? SelectedInstance { get; set; }
    partial void OnSelectedInstanceChanged(PCL.Launcher.InstanceManager.InstanceInfo? value)
    {
        CurrentMods.Clear();
        if (value != null) 
        {
            AppendLog($"已选中实例: {value.Name}");
            var mods = PCL.Launcher.ModScanner.ScanMods(value.Path);
            foreach (var m in mods) CurrentMods.Add(m);

            // 加载已有皮肤预览
            var skinPath = Path.Combine(value.Path, "PCL", "skin.png");
            if (File.Exists(skinPath))
            {
                try { SkinPreviewImage = new Bitmap(skinPath); }
                catch { SkinPreviewImage = null; }
            }
            else
            {
                SkinPreviewImage = null;
            }
        }
        else
        {
            SkinPreviewImage = null;
        }
    }

    [ObservableProperty] public partial string? VersionName { get; set; }
    [ObservableProperty] public partial string PlayerName { get; set; } = "Player";
    [ObservableProperty] public partial string LogOutput { get; set; } = "";
    [ObservableProperty] public partial bool IsBusy { get; set; } = false;
    [ObservableProperty] public partial int MaxMemoryMb { get; set; } = 2048;
    [ObservableProperty] public partial string JavaPath { get; set; } = "";
    [ObservableProperty] public partial bool IsOfflineMode { get; set; } = true;
    [ObservableProperty] public partial bool IsLoggedIn { get; set; } = false;
    [ObservableProperty] public partial string LoginStatus { get; set; } = "未登录";
    [ObservableProperty] public partial string SkinPathInput { get; set; } = "";
    [ObservableProperty] public partial Bitmap? SkinPreviewImage { get; set; }
    [ObservableProperty] public partial string NewInstanceName { get; set; } = "";
    [ObservableProperty] public partial string SelectedBaseVersion { get; set; } = "";

    // 下载相关
    public ObservableCollection<PCL.Launcher.VersionManifest.VersionInfo> DownloadVersions { get; } = new();
    [ObservableProperty] public partial bool IsFetchingVersions { get; set; } = false;
    [ObservableProperty] public partial string DownloadStatusText { get; set; } = "点击刷新获取版本列表";
    [ObservableProperty] public partial bool IsUsingMirror { get; set; } = true;
    [ObservableProperty] public partial double DownloadProgress { get; set; } = 0;
    [ObservableProperty] public partial PCL.Launcher.VersionManifest.VersionInfo? SelectedDownloadVersion { get; set; }
    [ObservableProperty] public partial string FabricInstallStatus { get; set; } = "";
    [ObservableProperty] public partial bool IsInstallingFabric { get; set; } = false;
    [ObservableProperty] public partial string ForgeInstallStatus { get; set; } = "";
    [ObservableProperty] public partial bool IsInstallingForge { get; set; } = false;

    // 资源搜索
    public ObservableCollection<PCL.Launcher.ModrinthApi.SearchResult> SearchResults { get; } = new();
    [ObservableProperty] public partial string SearchQuery { get; set; } = "";
    [ObservableProperty] public partial string SearchProjectType { get; set; } = "mod";
    [ObservableProperty] public partial bool IsSearching { get; set; } = false;
    [ObservableProperty] public partial string SearchStatus { get; set; } = "输入关键词搜索";
    [ObservableProperty] public partial bool IsSearchingMode { get; set; } = false;
    [ObservableProperty] public partial bool IsManualInstallMode { get; set; } = false;
    public bool IsAutoInstallMode => !IsSearchingMode && !IsManualInstallMode;
    public bool IsDownloadInstallTab => IsAutoInstallMode;
    public bool IsDownloadModTab => IsSearchingMode && SearchProjectType == "mod";
    public bool IsDownloadModpackTab => IsSearchingMode && SearchProjectType == "modpack";

    partial void OnSearchProjectTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsDownloadInstallTab));
        OnPropertyChanged(nameof(IsDownloadModTab));
        OnPropertyChanged(nameof(IsDownloadModpackTab));
    }

    partial void OnIsSearchingModeChanged(bool value) => OnPropertyChanged(nameof(IsAutoInstallMode));
    partial void OnIsManualInstallModeChanged(bool value) => OnPropertyChanged(nameof(IsAutoInstallMode));

    // 手动安装
    [ObservableProperty] public partial string ImportPath { get; set; } = "";
    [ObservableProperty] public partial string ImportVersionName { get; set; } = "ImportedPack";
    [ObservableProperty] public partial string ImportStatus { get; set; } = "";

    // 更新弹窗
    [ObservableProperty] public partial bool IsUpdatePopupVisible { get; set; } = false;
    [ObservableProperty] public partial string UpdatePopupTitle { get; set; } = "发现新版本";
    [ObservableProperty] public partial string UpdatePopupMessage { get; set; } = "";
    [ObservableProperty] public partial string PendingDownloadUrl { get; set; } = "";

    public MainViewModel()
    {
        var config = PCL.Launcher.LauncherConfig.Load();
        PlayerName = config.PlayerName;
        MaxMemoryMb = config.MaxMemoryMb;
        JavaPath = config.JavaPath;
        IsOfflineMode = config.IsOfflineMode;

        RefreshVersions();

        if (!string.IsNullOrEmpty(config.VersionName) && AvailableVersions.Contains(config.VersionName))
            VersionName = config.VersionName;
    }

    private void SaveConfig()
    {
        new PCL.Launcher.LauncherConfig { PlayerName = this.PlayerName, VersionName = this.VersionName ?? "", MaxMemoryMb = this.MaxMemoryMb, JavaPath = this.JavaPath, IsOfflineMode = this.IsOfflineMode }.Save();
    }

    partial void OnPlayerNameChanged(string value) => SaveConfig();
    partial void OnVersionNameChanged(string? value) => SaveConfig();
    partial void OnMaxMemoryMbChanged(int value) => SaveConfig();
    partial void OnJavaPathChanged(string value) => SaveConfig();
    partial void OnIsOfflineModeChanged(bool value) => SaveConfig();

    [RelayCommand]
    private void RefreshVersions()
    {
        AvailableVersions.Clear();
        Instances.Clear();
        try
        {
            foreach (var v in PCL.Launcher.VersionScanner.ScanVersions(McFolder)) AvailableVersions.Add(v);
            
            if (AvailableVersions.Count > 0)
            {
                VersionName = AvailableVersions[0];
                if (string.IsNullOrEmpty(SelectedBaseVersion)) SelectedBaseVersion = AvailableVersions[0];
            }

            foreach (var inst in PCL.Launcher.InstanceManager.ScanInstances(McFolder)) Instances.Add(inst);
        }
        catch (Exception ex) { AppendLog($"[WARN] 扫描失败: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task CreateAndLaunchAsync()
    {
        if (string.IsNullOrWhiteSpace(NewInstanceName))
        {
            AppendLog("请填写实例名称");
            return;
        }
        if (string.IsNullOrWhiteSpace(SelectedBaseVersion))
        {
            AppendLog("请选择基础版本");
            return;
        }

        // 1. 创建实例（如果已存在则跳过创建，直接进目录）
        var success = PCL.Launcher.InstanceManager.CreateInstance(McFolder, NewInstanceName, SelectedBaseVersion);
        if (success)
        {
            AppendLog($"成功创建新实例: {NewInstanceName}");
        }
        else
        {
            AppendLog($"实例 {NewInstanceName} 已存在，将直接启动该实例");
        }

        // 2. 刷新列表并选中刚创建的实例
        RefreshVersions();
        VersionName = NewInstanceName; // 确保启动的是这个新实例
        SelectedInstance = Instances.FirstOrDefault(x => x.Name == NewInstanceName);

        // 3. 直接调用启动逻辑
        await LaunchAsync();
    }

    [RelayCommand]
    private void OpenSelectedFolder()
    {
        var inst = SelectedInstance ?? Instances.FirstOrDefault(x => x.Name == VersionName);
        if (inst == null) 
        {
            AppendLog("未找到对应的实例，请先在列表里选中一个实例");
            return;
        }

        // 检查文件夹是否真的存在
        if (!System.IO.Directory.Exists(inst.Path))
        {
            AppendLog($"实例文件夹已不存在: {inst.Path}，已从列表移除");
            Instances.Remove(inst);
            if (SelectedInstance == inst) SelectedInstance = null;
            return;
        }

        try 
        { 
            // 路径包含空格，必须加双引号
            System.Diagnostics.Process.Start("open", $"\"{inst.Path}\""); 
            AppendLog($"已打开文件夹: {inst.Path}");
        }
        catch (Exception ex) 
        { 
            AppendLog($"打开失败: {ex.Message}"); 
        }
    }


    [RelayCommand]
    private async Task PickAndImportSkinAsync()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window == null) return;

            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel?.StorageProvider == null) return;

            // 打开 macOS Finder
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择皮肤 PNG 图片",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
            });

            if (files.Count > 0)
            {
                var path = files[0].TryGetLocalPath();
                if (!string.IsNullOrEmpty(path))
                {
                    SkinPathInput = path;
                    try 
                    {
                        // 加载预览图
                        SkinPreviewImage = new Bitmap(path);
                    }
                    catch (Exception ex) 
                    {
                        AppendLog($"加载预览图失败: {ex.Message}");
                    }
                    ImportSkin(); // 选中后自动导入
                }
            }
        }
    }

    [RelayCommand]
    private void ImportSkin()
    {
        if (string.IsNullOrWhiteSpace(SkinPathInput)) 
        { 
            AppendLog("请输入皮肤图片路径"); 
            return; 
        }

        // 处理 macOS 拖拽可能带的单/双引号，以及 ~ 缩写
        var rawPath = SkinPathInput.Trim('"', '\'', ' ');
        var expandedPath = rawPath.StartsWith("~") 
            ? rawPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) 
            : rawPath;

        if (!File.Exists(expandedPath))
        {
            AppendLog($"找不到皮肤文件: {expandedPath}");
            return;
        }

        // 查找目标实例（优先右侧选中的，其次左侧启动下拉框的）
        var inst = SelectedInstance ?? Instances.FirstOrDefault(x => x.Name == VersionName);
        if (inst == null) 
        { 
            AppendLog("未找到对应的实例，请先在右侧实例列表选中一个实例"); 
            return; 
        }

        try
        {
            var pclDir = Path.Combine(inst.Path, "PCL");
            if (!Directory.Exists(pclDir)) Directory.CreateDirectory(pclDir);
            
            var destPath = Path.Combine(pclDir, "skin.png");
            File.Copy(expandedPath, destPath, true);
            
            // 同时写入 CustomSkinLoader 的本地皮肤缓存目录（如果存在）
            var cslDir = Path.Combine(inst.Path, "CustomSkinLoader", "LocalSkin");
            if (Directory.Exists(cslDir))
            {
                File.Copy(expandedPath, Path.Combine(cslDir, "skin.png"), true);
                AppendLog($"✓ 皮肤已同时写入 CustomSkinLoader 目录");
            }
            
            AppendLog($"✓ 皮肤已成功导入到实例 [{inst.Name}]");
            SkinPathInput = ""; // 清空输入框
        }
        catch (Exception ex)
        {
            AppendLog($"导入失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task FetchDownloadVersionsAsync()
    {
        if (IsFetchingVersions) return;
        IsFetchingVersions = true;
        DownloadStatusText = "正在获取...";
        DownloadVersions.Clear();
        try
        {
            var list = await Task.Run(() => PCL.Launcher.VersionManifest.FetchMojangVersions());
            foreach (var v in list) DownloadVersions.Add(v);
            DownloadStatusText = $"已加载 {list.Count} 个版本";
        }
        catch (Exception ex) { DownloadStatusText = $"失败: {ex.Message}"; }
        finally { IsFetchingVersions = false; }
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
        catch (Exception ex) { SearchStatus = $"失败: {ex.Message}"; }
        finally { IsSearching = false; }
    }

    [RelayCommand]
    private async Task InstallVersionAsync()
    {
        if (SelectedDownloadVersion == null) return;
        DownloadStatusText = $"正在安装 {SelectedDownloadVersion.Id}...";
        DownloadProgress = 0;
        try
        {
            await Task.Run(() =>
            {
                var dlOptions = new PCL.Launcher.DownloadCore.DownloadOptions { McFolder = McFolder, VersionName = SelectedDownloadVersion.Id, MaxParallel = 8, UseMirror = IsUsingMirror, ProgressCallback = (stage, done, total, speed) =>
                {
                    DownloadStatusText = $"[{stage}] {done}/{total} - {speed:F1} KB/s";
                    DownloadProgress = total > 0 ? (double)done / total * 100 : 0;
                } };
                var result = PCL.Launcher.DownloadCore.Download(dlOptions);
                if (!result.Success) throw new Exception(result.Message);
            });
            DownloadStatusText = $"{SelectedDownloadVersion.Id} 安装完成！";
            RefreshVersions();
        }
        catch (Exception ex) { DownloadStatusText = $"下载失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task InstallFabricAsync()
    {
        if (SelectedDownloadVersion == null || IsInstallingFabric) return;
        IsInstallingFabric = true;
        FabricInstallStatus = "正在安装 Fabric...";
        try
        {
            var options = new PCL.Launcher.FabricInstaller.FabricInstallOptions { McFolder = McFolder, McVersion = SelectedDownloadVersion.Id, JavaPath = string.IsNullOrWhiteSpace(JavaPath) ? null : JavaPath };
            var result = await Task.Run(() => PCL.Launcher.FabricInstaller.Install(options));
            FabricInstallStatus = result.Message;
            if (result.Success) RefreshVersions();
        }
        catch (Exception ex) { FabricInstallStatus = $"失败: {ex.Message}"; }
        finally { IsInstallingFabric = false; }
    }

    [RelayCommand]
    private async Task InstallForgeAsync()
    {
        if (SelectedDownloadVersion == null || IsInstallingForge) return;
        IsInstallingForge = true;
        ForgeInstallStatus = "正在安装 Forge...";
        try
        {
            var forgeVersion = $"{SelectedDownloadVersion.Id}-47.2.0";
            var options = new PCL.Launcher.ForgeInstaller.ForgeInstallOptions { McFolder = McFolder, McVersion = SelectedDownloadVersion.Id, ForgeVersion = forgeVersion, JavaPath = string.IsNullOrWhiteSpace(JavaPath) ? null : JavaPath };
            var result = await Task.Run(() => PCL.Launcher.ForgeInstaller.Install(options));
            ForgeInstallStatus = result.Message;
            if (result.Success) RefreshVersions();
        }
        catch (Exception ex) { ForgeInstallStatus = $"失败: {ex.Message}"; }
        finally { IsInstallingForge = false; }
    }

    [RelayCommand]
    private async Task ImportPackageAsync()
    {
        if (string.IsNullOrEmpty(ImportPath) || string.IsNullOrEmpty(ImportVersionName)) { ImportStatus = "请填写路径和版本名"; return; }
        ImportStatus = "正在导入...";
        var success = await Task.Run(() => PCL.Launcher.PackageImporter.ImportPackage(ImportPath, ImportVersionName, McFolder));
        ImportStatus = success ? "导入成功！" : "导入失败，请查看日志";
        if (success) { RefreshVersions(); VersionName = ImportVersionName; }
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
        if (IsOfflineMode)
        {
            if (string.IsNullOrWhiteSpace(PlayerName)) { LoginStatus = "请输入玩家名"; return; }
            IsLoggedIn = true;
            LoginStatus = $"已登录为离线账户: {PlayerName}";
        }
        else { LoginStatus = "微软在线登录开发中..."; }
    }

    [RelayCommand]
    private void Logout() { IsLoggedIn = false; LoginStatus = "未登录"; }

    [RelayCommand]
    private void SwitchDownloadTab(string tab)
    {
        if (tab == "Install") { IsSearchingMode = false; IsManualInstallMode = false; }
        else if (tab == "Mod") { IsSearchingMode = true; IsManualInstallMode = false; SearchProjectType = "mod"; _ = SearchResourcesAsync(); }
        else if (tab == "Modpack") { IsSearchingMode = true; IsManualInstallMode = false; SearchProjectType = "modpack"; _ = SearchResourcesAsync(); }
    }

    [RelayCommand]
    private void Navigate(string page) => CurrentPage = page;

    [RelayCommand]
    private void NavigateMenu(string menu)
    {
        if (menu == "Install") { CurrentPage = "Download"; IsSearchingMode = false; IsManualInstallMode = false; }
        else if (menu == "Manual") { CurrentPage = "Download"; IsSearchingMode = false; IsManualInstallMode = true; }
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
    private async Task LaunchSelectedInstanceAsync()
    {
        if (SelectedInstance == null) 
        {
            AppendLog("请先点击选中一个实例");
            return;
        }
        
        if (IsBusy)
        {
            AppendLog("程序正在运行中，请等待当前操作完成");
            return;
        }

        AppendLog($"触发双击启动: {SelectedInstance.Name}");
        // 将当前版本切换为该实例名，然后启动
        VersionName = SelectedInstance.Name;
        await LaunchAsync();
    }

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (IsBusy || string.IsNullOrEmpty(VersionName)) return;

        IsBusy = true;
        LogOutput = "";
        AppendLog($"=== 启动 {VersionName} ===");

        try
        {
            AppendLog("--- 下载阶段 ---");
            var dlOptions = new PCL.Launcher.DownloadCore.DownloadOptions { McFolder = McFolder, VersionName = VersionName, MaxParallel = 8, UseMirror = IsUsingMirror, ProgressCallback = (stage, done, total, speed) => { if (done == total || done % 50 == 0) AppendLog($"[{stage}] {done}/{total} - {speed:F1} KB/s"); } };
            var dlResult = await Task.Run(() => PCL.Launcher.DownloadCore.Download(dlOptions));
            AppendLog($"下载结果: {(dlResult.Success ? "成功" : "失败")} - {dlResult.Message}");

            if (!dlResult.Success) { IsBusy = false; return; }

            AppendLog("--- 启动阶段 ---");
            var launchOptions = new PCL.Launcher.LaunchCore.LaunchOptions { McFolder = McFolder, VersionName = VersionName, PlayerName = PlayerName, JavaPath = string.IsNullOrWhiteSpace(JavaPath) ? null : JavaPath, MaxMemoryMb = MaxMemoryMb, TimeoutMs = 0 };
            var result = await Task.Run(() => PCL.Launcher.LaunchCore.Launch(launchOptions));
            AppendLog($"启动结果: {(result.Success ? "成功" : "失败")} - {result.Message}");
            foreach (var line in result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries)) AppendLog(line);
        }
        catch (Exception ex) { AppendLog($"[FAIL] {ex.GetType().Name}: {ex.Message}"); }
        finally { IsBusy = false; }
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
    private void CancelUpdate() => IsUpdatePopupVisible = false;

    [RelayCommand]
    private async Task ConfirmUpdateAsync()
    {
        if (string.IsNullOrEmpty(PendingDownloadUrl)) { IsUpdatePopupVisible = false; return; }
        UpdatePopupTitle = "正在下载更新...";
        UpdatePopupMessage = "请稍候，更新完成后程序将自动重启。";

        try
        {
            var success = await Task.Run(() => PCL.Launcher.AutoUpdater.ApplyUpdate(PendingDownloadUrl));
            if (success) { Environment.Exit(0); }
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
    private async Task CopyLogAsync()
    {
        try
        {
            // macOS 原生剪贴板命令
            using var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("pbcopy") 
            { 
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            
            if (proc != null)
            {
                await proc.StandardInput.WriteAsync(LogOutput);
                proc.StandardInput.Close();
                proc.WaitForExit();
                AppendLog("【提示】日志已成功复制到剪贴板！");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"复制日志失败: {ex.Message}");
        }
    }

    private void AppendLog(string line) => LogOutput += line + Environment.NewLine;
}
