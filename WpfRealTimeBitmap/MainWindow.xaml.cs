using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using WpfRealTimeBitmap.ViewExtenders;

namespace WpfRealTimeBitmap;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private int _framesPerSecond;
    public int FramesPerSecond
    {
        get => _framesPerSecond;
        set => SetField(ref _framesPerSecond, value);
    }

    private ColorScaleBitmapAdapter _adapter;
    public ColorScaleBitmapAdapter Adapter
    {
        get => _adapter;
        set => SetField(ref _adapter, value);
    }

    private ColorScaleImage _image;
    private readonly DispatcherTimer _renderTimer;
    private DateTime _lastFrameUpdate;
    private int _framesSinceUpdate;

    public MainWindow()
    {
        _image = new ColorScaleImage(1000, 1000);
        _adapter = new ColorScaleBitmapAdapter(_image);
        _adapter.UseParallelRenderStrategy = true;
        InitializeComponent();
        DataContext = this;

        _lastFrameUpdate = DateTime.Now;

        _renderTimer = new();
        _renderTimer.Interval = TimeSpan.FromMilliseconds(10);
        _renderTimer.Tick += RenderTimer_Tick;
        _renderTimer.Start();
    }

    private void RenderTimer_Tick(object? sender, EventArgs e)
    {
        _image.Render();
        _adapter.Invalidate();

        _framesSinceUpdate++;
        if (DateTime.Now - _lastFrameUpdate > TimeSpan.FromSeconds(1))
        {
            FramesPerSecond = _framesSinceUpdate;
            _framesSinceUpdate = 0;
            _lastFrameUpdate = DateTime.Now;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
