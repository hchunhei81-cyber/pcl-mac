Imports System.IO
Imports System.Collections.Generic

Public Module ModScanner

    Public Class ModInfo
        Public Property FileName As String
        Public Property FullPath As String
        Public Property IsEnabled As Boolean
    End Class

    Public Function ScanMods(instancePath As String) As List(Of ModInfo)
        Dim result As New List(Of ModInfo)
        Dim modsDir = Path.Combine(instancePath, "mods")
        If Not Directory.Exists(modsDir) Then Return result

        ' 扫描所有以 .jar 或 .jar.disabled 结尾的文件
        For Each file In Directory.GetFiles(modsDir, "*.jar*")
            Dim fName = Path.GetFileName(file)
            Dim isEnabled = fName.EndsWith(".jar")
            
            result.Add(New ModInfo With {
                .FileName = If(isEnabled, fName, fName.Replace(".jar.disabled", ".jar")),
                .FullPath = file,
                .IsEnabled = isEnabled
            })
        Next

        result.Sort(Function(a, b) String.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase))
        Return result
    End Function

End Module
