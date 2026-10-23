' 跨平台桩：为被排除的 UI/平台符号提供最小声明，让业务逻辑层能编译。
' 这些桩在运行时是空操作，后续由 Avalonia UI 层替换。

Imports System.Collections.ObjectModel

' ===== FrmMain / FormMain =====
' 原类型是 WPF 窗口，跨平台版用 Object 占位，所有属性访问走晚期绑定（Option Strict Off）。
Public Module FrmMainStub
    Public FrmMain As Object = Nothing
    Public FormMain As Object = Nothing
End Module

' ===== Application =====
Public Class Application
    Public Shared ReadOnly Current As New Application()
    Public Property Dispatcher As Object = Nothing
    Public Property Resources As Object = Nothing
End Class

' ===== MyMsgBox 系列 =====
Public Module MyMsgBoxStub
    Public Function MyMsgBox(Caption As String, Optional Title As String = "提示",
                             Optional Button1 As String = "确定", Optional Button2 As String = "",
                             Optional IsWarn As Boolean = False,
                             Optional Button1Action As Action = Nothing,
                             Optional Button2Action As Action = Nothing,
                             Optional ForceWait As Boolean = False,
                             Optional HighLight As Boolean = False,
                             Optional Button3 As String = "",
                             Optional Button3Action As Action = Nothing) As Integer
        Console.WriteLine($"[MyMsgBox] {Title}: {Caption}")
        Return 1
    End Function

    Public Function MyMsgBoxInput(Title As String, Optional Text As String = Nothing,
                                  Optional DefaultInput As String = Nothing,
                                  Optional ValidateRules As Object = Nothing,
                                  Optional HintText As String = "",
                                  Optional Button1 As String = "确定", Optional Button2 As String = "取消",
                                  Optional IsWarn As Boolean = False) As String
        Console.WriteLine($"[MyMsgBoxInput] {Title}: {Text}")
        Return DefaultInput
    End Function

    Public Function MyMsgBoxSelect(Selections As IEnumerable(Of Object),
                                   Optional Title As String = "提示",
                                   Optional Button1 As String = "确定", Optional Button2 As String = "",
                                   Optional IsWarn As Boolean = False) As Integer?
        Console.WriteLine($"[MyMsgBoxSelect] {Title}")
        Return 0
    End Function
End Module

' ===== Telemetry =====
Public Module TelemetryStub
    Public Sub Telemetry([Event] As String, ParamArray Datas As String())
        ' 跨平台版不发送遥测
    End Sub
End Module

' ===== Dialogs =====
Public Module DialogsStub
    Public Dialogs As Object = Nothing
End Module

' ===== RequestTaskTempFolder =====
Public Module TempFolderStub
    Public Function RequestTaskTempFolder(Optional RequireNonSpace As Boolean = False) As String
        Dim p = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PCL", "TaskTemp", Guid.NewGuid().ToString())
        System.IO.Directory.CreateDirectory(p)
        Return p & System.IO.Path.DirectorySeparatorChar
    End Function
End Module

' ===== ArgumentReplace（简化版：只做占位符替换，不依赖 PathUtils 等） =====
Public Module ArgumentReplaceStub
    Public Function ArgumentReplace(Text As String,
                                    Optional EscapeHandler As Func(Of String, String) = Nothing,
                                    Optional ReplaceTime As Boolean = True) As String
        If Text Is Nothing Then Return Nothing
        Return Text
    End Function
End Module

' ===== 页面/窗口桩（Settings.vb 等引用的 UI 类） =====
Public Module PageStubs
    Public FrmSpeedLeft As Object = Nothing
    Public PageLaunchLeft As Object = Nothing
    Public PageSetupLaunch As Object = Nothing
    Public PageSetupUI As Object = Nothing
    Public PageSetupSystem As Object = Nothing
    Public PageInstanceLeft As Object = Nothing
    Public PageInstanceMod As Object = Nothing
    Public PageInstanceSetup As Object = Nothing
    Public FrmInstanceMod As Object = Nothing
    Public FrmInstanceSetup As Object = Nothing
    Public FrmLaunchLeft As Object = Nothing
    Public FrmLaunchRight As Object = Nothing
    Public FrmSetupUI As Object = Nothing
    Public FrmSetupLaunch As Object = Nothing
