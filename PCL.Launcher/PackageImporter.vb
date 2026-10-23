Imports System
Imports System.IO
Imports System.IO.Compression
Imports Newtonsoft.Json.Linq

Public Module PackageImporter

    ''' <summary>
    ''' 导入本地整合包（支持 .mrpack 和 包含 mods 的 .zip）。
    ''' </summary>
    ''' <param name="packagePath">压缩包路径</param>
    ''' <param name="targetVersionName">安装后的版本名称（文件夹名）</param>
    ''' <param name="mcFolder">Minecraft 根目录</param>
    Public Function ImportPackage(packagePath As String, targetVersionName As String, mcFolder As String) As Boolean
        Try
            Dim versionDir = Path.Combine(mcFolder, "versions", targetVersionName)
            Directory.CreateDirectory(versionDir)

            If packagePath.EndsWith(".mrpack", StringComparison.OrdinalIgnoreCase) Then
                Return ImportMrpack(packagePath, versionDir)
            Else
                ' 假定是普通 zip
                ExtractZipToFolder(packagePath, versionDir)
                Return True
            End If
        Catch ex As Exception
            Console.WriteLine($"导入失败: {ex.Message}")
            Return False
        End Try
    End Function

    Private Function ImportMrpack(packagePath As String, versionDir As String) As Boolean
        ' 1. 解压到临时目录
        Dim tempDir = Path.Combine(Path.GetTempPath(), "PCL_Import_" & Guid.NewGuid().ToString("N").Substring(0, 8))
        Directory.CreateDirectory(tempDir)
        ExtractZipToFolder(packagePath, tempDir)

        ' 2. 读取 modrinth.index.json
        Dim indexFile = Path.Combine(tempDir, "modrinth.index.json")
        If Not File.Exists(indexFile) Then Return False

        Dim indexJson = JObject.Parse(File.ReadAllText(indexFile))

        ' 3. 移动 overrides 文件夹
        Dim overridesDir = Path.Combine(tempDir, "overrides")
        If Directory.Exists(overridesDir) Then
            CopyDirectory(overridesDir, versionDir)
        End If

        ' 4. 下载依赖文件（简化版：只打印，未真正下载）
        Dim files = TryCast(indexJson("files"), JArray)
        If files IsNot Nothing Then
            For Each file In files
                Dim path = file("path")?.ToString()
                Dim downloads = TryCast(file("downloads"), JArray)
                If path IsNot Nothing AndAlso downloads IsNot Nothing AndAlso downloads.Count > 0 Then
                    ' 这里应该调用你的 Downloader 去下载单文件
                    Console.WriteLine($"[MRPACK] 需要下载: {path}")
                End If
            Next
        End If

        ' 5. 清理临时目录
        Try
            Directory.Delete(tempDir, True)
        Catch
        End Try
        Return True
    End Function

    Private Sub ExtractZipToFolder(zipPath As String, targetDir As String)
        Using archive As ZipArchive = ZipFile.OpenRead(zipPath)
            For Each entry In archive.Entries
                Dim fullPath = Path.Combine(targetDir, entry.FullName)
                If entry.Name = "" Then
                    Directory.CreateDirectory(fullPath)
                Else
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath))
                    entry.ExtractToFile(fullPath, True)
                End If
            Next
        End Using
    End Sub

    Private Sub CopyDirectory(sourceDir As String, targetDir As String)
        For Each dirPath In Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories)
            Directory.CreateDirectory(dirPath.Replace(sourceDir, targetDir))
        Next
        For Each filePath In Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories)
            File.Copy(filePath, filePath.Replace(sourceDir, targetDir), True)
        Next
    End Sub

End Module
