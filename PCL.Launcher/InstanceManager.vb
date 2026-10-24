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

    Public Function ScanInstances(mcFolder As String) As List(Of InstanceInfo)
        Dim result As New List(Of InstanceInfo)
        Dim versionsDir = Path.Combine(mcFolder, "versions")
        If Not Directory.Exists(versionsDir) Then Return result

        For Each verDir In Directory.GetDirectories(versionsDir)
            Dim name = Path.GetFileName(verDir)
            Dim instanceJsonPath = Path.Combine(verDir, "PCL", "instance.json")
            
            If File.Exists(instanceJsonPath) Then
                Try
                    Dim instJson = JObject.Parse(File.ReadAllText(instanceJsonPath))
                    result.Add(New InstanceInfo With {
                        .Name = name,
                        .Path = verDir,
                        .IsIndependent = True,
                        .BaseVersion = instJson("baseVersion")?.ToString()
                    })
                Catch
                End Try
            End If
        Next
        Return result
    End Function

    Public Function CreateInstance(mcFolder As String, instanceName As String, baseVersion As String) As Boolean
        Try
            Dim versionDir = Path.Combine(mcFolder, "versions", instanceName)
            ' 如果已存在，不报错，直接覆盖
            If Not Directory.Exists(versionDir) Then
                Directory.CreateDirectory(versionDir)
            End If

            ' 1. 读取原版的完整 JSON
            Dim baseJsonPath = Path.Combine(mcFolder, "versions", baseVersion, baseVersion & ".json")
            If Not File.Exists(baseJsonPath) Then 
                Console.WriteLine($"找不到基础版本 JSON: {baseJsonPath}")
                Return False
            End If

            Dim baseJson = JObject.Parse(File.ReadAllText(baseJsonPath))
            ' 修改 id 为实例名
            baseJson("id") = instanceName
            ' 覆盖写入新实例的完整 JSON
            File.WriteAllText(Path.Combine(versionDir, instanceName & ".json"), baseJson.ToString())

            ' 2. 写入 PCL/instance.json 标记
            Dim pclDir = Path.Combine(versionDir, "PCL")
            If Not Directory.Exists(pclDir) Then Directory.CreateDirectory(pclDir)
            Dim instJson As New JObject From {
                {"baseVersion", baseVersion},
                {"createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
            }
            File.WriteAllText(Path.Combine(pclDir, "instance.json"), instJson.ToString())

            ' 3. 自动创建标准目录
            Directory.CreateDirectory(Path.Combine(versionDir, "mods"))
            Directory.CreateDirectory(Path.Combine(versionDir, "saves"))
            Directory.CreateDirectory(Path.Combine(versionDir, "shaderpacks"))
            Directory.CreateDirectory(Path.Combine(versionDir, "resourcepacks"))

            Return True
        Catch ex As Exception
            Console.WriteLine($"创建实例失败: {ex.Message}")
            Return False
        End Try
    End Function

End Module
