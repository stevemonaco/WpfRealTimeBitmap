using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfRealTimeBitmap.ViewExtenders;

public abstract class BitmapAdapter : ObservableObject
{
    private WriteableBitmap? _bitmap;
    public WriteableBitmap? Bitmap
    {
        get => _bitmap;
        set => SetProperty(ref _bitmap, value);
    }

    private int _width;
    public int Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    private int _height;
    public int Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public int DpiX { get; protected set; } = 96;
    public int DpiY { get; protected set; } = 96;
    public PixelFormat PixelFormat { get; protected set; } = PixelFormats.Bgra32;

    public abstract void Invalidate();
    public abstract void Invalidate(Rectangle redrawRect);
    public abstract void Invalidate(int x, int y, int width, int height);

    protected abstract void Render(int x, int y, int width, int height);
}
