using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WpfRealTimeBitmap.ViewExtenders;

public class ColorScaleBitmapAdapter : BitmapAdapter
{
    public ColorScaleImage Image { get; }

    public bool UseParallelRenderStrategy { get; set; }

    public ColorScaleBitmapAdapter(ColorScaleImage image)
    {
        Image = image;
        Width = Image.Width;
        Height = Image.Height;

        Bitmap = new WriteableBitmap(Width, Height, DpiX, DpiY, PixelFormat, null);
    }

    public override void Invalidate()
    {
        Render(0, 0, Image.Width, Image.Height);
    }

    public override void Invalidate(Rectangle redrawRect)
    {
        var imageRect = new Rectangle(0, 0, Image.Width, Image.Height);
        var bitmapRect = new Rectangle(0, 0, Bitmap!.PixelWidth, Bitmap!.PixelHeight);

        if (imageRect.Contains(redrawRect) && bitmapRect.Contains(redrawRect))
        {
            Render(redrawRect.X, redrawRect.Y, redrawRect.Width, redrawRect.Height);
        }
        else
        {
            throw new ArgumentOutOfRangeException($"{nameof(Invalidate)}: Parameter '{nameof(redrawRect)}' {redrawRect} was not contained within '{nameof(Image)}' (0, 0, {Image.Width}, {Image.Height}) and '{nameof(Bitmap)}' (0, 0, {Bitmap.Width}, {Bitmap.Height})");
        }
    }

    public override void Invalidate(int x, int y, int width, int height)
    {
        var imageRect = new Rectangle(0, 0, Image.Width, Image.Height);
        var bitmapRect = new Rectangle(0, 0, Bitmap!.PixelWidth, Bitmap!.PixelHeight);
        var redrawRect = new Rectangle(x, y, width, height);

        if (imageRect.Contains(redrawRect) && bitmapRect.Contains(redrawRect))
        {
            Render(x, y, width, height);
        }
        else
        {
            throw new ArgumentOutOfRangeException($"{nameof(Invalidate)}: Parameter '{nameof(redrawRect)}' {redrawRect} was not contained within '{nameof(Image)}' (0, 0, {Image.Width}, {Image.Height}) and '{nameof(Bitmap)}' (0, 0, {Bitmap.Width}, {Bitmap.Height})");
        }
    }

    protected override void Render(int xStart, int yStart, int width, int height)
    {
        try
        {
            if (!Bitmap!.TryLock(new System.Windows.Duration(TimeSpan.FromMilliseconds(500))))
                throw new TimeoutException($"{nameof(ColorScaleBitmapAdapter)}.{nameof(Render)} could not lock the Bitmap for rendering");

            unsafe
            {
                if (UseParallelRenderStrategy)
                {
                    var backBuffer = (uint*)Bitmap.BackBuffer.ToPointer();
                    var stride = Bitmap.BackBufferStride;

                    Parallel.For(yStart, yStart + Height - 1, (int scanline) =>
                    {
                        var dest = backBuffer + scanline * stride / 4 + xStart;
                        var row = Image.GetPixelRowSpan(scanline);

                        for (int x = 0; x < width; x++)
                        {
                            var color = row[x + xStart];
                            dest[x] = TranslateColor(color);
                        }
                    });
                }
                else
                {
                    for (int y = yStart; y < yStart + height; y++)
                    {
                        var dest = (uint*)Bitmap.BackBuffer.ToPointer();
                        dest += y * Bitmap.BackBufferStride / 4 + xStart;
                        var row = Image.GetPixelRowSpan(y);

                        for (int x = 0; x < width; x++)
                        {
                            var color = row[x + xStart];
                            dest[x] = TranslateColor(color);
                        }
                    }
                }
            }

            Bitmap.AddDirtyRect(new System.Windows.Int32Rect(xStart, yStart, width, height));
        }
        finally
        {
            Bitmap!.Unlock();
        }
    }

    private uint TranslateColor(float input)
    {
        byte channel = (byte)(255 * input);
        uint color = (uint)((channel << 24) | (channel << 16) | (channel << 8) | channel);
        return color;
    }
}
