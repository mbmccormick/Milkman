using IronCow.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Milkman.Common;
using System;
using System.Linq;
using System.Windows;
using Windows.ApplicationModel.Store;

namespace Milkman
{
    public partial class WelcomePage : PhoneApplicationPage
    {
        #region Construction and Navigation

        ApplicationBarMenuItem about;
        ApplicationBarMenuItem feedback;
        ApplicationBarMenuItem donate;

        public WelcomePage()
        {
            InitializeComponent();

            this.BuildApplicationBar();
        }

        private void BuildApplicationBar()
        {
            about = new ApplicationBarMenuItem();
            about.Text = Strings.AboutMenuLower;
            about.Click += mnuAbout_Click;

            feedback = new ApplicationBarMenuItem();
            feedback.Text = Strings.FeedbackMenuLower;
            feedback.Click += mnuFeedback_Click;

            donate = new ApplicationBarMenuItem();
            donate.Text = Strings.DonateMenuLower;
            donate.Click += mnuDonate_Click;

            // build application bar
            ApplicationBar.MenuItems.Add(about);
            ApplicationBar.MenuItems.Add(feedback);
            ApplicationBar.MenuItems.Add(donate);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (NavigationService.CanGoBack == true)
                NavigationService.RemoveBackEntry();
            
            base.OnNavigatedTo(e);
        }

        #endregion

        #region Event Handlers

        private void stkSignIn_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/AuthorizationPage.xaml", UriKind.Relative));
            });
        }

        private void stkRegister_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBox.Show(Strings.RegistrationDialog.Replace("\\n", "\n"), Strings.RegistrationDialogTitle, MessageBoxButton.OK);

            WebBrowserTask webBrowserTask = new WebBrowserTask();

            webBrowserTask.Uri = new Uri("http://www.rememberthemilk.com/signup/");
            webBrowserTask.Show();
        }

        private void stkAbout_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
            });
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
            });
        }

        private void mnuFeedback_Click(object sender, EventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.To = "feedback@mbmccormick.com";
            emailComposeTask.Subject = "Milkman Feedback";
            emailComposeTask.Body = "Version " + App.ExtendedVersionNumber + " (" + App.PlatformVersionNumber + ")\n\n";
            emailComposeTask.Show();
        }

        private async void mnuDonate_Click(object sender, EventArgs e)
        {
            try
            {
                var productList = await CurrentApp.LoadListingInformationAsync();
                var product = productList.ProductListings.FirstOrDefault(p => p.Value.ProductType == ProductType.Consumable);
                var receipt = await CurrentApp.RequestProductPurchaseAsync(product.Value.ProductId, true);

                if (CurrentApp.LicenseInformation.ProductLicenses[product.Value.ProductId].IsActive)
                {
                    CurrentApp.ReportProductFulfillment(product.Value.ProductId);

                    MessageBox.Show(Strings.DonateDialog, Strings.DonateDialogTitle, MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                // do nothing
            }
        }

        #endregion
    }
}