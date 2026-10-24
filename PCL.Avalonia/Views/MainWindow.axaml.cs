using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PCL.Avalonia.ViewModels;

namespace PCL.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 强行挂载双击事件，穿透 ListBox 的内部拦截
        var listBox = this.FindControl<ListBox>("InstanceListBox");
        if (listBox != null)
        {
            listBox.AddHandler(PointerPressedEvent, InstanceList_PointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        }
    }

    private void InstanceList_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 左键双击
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.ClickCount == 2)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.LaunchSelectedInstanceCommand.Execute(null);
                }
            }
        }
    }
}
