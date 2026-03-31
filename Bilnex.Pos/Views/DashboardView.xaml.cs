using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Bilnex.Pos.Views;

public partial class DashboardView : UserControl
{
    private Point _dragStartPoint;
    private double _dragStartHorizontalOffset;
    private double _dragStartVerticalOffset;
    private bool _isDraggingTiles;
    private bool _didDragTiles;

    public DashboardView()
    {
        InitializeComponent();
    }

    private void TileScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        _dragStartPoint = e.GetPosition(scrollViewer);
        _dragStartHorizontalOffset = scrollViewer.HorizontalOffset;
        _dragStartVerticalOffset = scrollViewer.VerticalOffset;
        _isDraggingTiles = true;
        _didDragTiles = false;
    }

    private void TileScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingTiles || sender is not ScrollViewer scrollViewer || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentPoint = e.GetPosition(scrollViewer);
        var horizontalDelta = currentPoint.X - _dragStartPoint.X;
        var verticalDelta = currentPoint.Y - _dragStartPoint.Y;

        if (!_didDragTiles && (Math.Abs(horizontalDelta) > 4 || Math.Abs(verticalDelta) > 4))
        {
            _didDragTiles = true;
            scrollViewer.CaptureMouse();
            Cursor = Cursors.SizeAll;
        }

        if (!_didDragTiles)
        {
            return;
        }

        scrollViewer.ScrollToHorizontalOffset(_dragStartHorizontalOffset - horizontalDelta);
        scrollViewer.ScrollToVerticalOffset(_dragStartVerticalOffset - verticalDelta);
        e.Handled = true;
    }

    private void TileScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingTiles)
        {
            return;
        }

        ReleaseTileDrag();
        if (_didDragTiles)
        {
            e.Handled = true;
        }
    }

    private void TileScrollViewer_LostMouseCapture(object sender, MouseEventArgs e)
    {
        ReleaseTileDrag();
    }

    private void ReleaseTileDrag()
    {
        _isDraggingTiles = false;
        _didDragTiles = false;

        if (TileScrollViewer.IsMouseCaptured)
        {
            TileScrollViewer.ReleaseMouseCapture();
        }

        Cursor = null;
    }
}
