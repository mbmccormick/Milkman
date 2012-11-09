
using System;

namespace YourLastAboutDialog.Models
{
    /// <summary>
    /// A container class for all the information needed to create a pivot item.
    /// </summary>
    public sealed class ItemData
    {
        /// <summary>
        /// The title of the pivot item.
        /// </summary>
        public string Title
        {
            get;
            set;
        }

        /// <summary>
        /// The uri where the content of the page should be downloaded from.
        /// If this is <c>null</c>, the <see cref="OfflineContent"/> is used.
        /// </summary>
        public Uri Uri
        {
            get;
            set;
        }

        /// <summary>
        /// The type of the content, used for the formatting/preparation of the page.
        /// </summary>
        public ItemType Type
        {
            get;
            set;
        }

        /// <summary>
        /// The alternate content that is used when downloading the content from the remote
        /// <c>Uri</c> fails, or if no <c>Uri</c> is given at all. The <see cref="Type"/> property
        /// is respected for this content too.
        /// </summary>
        public string OfflineContent
        {
            get;
            set;
        }
    }
}
