Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Newtonsoft.Json.Linq

Module Program
    Sub Main(args As String())
        Console.WriteLine("=== PCL macOS CLI 启动验证 ===")

        ' 1. 本地 MC 目录
        Dim mcDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "minecraft")
        Dim version = "26.2"
        Console.WriteLine($"[INFO] MC 目录: {mcDir}")
        Console.WriteLine($"[INFO] 版本: {version}")

        If Not Directory.Exists(Path.Combine(mcDir, "versions", version)) Then
            Console.WriteLine("[FAIL] 版本目录不存在")
            Return
        End If

        ' 2. 用 PCL 解析
        PCL.McFolderSelected = mcDir & "/"
        Dim inst As New PCL.McInstance(version)
        Dim ok = inst.Check()
        Console.WriteLine($"[{(If(ok, "OK", "FAIL"))}] Check() = {ok}")
        If Not ok Then Return

        Dim loaded = inst.Load()
        Console.WriteLine($"[OK] Load() 成功, VanillaName = {loaded.Version.VanillaName}")

        Dim json = loaded.JsonObject
        Dim mainClass = json("mainClass").ToString()
        Console.WriteLine($"[OK] mainClass = {mainClass}")

        ' 3. 拼 classpath
        Dim libsDir = Path.Combine(mcDir, "libraries")
        Dim cpParts As New List(Of String)
        For Each libItem As JToken In CType(json("libraries"), JArray)
            Dim downloads = TryCast(libItem("downloads"), JObject)
            If downloads IsNot Nothing Then
                Dim artifact = TryCast(downloads("artifact"), JObject)
                If artifact IsNot Nothing AndAlso artifact("path") IsNot Nothing Then
                    Dim libPath = Path.Combine(libsDir, artifact("path").ToString().Replace("/", Path.DirectorySeparatorChar))
                    If File.Exists(libPath) Then
                        cpParts.Add(libPath)
                    Else
                        Console.WriteLine($"[WARN] 库不存在: {libPath}")
                    End If
                End If
            End If
        Next
        ' 加版本 jar
        Dim versionJar = Path.Combine(mcDir, "versions", version, version & ".jar")
        If File.Exists(versionJar) Then
            cpParts.Add(versionJar)
        Else
            Console.WriteLine($"[FAIL] 版本 jar 不存在: {versionJar}")
            Return
        End If
        Console.WriteLine($"[OK] classpath 条目数 = {cpParts.Count}")

        ' 4. 替换占位符
        Dim nativesDir = Path.Combine(Path.GetTempPath(), "pcl-natives-" & Guid.NewGuid().ToString("N").Substring(0, 8))
        Directory.CreateDirectory(nativesDir)

        Dim Replacements As New Dictionary(Of String, String) From {
            {"${natives_directory}", nativesDir},
            {"${classpath}", String.Join(Path.PathSeparator.ToString(), cpParts)},
            {"${launcher_name}", "PCL-macOS"},
            {"${launcher_version}", "1.0"},
            {"${auth_player_name}", "Player"},
            {"${version_name}", version},
            {"${game_directory}", mcDir},
            {"${assets_root}", Path.Combine(mcDir, "assets")},
            {"${assets_index_name}", json("assetIndex")("id").ToString()},
            {"${auth_uuid}", Guid.NewGuid().ToString("N")},
            {"${auth_access_token}", "0"},
            {"${clientid}", "0"},
            {"${auth_xuid}", "0"},
            {"${user_type}", "legacy"},
            {"${version_type}", "release"},
            {"${game_assets}", Path.Combine(mcDir, "assets")},
            {"${user_properties}", "{}"}
        }

        Dim ReplaceFn As Func(Of String, String) = Function(s)
            For Each kv In Replacements
                s = s.Replace(kv.Key, kv.Value)
            Next
            Return s
        End Function

        ' 5. 组装 JVM 参数（处理 rules）
        Dim jvmArgs As New List(Of String)
        Dim jvmArr = TryCast(json("arguments")?("jvm"), JArray)
        If jvmArr IsNot Nothing Then
            For Each item In jvmArr
                If item.Type = JTokenType.String Then
                    jvmArgs.Add(ReplaceFn(item.ToString()))
                ElseIf item.Type = JTokenType.Object Then
                    Dim obj = CType(item, JObject)
                    Dim rules = TryCast(obj("rules"), JArray)
                    Dim allowed = True
                    If rules IsNot Nothing Then
                        allowed = False
                        For Each rule In rules
                            Dim action = rule("action")?.ToString()
                            Dim osName = rule("os")?("name")?.ToString()
                            Dim osArch = rule("os")?("arch")?.ToString()
                            Dim matches = True
                            If osName IsNot Nothing AndAlso osName <> "osx" Then matches = False
                            If osArch IsNot Nothing AndAlso osArch <> "arm64" Then matches = False
                            If matches Then
                                allowed = (action = "allow")
                            End If
                        Next
                    End If
                    If allowed Then
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

        ' 6. 组装 game 参数
        Dim gameArgs As New List(Of String)
        Dim gameArr = TryCast(json("arguments")?("game"), JArray)
        If gameArr IsNot Nothing Then
            For Each item In gameArr
                If item.Type = JTokenType.String Then
                    gameArgs.Add(ReplaceFn(item.ToString()))
                ElseIf item.Type = JTokenType.Object Then
                    Dim obj = CType(item, JObject)
                    Dim rules = TryCast(obj("rules"), JArray)
                    Dim allowed = True
                    If rules IsNot Nothing Then
                        allowed = False
                        For Each rule In rules
                            Dim action = rule("action")?.ToString()
                            Dim osName = rule("os")?("name")?.ToString()
                            Dim features = TryCast(rule("features"), JObject)
                            Dim matches = True
                            If osName IsNot Nothing AndAlso osName <> "osx" Then matches = False
                            If features IsNot Nothing Then matches = False ' 特性规则暂不支持
                            If matches Then allowed = (action = "allow")
                        Next
                    End If
                    If allowed Then
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

        ' 7. 打印完整命令
        Dim javaPath = "/Library/Java/JavaVirtualMachines/temurin-25.jdk/Contents/Home/bin/java"
        If Not File.Exists(javaPath) Then
            javaPath = "/usr/bin/java"
        End If

        Console.WriteLine()
        Console.WriteLine("=== Java 命令 ===")
        Console.WriteLine($"Java: {javaPath}")
        Console.WriteLine($"JVM 参数数: {jvmArgs.Count}")
        Console.WriteLine($"Game 参数数: {gameArgs.Count}")
        Console.WriteLine($"Classpath 长度: {String.Join(Path.PathSeparator.ToString(), cpParts).Length} 字符")
        Console.WriteLine()

        ' 8. 启动 Java，捕获输出 15 秒
        Dim psi As New ProcessStartInfo()
        psi.FileName = javaPath
        For Each a In jvmArgs
            psi.ArgumentList.Add(a)
        Next
        psi.ArgumentList.Add(mainClass)
        For Each a In gameArgs
            psi.ArgumentList.Add(a)
        Next
        psi.WorkingDirectory = mcDir
        psi.RedirectStandardOutput = True
        psi.RedirectStandardError = True
        psi.UseShellExecute = False

        Console.WriteLine("=== 启动 Java ===")
        Dim proc As Process = Process.Start(psi)
        Dim stdout = proc.StandardOutput.ReadToEndAsync()
        Dim stderr = proc.StandardError.ReadToEndAsync()

        Dim exited = proc.WaitForExit(15000)
        If Not exited Then
            Console.WriteLine("[INFO] 15 秒未退出，强制结束")
            proc.Kill()
            proc.WaitForExit()
        End If

        Console.WriteLine()
        Console.WriteLine("=== stdout（前 30 行）===")
        Dim outLines = stdout.Result.Split(New Char() {ChrW(10)}, StringSplitOptions.RemoveEmptyEntries)
        For i = 0 To Math.Min(29, outLines.Length - 1)
            Console.WriteLine(outLines(i))
        Next

        Console.WriteLine()
        Console.WriteLine("=== stderr（前 20 行）===")
        Dim errLines = stderr.Result.Split(New Char() {ChrW(10)}, StringSplitOptions.RemoveEmptyEntries)
        For i = 0 To Math.Min(19, errLines.Length - 1)
            Console.WriteLine(errLines(i))
        Next

        Console.WriteLine()
        Console.WriteLine($"=== 退出码: {proc.ExitCode} ===")
    End Sub
End Module
