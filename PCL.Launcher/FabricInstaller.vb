Imports System
Imports System.IO
Imports System.Net.Http
Imports System.Diagnostics
Imports System.Threading.Tasks
Imports Newtonsoft.Json.Linq

Public Module FabricInstaller

    Public Class FabricInstallOptions
        Public Property McFolder As String
        Public Property McVersion As String
        Public Property JavaPath As String ' 可选，留空自动检测
    End Class

    Public Class FabricInstallResult
        Public Property Success As Boolean
        Public Property Message As String
        Public Property LoaderVersion As String
    End Class

    Private ReadOnly HttpClient As New HttpClient() With {.Timeout = TimeSpan.FromMinutes(2)}

    ''' <summary>
    ''' 自动安装 Fabric Loader。
    ''' </summary>
    Public Function Install(options As FabricInstallOptions) As FabricInstallResult
        Dim result As New FabricInstallResult()
        Try
            ' 1. 获取最新的 Fabric Loader 和 Installer 版本
            Dim loaderVersion = GetLatestLoaderVersion(options.McVersion)
            If loaderVersion Is Nothing Then
                result.Message = $"未找到 {options.McVersion} 对应的 Fabric Loader"
                Return result
            End If
            result.LoaderVersion = loaderVersion

            Dim installerUrl = GetLatestInstallerUrl()
            If installerUrl Is Nothing Then
                result.Message = "无法获取 Fabric Installer 下载地址"
                Return result
            End If

            ' 2. 下载 Installer jar 到临时目录
            Dim tempDir = Path.Combine(Path.GetTempPath(), "PCL_Fabric_" & Guid.NewGuid().ToString("N").Substring(0, 8))
            Directory.CreateDirectory(tempDir)
            Dim installerPath = Path.Combine(tempDir, "fabric-installer.jar")
            Dim bytes = HttpClient.GetByteArrayAsync(installerUrl).Result
            File.WriteAllBytes(installerPath, bytes)

            ' 3. 找 Java
            Dim javaPath = options.JavaPath
            If String.IsNullOrEmpty(javaPath) Then
                Dim detected = JavaDetector.Detect()
                If detected Is Nothing Then
                    result.Message = "未找到可用的 Java"
                    Return result
                End If
                javaPath = detected.Path
            End If

            ' 4. 运行 Installer（静默模式）
            Dim psi As New ProcessStartInfo(javaPath) With {
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .UseShellExecute = False,
                .CreateNoWindow = True
            }
            psi.ArgumentList.Add("-jar")
            psi.ArgumentList.Add(installerPath)
            psi.ArgumentList.Add("client")
            psi.ArgumentList.Add("-dir")
            psi.ArgumentList.Add(options.McFolder)
            psi.ArgumentList.Add("-mcversion")
            psi.ArgumentList.Add(options.McVersion)
            psi.ArgumentList.Add("-loader")
            psi.ArgumentList.Add(loaderVersion)
            psi.ArgumentList.Add("-noprofile")

            Dim proc = Process.Start(psi)
            Dim stdout = proc.StandardOutput.ReadToEnd()
            Dim stderr = proc.StandardError.ReadToEnd()
            proc.WaitForExit()

            ' 5. 清理临时文件
            Try
                Directory.Delete(tempDir, True)
            Catch
            End Try

            If proc.ExitCode = 0 Then
                result.Success = True
                result.Message = $"Fabric {loaderVersion} 安装成功"
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

    ''' <summary>
    ''' 获取指定 MC 版本最新的 Fabric Loader 版本号。
    ''' </summary>
    Private Function GetLatestLoaderVersion(mcVersion As String) As String
        Try
            Dim url = $"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}"
            Dim json = JArray.Parse(HttpClient.GetStringAsync(url).Result)
            If json.Count = 0 Then Return Nothing
            Return json(0)("loader")("version")?.ToString()
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' 获取最新的 Fabric Installer jar 下载地址。
    ''' </summary>
    Private Function GetLatestInstallerUrl() As String
        Try
            Dim url = "https://meta.fabricmc.net/v2/versions/installer"
            Dim json = JArray.Parse(HttpClient.GetStringAsync(url).Result)
            If json.Count = 0 Then Return Nothing
            ' 找 stable 版本
            For Each item In json
                If item("stable") IsNot Nothing AndAlso item("stable").Value(Of Boolean)() Then
                    Return item("url")?.ToString()
                End If
            Next
            ' 没找到 stable 就用第一个
            Return json(0)("url")?.ToString()
        Catch
            Return Nothing
        End Try
    End Function

End Module
