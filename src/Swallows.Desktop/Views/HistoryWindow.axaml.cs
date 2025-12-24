using Avalonia.Controls;
using Avalonia.Interactivity;
using Swallows.Desktop.ViewModels;
using System;

namespace Swallows.Desktop.Views;

public partial class HistoryWindow : Window
{
    public HistoryWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void Open_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is HistoryWindowViewModel vm && vm.SelectedSession != null)
        {
            Close(vm.SelectedSession);
        }
    }
}
