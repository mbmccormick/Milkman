using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;

namespace YourLastAboutDialog.Common
{
    /// <summary>
    /// Extracts the information contained in the WMAppManifest.xml file.
    /// Extended and improved version based on a post by Joost van Schaik:
    /// http://dotnetbyexample.blogspot.com/2011/03/easy-access-to-wmappmanifestxml-app.html
    /// </summary>
    internal sealed class ManifestAppInfo
    {
        private const string VersionLabel = "VERSION";
        private const string ProductIdLabel = "PRODUCTID";
        private const string TitleLabel = "TITLE";
        private const string GenreLabel = "GENRE";
        private const string DescriptionLabel = "DESCRIPTION";
        private const string AuthorLabel = "AUTHOR";
        private const string PublisherLabel = "PUBLISHER";

        private static Dictionary<string, string> _attributes;
        private static Dictionary<string, string> Attributes
        {
            get
            {
                return _attributes;
            }
        }

        static ManifestAppInfo()
        {
            // add a bit of design-time data
            if (DesignerProperties.IsInDesignTool)
            {
                LoadDesignTimeData();
            }
            else
            {
                LoadData();
            }
        }

        private static void LoadDesignTimeData()
        {
            _attributes = new Dictionary<string, string>
                              {
                                  {VersionLabel, "1.2.3.4"},
                                  {ProductIdLabel, "{CF68A1E0-578C-4A7C-9278-6AC10F51EAE1}"},
                                  {TitleLabel, "My Title"},
                                  {GenreLabel, "apps.normal"},
                                  {DescriptionLabel, "Some really long sample description that exceeds the available width of the screen to test whether wrapping works correctly :-)."},
                                  {AuthorLabel, "Mr. Author"},
                                  {PublisherLabel, "My Publisher"}
                              };
        }

        private static void LoadData()
        {
            // create the dictionary and parse the xml file
            _attributes = new Dictionary<string, string>();

            try
            {
                // load the manifest file
                using (var stream = TitleContainer.OpenStream("WMAppManifest.xml"))
                {
                    // create the document
                    var xml = XElement.Load(stream);

                    // get the app element
                    var appElement = xml.Descendants("App").FirstOrDefault();
                    if (appElement != null)
                    {
                        // get all attributes
                        var attributes = appElement.Attributes();

                        // add all attributes to the dictionary
                        foreach (var attribute in attributes)
                        {
                            _attributes.Add(attribute.Name.LocalName.ToUpper(), attribute.Value);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        /// <summary>
        /// Gets the version string in the WMAppManifest.xml or <c>null</c> if this information could not be retrieved.
        /// </summary>
        public string Version
        {
            get
            {
                return Attributes.ContainsKey(VersionLabel) ? Attributes[VersionLabel] : null;
            }
        }

        /// <summary>
        /// Gets the product Id in the WMAppManifest.xml or <c>null</c> if this information could not be retrieved.
        /// </summary>
        public string ProductId
        {
            get
            {
                return Attributes.ContainsKey(ProductIdLabel) ? Attributes[ProductIdLabel] : null;
            }
        }

        /// <summary>
        /// Gets the title in the WMAppManifest.xml or <c>null</c> if this information could not be retrieved.
        /// </summary>
        public string Title
        {
            get
            {
                return Attributes.ContainsKey(TitleLabel) ? Attributes[TitleLabel] : null;
            }
        }

        /// <summary>
        /// Gets the genre in the WMAppManifest.xml or <c>null</c> if this information could not be retrieved.
        /// </summary>
        public string Genre
        {
            get
            {
                return Attributes.ContainsKey(GenreLabel) ? Attributes[GenreLabel] : null;
            }
        }

        /// <summary>
        /// Gets the description in the WMAppManifest.xml or <c>null</c> if this information could not be retrieved.
        /// </summary>
        public string Description
        {
            get
            {
                return Attributes.ContainsKey(DescriptionLabel) ? Attributes[DescriptionLabel] : null;
            }
        }

        /// <summary>
        /// Gets the author in the WMAppManifest.xml or <c>null</c> if this information could not be retrieved.
        /// </summary>
        public string Author
        {
            get
            {
                return Attributes.ContainsKey(AuthorLabel) ? Attributes[AuthorLabel] : null;
            }
        }

        /// <summary>
        /// Gets the publisher in the WMAppManifest.xml or <c>null</c> if this information could not be retrieved.
        /// </summary>
        public string Publisher
        {
            get
            {
                return Attributes.ContainsKey(PublisherLabel) ? Attributes[PublisherLabel] : null;
            }
        }
    }
}
