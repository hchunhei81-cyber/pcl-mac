using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PCL.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string McFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "Application Support", "minecraft");

    [ObservableProperty]
    public partial string VersionName { get; set; } = "26.2";

    [ObservableProperty]
    public partial string PlayerName { get; set; } = "Player";

    [ObservableProperty]
    public partial string LogOutput { get; set; } = "";

    [ObservableProperty]
    public partial bool IsBusy { get; set; } = false;

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        LogOutput = "";
        AppendLog($"=== 启动 {VersionName} ===");
        AppendLog($"MC 目录: {McFolder}");
        AppendLog($"玩家名: {PlayerName}");
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
                MaxMemoryMb = 2048,
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
