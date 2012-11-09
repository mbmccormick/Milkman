
namespace YourLastAboutDialog.Models
{
    /// <summary>
    /// A container class for all the information required to build a hyperlink button element.
    /// </summary>
    public sealed class LinkData
    {
        private static LinkData _empty;

        /// <summary>
        /// An empty link data object.
        /// </summary>
        public static LinkData Empty
        {
            get
            {
                if (_empty == null)
                {
                    _empty = new LinkData();
                }

                return _empty;
            }
        }

        /// <summary>
        /// The uri used as navigation target.
        /// </summary>
        public string NavigateUri
        {
            get;
            set;
        }

        /// <summary>
        /// The content used as display.
        /// </summary>
        public string Content
        {
            get;
            set;
        }

        /// <summary>
        /// An additional label that is not part of the hyperlink button.
        /// </summary>
        public string Label
        {
            get;
            set;
        }
    }
}
