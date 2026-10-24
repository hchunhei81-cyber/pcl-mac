Imports System
Imports System.Net.Http
Imports System.Reflection
Imports Newtonsoft.Json.Linq

Public Module UpdateChecker

    Public Class UpdateInfo
        Public Property HasUpdate As Boolean
        Public Property CurrentVersion As String
        Public Property LatestVersion As String
        Public Property DownloadUrl As String
        Public Property ReleaseNotes As String
    End Class

    Private Const RepoOwner As String = "hchunhei81-cyber"
    Private Const RepoName As String = "pcl-mac"

    ''' <summary>
    ''' 从 GitHub Releases API 检查是否有新版本。
    ''' </summary>
    Public Function CheckForUpdates() As UpdateInfo
        Dim result As New UpdateInfo()
        Try
            ' 获取当前程序版本（从 csproj 的 <Version> 或默认 1.0.0）
            Dim asm = Assembly.GetEntryAssembly()
            Dim currentVersion = asm?.GetName()?.Version?.ToString(3) ' x.y.z
            If String.IsNullOrEmpty(currentVersion) Then currentVersion = "1.0.0"
            result.CurrentVersion = currentVersion

            ' 请求 GitHub API
            Using client As New HttpClient()
                client.DefaultRequestHeaders.Add("User-Agent", "PCL-macOS-Port")
                client.Timeout = TimeSpan.FromSeconds(10)
                Dim url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest"
                Dim jsonStr = client.GetStringAsync(url).Result
                Dim json = JObject.Parse(jsonStr)

                Dim tagName = json("tag_name")?.ToString() ' e.g. "v1.0.1-mac"
                Dim releaseName = json("name")?.ToString()
                Dim body = json("body")?.ToString()

                ' 解析版本号（去掉 v 和 -mac）
                Dim latestVersion = tagName?.TrimStart("v"c).Replace("-mac", "")
                result.LatestVersion = latestVersion
                result.ReleaseNotes = body

                ' 找 zip 附件
                Dim assets = TryCast(json("assets"), JArray)
                If assets IsNot Nothing Then
                    For Each asset In assets
                        Dim assetName = asset("name")?.ToString()
                        If assetName IsNot Nothing AndAlso assetName.EndsWith(".zip") Then
                            result.DownloadUrl = asset("browser_download_url")?.ToString()
                            Exit For
                        End If
                    Next
                End If

                ' 对比版本
                result.HasUpdate = CompareVersions(latestVersion, currentVersion) > 0
            End Using
        Catch ex As Exception
            Console.WriteLine($"检查更新失败: {ex.Message}")
            result.HasUpdate = False
        End Try
        Return result
    End Function

    ''' <summary>
    ''' 对比版本号，返回 1 表示 v1 > v2，0 相等，-1 v1 < v2。
    ''' </summary>
    Private Function CompareVersions(v1 As String, v2 As String) As Integer
        Try
            Dim a = Version.Parse(v1)
            Dim b = Version.Parse(v2)
            Return a.CompareTo(b)
        Catch
            Return 0
        End Try
    End Function

End Module
