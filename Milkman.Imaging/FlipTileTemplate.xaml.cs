using Milkman.Imaging.Common;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Milkman.Imaging
{
    public partial class FlipTileTemplate : UserControl
    {
        public FlipTileTemplate()
        {
            InitializeComponent();
        }

        public void RenderLiveTileImage(string filename, string title, string content)
        {
            this.txtTitle.Text = title;
            this.txtContent.Text = content;

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
                    image.SavePng(stream);
                }
            }
        }
    }
}