End Module

' ===== Feedback =====
Public Module FeedbackStub
    Public Sub FeedbackCall(Optional A As Boolean = False, Optional B As Boolean = False)
        Console.WriteLine("[Feedback] 已记录反馈")
    End Sub
End Module

' ===== 剪贴板桩（替代 My.Computer.Clipboard） =====
Public Module ClipboardCompat
    Private _text As String = ""
    Public Sub Clear()
        _text = ""
    End Sub
    Public Sub SetText(Text As String)
        _text = If(Text, "")
    End Sub
    Public Function ContainsText() As Boolean
        Return Not String.IsNullOrEmpty(_text)
    End Function
    Public Function GetText() As String
        Return _text
    End Function
End Module

' ===== 资源管理桩（替代 My.Resources.ResourceManager） =====
Public Module ResourceManagerCompat
    Public ReadOnly ResourceManager As New System.Resources.ResourceManager("PCL.Resources", GetType(ResourceManagerCompat).Assembly)
End Module

' ===== 杂项全局符号桩 =====
Public Module MiscStubs
    Public AniControlEnabled As Integer = 0
    Public RestrictMcDownload As Boolean = False
    Public RegFolder As String = "PCL"
    Public Function GetIdentify(Optional index As Integer = 0) As String
        Return "unknown"
    End Function
    Public PageDownloadResourceDetail As Object = Nothing
End Module

Public Module ThemeStub
    Public Sub ThemeRefresh()
    End Sub
End Module

' ===== 签名/页面/窗口桩 =====
Public Module SecretStubs
    Public Function SecretCdnSign(Url As String) As String
        Return Url
    End Function
    Public Sub SecretHeadersSign(ParamArray Args As Object())
    End Sub
End Module

Public Module MorePageStubs
    Public PageOtherTest As Object = Nothing
    Public PageDownloadResourceDetail As Object = Nothing
    Public FrmLoginMs As Object = Nothing
End Module

Public Module LoginStubs
    Public FrmLoginLegacy As Object = Nothing
    Public FrmLoginNide As Object = Nothing
    Public FrmLoginAuth As Object = Nothing
    Public PageLoginMs As Object = Nothing
    Public PageLoginMsSkin As Object = Nothing
    Public PageLoginLegacy As Object = Nothing
    Public PageLoginNide As Object = Nothing
    Public PageLoginNideSkin As Object = Nothing
    Public PageLoginAuth As Object = Nothing
    Public PageLoginAuthSkin As Object = Nothing
End Module

' ===== 覆盖 LINQ 的 DistinctBy/MaxBy（绕过 .NET 8 的 VB 重载歧义） =====
Public Module LinqCompat
    <System.Runtime.CompilerServices.Extension>
    Public Function DistinctBy(Of T, TKey)(source As IEnumerable(Of T), keySelector As Func(Of T, TKey)) As IEnumerable(Of T)
        Dim seen As New HashSet(Of TKey)()
        Dim result As New List(Of T)()
        For Each item In source
            If seen.Add(keySelector(item)) Then result.Add(item)
        Next
        Return result
    End Function

    <System.Runtime.CompilerServices.Extension>
    Public Function MaxBy(Of T, TKey)(source As IEnumerable(Of T), keySelector As Func(Of T, TKey)) As T
        Dim best As T = Nothing
        Dim bestKey As TKey = Nothing
        Dim first As Boolean = True
        For Each item In source
            Dim k = keySelector(item)
            If first OrElse Comparer(Of TKey).Default.Compare(k, bestKey) > 0 Then
                best = item
                bestKey = k
                first = False
            End If
        Next
        Return best
    End Function
