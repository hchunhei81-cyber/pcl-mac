Imports System.IO

Public Module SkinManager

    ''' <summary>
    ''' 保存本地皮肤到实例目录。
    ''' </summary>
    Public Function SaveLocalSkin(instancePath As String, skinFilePath As String) As Boolean
        Try
            If Not File.Exists(skinFilePath) Then Return False
            
            Dim pclDir = Path.Combine(instancePath, "PCL")
            If Not Directory.Exists(pclDir) Then Directory.CreateDirectory(pclDir)

            Dim destPath = Path.Combine(pclDir, "skin.png")
            File.Copy(skinFilePath, destPath, True)
            
            Return True
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 获取实例当前的本地皮肤路径。
    ''' </summary>
    Public Function GetLocalSkin(instancePath As String) As String
        Dim destPath = Path.Combine(instancePath, "PCL", "skin.png")
        If File.Exists(destPath) Then Return destPath
        Return Nothing
    End Function

End Module
