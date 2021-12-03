using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
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

    private int _imageWidth = 1000;
    public int ImageWidth
    {
        get => _imageWidth;
        set => SetField(ref _imageWidth, value);
    }

    private int _imageHeight = 1000;
    public int ImageHeight
    {
        get => _imageHeight;
        set => SetField(ref _imageHeight, value);
    }

    private bool _useParallelStrategy = true;
    public bool UseParallelStrategy
    {
        get => _useParallelStrategy;
        set
        {
            SetField(ref _useParallelStrategy, value);
        }
    }

    private bool _useCompositionRenderer;
    public bool UseCompositionRenderer
    {
        get => _useCompositionRenderer;
        set
        {
            SetField(ref _useCompositionRenderer, value);
        }
    }

    private ColorScaleImage _image;
    private readonly DispatcherTimer _renderTimer;
    private DateTime _lastFrameUpdate;
    private int _framesSinceUpdate;

    public MainWindow()
    {
        InitializeComponent();

        _lastFrameUpdate = DateTime.Now;

        _renderTimer = new(DispatcherPriority.Render);
        _renderTimer.Interval = TimeSpan.FromMilliseconds(1);
        _renderTimer.Tick += RenderTimer_Tick;

        UseParallelStrategy = true;
        UseCompositionRenderer = true;
        _lastFrameUpdate = DateTime.Now;
        _image = new ColorScaleImage(ImageWidth, ImageHeight);
        _adapter = new ColorScaleBitmapAdapter(_image)
        {
            UseParallelRenderStrategy = UseParallelStrategy
        };

        StartRendering();
        DataContext = this;
    }

    private void ApplySettings_Click(object sender, RoutedEventArgs e)
    {
        PauseRendering();
        _image = new ColorScaleImage(ImageWidth, ImageHeight);
        Adapter = new ColorScaleBitmapAdapter(_image);
        Adapter.UseParallelRenderStrategy = UseParallelStrategy;
        StartRendering();
    }

    private void RenderFrame()
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

        RenderTextOverlay();
    }

    private void RenderTextOverlay()
    {
        var imageInfo = new SKImageInfo()
        {
            Width = _image.Width,
            Height = _image.Height,
            ColorType = SKColorType.Bgra8888,
            AlphaType = SKAlphaType.Premul,
            ColorSpace = SKColorSpace.CreateSrgb()
        };

        try
        {
            Adapter.Bitmap!.Lock();

            using var surface = SKSurface.Create(imageInfo, Adapter.Bitmap.BackBuffer);
            using var paint = new SKPaint()
            {
                TextSize = 24.0f,
                IsAntialias = true,
                Color = new SKColor(0x42, 0x81, 0xA4),
                IsStroke = false
            };

            var canvas = surface.Canvas;
            canvas.DrawText($"FPS: {_framesPerSecond}", 20, 40, paint);
            canvas.DrawText($"Size: {_imageWidth}x{_imageHeight}", 20, 70, paint);

            Adapter.Bitmap.AddDirtyRect(new Int32Rect(0, 0, _image.Width, _image.Height));
        }
        finally
        {
            Adapter.Bitmap!.Unlock();
        }
    }

    private void Composite_Renderer(object? sender, EventArgs e) => RenderFrame();

    private void RenderTimer_Tick(object? sender, EventArgs e) => RenderFrame();

    private void StartRendering()
    {
        if (UseCompositionRenderer)
            CompositionTarget.Rendering += Composite_Renderer;
        else
            _renderTimer?.Start();
    }

    private void PauseRendering()
    {
        CompositionTarget.Rendering -= Composite_Renderer;
        _renderTimer?.Stop();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
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
