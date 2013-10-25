using Milkman.Imaging.Common;
using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Milkman.Imaging
{
    public class RenderCompleteEventArgs
    {
        bool _success;
        string _path;

        public RenderCompleteEventArgs(bool success, string path)
        {
            _success = success;
            _path = path;
        }
    }

    public partial class FlipTileTemplate : UserControl
    {
        public event EventHandler<RenderCompleteEventArgs> RenderLiveTileImageComplete;

        public FlipTileTemplate()
        {
            InitializeComponent();
        }

        public void RenderLiveTileImage(string filename, string title, string content)
        {
            BitmapImage iconImage = new BitmapImage(new Uri("/Assets/FlipCycleTileSmall.jpg", UriKind.Relative));
            iconImage.CreateOptions = BitmapCreateOptions.None;

            iconImage.ImageOpened += (s, e) =>
            {
                try
                {
                    this.LayoutRoot.Background = new SolidColorBrush(IsolatedStorageHelper.GetObject<Color>("AccentColor"));

                    this.txtTitle.Text = title;
                    this.txtContent.Text = content;

                    this.imgIcon.Source = iconImage;

                    this.Measure(new Size(336, 336));
                    this.Arrange(new Rect(0, 0, 336, 336));
                    this.UpdateLayout();

                    WriteableBitmap image = new WriteableBitmap(336, 336);
                    image.Render(this, null);
                    image.Invalidate();

                    using (IsolatedStorageFile output = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (var stream = output.OpenFile(filename, System.IO.FileMode.OpenOrCreate))
                        {
                            image.SaveJpeg(stream, 336, 336, 0, 100);
                        }
                    }

                    if (RenderLiveTileImageComplete != null)
                    {
                        RenderLiveTileImageComplete(this, new RenderCompleteEventArgs(true, filename));
                    }
                }
                catch (Exception ex)
                {
                    if (RenderLiveTileImageComplete != null)
                    {
                        RenderLiveTileImageComplete(this, new RenderCompleteEventArgs(false, null));
                    }
                }
            };            
        }
    }
}