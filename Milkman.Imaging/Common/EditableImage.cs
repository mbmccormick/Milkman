using System;
using System.IO;
using System.Windows.Media;

namespace Milkman.Imaging.Common
{
    public class EditableImage
    {
        private int width;
        private int height;
        private bool init;
        private byte[] buffer;
        private int rowLength;

        public event EventHandler<EditableImageErrorEventArgs> ImageError;

        public EditableImage(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width
        {
            get { return width; }
            set
            {
                if (init)
                {
                    OnImageError("Error: Cannot change Width after the EditableImage has been initialized");
                }
                else if ((value <= 0) || (value > 2047))
                {
                    OnImageError("Error: Width must be between 0 and 2047");
                }
                else
                {
                    width = value;
                }
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                if (init)
                {
                    OnImageError("Error: Cannot change Height after the EditableImage has been initialized");
                }
                else if ((value <= 0) || (value > 2047))
                {
                    OnImageError("Error: Height must be between 0 and 2047");
                }
                else
                {
                    height = value;
                }
            }
        }

        public void SetPixel(int col, int row, Color color)
        {
            SetPixel(col, row, color.R, color.G, color.B, color.A);
        }

        public void SetPixel(int col, int row, byte red, byte green, byte blue, byte alpha)
        {
            if (!init)
            {
                rowLength = width*4 + 1;
                buffer = new byte[rowLength*height];

                // Initialize
                for (int idx = 0; idx < height; idx++)
                {
                    buffer[idx*rowLength] = 0; // Filter bit
                }

                init = true;
            }

            if ((col > width) || (col < 0))
            {
                OnImageError("Error: Column must be greater than 0 and less than the Width");
            }
            else if ((row > height) || (row < 0))
            {
                OnImageError("Error: Row must be greater than 0 and less than the Height");
            }

            // Set the pixel
            int start = rowLength*row + col*4 + 1;
            buffer[start] = red;
            buffer[start + 1] = green;
            buffer[start + 2] = blue;
            buffer[start + 3] = alpha;
        }

        public Color GetPixel(int col, int row)
        {
            if ((col > width) || (col < 0))
            {
                OnImageError("Error: Column must be greater than 0 and less than the Width");
            }
            else if ((row > height) || (row < 0))
            {
                OnImageError("Error: Row must be greater than 0 and less than the Height");
            }

            Color color = new Color();
            int _base = rowLength*row + col + 1;

            color.R = buffer[_base];
            color.G = buffer[_base + 1];
            color.B = buffer[_base + 2];
            color.A = buffer[_base + 3];

            return color;
        }

        public Stream GetStream()
        {
            Stream stream;

            if (!init)
            {
                OnImageError("Error: Image has not been initialized");
                stream = null;
            }
            else
            {
                stream = PngEncoder.Encode(buffer, width, height);
            }

            return stream;
        }

        private void OnImageError(string msg)
        {
            if (null != ImageError)
            {
                EditableImageErrorEventArgs args = new EditableImageErrorEventArgs {ErrorMessage = msg};
                ImageError(this, args);
            }
        }

        public class EditableImageErrorEventArgs : EventArgs
        {
            private string errorMessage = string.Empty;

            public string ErrorMessage
            {
                get { return errorMessage; }
                set { errorMessage = value; }
            }
        }
    }
}
