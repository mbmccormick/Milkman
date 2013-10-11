using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.Windows.Media;

namespace Milkman.Controls
{
    public partial class FlipTileTemplateWide : UserControl
    {
        public FlipTileTemplateWide()
        {
            InitializeComponent();
        }

        public void RenderLiveTileImage(string filename)
        {
            this.LayoutRoot.Background = (App.Current.Resources["PhoneAccentBrush"] as SolidColorBrush);

            this.Measure(new Size(691, 336));
            this.Arrange(new Rect(0, 0, 691, 336));
            
            WriteableBitmap image = new WriteableBitmap(691, 336);
            image.Render(this, null);
            image.Invalidate();

            using (IsolatedStorageFile output = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var stream = output.OpenFile(filename, System.IO.FileMode.OpenOrCreate))
                {
                    image.SaveJpeg(stream, 691, 336, 0, 100);
                }
            }
        }
    }
}
