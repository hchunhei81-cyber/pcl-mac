Imports System.IO
Imports System.Collections.Generic

Public Module ModScanner

    Public Class ModInfo
        Public Property FileName As String
        Public Property FullPath As String
    End Class

    ''' <summary>
    ''' 扫描实例目录下的 mods 文件夹，返回所有 jar 文件。
    ''' </summary>
    Public Function ScanMods(instancePath As String) As List(Of ModInfo)
        Dim result As New List(Of ModInfo)
        Dim modsDir = Path.Combine(instancePath, "mods")
        If Not Directory.Exists(modsDir) Then Return result

        For Each file In Directory.GetFiles(modsDir, "*.jar")
            result.Add(New ModInfo With {
                .FileName = Path.GetFileName(file),
                .FullPath = file
            })
        Next

        result.Sort(Function(a, b) String.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase))
        Return result
    End Function

End Module
