using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using TimeTracker.App.ViewModels;

namespace TimeTracker.App;

/// <summary>
/// Interaction logic for OverlayWindow.xaml
/// </summary>
public partial class OverlayWindow : Window
{
    private const double EdgeMargin = 16d;
    private readonly MainViewModel _viewModel;

    public OverlayWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        Closed += OnClosed;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyOverlayPlacement();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyOverlayPlacement();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        Loaded -= OnLoaded;
        SizeChanged -= OnSizeChanged;
        Closed -= OnClosed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.OverlayPosition))
        {
            ApplyOverlayPlacement();
        }
    }

    private void ApplyOverlayPlacement()
    {
        Rect workArea = SystemParameters.WorkArea;
        double overlayWidth = ResolveWindowWidth();
        double overlayHeight = ResolveWindowHeight();

        double left = _viewModel.OverlayPosition switch
        {
            OverlayAnchor.TopLeft or OverlayAnchor.MiddleLeft or OverlayAnchor.BottomLeft => workArea.Left + EdgeMargin,
            OverlayAnchor.TopCenter or OverlayAnchor.Center or OverlayAnchor.BottomCenter => workArea.Left + ((workArea.Width - overlayWidth) / 2d),
            _ => workArea.Right - overlayWidth - EdgeMargin
        };

        double top = _viewModel.OverlayPosition switch
        {
            OverlayAnchor.TopLeft or OverlayAnchor.TopCenter or OverlayAnchor.TopRight => workArea.Top + EdgeMargin,
            OverlayAnchor.MiddleLeft or OverlayAnchor.Center or OverlayAnchor.MiddleRight => workArea.Top + ((workArea.Height - overlayHeight) / 2d),
            _ => workArea.Bottom - overlayHeight - EdgeMargin
        };

        Left = left;
        Top = top;
    }

    private double ResolveWindowWidth()
    {
        return ActualWidth > 0
            ? ActualWidth
            : Math.Max(Width, MinWidth);
    }

    private double ResolveWindowHeight()
    {
        return ActualHeight > 0
            ? ActualHeight
            : Math.Max(Height, MinHeight);
    }

    private void OverlayBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
