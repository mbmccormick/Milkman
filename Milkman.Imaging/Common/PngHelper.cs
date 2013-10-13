using System.IO;
using System.Windows.Media.Imaging;

namespace Milkman.Imaging.Common
{
    public static class PngHelper
    {
        public static void SavePng(this WriteableBitmap bitmap, Stream stream)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            EditableImage ei = new EditableImage(width, height);

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int pixel = bitmap.Pixels[(i * width) + j];
                    ei.SetPixel(j, i,
                                (byte)((pixel >> 16) & 0xFF),
                                (byte)((pixel >> 8) & 0xFF),
                                (byte)(pixel & 0xFF),
                                (byte)((pixel >> 24) & 0xFF)
                        );
                }
            }
            Stream png = ei.GetStream();
            int len = (int)png.Length;
            byte[] bytes = new byte[len];
            png.Read(bytes, 0, len);
            stream.Write(bytes, 0, len);
        }
    }
}