End Module

' ===== 更多杂项桩 =====
Public Module MiscStubs2
    Public Sub FeedbackInfo(Optional A As Object = Nothing, Optional B As Object = Nothing)
    End Sub
    Public Function ThemeUnlock(Optional A As Integer = 0, Optional B As Boolean = False) As Boolean
        Return False
    End Function
    Public OAuthClientId As String = ""
    Public MyMsgBoxType As Object = Nothing
    Public WaitingMyMsgBox As Object = Nothing
End Module

' ===== MyMsgBoxConverter 桩 =====
Public Class MyMsgBoxConverter
    Public Property Uuid As String
    Public Property Title As String
    Public Property Text As String
    Public Property Content As Object
    Public Property ForceWait As Boolean
    Public Property Type As Object
    Public Property Result As Object
End Class

' ===== Size 桩（替代 System.Windows.Size） =====
Public Structure Size
    Public Width As Double
    Public Height As Double
    Public Sub New(w As Double, h As Double)
        Width = w
        Height = h
    End Sub
End Structure

' ===== McInstall 相关桩 =====
Public Class McInstallRequest
    Public Property Target As Object
    Public Property Loaders As Object
    Public Property NewInstanceName As String
    Public Property InstanceFolder As String
    Public Property Components As Object
End Class

Public Module McInstallStubs
    Public Function McInstallLoadersGet(Req As Object) As Object
        Return Nothing
    End Function
    Public Sub McInstallState(Optional A As Object = Nothing, Optional B As Object = Nothing)
    End Sub
    Public Function McDownloadForgeRecommendedGet(Optional A As Object = Nothing) As Object
        Return Nothing
    End Function
End Module

' ===== SettingService 桩 =====
Public Class SettingService
    Public Shared Function GetKey(obj As Object) As String
        Return Nothing
    End Function
    Public Shared Sub SetKey(obj As Object, value As String)
    End Sub
    Public Shared Sub RefreshSettings(Optional A As Object = Nothing)
    End Sub
End Class

' ===== MyBitmap 桩（跨平台版暂不实现图像处理） =====
Public Class MyBitmap
    Public Property Pic As Object = Nothing
    Public Sub New(Optional A As Object = Nothing)
    End Sub
    Public Sub Save(Optional Path As String = Nothing, Optional Format As Object = Nothing)
    End Sub
    Public Function Clip(Optional X As Integer = 0, Optional Y As Integer = 0, Optional W As Integer = 0, Optional H As Integer = 0) As MyBitmap
        Return New MyBitmap()
    End Function
End Class

' ===== ComputerCompat.Info =====
Public Module ComputerCompat
    Public ReadOnly Info As New ComputerInfoCompat()
End Module

Public Class ComputerInfoCompat
    Public ReadOnly AvailablePhysicalMemory As Long = 0
    Public ReadOnly OSVersion As String = Environment.OSVersion.VersionString
End Class

' ===== 单选控件类型桩 =====
Public Interface IMyRadio
End Interface

Public Class MyRadioBox
    Implements IMyRadio
    Public Property Text As String
End Class

' ===== 更多杂项桩（GPU/音乐/窗口/页面） =====
Public Module MiscStubs3
    Public Sub SetGPUPreference(Optional A As Object = Nothing, Optional B As Object = Nothing)
    End Sub
    Public WindowsUtils As Object = Nothing
    Public Function MusicPause() As Boolean
        Return False
    End Function
    Public Function MusicResume() As Boolean
        Return False
    End Function
    Public WindowState As Object = Nothing
    Public PageSelectLeft As Object = Nothing
    Public PageDownloadInstall As Object = Nothing
    Public Sub McUpdateLogShow(Optional A As Object = Nothing)
    End Sub
End Module
