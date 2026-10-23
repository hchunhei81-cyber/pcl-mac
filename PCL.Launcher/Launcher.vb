Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Newtonsoft.Json.Linq

''' <summary>
''' 跨平台 Minecraft 启动器核心。
''' 从 PCL2 的 Modules 层解析版本 JSON，组装 classpath 和参数，唤起 Java。
''' </summary>
Public Module Launcher

    Public Class LaunchOptions
        Public Property McFolder As String
        Public Property VersionName As String
        Public Property PlayerName As String = "Player"
        Public Property JavaPath As String
        Public Property MaxMemoryMb As Integer = 2048
        Public Property ExtraJvmArgs As List(Of String) = New List(Of String)
        Public Property ExtraGameArgs As List(Of String) = New List(Of String)
        Public Property TimeoutMs As Integer = 0 ' 0 = 不超时
    End Class

    Public Class LaunchResult
        Public Property Success As Boolean
        Public Property Message As String
        Public Property ExitCode As Integer
        Public Property Stdout As String
        Public Property Stderr As String
    End Class

    ''' <summary>
    ''' 启动 Minecraft。阻塞直到进程退出或超时。
    ''' </summary>
    Public Function Launch(options As LaunchOptions) As LaunchResult
        Dim result As New LaunchResult()

        ' 1. 校验 MC 目录
        If String.IsNullOrEmpty(options.McFolder) OrElse Not Directory.Exists(options.McFolder) Then
            result.Message = $"MC 目录不存在: {options.McFolder}"
            Return result
        End If

        Dim versionDir = Path.Combine(options.McFolder, "versions", options.VersionName)
        If Not Directory.Exists(versionDir) Then
            result.Message = $"版本目录不存在: {versionDir}"
            Return result
        End If

        ' 2. 用 PCL.Modules 解析版本
        PCL.McFolderSelected = PathUtils.AddSlashSuffix(options.McFolder)
        Dim inst As New PCL.McInstance(options.VersionName)
        If Not inst.Check() Then
            result.Message = $"版本检查失败: {inst.Info}"
            Return result
        End If
        Dim loaded = inst.Load()
        Dim json = loaded.JsonObject
        Dim mainClass = json("mainClass").ToString()

        ' 3. 拼 classpath
        Dim libsDir = Path.Combine(options.McFolder, "libraries")
        Dim cpParts As New List(Of String)
        For Each libItem As JToken In CType(json("libraries"), JArray)
            Dim downloads = TryCast(libItem("downloads"), JObject)
            If downloads IsNot Nothing Then
                Dim artifact = TryCast(downloads("artifact"), JObject)
                If artifact IsNot Nothing AndAlso artifact("path") IsNot Nothing Then
                    Dim libPath = Path.Combine(libsDir, artifact("path").ToString().Replace("/", Path.DirectorySeparatorChar))
                    If File.Exists(libPath) Then cpParts.Add(libPath)
                End If
            End If
        Next

        Dim versionJar = Path.Combine(versionDir, options.VersionName & ".jar")
        If Not File.Exists(versionJar) Then
            result.Message = $"版本 jar 不存在: {versionJar}"
            Return result
        End If
        cpParts.Add(versionJar)

        ' 4. 替换占位符
        Dim nativesDir = Path.Combine(Path.GetTempPath(), "pcl-natives-" & Guid.NewGuid().ToString("N").Substring(0, 8))
        Directory.CreateDirectory(nativesDir)

        Dim Replacements As New Dictionary(Of String, String) From {
            {"${natives_directory}", nativesDir},
            {"${classpath}", String.Join(Path.PathSeparator.ToString(), cpParts)},
            {"${launcher_name}", "PCL-macOS"},
            {"${launcher_version}", "1.0"},
            {"${auth_player_name}", options.PlayerName},
            {"${version_name}", options.VersionName},
            {"${game_directory}", options.McFolder},
            {"${assets_root}", Path.Combine(options.McFolder, "assets")},
            {"${assets_index_name}", json("assetIndex")("id").ToString()},
            {"${auth_uuid}", Guid.NewGuid().ToString("N")},
            {"${auth_access_token}", "0"},
            {"${clientid}", "0"},
            {"${auth_xuid}", "0"},
            {"${user_type}", "legacy"},
            {"${version_type}", "release"},
            {"${game_assets}", Path.Combine(options.McFolder, "assets")},
            {"${user_properties}", "{}"}
        }

        Dim ReplaceFn As Func(Of String, String) = Function(s)
            For Each kv In Replacements
                s = s.Replace(kv.Key, kv.Value)
            Next
            Return s
        End Function

        ' 5. JVM 参数（含 -Xmx）
        Dim jvmArgs As New List(Of String)
        jvmArgs.Add($"-Xmx{options.MaxMemoryMb}M")
        jvmArgs.AddRange(options.ExtraJvmArgs)

        Dim jvmArr = TryCast(json("arguments")?("jvm"), JArray)
        If jvmArr IsNot Nothing Then
            For Each item In jvmArr
                If item.Type = JTokenType.String Then
                    jvmArgs.Add(ReplaceFn(item.ToString()))
                ElseIf item.Type = JTokenType.Object Then
                    Dim obj = CType(item, JObject)
                    If EvaluateRules(obj) Then
                        Dim value = obj("value")
                        If value IsNot Nothing Then
                            If value.Type = JTokenType.Array Then
                                For Each v In CType(value, JArray)
                                    jvmArgs.Add(ReplaceFn(v.ToString()))
                                Next
                            Else
                                jvmArgs.Add(ReplaceFn(value.ToString()))
                            End If
                        End If
                    End If
                End If
            Next
        End If

        ' 6. Game 参数
        Dim gameArgs As New List(Of String)
        Dim gameArr = TryCast(json("arguments")?("game"), JArray)
        If gameArr IsNot Nothing Then
            For Each item In gameArr
                If item.Type = JTokenType.String Then
                    gameArgs.Add(ReplaceFn(item.ToString()))
                ElseIf item.Type = JTokenType.Object Then
                    Dim obj = CType(item, JObject)
                    If EvaluateRules(obj) Then
                        Dim value = obj("value")
                        If value IsNot Nothing Then
                            If value.Type = JTokenType.Array Then
                                For Each v In CType(value, JArray)
                                    gameArgs.Add(ReplaceFn(v.ToString()))
                                Next
                            Else
                                gameArgs.Add(ReplaceFn(value.ToString()))
                            End If
                        End If
                    End If
                End If
            Next
        End If
        gameArgs.AddRange(options.ExtraGameArgs)

        ' 7. Java 路径
        Dim javaPath = options.JavaPath
        If String.IsNullOrEmpty(javaPath) Then
            javaPath = "/usr/bin/java"
        End If

        ' 8. 启动
        Dim psi As New ProcessStartInfo()
        psi.FileName = javaPath
        For Each a In jvmArgs
            psi.ArgumentList.Add(a)
        Next
        psi.ArgumentList.Add(mainClass)
        For Each a In gameArgs
            psi.ArgumentList.Add(a)
        Next
        psi.WorkingDirectory = options.McFolder
        psi.RedirectStandardOutput = True
        psi.RedirectStandardError = True
        psi.UseShellExecute = False

        Try
            Dim proc As Process = Process.Start(psi)
            Dim stdout = proc.StandardOutput.ReadToEndAsync()
            Dim stderr = proc.StandardError.ReadToEndAsync()

            Dim exited As Boolean
            If options.TimeoutMs > 0 Then
                exited = proc.WaitForExit(options.TimeoutMs)
                If Not exited Then
                    proc.Kill()
                    proc.WaitForExit()
                End If
            Else
                proc.WaitForExit()
                exited = True
            End If

            result.ExitCode = proc.ExitCode
            result.Stdout = stdout.Result
            result.Stderr = stderr.Result
            result.Success = True
            result.Message = If(exited, "进程正常退出", "进程超时被强制结束")
        Catch ex As Exception
            result.Success = False
            result.Message = $"启动失败: {ex.GetType().Name}: {ex.Message}"
        End Try

        Return result
    End Function

    ''' <summary>
    ''' 评估 library/argument 的 rules，返回是否适用于当前平台（macOS arm64）。
    ''' </summary>
    Private Function EvaluateRules(obj As JObject) As Boolean
        Dim rules = TryCast(obj("rules"), JArray)
        If rules Is Nothing Then Return True

        Dim allowed As Boolean = False
        For Each rule In rules
            Dim action = rule("action")?.ToString()
            Dim osName = rule("os")?("name")?.ToString()
            Dim osArch = rule("os")?("arch")?.ToString()
            Dim features = TryCast(rule("features"), JObject)

            Dim matches As Boolean = True
            If osName IsNot Nothing AndAlso osName <> "osx" Then matches = False
            If osArch IsNot Nothing AndAlso osArch <> "arm64" Then matches = False
            If features IsNot Nothing Then matches = False ' 特性规则暂不支持

            If matches Then allowed = (action = "allow")
        Next
        Return allowed
    End Function

End Module
