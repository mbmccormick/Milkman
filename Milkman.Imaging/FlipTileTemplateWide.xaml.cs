using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Milkman.Imaging
{
    public partial class FlipTileTemplateWide : UserControl
    {
        public FlipTileTemplateWide()
        {
            InitializeComponent();
        }

        public void RenderLiveTileImage(string filename, string title, string content)
        {
            this.txtTitle.Text = title;
            this.txtContent.Text = content;

            this.Measure(new Size(691, 336));
            this.Arrange(new Rect(0, 0, 691, 336));
            this.UpdateLayout();
            
            WriteableBitmap image = new WriteableBitmap(691, 336);
            image.Render(this, null);
            image.Invalidate();

            CompensateForRender(image.Pixels);

            using (IsolatedStorageFile output = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var stream = output.OpenFile(filename, System.IO.FileMode.OpenOrCreate))
                {
                    image.WritePNG(stream);
                }

                output.Dispose();
            }
        }

        private void CompensateForRender(int[] bitmapPixels)
        {
            if (bitmapPixels.Length == 0) return;

            for (int i = 0; i < bitmapPixels.Length; i++)
            {
                uint pixel = unchecked((uint)bitmapPixels[i]);

                double a = (pixel >> 24) & 255;
                if ((a == 255) || (a == 0)) continue;

                double r = (pixel >> 16) & 255;
                double g = (pixel >> 8) & 255;
                double b = (pixel) & 255;

                double factor = 255 / a;
                uint newR = (uint)Math.Round(r * factor);
                uint newG = (uint)Math.Round(g * factor);
                uint newB = (uint)Math.Round(b * factor);

                // compose
                bitmapPixels[i] = unchecked((int)((pixel & 0xFF000000) | (newR << 16) | (newG << 8) | newB));
            }
        }
    }
}
