Imports System.IO
Imports System.Collections.Generic

Public Module VersionScanner

    ''' <summary>
    ''' 扫描指定 Minecraft 目录下的 versions 文件夹，返回包含有效 .json 的版本名列表。
    ''' </summary>
    Public Function ScanVersions(mcFolder As String) As List(Of String)
        Dim result As New List(Of String)
        If String.IsNullOrEmpty(mcFolder) Then Return result

        Dim versionsDir = Path.Combine(mcFolder, "versions")
        If Not Directory.Exists(versionsDir) Then Return result

        For Each verDir In Directory.GetDirectories(versionsDir)
            Dim versionName = Path.GetFileName(verDir)
            Dim jsonPath = Path.Combine(verDir, versionName & ".json")
            If File.Exists(jsonPath) Then
                result.Add(versionName)
            End If
        Next

        ' 按名称降序（通常数字大的版本在前面）
        result.Sort(Function(a, b) b.CompareTo(a))
        Return result
    End Function

End Module
