Imports System
Imports System.IO

Module Program
    Sub Main(args As String())
        Console.WriteLine("=== PCL macOS CLI 验证 ===")

        Dim mcDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "minecraft")
        Dim version = "26.2"

        ' 1. 下载（如果本地已有会跳过）
        Console.WriteLine()
        Console.WriteLine("=== 下载模块 ===")
        Dim dlOptions As New PCL.Launcher.DownloadCore.DownloadOptions With {
            .McFolder = mcDir,
            .VersionName = version,
            .MaxParallel = 8,
            .ProgressCallback = Sub(stage, done, total)
                Console.WriteLine($"[{stage}] {done}/{total}")
            End Sub
        }
        Dim dlResult = PCL.Launcher.DownloadCore.Download(dlOptions)
        Console.WriteLine($"下载结果: {(If(dlResult.Success, "成功", "失败"))} - {dlResult.Message}")
        Console.WriteLine($"  已下载: {dlResult.DownloadedFiles}, 跳过: {dlResult.SkippedFiles}, 失败: {dlResult.FailedFiles}")

        ' 2. 启动
        Console.WriteLine()
        Console.WriteLine("=== 启动 ===")
        Dim options As New PCL.Launcher.LaunchOptions With {
            .McFolder = mcDir,
            .VersionName = version,
            .PlayerName = "Player",
            .MaxMemoryMb = 2048,
            .TimeoutMs = 15000
        }
        Dim result = PCL.Launcher.LaunchCore.Launch(options)
        Console.WriteLine($"启动结果: {(If(result.Success, "成功", "失败"))} - {result.Message}")
        Console.WriteLine($"退出码: {result.ExitCode}")
        Dim outLines = result.Stdout.Split(New Char() {ChrW(10)}, StringSplitOptions.RemoveEmptyEntries)
        Console.WriteLine("stdout 前 5 行:")
        For i = 0 To Math.Min(4, outLines.Length - 1)
            Console.WriteLine("  " & outLines(i))
        Next
    End Sub
End Module
