' 从 PCL2 的 Controls/MyLoading.xaml.vb 抽出的跨平台加载状态类型。
' 原文件中 ILoadingTrigger 和 MyLoadingStateSimulator 不依赖 WPF，可直接复用。

Public Enum MyLoadingState
    Unloaded = -1
    Run = 0
    [Stop] = 1
    [Error] = 2
End Enum

Public Interface ILoadingTrigger
    ReadOnly Property IsLoader As Boolean
    Property LoadingState As MyLoadingState
    Event LoadingStateChanged(NewState As MyLoadingState, OldState As MyLoadingState)
    Event ProgressChanged(NewProgress As Double, OldProgress As Double)
End Interface

Public Class MyLoadingStateSimulator
    Implements ILoadingTrigger
    Private Property _LoadingState As MyLoadingState = MyLoadingState.Unloaded
    Public Property LoadingState As MyLoadingState Implements ILoadingTrigger.LoadingState
        Get
            Return _LoadingState
        End Get
        Set(value As MyLoadingState)
            If _LoadingState = value Then Return
            Dim OldState = _LoadingState
            _LoadingState = value
            RaiseEvent LoadingStateChanged(value, OldState)
        End Set
    End Property
    Public ReadOnly Property IsLoader As Boolean = False Implements ILoadingTrigger.IsLoader

    Public Event LoadingStateChanged(NewState As MyLoadingState, OldState As MyLoadingState) Implements ILoadingTrigger.LoadingStateChanged
    Public Event ProgressChanged(NewProgress As Double, OldProgress As Double) Implements ILoadingTrigger.ProgressChanged
End Class
