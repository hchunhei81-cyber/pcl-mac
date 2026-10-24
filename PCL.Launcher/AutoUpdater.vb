Imports System
Imports System.IO
Imports System.IO.Compression
Imports System.Net.Http
Imports System.Diagnostics
Imports System.Reflection

Public Module AutoUpdater

    ''' <summary>
    ''' 下载新版本并执行自替换脚本。
    ''' </summary>
    Public Function ApplyUpdate(zipUrl As String) As Boolean
        Try
            Dim tempDir = Path.Combine(Path.GetTempPath(), "PCL_Update_" & Guid.NewGuid().ToString("N").Substring(0, 8))
            Directory.CreateDirectory(tempDir)
            Dim zipPath = Path.Combine(tempDir, "update.zip")

            ' 1. 下载 zip
            Using client As New HttpClient()
                client.Timeout = TimeSpan.FromMinutes(5)
                Dim bytes = client.GetByteArrayAsync(zipUrl).Result
                File.WriteAllBytes(zipPath, bytes)
            End Using

            ' 2. 解压
            ZipFile.ExtractToDirectory(zipPath, tempDir)
            Dim newAppPath = Path.Combine(tempDir, "PCL.app")
            If Not Directory.Exists(newAppPath) Then
                Dim dirs = Directory.GetDirectories(tempDir, "PCL.app", SearchOption.AllDirectories)
                If dirs.Length > 0 Then newAppPath = dirs(0)
            End If
            If Not Directory.Exists(newAppPath) Then Return False

            ' 3. 获取当前 App 路径
            Dim currentExecutable = Assembly.GetEntryAssembly()?.Location
            Dim currentAppPath = ""
            If currentExecutable IsNot Nothing Then
                Dim macOsDir = Path.GetDirectoryName(currentExecutable)
                Dim contentsDir = Path.GetDirectoryName(macOsDir)
                currentAppPath = Path.GetDirectoryName(contentsDir)
            End If
            If String.IsNullOrEmpty(currentAppPath) OrElse Not currentAppPath.EndsWith(".app") Then
                Return False
            End If

            ' 4. 生成 update.sh
            Dim scriptPath = Path.Combine(tempDir, "update.sh")
            Dim sb As New System.Text.StringBuilder()
            sb.AppendLine("#!/bin/bash")
            sb.AppendLine("sleep 1.5")
            sb.AppendLine("rm -rf """ & currentAppPath & """")
            sb.AppendLine("cp -R """ & newAppPath & """ """ & currentAppPath & """")
            sb.AppendLine("xattr -dr com.apple.quarantine """ & currentAppPath & """")
            sb.AppendLine("open """ & currentAppPath & """")
            File.WriteAllText(scriptPath, sb.ToString())

            Process.Start("chmod", "+x " & scriptPath)?.WaitForExit()
            Process.Start("sh", scriptPath)

            Return True
        Catch ex As Exception
            Console.WriteLine($"更新失败: {ex.Message}")
            Return False
        End Try
    End Function

End Module
