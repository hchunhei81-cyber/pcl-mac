' 从 PCL2 的 ModMain.vb 抽出的提示系统跨平台部分。
' 原 ModMain.vb 因页面导航被排除，这里保留 HintType 枚举和 Hint 方法。

Public Enum HintType
    Blue
    Green
    Red
End Enum

Public Structure HintMessage
    Public Text As String
    Public Type As HintType
    Public Log As Boolean
End Structure

Public Module HintCompat
    Private HintWaiting As New System.Collections.Concurrent.ConcurrentQueue(Of HintMessage)

    ''' <summary>
    ''' 在窗口左下角弹出提示文本（跨平台版本：仅入队，UI 层消费）。
    ''' </summary>
    Public Sub Hint(Text As String, Optional Type As HintType = HintType.Blue, Optional Log As Boolean = True)
        HintWaiting.Enqueue(New HintMessage With {.Text = If(Text, ""), .Type = Type, .Log = Log})
    End Sub

    Public Function DequeueHint() As HintMessage
        Dim msg As HintMessage = Nothing
        If HintWaiting.TryDequeue(msg) Then Return msg
        Return Nothing
    End Function
End Module
