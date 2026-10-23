Imports System
Imports System.IO
Imports System.Net.Http
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Concurrent
Imports Newtonsoft.Json.Linq

''' <summary>
''' 最小 Minecraft 下载器：版本 JSON、jar、libraries、assets。
''' 不依赖 PCL.Modules 的 UI 进度系统，独立实现。
''' </summary>
Public Module Downloader

    Public Class DownloadOptions
        Public Property McFolder As String
        Public Property VersionName As String
        Public Property MaxParallel As Integer = 8
        Public Property UseMirror As Boolean = False ' 预留：BMCLAPI 等镜像
        Public Property ProgressCallback As Action(Of String, Integer, Integer) ' (阶段, 已完成, 总数)
    End Class

    Public Class DownloadResult
        Public Property Success As Boolean
        Public Property Message As String
        Public Property DownloadedFiles As Integer
        Public Property SkippedFiles As Integer
        Public Property FailedFiles As Integer
    End Class

    Private ReadOnly HttpClient As New HttpClient() With {.Timeout = TimeSpan.FromMinutes(5)}

    ' Mojang 官方源
    Private Const VersionManifestUrl As String = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json"
    Private Const LibrariesBaseUrl As String = "https://libraries.minecraft.net/"
    Private Const ResourcesBaseUrl As String = "https://resources.download.minecraft.net/"

    ''' <summary>
    ''' 下载指定版本的 JSON、jar、libraries、assets。
    ''' </summary>
    Public Function Download(options As DownloadOptions) As DownloadResult
        Dim result As New DownloadResult()
        Try
            Dim mcDir = options.McFolder
            Dim versionDir = Path.Combine(mcDir, "versions", options.VersionName)
            Directory.CreateDirectory(versionDir)

            ' 1. 版本 JSON
            Dim versionJsonPath = Path.Combine(versionDir, options.VersionName & ".json")
            If Not File.Exists(versionJsonPath) Then
                Report(options, "获取版本清单", 0, 1)
                Dim manifestJson = HttpClient.GetStringAsync(VersionManifestUrl).Result
                Dim manifest = JObject.Parse(manifestJson)
                Dim versionUrl As String = Nothing
                For Each v In CType(manifest("versions"), JArray)
                    If v("id").ToString() = options.VersionName Then
                        versionUrl = v("url").ToString()
                        Exit For
                    End If
                Next
                If versionUrl Is Nothing Then
                    result.Message = $"版本清单中未找到 {options.VersionName}"
                    Return result
                End If
                Dim versionJson = HttpClient.GetStringAsync(versionUrl).Result
                File.WriteAllText(versionJsonPath, versionJson)
                result.DownloadedFiles += 1
            Else
                result.SkippedFiles += 1
            End If

            ' 2. 解析 JSON
            Dim json = JObject.Parse(File.ReadAllText(versionJsonPath))
            Dim downloads = TryCast(json("downloads"), JObject)

            ' 3. 版本 jar
            Dim versionJarPath = Path.Combine(versionDir, options.VersionName & ".jar")
            If Not File.Exists(versionJarPath) AndAlso downloads IsNot Nothing Then
                Dim jarInfo = TryCast(downloads("client"), JObject)
                If jarInfo IsNot Nothing Then
                    Dim jarUrl = jarInfo("url").ToString()
                    Report(options, "下载版本 jar", 0, 1)
                    DownloadFile(jarUrl, versionJarPath).Wait()
                    result.DownloadedFiles += 1
                End If
            Else
                result.SkippedFiles += 1
            End If

            ' 4. libraries
            Dim libsDir = Path.Combine(mcDir, "libraries")
            Dim libTasks As New List(Of (Url As String, Path As String))
            For Each libItem As JToken In CType(json("libraries"), JArray)
                Dim libDownloads = TryCast(libItem("downloads"), JObject)
                If libDownloads Is Nothing Then Continue For
                Dim artifact = TryCast(libDownloads("artifact"), JObject)
                If artifact Is Nothing Then Continue For
                Dim relPath = artifact("path").ToString()
                Dim libPath = Path.Combine(libsDir, relPath.Replace("/", Path.DirectorySeparatorChar))
                If File.Exists(libPath) Then
                    result.SkippedFiles += 1
                Else
                    libTasks.Add((artifact("url").ToString(), libPath))
                End If
            Next
            If libTasks.Count > 0 Then
                Report(options, "下载 libraries", 0, libTasks.Count)
                Dim done As Integer = 0
                Dim failed As Integer = 0
                Parallel.ForEach(libTasks, New ParallelOptions With {.MaxDegreeOfParallelism = options.MaxParallel},
                    Sub(task)
                        Try
                            Directory.CreateDirectory(Path.GetDirectoryName(task.Path))
                            DownloadFile(task.Url, task.Path).Wait()
                            Interlocked.Increment(done)
                            result.DownloadedFiles += 1
                            Report(options, "下载 libraries", done, libTasks.Count)
                        Catch ex As Exception
                            Interlocked.Increment(failed)
                            Console.WriteLine($"[WARN] 库下载失败: {task.Url} - {ex.Message}")
                        End Try
                    End Sub)
                result.FailedFiles += failed
            End If

            ' 5. assets
            Dim assetIndex = TryCast(json("assetIndex"), JObject)
            If assetIndex IsNot Nothing Then
                Dim assetIndexId = assetIndex("id").ToString()
                Dim assetIndexUrl = assetIndex("url").ToString()
                Dim assetIndexDir = Path.Combine(mcDir, "assets", "indexes")
                Directory.CreateDirectory(assetIndexDir)
                Dim assetIndexPath = Path.Combine(assetIndexDir, assetIndexId & ".json")
                If Not File.Exists(assetIndexPath) Then
                    Report(options, "下载 asset index", 0, 1)
                    DownloadFile(assetIndexUrl, assetIndexPath).Wait()
                    result.DownloadedFiles += 1
                End If

                Dim assetIndexJson = JObject.Parse(File.ReadAllText(assetIndexPath))
                Dim objects = TryCast(assetIndexJson("objects"), JObject)
                If objects IsNot Nothing Then
                    Dim assetsDir = Path.Combine(mcDir, "assets", "objects")
                    Dim assetTasks As New List(Of (Url As String, Path As String))
                    For Each prop In objects.Properties()
                        Dim hash = prop.Value("hash").ToString()
                        Dim prefix = hash.Substring(0, 2)
                        Dim assetPath = Path.Combine(assetsDir, prefix, hash)
                        If Not File.Exists(assetPath) Then
                            assetTasks.Add(($"{ResourcesBaseUrl}{prefix}/{hash}", assetPath))
                        Else
                            result.SkippedFiles += 1
                        End If
                    Next
                    If assetTasks.Count > 0 Then
                        Report(options, "下载 assets", 0, assetTasks.Count)
                        Dim done As Integer = 0
                        Parallel.ForEach(assetTasks, New ParallelOptions With {.MaxDegreeOfParallelism = options.MaxParallel},
                            Sub(task)
                                Try
                                    Directory.CreateDirectory(Path.GetDirectoryName(task.Path))
                                    DownloadFile(task.Url, task.Path).Wait()
                                    Interlocked.Increment(done)
                                    result.DownloadedFiles += 1
                                    Report(options, "下载 assets", done, assetTasks.Count)
                                Catch ex As Exception
                                    Console.WriteLine($"[WARN] Asset 下载失败: {task.Url} - {ex.Message}")
                                End Try
                            End Sub)
                    End If
                End If
            End If

            result.Success = True
            result.Message = "下载完成"
        Catch ex As Exception
            result.Success = False
            result.Message = $"下载失败: {ex.GetType().Name}: {ex.Message}"
        End Try
        Return result
    End Function

    Private Async Function DownloadFile(url As String, path As String) As Task
        Using resp = Await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
            resp.EnsureSuccessStatusCode()
            Using fs As New FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync:=True)
                Await resp.Content.CopyToAsync(fs)
            End Using
        End Using
    End Function

    Private Sub Report(options As DownloadOptions, stage As String, done As Integer, total As Integer)
        options.ProgressCallback?.Invoke(stage, done, total)
    End Sub

End Module
