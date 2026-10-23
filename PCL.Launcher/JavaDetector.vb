Imports System
Imports System.IO
Imports System.Diagnostics
Imports System.Collections.Generic

''' <summary>
''' macOS 上的 Java 检测。
''' 优先 /usr/libexec/java_home，其次 Homebrew openjdk，最后 PATH 里的 java。
''' </summary>
Public Module JavaDetector

    Public Class JavaInfo
        Public Property Path As String
        Public Property Version As String
        Public Property MajorVersion As Integer
        Public Property Source As String
    End Class

    ''' <summary>
    ''' 检测可用的 Java，按版本从高到低返回。
    ''' </summary>
    Public Function DetectAll() As List(Of JavaInfo)
        Dim result As New List(Of JavaInfo)

        ' 1. /usr/libexec/java_home -V 列出所有已安装的 JDK
        Try
            Dim psi As New ProcessStartInfo("/usr/libexec/java_home", "-V") With {
                .RedirectStandardError = True,
                .RedirectStandardOutput = True,
                .UseShellExecute = False
            }
            Dim proc = Process.Start(psi)
            Dim output = proc.StandardError.ReadToEnd()
            proc.WaitForExit()

            ' 解析类似：25.0.4, x86_64: "Temurin-25" /Library/Java/JavaVirtualMachines/temurin-25.jdk/Contents/Home
            For Each line In output.Split(New Char() {ChrW(10)}, StringSplitOptions.RemoveEmptyEntries)
                Dim trimmed = line.Trim()
                If Not trimmed.Contains("/Contents/Home") Then Continue For
                Dim homeIdx = trimmed.IndexOf("/Library/Java")
                If homeIdx < 0 Then Continue For
                Dim javaHome = trimmed.Substring(homeIdx)
                Dim javaBin = Path.Combine(javaHome, "bin", "java")
                If Not File.Exists(javaBin) Then Continue For

                Dim info = GetJavaInfo(javaBin, "java_home")
                If info IsNot Nothing Then result.Add(info)
            Next
        Catch
            ' java_home 不存在或失败，继续
        End Try

        ' 2. Homebrew openjdk
        For Each candidate In {
            "/opt/homebrew/opt/openjdk/bin/java",
            "/opt/homebrew/opt/openjdk@25/bin/java",
            "/opt/homebrew/opt/openjdk@21/bin/java",
            "/opt/homebrew/opt/openjdk@17/bin/java",
            "/usr/local/opt/openjdk/bin/java"
        }
            If File.Exists(candidate) Then
                Dim info = GetJavaInfo(candidate, "homebrew")
                If info IsNot Nothing AndAlso Not result.Exists(Function(j) j.Path = info.Path) Then
                    result.Add(info)
                End If
            End If
        Next

        ' 3. PATH 里的 java
        Try
            Dim psi As New ProcessStartInfo("which", "java") With {
                .RedirectStandardOutput = True,
                .UseShellExecute = False
            }
            Dim proc = Process.Start(psi)
            Dim path = proc.StandardOutput.ReadToEnd().Trim()
            proc.WaitForExit()
            If Not String.IsNullOrEmpty(path) AndAlso File.Exists(path) Then
                Dim info = GetJavaInfo(path, "PATH")
                If info IsNot Nothing AndAlso Not result.Exists(Function(j) j.Path = info.Path) Then
                    result.Add(info)
                End If
            End If
        Catch
        End Try

        ' 按主版本号降序
        result.Sort(Function(a, b) b.MajorVersion.CompareTo(a.MajorVersion))
        Return result
    End Function

    ''' <summary>
    ''' 找最合适的 Java：优先满足 requiredMajor（等于或更高），否则返回最高版本。
    ''' </summary>
    Public Function Detect(Optional requiredMajor As Integer = 0) As JavaInfo
        Dim all = DetectAll()
        If all.Count = 0 Then Return Nothing

        If requiredMajor > 0 Then
            Dim match = all.Find(Function(j) j.MajorVersion >= requiredMajor)
            If match IsNot Nothing Then Return match
        End If
        Return all(0)
    End Function

    Private Function GetJavaInfo(javaPath As String, source As String) As JavaInfo
        Try
            Dim psi As New ProcessStartInfo(javaPath, "-version") With {
                .RedirectStandardError = True,
                .RedirectStandardOutput = True,
                .UseShellExecute = False
            }
            Dim proc = Process.Start(psi)
            Dim stderr = proc.StandardError.ReadToEnd()
            proc.WaitForExit()

            ' 解析：openjdk version "25.0.4.1" 2026-08-18 LTS
            Dim version As String = Nothing
            For Each line In stderr.Split(New Char() {ChrW(10)}, StringSplitOptions.RemoveEmptyEntries)
                Dim idx = line.IndexOf("version ")
                If idx >= 0 Then
                    Dim startQ = line.IndexOf(""""c, idx)
                    Dim endQ = line.IndexOf(""""c, startQ + 1)
                    If startQ >= 0 AndAlso endQ > startQ Then
                        version = line.Substring(startQ + 1, endQ - startQ - 1)
                        Exit For
                    End If
                End If
            Next

            If version Is Nothing Then Return Nothing

            Dim major As Integer = 0
            Dim firstDot = version.IndexOf("."c)
            If firstDot > 0 Then
                Integer.TryParse(version.Substring(0, firstDot), major)
            Else
                Integer.TryParse(version, major)
            End If
            ' Java 1.8 这种老版本
            If major = 1 AndAlso version.StartsWith("1.") Then
                Dim secondDot = version.IndexOf("."c, 2)
                If secondDot > 2 Then Integer.TryParse(version.Substring(2, secondDot - 2), major)
            End If

            Return New JavaInfo With {
                .Path = javaPath,
                .Version = version,
                .MajorVersion = major,
                .Source = source
            }
        Catch
            Return Nothing
        End Try
    End Function

End Module
