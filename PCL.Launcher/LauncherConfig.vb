Imports System
Imports System.IO
Imports Newtonsoft.Json.Linq

Public Class LauncherConfig
    Public Property PlayerName As String = "Player"
    Public Property VersionName As String = ""
    Public Property MaxMemoryMb As Integer = 2048
    Public Property JavaPath As String = ""
    Public Property IsOfflineMode As Boolean = True

    Private Shared ReadOnly ConfigPath As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "Application Support", "pcl-mac", "config.json")

    Public Shared Function Load() As LauncherConfig
        Try
            If File.Exists(ConfigPath) Then
                Dim json = JObject.Parse(File.ReadAllText(ConfigPath))
                Return New LauncherConfig With {
                    .PlayerName = If(json("PlayerName")?.ToString(), "Player"),
                    .VersionName = If(json("VersionName")?.ToString(), ""),
                    .MaxMemoryMb = If(json("MaxMemoryMb") IsNot Nothing, json("MaxMemoryMb").Value(Of Integer)(), 2048),
                    .JavaPath = If(json("JavaPath")?.ToString(), ""),
                    .IsOfflineMode = If(json("IsOfflineMode") IsNot Nothing, json("IsOfflineMode").Value(Of Boolean)(), True)
                }
            End If
        Catch ex As Exception
            Console.WriteLine($"加载配置失败: {ex.Message}")
        End Try
        Return New LauncherConfig()
    End Function

    Public Sub Save()
        Try
            Dim dir = Path.GetDirectoryName(ConfigPath)
            If Not Directory.Exists(dir) Then Directory.CreateDirectory(dir)

            Dim json As New JObject From {
                {"PlayerName", PlayerName},
                {"VersionName", VersionName},
                {"MaxMemoryMb", MaxMemoryMb},
                {"JavaPath", JavaPath},
                {"IsOfflineMode", IsOfflineMode}
            }
            File.WriteAllText(ConfigPath, json.ToString())
        Catch ex As Exception
            Console.WriteLine($"保存配置失败: {ex.Message}")
        End Try
    End Sub
End Class
