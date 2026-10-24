Imports System
Imports System.IO
Imports Newtonsoft.Json.Linq

Public Module InstanceManager

    Public Class InstanceInfo
        Public Property Name As String
        Public Property IsIndependent As Boolean
        Public Property BaseVersion As String
        Public Property Path As String
    End Class

    ''' <summary>
    ''' 扫描所有版本，并区分独立实例和原版。
    ''' </summary>
    Public Function ScanInstances(mcFolder As String) As List(Of InstanceInfo)
        Dim result As New List(Of InstanceInfo)
        Dim versionsDir = Path.Combine(mcFolder, "versions")
        If Not Directory.Exists(versionsDir) Then Return result

        For Each verDir In Directory.GetDirectories(versionsDir)
            Dim name = Path.GetFileName(verDir)
            Dim jsonPath = Path.Combine(dir, name & ".json")
            If Not File.Exists(jsonPath) Then Continue For

            Dim info As New InstanceInfo With {
                .Name = name,
                .Path = verDir,
                .IsIndependent = False
            }

            ' 检查 PCL/instance.json 是否存在
            Dim instanceJsonPath = Path.Combine(dir, "PCL", "instance.json")
            If File.Exists(instanceJsonPath) Then
                Try
                    Dim instJson = JObject.Parse(File.ReadAllText(instanceJsonPath))
                    info.IsIndependent = True
                    info.BaseVersion = instJson("baseVersion")?.ToString()
                Catch
                End Try
            End If

            result.Add(info)
        Next
        Return result
    End Function

    ''' <summary>
    ''' 创建一个新的独立实例。
    ''' </summary>
    Public Function CreateInstance(mcFolder As String, instanceName As String, baseVersion As String) As Boolean
        Try
            Dim versionDir = Path.Combine(mcFolder, "versions", instanceName)
            If Directory.Exists(versionDir) Then Return False ' 已存在
            Directory.CreateDirectory(versionDir)

            ' 1. 写入版本的 JSON (使用 inheritsFrom)
            Dim versionJson As New JObject From {
                {"id", instanceName},
                {"inheritsFrom", baseVersion},
                {"type", "release"}
            }
            File.WriteAllText(Path.Combine(versionDir, instanceName & ".json"), versionJson.ToString())

            ' 2. 写入 PCL/instance.json 标记
            Dim pclDir = Path.Combine(versionDir, "PCL")
            Directory.CreateDirectory(pclDir)
            Dim instJson As New JObject From {
                {"baseVersion", baseVersion},
                {"createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
            }
            File.WriteAllText(Path.Combine(pclDir, "instance.json"), instJson.ToString())

            Return True
        Catch ex As Exception
            Console.WriteLine($"创建实例失败: {ex.Message}")
            Return False
        End Try
    End Function

End Module
