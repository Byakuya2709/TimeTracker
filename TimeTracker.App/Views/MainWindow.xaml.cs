using System.Windows;
using System.Windows.Input;
using System.IO;
using TimeTracker.App.ViewModels;
using TimeTracker.Application.Services;
using TimeTracker.Infrastructure.Services;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        string databasePath = Path.Combine(AppContext.BaseDirectory, "timetracker.db");

        ActivityTracker tracker = new(
            new Win32ActiveAppReader(),
            new SqliteActivityLogStore(databasePath));

        _viewModel = new MainViewModel(tracker);
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Rect workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 16;
        Top = workArea.Top + 16;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.Dispose();
    }

    private void OverlayBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

}