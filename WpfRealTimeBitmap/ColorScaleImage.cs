using System;
using WpfRealTimeBitmap.Rng;

namespace WpfRealTimeBitmap;

public class ColorScaleImage : ImageBase<float>
{
    //private Random _rng = new();
    private XoshiroRandom _rng = new();

    public ColorScaleImage(int width, int height)
    {
        Image = new float[width * height];
        Width = width;
        Height = height;
    }

    public override void Render()
    {
        if (Image is not null)
        {
            for (int i = 0; i < Image.Length; i++)
            {
                Image[i] = _rng.NextSingle();
            }
        }
    }
}
