using System;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using YourLastAboutDialog.ViewModels;
using YourLastAboutDialog.Views;

namespace YourLastAboutDialog
{
    /// <summary>
    /// Based on and inspired by ideas by Jeff Wilcox:
    /// http://www.jeff.wilcox.name/2011/07/my-app-about-page/
    /// </summary>
    public partial class AboutPage
    {
        /// <summary>
        /// A key used to indicate what pivot item should be pre-selected in the about dialog,
        /// based on the item index. Use this as query parameter.
        /// </summary>
        public const string SelectedPivotItemIndexKey = "SelectedPivotItemIndex";

        /// <summary>
        /// A key used to indicate what pivot item should be pre-selected in the about dialog,
        /// based on the item header. Use this as query parameter.
        /// </summary>
        public const string SelectedPivotItemHeaderKey = "SelectedPivotItemHeader";

        /// <summary>
        /// A key used to indicate that the "Buy this app!" button should be shown even when 
        /// the app does not run in trial mode (this is helpful if you do not use the built-in
        /// trial mode, but are running a free "lite" version of your app). Use this as query
        /// parameter, and set the value to "True" to override the default behavior.
        /// </summary>
        public const string ForceBuyButtonKey = "ForceBuyButton";

        private bool _isInitialized;
        private int _selectedPivotItemIndex = -1;
        private string _selectedPivotItemHeader;

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutPage"/> class.
        /// </summary>
        public AboutPage()
        {
            InitializeComponent();

            Style = (Style)Resources["AboutPageStyle"];

            InitializeMainPivotItem();
        }

        private void InitializeMainPivotItem()
        {
            var vm = DataContext as AboutViewModel;
            if (vm == null)
            {
                return;
            }

            // generate default page instantly
            var defaultItem = new ApplicationInfoView();
            AddPivotItem(vm.MainItemData.Title, defaultItem);

            // create the rest of the pages after the 
            // dialog has loaded (to make it visible faster)
            Loaded += AboutPage_Loaded;
        }

        private void AboutPage_Loaded(object sender, RoutedEventArgs e)
        {
            // first create all the secondary items
            InitializeSecondaryPivotItems();

            // these values are filled in the OnNavigatedTo override, if available
            // => now use them to preselect an item
            if (_selectedPivotItemIndex > -1 && _selectedPivotItemIndex < PivotControl.Items.Count)
            {
                PivotControl.SelectedIndex = _selectedPivotItemIndex;
            }
            else if (_selectedPivotItemHeader != null)
            {
                var item = PivotControl.Items
                            .OfType<PivotItem>()
                            .Where(o => o.Header.ToString() == _selectedPivotItemHeader)
                            .FirstOrDefault();

                if (item != null)
                {
                    PivotControl.SelectedItem = item;
                }
            }
        }

        /// <summary>
        /// Called when a page becomes the active page in a frame.
        /// </summary>
        /// <param name="e">An object that contains the event data.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // initialize to none
            _selectedPivotItemIndex = -1;
            _selectedPivotItemHeader = null;

            try
            {
                // decide what to do: if we are navigated to (new) check whether we have a 
                // request for a particular pivot item index from whoever called us.
                // If we navigate back to the page (i.e. after activation or returning from tombstoning), 
                // retrieve the previously stored pivot item index and restore it.
                if (e.NavigationMode == NavigationMode.New)
                {
                    var queryString = NavigationContext.QueryString;
                    if (queryString != null)
                    {
                        if (queryString.ContainsKey(SelectedPivotItemIndexKey))
                        {
                            var selectedIndexText = queryString[SelectedPivotItemIndexKey];
                            int selectedIndex;
                            if (int.TryParse(selectedIndexText, out selectedIndex))
                            {
                                _selectedPivotItemIndex = selectedIndex;
                            }
                        }
                        else if (queryString.ContainsKey(SelectedPivotItemHeaderKey))
                        {
                            _selectedPivotItemHeader = queryString[SelectedPivotItemHeaderKey];
                        }
                    }
                }
                else if (e.NavigationMode == NavigationMode.Back)
                {
                    if (State.ContainsKey(SelectedPivotItemIndexKey))
                    {
                        var index = (int)State[SelectedPivotItemIndexKey];
                        _selectedPivotItemIndex = index;

                        State.Remove(SelectedPivotItemIndexKey);
                    }
                }
            }
            catch (Exception)
            {
                // better safe than sorry
                // => if anything bad happens here, we simply ignore it, because restoring/setting
                //    a selected index is not vital to our component
                _selectedPivotItemIndex = -1;
                _selectedPivotItemHeader = null;
            }
        }

        private void InitializeSecondaryPivotItems()
        {
            // only do this once
            if (_isInitialized)
            {
                return;
            }
            _isInitialized = true;

            // get view model
            var vm = DataContext as AboutViewModel;
            if (vm == null)
            {
                return;
            }

            foreach (var page in vm.Items)
            {
                // create view model
                var pageViewModel = new GenericItemViewModel(page);

                // create view
                var pageView = new GenericItemView();
                pageView.DataContext = pageViewModel;

                // add to pivot control
                AddPivotItem(page.Title, pageView);
            }
        }

        private void AddPivotItem(string header, object content)
        {
            // create the item
            var item = new PivotItem();

            // set properties and add
            item.Style = (Style)Resources["AboutPivotItemStyle"];
            item.Header = header;
            item.Content = content;
            PivotControl.Items.Add(item);
        }

        /// <summary>
        /// Called when a page is no longer the active page in a frame.
        /// </summary>
        /// <param name="e">An object that contains the event data.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New)
            {
                // we're navigating away from the app => store the current index
                // so we can restore it when the user returns
                State[SelectedPivotItemIndexKey] = PivotControl.SelectedIndex;
            }

            base.OnNavigatedFrom(e);
        }
    }
}