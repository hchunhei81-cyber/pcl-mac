Imports System
Imports System.Net.Http
Imports System.Collections.Generic
Imports Newtonsoft.Json.Linq

Public Module ModrinthApi

    Public Class SearchResult
        Public Property Id As String
        Public Property Slug As String
        Public Property Title As String
        Public Property Description As String
        Public Property Author As String
        Public Property Downloads As Long
        Public Property IconUrl As String
        Public Property ProjectType As String
    End Class

    Private ReadOnly HttpClient As New HttpClient(New HttpClientHandler With {
        .UseProxy = True,
        .Proxy = System.Net.WebRequest.DefaultWebProxy
    }) With {
        .Timeout = TimeSpan.FromSeconds(15)
    }

    ''' <summary>
    ''' 搜索 Modrinth 项目（修复了 facets 编码问题）。
    ''' projectType: "mod", "modpack", "resourcepack", "shader"
    ''' </summary>
    Public Function Search(query As String, projectType As String, limit As Integer) As List(Of SearchResult)
        Dim result As New List(Of SearchResult)
        Try
            ' 构建 facets 原始 JSON，然后进行 URL 编码
            Dim rawFacets = $"[[""project_type:{projectType}""]]"
            Dim encodedFacets = Uri.EscapeDataString(rawFacets)
            
            Dim url = $"https://api.modrinth.com/v2/search?query={Uri.EscapeDataString(query)}&limit={limit}&facets={encodedFacets}"
            Console.WriteLine($"Modrinth 搜索请求: {url}")

            Dim jsonStr As String = Nothing
            Dim maxRetries As Integer = 2
            For attempt As Integer = 1 To maxRetries
                Try
                    HttpClient.DefaultRequestHeaders.Clear()
                    HttpClient.DefaultRequestHeaders.Add("User-Agent", "PCL-macOS-Port/1.0 (github.com/hchunhei81-cyber/pcl-mac)")
                    jsonStr = HttpClient.GetStringAsync(url).Result
                    Exit For ' 成功则跳出重试
                Catch ex As Exception
                    If attempt = maxRetries Then Throw ' 最后一次也失败，抛出异常
                    Console.WriteLine($"请求失败，正在重试 ({attempt}/{maxRetries})...")
                    Threading.Thread.Sleep(2000) ' 等 2 秒再试
                End Try
            Next
            
            Dim json = JObject.Parse(jsonStr)
            Dim hits = TryCast(json("hits"), JArray)
            If hits IsNot Nothing Then
                For Each hit In hits
                    result.Add(New SearchResult With {
                        .Id = hit("project_id")?.ToString(),
                        .Slug = hit("slug")?.ToString(),
                        .Title = hit("title")?.ToString(),
                        .Description = hit("description")?.ToString(),
                        .Author = hit("author")?.ToString(),
                        .Downloads = If(hit("downloads") IsNot Nothing, hit("downloads").Value(Of Long)(), 0),
                        .IconUrl = hit("icon_url")?.ToString(),
                        .ProjectType = hit("project_type")?.ToString()
                    })
                Next
            End If
        Catch ex As Exception
            Console.WriteLine($"Modrinth 搜索异常: {ex}")
            Throw New Exception($"网络请求失败: {ex.Message}", ex)
        End Try
        Return result
    End Function

    ''' <summary>
    ''' 获取指定 Modrinth 项目对应游戏版本和加载器的下载链接。
    ''' </summary>
    Public Function GetDownloadUrl(projectId As String, mcVersion As String, loader As String) As String
        Try
            Dim url = $"https://api.modrinth.com/v2/project/{projectId}/version?game_versions=[%22{mcVersion}%22]&loaders=[%22{loader}%22]"
            HttpClient.DefaultRequestHeaders.Clear()
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "PCL-macOS-Port/1.0 (github.com/hchunhei81-cyber/pcl-mac)")
            
            Dim jsonStr = HttpClient.GetStringAsync(url).Result
            Dim versions = JArray.Parse(jsonStr)
            If versions.Count > 0 Then
                Dim files = TryCast(versions(0)("files"), JArray)
                If files IsNot Nothing AndAlso files.Count > 0 Then
                    Return files(0)("url")?.ToString()
                End If
            End If
        Catch ex As Exception
            Console.WriteLine($"获取下载链接失败: {ex.Message}")
        End Try
        Return Nothing
    End Function

End Module
