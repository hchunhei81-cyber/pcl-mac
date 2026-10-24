using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PCL.Avalonia.ViewModels;
using PCL.Avalonia.Views;
using System;

namespace PCL.Avalonia;

public class App : global::Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
            desktop.MainWindow = mainWindow;

            // 构建 macOS 原生菜单
            var menu = new NativeMenu();
            
            // 文件菜单
            var fileMenu = new NativeMenuItem("文件");
            var fileSubMenu = new NativeMenu();
            var checkUpdateItem = new NativeMenuItem("检查更新");
            checkUpdateItem.Click += (s, e) =>
            {
                if (mainWindow.DataContext is MainViewModel vm)
                {
                    vm.ShowTestUpdateCommand.Execute(null);
                }
            };
            fileSubMenu.Add(checkUpdateItem);
            fileSubMenu.Add(new NativeMenuItemSeparator());
            
            var exitItem = new NativeMenuItem("退出 PCL");
            exitItem.Click += (s, e) => desktop.Shutdown();
            fileSubMenu.Add(exitItem);
            
            fileMenu.Menu = fileSubMenu;

            // 帮助菜单
            var helpMenu = new NativeMenuItem("帮助");
            var helpSubMenu = new NativeMenu();
            var githubItem = new NativeMenuItem("打开项目主页");
            githubItem.Click += (s, e) =>
            {
                System.Diagnostics.Process.Start("open", "https://github.com/hchunhei81-cyber/pcl-mac");
            };
            helpSubMenu.Add(githubItem);
            helpMenu.Menu = helpSubMenu;

            menu.Add(fileMenu);
            menu.Add(helpMenu);

            NativeMenu.SetMenu(mainWindow, menu);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
