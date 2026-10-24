Imports System
Imports System.IO
Imports System.Net.Http
Imports System.Diagnostics
Imports System.Threading.Tasks

Public Module ForgeInstaller

    Public Class ForgeInstallOptions
        Public Property McFolder As String
        Public Property McVersion As String
        Public Property ForgeVersion As String ' 例如 "1.20.1-47.2.0"
        Public Property JavaPath As String
    End Class

    Public Class ForgeInstallResult
        Public Property Success As Boolean
        Public Property Message As String
    End Class

    Private ReadOnly HttpClient As New HttpClient() With {.Timeout = TimeSpan.FromMinutes(10)}

    ''' <summary>
    ''' 下载 Forge installer 并调用其 --installClient 模式完成安装。
    ''' </summary>
    Public Function Install(options As ForgeInstallOptions) As ForgeInstallResult
        Dim result As New ForgeInstallResult()
        Try
            ' 1. 下载 installer jar
            Dim installerUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{options.ForgeVersion}/forge-{options.ForgeVersion}-installer.jar"
            Dim tempDir = Path.Combine(Path.GetTempPath(), "PCL_Forge_" & Guid.NewGuid().ToString("N").Substring(0, 8))
            Directory.CreateDirectory(tempDir)
            Dim installerPath = Path.Combine(tempDir, "forge-installer.jar")
            Dim bytes = HttpClient.GetByteArrayAsync(installerUrl).Result
            File.WriteAllBytes(installerPath, bytes)

            ' 2. 找 Java
            Dim javaPath = options.JavaPath
            If String.IsNullOrEmpty(javaPath) Then
                Dim detected = JavaDetector.Detect()
                If detected Is Nothing Then
                    result.Message = "未找到可用的 Java"
                    Return result
                End If
                javaPath = detected.Path
            End If

            ' 3. 运行 installer
            Dim psi As New ProcessStartInfo(javaPath) With {
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .UseShellExecute = False,
                .CreateNoWindow = True
            }
            psi.ArgumentList.Add("-jar")
            psi.ArgumentList.Add(installerPath)
            psi.ArgumentList.Add("--installClient")
            psi.ArgumentList.Add(options.McFolder)

            Dim proc = Process.Start(psi)
            Dim stdout = proc.StandardOutput.ReadToEnd()
            Dim stderr = proc.StandardError.ReadToEnd()
            proc.WaitForExit()

            ' 4. 清理
            Try
                Directory.Delete(tempDir, True)
            Catch
            End Try

            If proc.ExitCode = 0 Then
                result.Success = True
                result.Message = "Forge 安装成功"
            Else
                result.Success = False
                result.Message = $"安装失败（退出码 {proc.ExitCode}）: {stderr}"
            End If
        Catch ex As Exception
            result.Success = False
            result.Message = $"安装出错: {ex.GetType().Name}: {ex.Message}"
        End Try
        Return result
    End Function

End Module
