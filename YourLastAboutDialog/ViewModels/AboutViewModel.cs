using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Phone.Marketplace;
using Microsoft.Phone.Tasks;
using YourLastAboutDialog.Commands;
using YourLastAboutDialog.Common;
using YourLastAboutDialog.Models;

namespace YourLastAboutDialog.ViewModels
{
    /// <summary>
    /// A view model for the about page and its items.
    /// </summary>
    public sealed class AboutViewModel : NavigationViewModelBase
    {
        private const string ForceBuyButtonKey = "ForceBuyButton";

        private MainItemData _mainItemData;
        private string _applicationTitle;
        private string _applicationTitleUpper;
        private string _applicationProductId;
        private string _applicationFullVersionProductId;
        private string _applicationVersion;
        private string _applicationAuthor;
        private string _applicationPublisher;
        private string _applicationDescription;
        private string _additionalNotes;
        private ObservableCollection<LinkData> _links = new ObservableCollection<LinkData>();
        private ObservableCollection<ItemData> _items = new ObservableCollection<ItemData>();
        private bool _isReviewAllowed;
        private bool _isBuyAllowed;
        private bool _forceBuyButton;
        private Visibility _buyOptionVisibility;

        #region Properties

        /// <summary>
        /// Gets or sets the main item data that is displayed in the first pivot item.
        /// </summary>
        /// <value>
        /// The main page data.
        /// </value>
        public MainItemData MainItemData
        {
            get
            {
                return _mainItemData;
            }
            set
            {
                if (_mainItemData != value)
                {
                    _mainItemData = value;
                    RaisePropertyChanged("MainPageData");
                }
            }
        }

        /// <summary>
        /// The list of items that should be shown.
        /// </summary>
        public ObservableCollection<ItemData> Items
        {
            get
            {
                return _items;
            }
            set
            {
                if (_items != value)
                {
                    _items = value;
                    RaisePropertyChanged("Pages");
                }
            }
        }

        /// <summary>
        /// The links that should be added to the list of links on the main pivot item.
        /// </summary>
        public ObservableCollection<LinkData> Links
        {
            get
            {
                return _links;
            }
            set
            {
                if (_links != value)
                {
                    _links = value;
                    RaisePropertyChanged("Links");
                }
            }
        }

        /// <summary>
        /// The description of the application.
        /// </summary>
        public string ApplicationDescription
        {
            get
            {
                return _applicationDescription;
            }
            set
            {
                if (_applicationDescription != value)
                {
                    _applicationDescription = value;
                    RaisePropertyChanged("ApplicationDescription");
                }
            }
        }

        /// <summary>
        /// The publisher of the application.
        /// </summary>
        public string ApplicationPublisher
        {
            get
            {
                return _applicationPublisher;
            }
            set
            {
                if (_applicationPublisher != value)
                {
                    _applicationPublisher = value;
                    RaisePropertyChanged("ApplicationPublisher");
                }
            }
        }

        /// <summary>
        /// The author of the application.
        /// </summary>
        public string ApplicationAuthor
        {
            get
            {
                return _applicationAuthor;
            }
            set
            {
                if (_applicationAuthor != value)
                {
                    _applicationAuthor = value;
                    RaisePropertyChanged("ApplicationAuthor");
                }
            }
        }

        /// <summary>
        /// The version of the application.
        /// </summary>
        public string ApplicationVersion
        {
            get
            {
                return _applicationVersion;
            }
            set
            {
                if (_applicationVersion != value)
                {
                    _applicationVersion = value;
                    RaisePropertyChanged("ApplicationVersion");
                }
            }
        }

        /// <summary>
        /// The title of the application.
        /// </summary>
        public string ApplicationTitle
        {
            get
            {
                return _applicationTitle;
            }
            set
            {
                if (_applicationTitle != value)
                {
                    _applicationTitle = value;
                    RaisePropertyChanged("ApplicationTitle");
                }
            }
        }

        /// <summary>
        /// The application title converted to upper case.
        /// </summary>
        public string ApplicationTitleUpper
        {
            get
            {
                return _applicationTitleUpper;
            }
            set
            {
                if (_applicationTitleUpper != value)
                {
                    _applicationTitleUpper = value;
                    RaisePropertyChanged("ApplicationTitleUpper");
                }
            }
        }

