Imports System
Imports System.Net.Http
Imports System.Collections.Generic
Imports Newtonsoft.Json.Linq

Public Module VersionManifest

    Public Class VersionInfo
        Public Property Id As String
        Public Property Type As String ' release / snapshot
    End Class

    Public Function FetchMojangVersions() As List(Of VersionInfo)
        Dim result As New List(Of VersionInfo)
        Try
            Using client As New HttpClient()
                client.Timeout = TimeSpan.FromSeconds(10)
                Dim jsonStr = client.GetStringAsync("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json").Result
                Dim json = JObject.Parse(jsonStr)
                Dim versions = TryCast(json("versions"), JArray)
                If versions IsNot Nothing Then
                    For Each v In versions
                        result.Add(New VersionInfo With {
                            .Id = v("id").ToString(),
                            .Type = v("type").ToString()
                        })
                    Next
                End If
            End Using
        Catch ex As Exception
            Console.WriteLine($"拉取版本列表失败: {ex.Message}")
        End Try
        Return result
    End Function

End Module
