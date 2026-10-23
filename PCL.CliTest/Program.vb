Imports System
Imports System.IO

Module Program
    Sub Main(args As String())
        Console.WriteLine("=== PCL macOS CLI 启动验证（使用 PCL.Launcher）===")

        Dim mcDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "minecraft")
        Dim version = "26.2"

        If Not Directory.Exists(Path.Combine(mcDir, "versions", version)) Then
            Console.WriteLine($"[FAIL] 版本目录不存在: {Path.Combine(mcDir, "versions", version)}")
            Return
        End If

        Dim options As New PCL.Launcher.LaunchOptions With {
            .McFolder = mcDir,
            .VersionName = version,
            .PlayerName = "Player",
            .JavaPath = "/Library/Java/JavaVirtualMachines/temurin-25.jdk/Contents/Home/bin/java",
            .MaxMemoryMb = 2048,
            .TimeoutMs = 15000
        }

        Console.WriteLine($"[INFO] MC 目录: {options.McFolder}")
        Console.WriteLine($"[INFO] 版本: {options.VersionName}")
        Console.WriteLine("[INFO] 启动中...")

        Dim result = PCL.Launcher.Launcher.Launch(options)

        Console.WriteLine()
        Console.WriteLine($"=== 结果: {(If(result.Success, "成功", "失败"))} ===")
        Console.WriteLine($"消息: {result.Message}")
        Console.WriteLine($"退出码: {result.ExitCode}")
        Console.WriteLine()
        Console.WriteLine("=== stdout（前 15 行）===")
        Dim outLines = result.Stdout.Split(New Char() {ChrW(10)}, StringSplitOptions.RemoveEmptyEntries)
        For i = 0 To Math.Min(14, outLines.Length - 1)
            Console.WriteLine(outLines(i))
        Next
    End Sub
End Module