        /// <summary>
        /// The product ID of the application.
        /// </summary>
        public string ApplicationProductId
        {
            get
            {
                return _applicationProductId;
            }
            set
            {
                if (_applicationProductId != value)
                {
                    _applicationProductId = value;
                    RaisePropertyChanged("ApplicationProductId");
                }
            }
        }

        /// <summary>
        /// The alternate product ID of the application's full version.
        /// </summary>
        public string ApplicationFullVersionProductId
        {
            get
            {
                return _applicationFullVersionProductId;
            }
            set
            {
                if (_applicationFullVersionProductId != value)
                {
                    _applicationFullVersionProductId = value;
                    RaisePropertyChanged("ApplicationFullVersionProductId");
                }
            }
        }

        /// <summary>
        /// Additional notes that should be displayed on the main item.
        /// </summary>
        public string AdditionalNotes
        {
            get
            {
                return _additionalNotes;
            }
            set
            {
                if (_additionalNotes != value)
                {
                    _additionalNotes = value;
                    RaisePropertyChanged("AdditionalNotes");
                }
            }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutViewModel"/> class.
        /// </summary>
        public AboutViewModel()
        {
            LoadManifestData();
            LoadAboutData();

            ReviewCommand = new RelayCommand(Review, IsReviewAllowed);
            BuyCommand = new RelayCommand(Buy, IsBuyAllowed);
            BuyOptionVisibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Invoked when the page is being navigated to.
        /// </summary>
        /// <param name="e">The <see cref="System.Windows.Navigation.NavigationEventArgs"/> instance containing the event data.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // extract query string parameters
            if (NavigationContext != null && NavigationContext.QueryString != null && NavigationContext.QueryString.ContainsKey(ForceBuyButtonKey))
            {
                var boolString = NavigationContext.QueryString[ForceBuyButtonKey];
                bool result;
                if (bool.TryParse(boolString, out result))
                {
                    _forceBuyButton = result;
                }
            }

            // initialize the trial settings
            InitializeTrialMode();

            // make sure we notify the UI about that we can use the commands
            _isReviewAllowed = true;
            ReviewCommand.RaiseCanExecuteChanged();

            _isBuyAllowed = true;
            BuyCommand.RaiseCanExecuteChanged();
        }

        private void InitializeTrialMode()
        {
            // if the user explicitly set the trial button content to empty,
            // they want to hide the button => nothing more to do here
            if (string.IsNullOrWhiteSpace(MainItemData.AppBuyButtonContent))
            {
                BuyOptionVisibility = Visibility.Collapsed;
                return;
            }

            // does the user want to show the buy button in any case?
            if (_forceBuyButton)
            {
                BuyOptionVisibility = Visibility.Visible;
            }
            else
            {
                // takes ~60ms to obtain, but that's ok here, 
                // we only do this once when the dialog is shown.
                var license = new LicenseInformation();
                BuyOptionVisibility = license.IsTrial() ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #region ReviewCommand

        /// <summary>
        /// Wraps the review application operation.
        /// </summary>
        public RelayCommand ReviewCommand
        {
            get;
            private set;
        }

        private void Review()
        {
            // should be covered by the command's CanExecute functionality
            if (!_isReviewAllowed)
            {
                return;
            }

            try
            {
                // disable review feature
                _isReviewAllowed = false;
                ReviewCommand.RaiseCanExecuteChanged();

                // simply show the review task
                var task = new MarketplaceReviewTask();
                task.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while opening the marketplace: " + ex.Message);

                // in case of errors, allow the review feature again
                _isReviewAllowed = true;
                ReviewCommand.RaiseCanExecuteChanged();
            }
        }

        private bool IsReviewAllowed()
        {
            return _isReviewAllowed;
        }

        #endregion

        #region BuyCommand

        /// <summary>
        /// Wraps the buy application operation.
        /// </summary>
        public RelayCommand BuyCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets whether the buy options in the UI should be visible or not.
        /// </summary>
        public Visibility BuyOptionVisibility
        {
            get
            {
                return _buyOptionVisibility;
            }
            set
            {
                if (_buyOptionVisibility != value)
                {
                    _buyOptionVisibility = value;
                    RaisePropertyChanged("BuyOptionVisibility");
                }
            }
        }

        private void Buy()
        {
            // should be covered by the command's CanExecute functionality
            if (!_isBuyAllowed)
            {
                return;
            }

            try
            {
                // disable buy feature
                _isBuyAllowed = false;
                BuyCommand.RaiseCanExecuteChanged();

                // simply show the detail task
                var task = new MarketplaceDetailTask();
                task.ContentIdentifier = ApplicationFullVersionProductId;
                task.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while opening the marketplace: " + ex.Message);

                // in case of errors, allow the review feature again
                _isBuyAllowed = true;
                BuyCommand.RaiseCanExecuteChanged();
            }
        }

        private bool IsBuyAllowed()
        {
            return _isBuyAllowed;
        }

        #endregion

        private void LoadManifestData()
        {
            try
            {
                // get the data from the manifest file
                var manifest = new ManifestAppInfo();

                ApplicationTitle = manifest.Title;
                ApplicationTitleUpper = ApplicationTitle != null ? ApplicationTitle.ToUpper() : null;
                ApplicationProductId = manifest.ProductId;
                ApplicationDescription = manifest.Description;
                ApplicationVersion = manifest.Version;
                ApplicationAuthor = manifest.Author;
                ApplicationPublisher = manifest.Publisher;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the basic application information: " + ex.Message);
            }
        }

        #region Processing of Data.xml

        private const string DataUri = "Content/About/Data.xml";
        private const string ElementApp = "App";
        private const string ElementItems = "Items";
        private const string ElementMainItem = "MainItem";
        private const string ElementLink = "Link";
        private const string AttributeTitle = "Title";
        private const string AttributeProductId = "ProductId";
        private const string AttributeFullVersionProductId = "FullVersionProductId";
        private const string AttributeAuthor = "Author";
        private const string AttributeDescription = "Description";
        private const string AttributePublisher = "Publisher";
        private const string AttributeVersion = "Version";
        private const string AttributeAdditionalNotes = "AdditionalNotes";
        private const string AttributeAppAuthorLabel = "AppAuthorLabel";
        private const string AttributeAppPublisherLabel = "AppPublisherLabel";
        private const string AttributeAppDescriptionLabel = "AppDescriptionLabel";
        private const string AttributeAppVersionLabel = "AppVersionLabel";
        private const string AttributeAppAdditionalNotesLabel = "AppAdditionalNotesLabel";
        private const string AttributeAppReviewButtonContent = "AppReviewButtonContent";
        private const string AttributeAppBuyButtonContent = "AppBuyButtonContent";
        private const string AttributeContent = "Content";
        private const string AttributeNavigateUri = "NavigateUri";
        private const string AttributeLabel = "Label";
        private const string AttributeType = "Type";
        private const string AttributeUri = "Uri";

        private void LoadAboutData()
        {
            try
            {
                // try full culture name localized uri first
                var localizedUri = LocalizationHelper.GetCultureNameLocalizedUri(DataUri);
                StreamResourceInfo sri = Application.GetResourceStream(new Uri(localizedUri, UriKind.Relative));
                if (sri != null)
                {
                    LoadFromStreamResourceInfo(sri);
                    return;
                }

                // the full culture name localization wasn't found
                // => try the language code localization instead
                localizedUri = LocalizationHelper.GetLanguageCodeLocalizedUri(DataUri);
                sri = Application.GetResourceStream(new Uri(localizedUri, UriKind.Relative));
                if (sri != null)
                {
                    LoadFromStreamResourceInfo(sri);
                    return;
                }

                // now try the invariant version as last resort
                sri = Application.GetResourceStream(new Uri(DataUri, UriKind.Relative));
                if (sri != null)
                {
                    LoadFromStreamResourceInfo(sri);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the extended application information: " + ex.Message);
            }
        }

        private void LoadFromStreamResourceInfo(StreamResourceInfo sri)
        {
            using (XmlReader xmlReader = XmlReader.Create(sri.Stream))
            {
                var doc = XDocument.Load(xmlReader);
                var root = doc.Root;

                if (root != null)
                {
                    // load app data
                    var appElement = root.Element(ElementApp);
                    if (appElement != null)
                    {
                        LoadAppData(appElement);
                    }

                    // load all pages
                    var pagesElements = root.Element(ElementItems);
                    if (pagesElements != null)
                    {
                        var pageElements = pagesElements.Elements();
                        foreach (var pageElement in pageElements)
                        {
                            if (pageElement.Name == ElementMainItem)
                            {
                                LoadMainItemData(pageElement);
                            }
                            else
                            {
                                var page = LoadItemData(pageElement);
                                Items.Add(page);
                            }
                        }
                    }
                }
            }
        }

        private void LoadAppData(XElement appElement)
        {
            // this loads the basic application data.
            // any element that is present here will override what has been 
            // retrieved from the WMAppManifest file, except the "AdditionalNotes"
            // (there's no corresponding entry in the app manifest for this)
            ApplicationTitle = GetAttributeValue(appElement, AttributeTitle) ?? ApplicationTitle;
            ApplicationTitleUpper = ApplicationTitle != null ? ApplicationTitle.ToUpper() : null;
            ApplicationProductId = GetAttributeValue(appElement, AttributeProductId) ?? ApplicationProductId;
            ApplicationFullVersionProductId = GetAttributeValue(appElement, AttributeFullVersionProductId) ?? ApplicationProductId;
            ApplicationAuthor = GetAttributeValue(appElement, AttributeAuthor) ?? ApplicationAuthor;
            ApplicationDescription = GetAttributeValue(appElement, AttributeDescription) ?? ApplicationDescription;
            ApplicationPublisher = GetAttributeValue(appElement, AttributePublisher) ?? ApplicationPublisher;
            ApplicationVersion = GetAttributeValue(appElement, AttributeVersion) ?? ApplicationVersion;
            AdditionalNotes = GetAttributeValue(appElement, AttributeAdditionalNotes);
        }

        private void LoadMainItemData(XElement itemElement)
        {
            MainItemData = new MainItemData();

            // get data
            MainItemData.Title = GetAttributeValue(itemElement, AttributeTitle, "about");
            MainItemData.AppAuthorLabel = GetAttributeValue(itemElement, AttributeAppAuthorLabel, "by");
            MainItemData.AppPublisherLabel = GetAttributeValue(itemElement, AttributeAppPublisherLabel, "Publisher:");
            MainItemData.AppDescriptionLabel = GetAttributeValue(itemElement, AttributeAppDescriptionLabel, "Description:");
            MainItemData.AppVersionLabel = GetAttributeValue(itemElement, AttributeAppVersionLabel, "Version:");
            MainItemData.AppAdditionalNotesLabel = GetAttributeValue(itemElement, AttributeAppAdditionalNotesLabel, "Additional notes:");
            MainItemData.AppReviewButtonContent = GetAttributeValue(itemElement, AttributeAppReviewButtonContent) ?? "Review this app!";
            MainItemData.AppBuyButtonContent = GetAttributeValue(itemElement, AttributeAppBuyButtonContent) ?? "Buy this app!";

            // load all links
            var linkElements = itemElement.Descendants(ElementLink);
            foreach (var linkElement in linkElements)
            {
                var link = LoadLinkData(linkElement);
                Links.Add(link);
            }
        }

        private LinkData LoadLinkData(XElement linkElement)
        {
            var result = new LinkData();

            // get data
            result.Content = GetAttributeValue(linkElement, AttributeContent);
            result.NavigateUri = GetAttributeValue(linkElement, AttributeNavigateUri);
            result.Label = GetAttributeValue(linkElement, AttributeLabel);

            return result;
        }

        private ItemData LoadItemData(XElement itemElement)
        {
            var result = new ItemData();

            // get data
            result.Title = GetAttributeValue(itemElement, AttributeTitle);
            var typeText = GetAttributeValue(itemElement, AttributeType);
            result.Type = (ItemType)Enum.Parse(typeof(ItemType), typeText, true);
            var url = GetAttributeValue(itemElement, AttributeUri);
            if (!string.IsNullOrEmpty(url))
            {
                result.Uri = new Uri(url, UriKind.RelativeOrAbsolute);
            }

            // get the (alternate) content
            var child = itemElement.Descendants().FirstOrDefault();
            if (child != null)
            {
                // we have XML content here 
                // => use ToString() here to preserve the content as XML (it's likely XAML)
                result.OfflineContent = child.ToString();
            }
            else
            {
                // no child elements => use the content as text
                result.OfflineContent = itemElement.Value;
            }

            return result;
        }

        private string GetAttributeValue(XElement element, string name, string valueIfNullOrEmpty)
        {
            var value = GetAttributeValue(element, name);
            if (string.IsNullOrEmpty(value))
            {
                return valueIfNullOrEmpty;
            }

            return value;
        }

        private string GetAttributeValue(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            if (attribute == null)
            {
                return null;
            }
            else
            {
                var value = attribute.Value;
                if (value != null)
                {
                    value = value.Replace("\\n", Environment.NewLine);
                }
                return value;
            }
        }

        #endregion
    }
}
