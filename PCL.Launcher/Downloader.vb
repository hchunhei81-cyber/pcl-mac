Imports System
Imports System.IO
Imports System.Net.Http
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Concurrent
Imports System.Diagnostics
Imports Newtonsoft.Json.Linq

Public Module DownloadCore

    Public Class DownloadOptions
        Public Property McFolder As String
        Public Property VersionName As String
        Public Property MaxParallel As Integer = 8
        Public Property UseMirror As Boolean = False ' True = BMCLAPI, False = Mojang
        ' (阶段, 已完成, 总数, 当前速度 KB/s)
        Public Property ProgressCallback As Action(Of String, Integer, Integer, Double)
    End Class

    Public Class DownloadResult
        Public Property Success As Boolean
        Public Property Message As String
        Public Property DownloadedFiles As Integer
        Public Property SkippedFiles As Integer
        Public Property FailedFiles As Integer
    End Class

    Private ReadOnly HttpClient As New HttpClient() With {.Timeout = TimeSpan.FromMinutes(5)}

    Public Function Download(options As DownloadOptions) As DownloadResult
        Dim result As New DownloadResult()
        Try
            Dim mcDir = options.McFolder
            Dim versionDir = Path.Combine(mcDir, "versions", options.VersionName)
            Directory.CreateDirectory(versionDir)

            ' 源地址
            Dim metaBase = If(options.UseMirror, "https://bmclapi2.bangbang93.com", "https://piston-meta.mojang.com")
            Dim libBase = If(options.UseMirror, "https://bmclapi2.bangbang93.com/maven", "https://libraries.minecraft.net")
            Dim assetBase = If(options.UseMirror, "https://bmclapi2.bangbang93.com/assets", "https://resources.download.minecraft.net")

            ' 1. 版本 JSON
            Dim versionJsonPath = Path.Combine(versionDir, options.VersionName & ".json")
            Dim json As JObject = Nothing

            If Not File.Exists(versionJsonPath) Then
                Report(options, "获取版本清单", 0, 1)
                Dim manifestUrl = $"{metaBase}/mc/game/version_manifest_v2.json"
                Dim manifestJson = HttpClient.GetStringAsync(manifestUrl).Result
                Dim manifest = JObject.Parse(manifestJson)
                Dim versionUrl As String = Nothing
                For Each v In CType(manifest("versions"), JArray)
                    If v("id").ToString() = options.VersionName Then
                        versionUrl = v("url").ToString().Replace("https://piston-meta.mojang.com", metaBase)
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

            json = JObject.Parse(File.ReadAllText(versionJsonPath))
            Dim downloads = TryCast(json("downloads"), JObject)

            ' 2. 版本 jar
            Dim versionJarPath = Path.Combine(versionDir, options.VersionName & ".jar")
            If Not File.Exists(versionJarPath) AndAlso downloads IsNot Nothing Then
                Dim jarInfo = TryCast(downloads("client"), JObject)
                If jarInfo IsNot Nothing Then
                    Dim jarUrl = jarInfo("url").ToString().Replace("https://piston-data.mojang.com", metaBase).Replace("https://launcher.mojang.com", metaBase)
                    Report(options, "下载版本 jar", 0, 1)
                    DownloadFile(jarUrl, versionJarPath).Wait()
                    result.DownloadedFiles += 1
                End If
            Else
                result.SkippedFiles += 1
            End If

            ' 3. libraries
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
                    Dim libUrl = $"{libBase}/{relPath}"
                    libTasks.Add((libUrl, libPath))
                End If
            Next
            If libTasks.Count > 0 Then
                Report(options, "下载 libraries", 0, libTasks.Count)
                Dim done As Integer = 0
                Dim failed As Integer = 0
                Dim bytesTotal As Long = 0
                Dim sw = Stopwatch.StartNew()
                Dim lastBytes As Long = 0
                Dim lastTime As Long = 0

                Parallel.ForEach(libTasks, New ParallelOptions With {.MaxDegreeOfParallelism = options.MaxParallel},
                    Sub(task)
                        Try
                            Directory.CreateDirectory(Path.GetDirectoryName(task.Path))
                            Dim bytes = DownloadFile(task.Url, task.Path).Result
                            Interlocked.Add(bytesTotal, bytes)
                            Interlocked.Increment(done)
                            result.DownloadedFiles += 1
                            
                            ' 计算速度
                            Dim elapsed = sw.ElapsedMilliseconds
                            If elapsed - lastTime > 200 Then
                                Dim speed = (bytesTotal - lastBytes) / 1024.0 / ((elapsed - lastTime) / 1000.0)
                                lastBytes = bytesTotal
                                lastTime = elapsed
                                Report(options, "下载 libraries", done, libTasks.Count, speed)
                            End If
                        Catch ex As Exception
                            Interlocked.Increment(failed)
                        End Try
                    End Sub)
                result.FailedFiles += failed
            End If

            ' 4. assets
            Dim assetIndex = TryCast(json("assetIndex"), JObject)
            If assetIndex IsNot Nothing Then
                Dim assetIndexId = assetIndex("id").ToString()
                Dim assetIndexUrl = assetIndex("url").ToString().Replace("https://piston-meta.mojang.com", metaBase)
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
                            assetTasks.Add(($"{assetBase}/{prefix}/{hash}", assetPath))
                        Else
                            result.SkippedFiles += 1
                        End If
                    Next
                    If assetTasks.Count > 0 Then
                        Report(options, "下载 assets", 0, assetTasks.Count)
                        Dim done As Integer = 0
                        Dim bytesTotal As Long = 0
                        Dim sw = Stopwatch.StartNew()
                        Dim lastBytes As Long = 0
                        Dim lastTime As Long = 0
                        Parallel.ForEach(assetTasks, New ParallelOptions With {.MaxDegreeOfParallelism = options.MaxParallel},
                            Sub(task)
                                Try
                                    Directory.CreateDirectory(Path.GetDirectoryName(task.Path))
                                    Dim bytes = DownloadFile(task.Url, task.Path).Result
                                    Interlocked.Add(bytesTotal, bytes)
                                    Interlocked.Increment(done)
                                    result.DownloadedFiles += 1
                                    
                                    Dim elapsed = sw.ElapsedMilliseconds
                                    If elapsed - lastTime > 200 Then
                                        Dim speed = (bytesTotal - lastBytes) / 1024.0 / ((elapsed - lastTime) / 1000.0)
                                        lastBytes = bytesTotal
                                        lastTime = elapsed
                                        Report(options, "下载 assets", done, assetTasks.Count, speed)
                                    End If
                                Catch ex As Exception
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

    Private Async Function DownloadFile(url As String, path As String) As Task(Of Long)
        Using resp = Await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
            resp.EnsureSuccessStatusCode()
            Dim contentLength = resp.Content.Headers.ContentLength.GetValueOrDefault(0)
            Using fs As New FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync:=True)
                Await resp.Content.CopyToAsync(fs)
            End Using
            Return contentLength
        End Using
    End Function

    Private Sub Report(options As DownloadOptions, stage As String, done As Integer, total As Integer, Optional speed As Double = 0)
        options.ProgressCallback?.Invoke(stage, done, total, speed)
    End Sub
End Module
