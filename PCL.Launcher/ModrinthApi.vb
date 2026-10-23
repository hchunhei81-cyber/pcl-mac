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

    Private ReadOnly HttpClient As New HttpClient() With {
        .Timeout = TimeSpan.FromSeconds(15)
    }

    ''' <summary>
    ''' 搜索 Modrinth 项目。
    ''' projectType: "mod", "modpack", "resourcepack", "shader"
    ''' </summary>
    Public Function Search(query As String, projectType As String, limit As Integer) As List(Of SearchResult)
        Dim result As New List(Of SearchResult)
        Try
            Dim url = $"https://api.modrinth.com/v2/search?query={Uri.EscapeDataString(query)}&limit={limit}&facets=[[%22project_type:{projectType}%22]]"
            HttpClient.DefaultRequestHeaders.Clear()
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "PCL-macOS-Port/1.0 (github.com/hchunhei81-cyber/pcl-mac)")
            Dim jsonStr = HttpClient.GetStringAsync(url).Result
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
            Console.WriteLine($"Modrinth 搜索失败: {ex.Message}")
        End Try
        Return result
    End Function

End Module
